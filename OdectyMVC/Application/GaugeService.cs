using Microsoft.Extensions.Options;
using OdectyMVC.Contracts;
using OdectyMVC.Dto;
using OdectyMVC.Options;
using System;

namespace OdectyMVC.Application
{
    public class GaugeService : IGaugeService
    {
        private readonly IGaugeContext context;
        private readonly IOptions<GaugeImageLocation> options;

        public GaugeService(IGaugeContext context, IOptions<GaugeImageLocation> options)
        {
            this.context=context;
            this.options = options;
        }

        public async Task AddNewValue(int gaugeId, decimal value, CancellationToken cancellationToken)
        {
            var gauge = await context.GaugeRepository.GetGauge(gaugeId, cancellationToken);
            gauge.SetNewValue(value);
            await context.SaveChangesAsync(cancellationToken);
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

        public async Task UpdateGaugeState(int gaugeId, decimal value, CancellationToken cancellationToken)
        {
            var gauge = await context.GaugeRepository.GetGauge(gaugeId, cancellationToken);
            gauge.LastValue = value;
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task SaveFileForGauge(int gaugeId, IFormFile file, CancellationToken cancellationToken)
        {
            FileInfo fi = new FileInfo(string.Format(options.Value.Path, gaugeId, file.FileName));
            var fileName = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length) + "_" + Guid.NewGuid() + fi.Extension;
            using var stream = File.Create(string.Format(options.Value.Path, gaugeId, fileName));
            await file.CopyToAsync(stream, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            stream.Close();
            await context.MessageQueue.Publish(MessageQueueRoutingKeys.GaugeMVC_Gauge_Fileuploaded,
                new { GaugeId = gaugeId, Datetime = DateTime.Now, FileName = fileName }, cancellationToken);
        }
    }
}
