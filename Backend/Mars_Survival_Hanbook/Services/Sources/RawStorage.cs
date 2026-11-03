namespace Mars_Survival_Hanbook.Services.Sources;

public sealed class RawStorage(IConfiguration config, IWebHostEnvironment env)
{
    private readonly string root = Path.GetFullPath(config["Storage:RawRoot"] ?? Path.Combine(env.ContentRootPath, "App_Data", "raw"));
    public async Task<(string Path, string Hash)> PutAsync(byte[] bytes, CancellationToken ct)
    {
        var hash = SourceNormalization.Hash(bytes);
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, hash);
        var temporary = Path.Combine(root, Guid.NewGuid().ToString("N") + ".tmp");
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
        var path = Path.Combine(root, key);
        if (!File.Exists(path)) throw ApiException.Missing();
        var bytes = await File.ReadAllBytesAsync(path, ct);
        if (SourceNormalization.Hash(bytes) != key) throw new IOException("Raw object checksum mismatch.");
        return bytes;
    }
}
