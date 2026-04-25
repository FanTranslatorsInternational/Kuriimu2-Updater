using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
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
        public async Task<Manifest?> GetManifest()
        {
            var responseStream = await GetResourceStream(ManifestName_);

            var reader = new StreamReader(responseStream);
            var text = await reader.ReadToEndAsync();

            return JsonSerializer.Deserialize(text, ManifestJsonContext.Default.Manifest);
        }

        /// <summary>
        /// Gets the latest update zip for the updateable application.
        /// </summary>
        /// <returns>The latest update zip.</returns>
        public async Task<Stream> GetUpdateFile()
        {
            var responseStream = await GetResourceStream(BuildName_);
            responseStream = ToMemoryStream(responseStream);

            // Verify signature
            var signature = await GetResourceStream(SignatureName_);
            var publicKey = await GetResourceStream(PublicKeyName_);

            using var publicKeyReader = new StreamReader(publicKey);
            var publicKeyPem = await publicKeyReader.ReadToEndAsync();

            if (!IsSignatureValid(responseStream, signature, publicKeyPem))
                throw new InvalidOperationException("Signature of the update is not valid.");

            responseStream.Position = 0;
            return responseStream;
        }

        /// <summary>
        /// Checks if a given signature for a file is valid.
        /// </summary>
        /// <param name="updateFile">The file to verify.</param>
        /// <param name="signature">The signature to verify against.</param>
        /// <param name="publicKeyPem">The public key to the signature in PEM format.</param>
        /// <returns>If the file is valid using the signature and public key.</returns>
        private static bool IsSignatureValid(Stream updateFile, Stream signature, string publicKeyPem)
        {
            Console.Write("Verifying signature... ");

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            var sigBuffer = new byte[signature.Length];
            _ = signature.Read(sigBuffer, 0, sigBuffer.Length);

            bool result = rsa.VerifyData(
                updateFile,
                sigBuffer,
                HashAlgorithmName.SHA512,
                RSASignaturePadding.Pkcs1 // or Pss if required
            );

            Console.WriteLine(result ? "OK" : "FAIL");

            return result;
        }

        /// <summary>
        /// Gets a resource from the repository.
        /// </summary>
        /// <param name="resourceName">The name of the resource.</param>
        /// <returns>The resource stream.</returns>
        private async Task<Stream> GetResourceStream(string resourceName)
        {
            Console.Write($"Retrieve resource '{resourceName}'... ");

            var client = new HttpClient();
            var request = CreateRequest(resourceName);

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("FAIL");
                throw new InvalidOperationException($"Could not retrieve data from '{resourceName}'.");
            }

            var responseStream = await response.Content.ReadAsStreamAsync();

            Console.WriteLine("OK");

            return ToMemoryStream(responseStream);
        }

        /// <summary>
        /// Creates a request to a resource.
        /// </summary>
        /// <param name="resourceName">The name of the resource.</param>
        /// <returns>The resource request.</returns>
        private HttpRequestMessage CreateRequest(string resourceName)
        {
            return new HttpRequestMessage(HttpMethod.Get, _parameters.BaseUrl + "/" + resourceName);
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
