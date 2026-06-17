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
                return new NotFoundResult();
            }
            response.EnsureSuccessStatusCode();

            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                           ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                           ?? $"{deviceName}.bin";

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return new FileStreamResult(stream, "application/octet-stream")
            {
                FileDownloadName = fileName
            };
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
    }
}
