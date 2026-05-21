using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OdectyMVC.Contracts;
using OdectyMVC.Dto;
using OdectyMVC.Options;

namespace OdectyMVC.Application
{
    public class GaugeService : IGaugeService
    {
        private readonly IGaugeContext context;
        private readonly IOptions<GaugeImageLocation> options;

        public GaugeService(IGaugeContext context, IOptions<GaugeImageLocation> options)
        {
            this.context = context;
            this.options = options;
        }

        public async Task AddNewValue(int gaugeId, decimal value, CancellationToken cancellationToken)
        {
            await context.MessageQueue.Publish(MessageQueueRoutingKeys.GaugeMVC_Gauge_Statechanged,
                new
                {
                    GaugeId = gaugeId,
                    Value = value,
                    Datetime = DateTime.Now
                }, cancellationToken);
        }

        public async Task<IEnumerable<Models.GaugeListModel>> GetGaugeList(CancellationToken cancellationToken)
        {
            return await context.GaugeListModelRepository.GetGaugeList(cancellationToken);
        }

        public async Task<IActionResult> GetLastPhoto(int gaugeId, CancellationToken cancellationToken)
        {
            return await context.GaugeListModelRepository.GetLastPhoto(gaugeId, cancellationToken);
        }

        public async Task SaveFileForGauge(int gaugeId, MemoryStream memoryStream, CancellationToken cancellationToken)
        {
            var gauge = await context.GaugeListModelRepository.GetById(gaugeId, cancellationToken);
            if (gauge == null)
            {
                throw new ArgumentException($"Gauge with id {gaugeId} not found");
            }
            var fileName = $"{gauge.Type}_{Guid.NewGuid():N}.jpg";
            memoryStream.Position = 0;
            await using var stream = File.Create(string.Format(options.Value.Path, gaugeId, fileName));
            await memoryStream.CopyToAsync(stream, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            await context.MessageQueue.Publish(MessageQueueRoutingKeys.GaugeMVC_Gauge_Fileuploaded,
                new { GaugeId = gaugeId, Datetime = DateTime.UtcNow, FileName = fileName }, cancellationToken);
        }
    }
}
