namespace OdectyMVC.Contracts
{
    public interface IGaugeListModelRepository
    {
        Task<IEnumerable<Models.GaugeListModel>> GetGaugeList(CancellationToken cancellationToken);
    }
}
