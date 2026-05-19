namespace E_Commerce.API.Services;

public class FileUploadService
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"];
    private static readonly HashSet<string> AllowedMimeTypes =
    [
        "image/jpeg", "image/jpg", "image/pjpeg",
        "image/png", "image/x-png",
        "image/gif",
        "image/webp", "image/x-webp",
        "image/bmp", "image/x-bmp", "image/x-ms-bmp",
        "image/tiff", "image/x-tiff",
    ];
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public async Task<string?> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return null;

        if (file.Length > MaxFileSize)
            throw new InvalidOperationException($"File size cannot exceed {MaxFileSize / 1024 / 1024}MB");

        var originalExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(originalExtension) || !AllowedExtensions.Contains(originalExtension))
            throw new InvalidOperationException($"File type '{originalExtension}' is not allowed");

        var mimeType = file.ContentType?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(mimeType) || !AllowedMimeTypes.Contains(mimeType))
            throw new InvalidOperationException(
                $"MIME type '{file.ContentType ?? "(null)"}' is not allowed");

        // Detect actual image format from content (reads 12-byte header)
        var (detectedExtension, isWebP) = await DetectImageFormatAsync(file, cancellationToken);

        if (detectedExtension is null && !isWebP)
            throw new InvalidOperationException("File content does not match any supported image format");

        // Use the ACTUAL format's extension so static files returns the correct Content-Type
        var savedExtension = detectedExtension ?? ".webp";
        if (!AllowedExtensions.Contains(savedExtension))
            throw new InvalidOperationException($"Detected image format '{savedExtension}' is not allowed");

        var uploadsFolder = Path.Combine("wwwroot", "images");
        Directory.CreateDirectory(uploadsFolder);

        var safeFileName = $"{Guid.NewGuid()}{savedExtension}";
        var filePath = Path.Combine(uploadsFolder, safeFileName);

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var sourceStream = file.OpenReadStream();
        await sourceStream.CopyToAsync(stream, cancellationToken);

        return safeFileName;
    }

    private static async Task<(string? extension, bool isWebP)> DetectImageFormatAsync(
        IFormFile file, CancellationToken ct)
    {
        const int headerSize = 12;
        using var stream = file.OpenReadStream();
        var header = new byte[headerSize];

        try
        {
            await stream.ReadExactlyAsync(header, 0, headerSize, ct);
        }
        catch (EndOfStreamException)
        {
            throw new InvalidOperationException("File is too small to validate as an image");
        }

        // Check known magic bytes (ordered by specificity: longer patterns first)
        if (StartsWith(header, [0x89, 0x50, 0x4E, 0x47]))
            return (".png", false);

        if (StartsWith(header, [0xFF, 0xD8, 0xFF]))
            return (".jpg", false);

        if (StartsWith(header, [0x47, 0x49, 0x46]))
            return (".gif", false);

        if (StartsWith(header, [0x42, 0x4D]))
            return (".bmp", false);

        if (StartsWith(header, [0x49, 0x49, 0x2A, 0x00]))
            return (".tiff", false);

        if (StartsWith(header, [0x4D, 0x4D, 0x00, 0x2A]))
            return (".tiff", false);

        if (StartsWith(header, [0x00, 0x00, 0x01, 0x00]))
            return (".ico", false);

        // WebP: "RIFF" + 4-byte-size + "WEBP"
        if (StartsWith(header, [0x52, 0x49, 0x46, 0x46]) &&
            StartsWith(header.AsSpan(8), [0x57, 0x45, 0x42, 0x50]))
            return (".webp", true);

        return (null, false);
    }

    private static bool StartsWith(ReadOnlySpan<byte> data, byte[] prefix)
    {
        if (data.Length < prefix.Length) return false;
        for (var i = 0; i < prefix.Length; i++)
            if (data[i] != prefix[i]) return false;
        return true;
    }

    public void DeleteFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;

        var safeName = Path.GetFileName(fileName);
        var filePath = Path.Combine("wwwroot", "images", safeName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
