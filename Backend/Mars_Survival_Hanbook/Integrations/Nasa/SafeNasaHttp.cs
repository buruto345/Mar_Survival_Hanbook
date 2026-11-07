using System.Net;
using System.Net.Sockets;
using Mars_Survival_Hanbook.Services;

namespace Mars_Survival_Hanbook.Integrations.Nasa;

public sealed class UpstreamException(string code, bool retryable, TimeSpan? retryAfter = null) : Exception(code)
{
    public string Code { get; } = code;
    public bool Retryable { get; } = retryable;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
public sealed record NasaResponse(byte[] Bytes, string MimeType, Uri Url);
public sealed class NasaUrlPolicy(IConfiguration config, IWebHostEnvironment env)
{
    private static readonly string[] Hosts = ["images-api.nasa.gov", "images-assets.nasa.gov", "ntrs.nasa.gov", "pds.nasa.gov", "pds.mcp.nasa.gov", "trek.nasa.gov", "science.nasa.gov", "www.nasa.gov"];
    public bool Simulation => env.IsDevelopment() && config.GetValue<bool>("Nasa:AllowLocalSimulation");
    public void Validate(Uri uri)
    {
        var local = Simulation && uri.Host == "127.0.0.1" && uri.Port == config.GetValue("Nasa:SimulationPort", 4010);
        var partners = config.GetSection("Nasa:ApprovedPartnerHosts").Get<string[]>() ?? [];
        if (!uri.IsAbsoluteUri || !string.IsNullOrEmpty(uri.UserInfo) ||
            (!local && (uri.Scheme != "https" || !uri.IsDefaultPort ||
             !Hosts.Concat(partners).Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase))))
            throw ApiException.Invalid("URL không thuộc NASA/partner allowlist đã được quản trị cấu hình.");
    }
    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return false;
        var bytes = address.GetAddressBytes();
        if (bytes.Length == 4)
            return !(bytes[0] is 0 or 10 or 127 || bytes[0] >= 224 ||
                bytes[0] == 169 && bytes[1] == 254 || bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                bytes[0] == 192 && bytes[1] is 0 or 168 || bytes[0] == 100 && bytes[1] is >= 64 and <= 127 ||
                bytes[0] == 198 && bytes[1] is 18 or 19);
        return !address.IsIPv6LinkLocal && !address.IsIPv6Multicast && !address.IsIPv6SiteLocal &&
            !address.Equals(IPAddress.IPv6Any) && (bytes[0] & 0xfe) != 0xfc && (bytes[0] & 0xe0) == 0x20;
    }
    public SocketsHttpHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false, UseCookies = false, UseProxy = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        ConnectCallback = async (context, ct) =>
        {
            var host = context.DnsEndPoint.Host;
            var local = Simulation && host == "127.0.0.1" && context.DnsEndPoint.Port == config.GetValue("Nasa:SimulationPort", 4010);
            var addresses = await Dns.GetHostAddressesAsync(host, ct);
            if (addresses.Length == 0 || (!local && addresses.Any(x => !IsPublic(x))))
                throw new HttpRequestException("Upstream resolved to a restricted address.");
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(addresses[0], context.DnsEndPoint.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch { socket.Dispose(); throw; }
        }
    };
}
public sealed class SafeNasaHttp(HttpClient client, NasaUrlPolicy policy, IConfiguration config)
{
    public async Task<NasaResponse> GetAsync(Uri uri, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(config.GetValue("Nasa:TimeoutSeconds", 20)));
        try
        {
            for (var redirect = 0; redirect < 5; redirect++)
            {
                policy.Validate(uri);
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null)
                { uri = new Uri(uri, response.Headers.Location); continue; }
                if (!response.IsSuccessStatusCode)
                {
                    var retry = response.Headers.RetryAfter?.Delta ??
                        (response.Headers.RetryAfter?.Date is { } at ? at - DateTimeOffset.UtcNow : null);
                    throw new UpstreamException("upstream_" + (int)response.StatusCode, (int)response.StatusCode is 429 or >= 500, retry);
                }
                var max = config.GetValue("Nasa:MaxResponseBytes", 20_000_000);
                if (response.Content.Headers.ContentLength > max) throw new UpstreamException("response_too_large", false);
                await using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var output = new MemoryStream();
                var buffer = new byte[81920];
                int read;
                while ((read = await input.ReadAsync(buffer, timeout.Token)) > 0)
                {
                    if (output.Length + read > max) throw new UpstreamException("response_too_large", false);
                    await output.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
                }
                return new(output.ToArray(), response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream", uri);
            }
            throw new UpstreamException("redirect_limit", false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new UpstreamException("upstream_timeout", true); }
        catch (HttpRequestException) { throw new UpstreamException("upstream_network", true); }
    }
}
