using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChecksumCosmosClient
{
    public interface IIncrementalCosmosClient<T>
    {
        public CosmosClient CosmosClient { get; }

        public Task UpsertItemAsync(T item);

        public Task<T?> ReadItemAsync(string partitionKey, string id);

        public Task DeleteItemAsync(string partitionKey, string id);

        public Task<T[]> GetAllResources(string partitionKey);

        public Task<T[]?> GetDiff(T[] items, string partitionKey);
    }
}
