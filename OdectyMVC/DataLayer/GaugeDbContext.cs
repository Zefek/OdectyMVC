using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OdectyMVC.Business;
using OdectyMVC.Models;
using System.Text.Json.Serialization;

namespace OdectyMVC.DataLayer
{
    public class GaugeDbContext
    {
        public GaugeDbContext()
        {
            var gauges = File.ReadAllText("GaugeList.json");
            Gauges = JsonConvert.DeserializeObject<List<Gauge>>(gauges);
            GaugeModels = JsonConvert.DeserializeObject<List<GaugeListModel>>(gauges);
        }
        public List<Gauge> Gauges { get; set; }

        public List<GaugeListModel> GaugeModels { get; set; }

        public void SaveChanges()
        {
            File.WriteAllText("GaugeList.json", JsonConvert.SerializeObject(Gauges, Formatting.Indented));

        }
    }
}
