using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Vaulted.Controllers
{
    // Passes external images through the API so the frontend can draw them on a canvas
    // (share cards). Browsers refuse to export a canvas with images from sites that don't
    // allow cross-origin use; served from here, the API's CORS policy applies.
    //
    // Because the server fetches whatever URL it's given, requests are limited to public
    // http(s) hosts (no localhost / private network), image responses, and 10 MB.
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private const long MaxBytes = 10 * 1024 * 1024;
        private const int MaxRedirects = 3;

        // Redirects are followed by hand so each hop is checked against the same rules
        private static readonly HttpClient Http = new(new SocketsHttpHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        static ImageController()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("MediaVaulted/1.0");
        }

        // GET -> PROXY AN IMAGE
        // GET api/Image/proxy?url=https://...
        [HttpGet("proxy")]
        public async Task<IActionResult> Proxy([FromQuery] string? url, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return BadRequest("A valid image URL is required.");

            try
            {
                for (var hop = 0; hop <= MaxRedirects; hop++)
                {
                    if (!await IsPublicHttpUrlAsync(uri, cancellationToken))
                        return BadRequest("Only public http(s) image links can be loaded.");

                    using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                    if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location)
                    {
                        uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                        return StatusCode(502, $"The image host answered {(int)response.StatusCode}.");

                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    if (contentType == null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        return StatusCode(502, "That link isn't an image.");

                    if (response.Content.Headers.ContentLength > MaxBytes)
                        return StatusCode(502, "The image is larger than 10 MB.");

                    // Read with a hard cap in case the length header was missing or wrong
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var buffer = new System.IO.MemoryStream();
                    var chunk = new byte[81920];
                    int read;
                    while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
                    {
                        if (buffer.Length + read > MaxBytes)
                            return StatusCode(502, "The image is larger than 10 MB.");
                        buffer.Write(chunk, 0, read);
                    }

                    Response.Headers.CacheControl = "public, max-age=86400";
                    return File(buffer.ToArray(), contentType);
                }

                return StatusCode(502, "Too many redirects.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                Console.WriteLine($"Error proxying image: {ex.Message}");
                return StatusCode(502, "Couldn't load that image.");
            }
        }


        // http(s) only, and every address the host resolves to must be public
        private static async Task<bool> IsPublicHttpUrlAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

            IPAddress[] addresses;
            try
            {
                addresses = IPAddress.TryParse(uri.Host, out var literal)
                    ? new[] { literal }
                    : await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
            }
            catch (SocketException)
            {
                return false;
            }

            return addresses.Length > 0 && addresses.All(IsPublic);
        }

        private static bool IsPublic(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            if (IPAddress.IsLoopback(address)) return false;

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = address.GetAddressBytes();
                return !(b[0] == 0                                   // 0.0.0.0/8
                    || b[0] == 10                                    // 10.0.0.0/8
                    || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)    // 100.64.0.0/10 (CGNAT)
                    || b[0] == 127                                   // loopback
                    || (b[0] == 169 && b[1] == 254)                  // link-local
                    || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)     // 172.16.0.0/12
                    || (b[0] == 192 && b[1] == 168)                  // 192.168.0.0/16
                    || b[0] >= 224);                                 // multicast / reserved
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var b = address.GetAddressBytes();
                return !(address.IsIPv6LinkLocal
                    || address.IsIPv6SiteLocal
                    || address.IsIPv6Multicast
                    || (b[0] & 0xFE) == 0xFC                         // fc00::/7 unique local
                    || address.Equals(IPAddress.IPv6Any)             // ::
                    || address.Equals(IPAddress.IPv6None));
            }

            return false;
        }
    }
}
