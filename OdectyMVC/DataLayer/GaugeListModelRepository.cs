using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using OdectyMVC.Contracts;
using OdectyMVC.Models;
using System.Net;
using System.Text.Json;

namespace OdectyMVC.DataLayer
{
    internal class GaugeListModelRepository : IGaugeListModelRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        private readonly HttpClient httpClient;

        public GaugeListModelRepository(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<GaugeListModel>> GetGaugeList(CancellationToken cancellationToken)
        {
            await using var stream = await httpClient.GetStreamAsync("api/gauges", cancellationToken);
            var dtos = await JsonSerializer.DeserializeAsync<List<OdectyStatGaugeDto>>(stream, JsonOptions, cancellationToken)
                       ?? new List<OdectyStatGaugeDto>();
            return dtos.Select(d => new GaugeListModel
            {
                Id = d.Id,
                Description = d.Description,
                Type = d.Type,
                LastValue = d.LastValue,
                LastMeasurementAt = d.LastMeasurementAt,
                HasPhoto = d.HasPhoto,
            });
        }

        public async Task<GaugeListModel?> GetById(int id, CancellationToken cancellationToken)
        {
            var gauges = await GetGaugeList(cancellationToken);
            return gauges.FirstOrDefault(g => g.Id == id);
        }

        public async Task<IActionResult> GetLastPhoto(int id, CancellationToken cancellationToken)
        {
            var response = await httpClient.GetAsync($"api/gauges/{id}/lastphoto", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new NotFoundResult();
            }
            response.EnsureSuccessStatusCode();

            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                           ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                           ?? $"gauge_{id}";
            var contentType = response.Content.Headers.ContentType?.MediaType
                              ?? (ContentTypeProvider.TryGetContentType(fileName, out var ct) ? ct : "application/octet-stream");

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return new FileStreamResult(stream, contentType);
        }

        private sealed class OdectyStatGaugeDto
        {
            public int Id { get; set; }
            public string? Description { get; set; }
            public required string Type { get; set; }
            public decimal LastValue { get; set; }
            public DateTime? LastMeasurementAt { get; set; }
            public bool HasPhoto { get; set; }
        }
    }
}
