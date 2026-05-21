namespace OdectyMVC.Models
{
    public class GaugeListModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public decimal LastValue { get; set; }
        public DateTime? LastMeasurementAt { get; set; }
        public bool HasPhoto { get; set; }
        public decimal? NewValue { get; set; }
    }
}
