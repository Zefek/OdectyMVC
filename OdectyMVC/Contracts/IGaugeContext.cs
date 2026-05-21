namespace OdectyMVC.Contracts
{
    public interface IGaugeContext
    {
        IGaugeListModelRepository GaugeListModelRepository { get; }
        IMessageQueue MessageQueue { get; }
    }
}
