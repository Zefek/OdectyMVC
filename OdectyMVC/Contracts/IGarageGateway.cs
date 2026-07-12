namespace OdectyMVC.Contracts;

public interface IGarageGateway
{
    Task<uint> RequestOpen(string identity, CancellationToken cancellationToken);
}
