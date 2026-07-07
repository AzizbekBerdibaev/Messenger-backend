namespace Messenger.Domain.Interfaces;

/// <summary>
/// Abstraction for object storage operations (e.g., AWS S3, MinIO).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Generates a pre-signed URL that clients can use to upload files directly to the storage.
    /// </summary>
    /// <param name="fileName">The intended name/path of the file in the bucket.</param>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <param name="expiration">How long the URL remains valid.</param>
    /// <returns>A pre-signed URL as a string.</returns>
    Task<string> GeneratePreSignedUploadUrlAsync(string fileName, string contentType, TimeSpan expiration);
}
