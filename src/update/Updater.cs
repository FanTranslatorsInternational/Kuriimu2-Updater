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
    /// <summary>
    /// A class to update an application.
    /// </summary>
    class Updater
    {
        private const string ManifestName_ = "manifest.json";
        private const string BuildName_ = "latest.zip";
        private const string SignatureName_ = "latest.sig";
        private const string PublicKeyName_ = "publickey.pem";

        private const string SigningAlg_ = "SHA-512withRSA";

        private readonly UpdateParameters _parameters;

        /// <summary>
        /// Creates a new instance of <see cref="Updater"/>.
        /// </summary>
        /// <param name="parameters">The parameters the updater uses.</param>
        public Updater(UpdateParameters parameters)
        {
            _parameters = parameters;
        }

        /// <summary>
        /// Gets the manifest of the updateable application.
        /// </summary>
        /// <returns>The parsed manifest.</returns>
        public Manifest GetManifest()
        {
            var responseStream = GetResourceStream(ManifestName_);

            var reader = new StreamReader(responseStream);
            return JsonConvert.DeserializeObject<Manifest>(reader.ReadToEnd());
        }

        /// <summary>
        /// Gets the latest update zip for the updateable application.
        /// </summary>
        /// <returns>The latest update zip.</returns>
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

        /// <summary>
        /// Checks if a given signature for a file is valid.
        /// </summary>
        /// <param name="updateFile">The file to verify.</param>
        /// <param name="signature">The signature to verify against.</param>
        /// <param name="publicKeyStream">The public key to the signature.</param>
        /// <returns>If the file is valid using the signature and public key.</returns>
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

        /// <summary>
        /// Gets a resource from the repository.
        /// </summary>
        /// <param name="resourceName">The name of the resource.</param>
        /// <returns>The resource stream.</returns>
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

        /// <summary>
        /// Creates a request to a resource.
        /// </summary>
        /// <param name="resourceName">The name of the resource.</param>
        /// <returns>The resource request.</returns>
        private WebRequest CreateRequest(string resourceName)
        {
            return WebRequest.CreateHttp(_parameters.BaseUrl + "/" + resourceName);
        }

        /// <summary>
        /// Converts any given stream to a memory stream.
        /// </summary>
        /// <param name="input">The stream to convert.</param>
        /// <returns>The converted stream.</returns>
        /// <remarks>Mainly used to fully read a stream that does not support reading by length, but reading in buffers, like an Internet Resource.</remarks>
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
