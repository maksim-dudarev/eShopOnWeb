using Newtonsoft.Json;
using System.Collections.Generic;

namespace DeliveryOrderProcessor.Models
{
    public class Order
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public Address ShipToAddress { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public decimal Total { get; set; }

        public decimal GetTotal()
        {
            var total = 0m;
            foreach (var item in OrderItems)
            {
                total += item.UnitPrice * item.Units;
            }
            return total;
        }
    }
}
