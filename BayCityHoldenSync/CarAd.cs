using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace BayCityHoldenSync
{
    [BsonIgnoreExtraElements]
    public class CarAd
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public string Id { get; set; }
        [BsonElement("stock")]
        public string StockNumber { get; set; }
        [BsonElement("vin")]
        public string Vin { get; set; }
        [BsonElement("make")]
        public string Make { get; set; }
        [BsonElement("model")]
        public string Model { get; set; }
        [BsonElement("year")]
        public string Year { get; set; }
        [BsonElement("dealer_id")]
        public string DealerId { get; set; }
        [BsonElement("body")]
        public string Body { get; set; }
        [BsonElement("title")]
        public string Title { get; set; }
        [BsonElement("price")]
        public string Price { get; set; }
        [BsonElement("image_url")]
        public string ImageUrl { get; set; }
        public string Category { get; set; }
        [BsonElement("sub_url")]
        public string FinalUrl { get; set; }
        [BsonElement("last_modified")]
        public DateTime LastModified { get; set; }
        public string Colour { get; set; }
        public string Odometer { get; set; }
        public string Rego { get; set; }
        public string Condition { get; set; }
        public string Transmission { get; set; }
        public string Engine { get; set; }
    }
}
