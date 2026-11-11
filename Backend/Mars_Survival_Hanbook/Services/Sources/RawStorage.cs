namespace Mars_Survival_Hanbook.Services.Sources;

public sealed class RawStorage(IConfiguration config, IWebHostEnvironment env)
{
    private static readonly Dictionary<string, string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp",
        ["image/gif"] = ".gif", ["image/tiff"] = ".tif", ["image/bmp"] = ".bmp",
        ["image/avif"] = ".avif", ["image/svg+xml"] = ".svg"
    };
    private readonly string root = Path.GetFullPath(config["Storage:RawRoot"] ?? Path.Combine(env.ContentRootPath, "App_Data", "raw"));
    private readonly string imageRoot = Path.GetFullPath(config["Storage:ImageRoot"]
        ?? Path.Combine(config["Storage:RawRoot"] ?? env.ContentRootPath, "ImageMarHanbook"));

    public async Task<(string Path, string Hash)> PutAsync(byte[] bytes, CancellationToken ct, string? mimeType = null)
    {
        var hash = SourceNormalization.Hash(bytes);
        var directory = mimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true ? imageRoot : root;
        Directory.CreateDirectory(directory);
        var extension = ImageExtensions.GetValueOrDefault(mimeType?.Split(';')[0].Trim() ?? "", "");
        var path = Path.Combine(directory, hash + extension);
        var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, ct);
            File.Move(temporary, path, overwrite: false);
        }
        catch (IOException) when (File.Exists(path))
        {
            // Reuse only a complete file whose bytes match its content-addressed name.
            var existing = await File.ReadAllBytesAsync(path, ct);
            if (SourceNormalization.Hash(existing) != hash) throw new IOException("Raw object checksum mismatch.");
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return (hash, hash);
    }
    public async Task<byte[]> ReadAsync(string key, CancellationToken ct)
    {
        if (key.Length != 64 || key.Any(c => !Uri.IsHexDigit(c))) throw ApiException.Missing();
        // Manifests, published versions and older installations all identify blobs by hash.
        var path = Path.Combine(imageRoot, key);
        foreach (var extension in ImageExtensions.Values)
        {
            if (File.Exists(path + extension))
            {
                path += extension;
                break;
            }
        }
        if (!File.Exists(path)) path = Path.Combine(root, key);
        if (!File.Exists(path)) throw ApiException.Missing();
        var bytes = await File.ReadAllBytesAsync(path, ct);
        if (SourceNormalization.Hash(bytes) != key) throw new IOException("Raw object checksum mismatch.");
        return bytes;
    }
}
