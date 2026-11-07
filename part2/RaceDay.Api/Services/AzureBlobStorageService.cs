using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public class AzureBlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0) throw new ArgumentException("Please select an image.");
        if (file.Length > 5 * 1024 * 1024) throw new ArgumentException("The image cannot exceed 5 MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Only JPG, PNG and WebP images are supported.");

        var connectionString = configuration.GetConnectionString("AzureBlobStorage");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage has not been configured.");

        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var safeFolder = folder.Trim('/').Replace("..", string.Empty, StringComparison.Ordinal);
        var blob = container.GetBlobClient($"{safeFolder}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        }, cancellationToken);
        return blob.Uri.ToString();
    }
}
