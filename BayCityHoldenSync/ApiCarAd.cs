using Newtonsoft.Json;
using System.Collections.Generic;

namespace BayCityHoldenSync
{
    public class ApiCarAd
    {
        [JsonProperty(PropertyName = "VIN")]
        public string Vin { get; set; }
        [JsonProperty(PropertyName = "make")]
        public string Make { get; set; }
        [JsonProperty(PropertyName = "model")]
        public string Model { get; set; }
        [JsonProperty(PropertyName = "model_year")]
        public string Year { get; set; }
        [JsonProperty(PropertyName = "car_dealership")]
        public string DealerId { get { return "BayCityHolden"; } }
        [JsonProperty(PropertyName = "vehicle_description")]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "initial_price")]
        public int Price { get; set; }
        [JsonProperty(PropertyName = "colour")]
        public string Colour { get; set; }
        [JsonProperty(PropertyName = "kilometres")]
        public string Odometer { get; set; }

        [JsonProperty(PropertyName = "registration_number")]
        public string Rego { get; set; }
        [JsonProperty(PropertyName = "car_images")]
        public List<CarImage> Images { get; set; }
        [JsonProperty(PropertyName = "condition")]
        public string Condition { get; set; }
        [JsonProperty(PropertyName = "transmission")]
        public string Transmission { get; set; }
        [JsonProperty(PropertyName = "engine")]
        public string Engine { get; set; }
        [JsonProperty(PropertyName = "body_type")]
        public string Body { get; set; }
    }

    public class CarImage
    {
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
    }
}
