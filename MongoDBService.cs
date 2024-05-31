using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Pfz.TravellingSalesman
{
    public class MongoDbService
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        public MongoDbService(string connectionString, string databaseName, string collectionName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _collection = database.GetCollection<BsonDocument>(collectionName);
        }

        public void InsertPopulationStatistics(int generation, List<double> distances)
        {
            var document = new BsonDocument
            {
                { "Generation", generation },
                { "Distances", new BsonArray(distances) },
                { "Timestamp", DateTime.UtcNow }
            };
            _collection.InsertOne(document);
        }
    }
}