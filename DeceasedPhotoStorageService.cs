using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CEMETRIX.Web.Services;

public class DeceasedPhotoStorageService
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private readonly IWebHostEnvironment _env;

    public DeceasedPhotoStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("Please choose a photo file.");

        if (file.Length > MaxBytes)
            throw new InvalidOperationException("Photo must be 5 MB or smaller.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Use JPG, PNG, WEBP, or GIF.");

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "deceased");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/deceased/{fileName}";
    }

    public async Task<string> SaveAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        if (file.Size == 0)
            throw new InvalidOperationException("Please choose a photo file.");

        if (file.Size > MaxBytes)
            throw new InvalidOperationException("Photo must be 5 MB or smaller.");

        var ext = Path.GetExtension(file.Name);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Use JPG, PNG, WEBP, or GIF.");

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "deceased");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(physicalPath, FileMode.CreateNew);
        await file.OpenReadStream(MaxBytes, cancellationToken).CopyToAsync(stream, cancellationToken);

        return $"/uploads/deceased/{fileName}";
    }
}
