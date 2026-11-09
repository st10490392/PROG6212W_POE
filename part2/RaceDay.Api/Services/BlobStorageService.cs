using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public sealed class BlobStorageService(IConfiguration configuration)
{
    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        if (file.Length == 0) throw new ArgumentException("The uploaded file is empty.");
        if (file.Length > 5 * 1024 * 1024) throw new ArgumentException("Images must be 5 MB or smaller.");
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only image files can be uploaded.");
        var connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Blob Storage is not configured.");
        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync();
        var extension = Path.GetExtension(file.FileName);
        var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
        return blob.Uri.ToString();
    }
}
