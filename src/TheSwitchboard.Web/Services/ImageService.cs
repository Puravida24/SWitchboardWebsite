using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace TheSwitchboard.Web.Services;

public interface IImageService
{
    Task<ImageResult> ProcessAndSaveAsync(Stream imageStream, string fileName, string category);
    void DeleteImage(string relativePath);
}

public record ImageResult(string OriginalPath, string ThumbnailPath, string MediumPath, string LargePath);

public class ImageService : IImageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageService> _logger;

    private static readonly (string Suffix, int Width)[] Sizes =
    [
        ("thumb", 200),
        ("medium", 600),
        ("large", 1200)
    ];

    public ImageService(IWebHostEnvironment env, ILogger<ImageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<ImageResult> ProcessAndSaveAsync(Stream imageStream, string fileName, string category)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath, "images", "uploads", category);
        Directory.CreateDirectory(uploadsDir);

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var uniqueName = $"{baseName}-{Guid.NewGuid():N[..8]}";

        using var image = await Image.LoadAsync(imageStream);

        // Save original as WebP
        var originalPath = Path.Combine(uploadsDir, $"{uniqueName}.webp");
        await image.SaveAsWebpAsync(originalPath, new WebpEncoder { Quality = 85 });

        var paths = new string[3];

        for (int i = 0; i < Sizes.Length; i++)
        {
            var (suffix, width) = Sizes[i];
            using var resized = image.Clone(ctx =>
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(width, 0),
                    Mode = ResizeMode.Max
                });
            });

            var resizedPath = Path.Combine(uploadsDir, $"{uniqueName}-{suffix}.webp");
            await resized.SaveAsWebpAsync(resizedPath, new WebpEncoder { Quality = 80 });
            paths[i] = $"/images/uploads/{category}/{uniqueName}-{suffix}.webp";
        }

        _logger.LogInformation("Image processed: {FileName} -> 4 variants in {Category}", fileName, category);

        return new ImageResult(
            $"/images/uploads/{category}/{uniqueName}.webp",
            paths[0],
            paths[1],
            paths[2]);
    }

    public void DeleteImage(string relativePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted image: {Path}", relativePath);
        }
    }
}
