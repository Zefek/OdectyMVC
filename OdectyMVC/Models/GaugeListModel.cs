using Microsoft.AspNetCore.Mvc;

namespace OdectyMVC.Models
{
    public class GaugeListModel
    {
        public string Description { get; set; }
        public decimal LastValue { get; set; }
        public string Type { get; set; }
        public decimal? NewValue { get;set; }
        public int Id { get; set; }
    }
}
