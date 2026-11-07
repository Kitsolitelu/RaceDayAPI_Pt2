using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayApi.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(string connectionString, string containerName)
        {
            _container = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            // creates the container the first time, and lets the browser show the images
            await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // a random name so two users uploading "photo.png" never overwrite each other
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
            var blob = _container.GetBlobClient(blobName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            };

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, options);

            return blob.Uri.ToString();
        }
    }
}
