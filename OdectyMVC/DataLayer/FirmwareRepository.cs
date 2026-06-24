using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Contracts;
using OdectyMVC.Models;
using System.Net;

namespace OdectyMVC.DataLayer
{
    internal class FirmwareRepository : IFirmwareRepository
    {
        private const string BasePath = "api/firmware";

        private readonly HttpClient httpClient;

        public FirmwareRepository(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<FirmwareManifest?> GetManifest(string deviceName, CancellationToken cancellationToken)
        {
            var encodedName = Uri.EscapeDataString(deviceName);
            var response = await httpClient.GetAsync($"{BasePath}/{encodedName}/manifest", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseManifest(content);
        }

        public async Task<IActionResult> GetFirmware(string deviceName, CancellationToken cancellationToken)
        {
            var encodedName = Uri.EscapeDataString(deviceName);
            var response = await httpClient.GetAsync($"{BasePath}/{encodedName}/firmware", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                response.Dispose();
                return new NotFoundResult();
            }
            response.EnsureSuccessStatusCode();

            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                           ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                           ?? $"{deviceName}.bin";

            // Content-Length musí zůstat zachovaná: ESP httpUpdate volá http.getSize() a při len <= 0
            // flashování vůbec nespustí (HTTP_UE_SERVER_NOT_REPORT_SIZE). FileStreamResult by ji ze
            // síťového (neseekovatelného) streamu nenastavil, proto ji propisujeme z horní odpovědi ručně.
            return new FirmwareFileResult(response, response.Content.Headers.ContentLength, fileName);
        }

        private static FirmwareManifest? ParseManifest(string content)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }
                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }
                var key = trimmed[..separatorIndex].Trim();
                var value = trimmed[(separatorIndex + 1)..].Trim();
                values[key] = value;
            }

            if (!values.TryGetValue("version", out var versionText) || !int.TryParse(versionText, out var version))
            {
                return null;
            }

            return new FirmwareManifest
            {
                Version = version,
                File = values.GetValueOrDefault("file"),
                Size = long.TryParse(values.GetValueOrDefault("size"), out var size) ? size : null,
                Sha256 = values.GetValueOrDefault("sha256"),
                Commit = values.GetValueOrDefault("commit"),
            };
        }

        private sealed class FirmwareFileResult : IActionResult
        {
            private readonly HttpResponseMessage upstreamResponse;
            private readonly long? contentLength;
            private readonly string fileName;

            public FirmwareFileResult(HttpResponseMessage upstreamResponse, long? contentLength, string fileName)
            {
                this.upstreamResponse = upstreamResponse;
                this.contentLength = contentLength;
                this.fileName = fileName;
            }

            public async Task ExecuteResultAsync(ActionContext context)
            {
                using var response = upstreamResponse;
                var httpResponse = context.HttpContext.Response;
                httpResponse.ContentType = "application/octet-stream";
                if (contentLength.HasValue)
                {
                    httpResponse.ContentLength = contentLength;
                }
                httpResponse.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";

                await using var stream = await upstreamResponse.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted);
                await stream.CopyToAsync(httpResponse.Body, context.HttpContext.RequestAborted);
            }
        }
    }
}
