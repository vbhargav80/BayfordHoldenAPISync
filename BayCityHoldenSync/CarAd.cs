using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System.Collections.Generic;

namespace BayCityHoldenSync
{
    [BsonIgnoreExtraElements]
    public class CarAd
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public string Id { get; set; }

        [BsonElement("last_modified")]
        public DateTime LastModified { get; set; }
        [BsonElement("stock_number")]
        public string StockNumber { get; set; }
        [BsonElement("category")]
        public string Category { get; set; }
        [BsonElement("body_type")]
        public string Body { get; set; }
        [BsonElement("seats")]
        public string Seats { get; set; }
        [BsonElement("fuel_type")]
        public string FuelType { get; set; }
        [BsonElement("year_of_manufacture")]
        public string Year { get; set; }

        [BsonElement("reg")]
        public string Reg { get; set; }
        [BsonElement("color")]
        public string Colour { get; set; }
        [BsonElement("vin")]
        public string Vin { get; set; }
        [BsonElement("make")]
        public string Make { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("transmission")]
        public string Transmission { get; set; }
        [BsonElement("odometer")]
        public string Odometer { get; set; }
        [BsonElement("drive_type")]
        public string DriveType { get; set; }
        [BsonElement("doors")]
        public string Doors { get; set; }
        [BsonElement("model")]
        public string Model { get; set; }
        [BsonElement("price")]
        public string Price { get; set; }
        [BsonElement("engine")]
        public string Engine { get; set; }
        [BsonElement("class")]
        public string Class { get; set; }
        [BsonElement("comments")]
        public string Comments { get; set; }
        [BsonElement("features")]
        public List<string> Features { get; set; }
        [BsonElement("images")]
        public List<string> Images { get; set; }
        [BsonElement("version")]
        public string Version { get; set; }
    }
}
