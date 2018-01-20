using System;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace BayCityHoldenSync
{
    public class MongoRepository
    {
        private MongoClient _mongoClient;

        public MongoRepository()
        {
            _mongoClient = new MongoClient("mongodb://varun:BrunoFriday13@52.163.255.167:27017/admin");
        }

        public void Upsert(CarAd item)
        {
            var db = _mongoClient.GetDatabase("Delos");

            var collection = db.GetCollection<CarAd>("bayfordHoldenCars");
            collection.ReplaceOneAsync(p => p.Id == item.Id, item, new UpdateOptions { IsUpsert = true });
        }

        public IMongoQueryable<CarAd> GetAllItems()
        {
            var client = new MongoClient("mongodb://varun:BrunoFriday13@52.163.255.167:27017/admin");
            var db = client.GetDatabase("Delos");

            var results = db.GetCollection<CarAd>("bayfordHoldenCars");
            return results.AsQueryable<CarAd>()
                .Where(a => a.LastModified >= DateTime.UtcNow.AddDays(-2));
        }
    }
}
