namespace OdectyMVC.Contracts
{
    public interface IGaugeContext
    {
        IGaugeRepository GaugeRepository { get; }
        IGaugeListModelRepository GaugeListModelRepository { get; }
        IMessageQueue MessageQueue { get; }
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
