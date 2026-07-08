using Messenger.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messenger.Infrastructure.Services;

public sealed class DummyStorageService : IStorageService
{
    private readonly ILogger<DummyStorageService> _logger;

    public DummyStorageService(ILogger<DummyStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> GeneratePreSignedUploadUrlAsync(string fileName, string contentType, TimeSpan expiration)
    {
        _logger.LogInformation("Generating pre-signed URL for {FileName} with content type {ContentType}", fileName, contentType);

        // This is a dummy URL for development purposes
        return Task.FromResult($"https://dummy-storage.local/upload?file={fileName}");
    }
}
