using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdectyMVC.Business
{
    [Table("Gauge")]
    public class Gauge
    {
        [Key]
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal LastValue { get; set; }
        public string Name { get; set; }

        public void SetNewValue(decimal newValue)
        {
            LastValue = newValue;
        }
    }
}
