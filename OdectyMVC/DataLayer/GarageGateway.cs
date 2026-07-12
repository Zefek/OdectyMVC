using System.Net.Http.Json;
using OdectyMVC.Contracts;

namespace OdectyMVC.DataLayer;

public class GarageGateway : IGarageGateway
{
    private readonly HttpClient httpClient;

    public GarageGateway(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<uint> RequestOpen(string identity, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "internal/garage/command", new { Identity = identity }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GarageAccepted>(cancellationToken);
        return result!.R;
    }

    private sealed record GarageAccepted(uint R);
}
