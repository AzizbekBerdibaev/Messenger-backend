using MediatR;
using Messenger.Domain.Interfaces;

namespace Messenger.Application.Features.Storage.Commands.GenerateUploadUrl;

public sealed class GenerateUploadUrlCommandHandler
    : IRequestHandler<GenerateUploadUrlCommand, GenerateUploadUrlResponse>
{
    private readonly IStorageService _storageService;

    public GenerateUploadUrlCommandHandler(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<GenerateUploadUrlResponse> Handle(
        GenerateUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        // Generate a unique file key to prevent overwriting
        var extension = Path.GetExtension(request.FileName);
        var fileKey   = $"{Guid.NewGuid()}{extension}";

        var url = await _storageService.GeneratePreSignedUploadUrlAsync(
            fileKey,
            request.ContentType,
            TimeSpan.FromMinutes(15)); // Pre-signed URL valid for 15 minutes

        return new GenerateUploadUrlResponse(url, fileKey);
    }
}
