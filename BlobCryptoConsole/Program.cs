using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BlobCryptoConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            StressTestScenario();
            
        }

        public static async Task StressTestScenario()
        {
            var keyVaultUri = new Uri("https://mughukeyvault.vault.azure.net/");
            var keyVaultSecretName = "ZUI7Q~wK4TM6rYavbeWWx5EkwNvlOyOtMt6_v";
            var keyVaultTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            var keyVaultClientId = "4ff8e6b0-004a-40ac-9899-dd9a2de9eefd";
            var certificateThumbprint = "EB8CA245426D7859719022107C9A62A21E9EE9E2";

            var x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadOnly);
            var certificates = x509Store.Certificates.Find(X509FindType.FindByThumbprint, certificateThumbprint, false);
            var certificate = certificates[0];
            x509Store.Close();

            var credential = new ClientCertificateCredential(keyVaultTenantId, keyVaultClientId, certificate);
            var secretClient = new SecretClient(keyVaultUri, credential);
            var secret = secretClient.GetSecret(keyVaultSecretName);

            var keyResolver = new KeyResolver(credential);
            var keyEncryptionKey = keyResolver.Resolve(secret.Value.Id);

            var clientSideEncryptionOptions = new ClientSideEncryptionOptions(ClientSideEncryptionVersion.V1_0)
            {
                KeyEncryptionKey = keyEncryptionKey,
                KeyResolver = keyResolver,
                KeyWrapAlgorithm = "A256KW"
            };

            var blobConnectionString = "DefaultEndpointsProtocol=https;AccountName=mughustorageportal;AccountKey=mNKVgKNuGtXPi8dIDywJTzffbDHpJGQVGK7aX9NmyDi9X0j6PVfr7N00b0FP/g8of29dKYYNTIJNS+MXalM/6Q==;EndpointSuffix=core.windows.net";
            var blobContainerName = "test";
            var blobName = "test";
            var blobContents = "test content";

            var blobContainerClient = new BlobContainerClient(blobConnectionString, blobContainerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            var encryptedBlobClient = blobClient.WithClientSideEncryptionOptions(clientSideEncryptionOptions);

            await UploadAsync();

            while (true)
            {
                try
                {
                    await Task.WhenAll(
                        UploadAsync(),
                        DownloadAsync(),
                        UploadAsync()
                    );
                }
                catch (Exception)
                {
                    throw;
                }
            }

            Task UploadAsync() => encryptedBlobClient.UploadAsync(new BinaryData(blobContents), new BlobUploadOptions { });
            Task DownloadAsync() => encryptedBlobClient.DownloadContentAsync();
        }

        
    
    }
}
