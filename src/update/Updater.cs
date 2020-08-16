using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using update.Models;
using update.Parameters;

namespace update
{
    class Updater
    {
        private const string ManifestName_ = "manifest.json";
        private const string BuildName_ = "latest.zip";
        private const string SignatureName_ = "latest.sig";
        private const string PublicKeyName_ = "publickey.pem";

        private const string SigningAlg_ = "SHA-512withRSA";

        public UpdateParameters Parameters { get; }

        public Updater(UpdateParameters parameters)
        {
            Parameters = parameters;
        }

        public Manifest GetManifest()
        {
            var responseStream = GetResourceStream(ManifestName_);

            var reader = new StreamReader(responseStream);
            return JsonConvert.DeserializeObject<Manifest>(reader.ReadToEnd());
        }

        public Stream GetUpdateFile()
        {
            var responseStream = GetResourceStream(BuildName_);

            // Verify signature
            var signature = GetResourceStream(SignatureName_);
            var publicKey = GetResourceStream(PublicKeyName_);
            if (!IsSignatureValid(responseStream, signature, publicKey))
                throw new InvalidOperationException("Signature of the update is not valid.");

            responseStream.Position = 0;
            return responseStream;
        }

        private bool IsSignatureValid(Stream updateFile, Stream signature, Stream publicKeyStream)
        {
            Console.Write("Verifying signature... ");

            var publicKey = (AsymmetricKeyParameter)new PemReader(new StreamReader(publicKeyStream)).ReadObject();
            if (publicKey == null)
                throw new InvalidOperationException("Could not read the public key.");

            var signer = SignerUtilities.GetSigner(SigningAlg_);
            signer.Init(false, publicKey);

            var buffer = new byte[updateFile.Length];
            updateFile.Read(buffer, 0, buffer.Length);
            signer.BlockUpdate(buffer, 0, buffer.Length);

            var sigBuffer = new byte[signature.Length];
            signature.Read(sigBuffer, 0, sigBuffer.Length);
            var result = signer.VerifySignature(sigBuffer);

            Console.WriteLine(result ? "OK" : "FAIL");

            return result;
        }

        private Stream GetResourceStream(string resourceName)
        {
            Console.Write($"Retrieve resource '{resourceName}'... ");
            var request = CreateRequest(resourceName);

            var responseStream = request.GetResponse().GetResponseStream();
            if (responseStream == null)
            {
                Console.WriteLine("FAIL");
                throw new InvalidOperationException($"Could not retrieve data from '{resourceName}'.");
            }

            Console.WriteLine("OK");

            return ToMemoryStream(responseStream);
        }

        private WebRequest CreateRequest(string resourceName)
        {
            return WebRequest.CreateHttp(Parameters.BaseUrl + "/" + resourceName);
        }

        private Stream ToMemoryStream(Stream input)
        {
            var ms = new MemoryStream();

            var buffer = new byte[4096];
            while (true)
            {
                var readBytes = input.Read(buffer, 0, buffer.Length);
                if (readBytes == 0)
                    break;

                var length = Math.Min(readBytes, buffer.Length);
                ms.Write(buffer, 0, length);
            }

            ms.Position = 0;
            return ms;
        }
    }
}
