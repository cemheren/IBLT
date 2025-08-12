namespace ChecksumCosmosClient
{
    using Azure;
    using Azure.Identity;
    using Microsoft.Azure.Cosmos;

    public static class CosmosClientExtensions
    {
        public static ExtendedCosmosClient<T> WithCosmosClientExtensions<T>(this CosmosClient cosmosClient) where T : PartitionedRecord
        {
            return new ExtendedCosmosClient<T>(cosmosClient);
        }
    }

    public class NameResolver
    {
        private readonly List<Func<string, string, string>> list;

        public NameResolver(Func<string> initialResolver)
        {
            this.list = new();
            Add((_) => initialResolver());
        }

        public void Add(Func<string, string> newResolver)
        {
            this.list.Add((s, t) => { return newResolver(s); });
        }

        public void Add(Func<string, string, string> newResolver)
        {
            this.list.Add(newResolver);
        }

        public string Resolve(string? partitionKey = default)
        {
            var r = this.list[0](null, partitionKey);
            foreach (var resolver in this.list[1 ..])
            {
                r = resolver(r, partitionKey);
            }

            return r;
        }
    }

    public class ExtendedCosmosClient<T> : IIncrementalCosmosClient<T> where T : PartitionedRecord
    {
        private CosmosClient cosmosClient;

        public NameResolver DatabaseResolver = new NameResolver(() => "test_db");
        public NameResolver ContainerResolver = new NameResolver(() => "test_container");

        public CosmosClient CosmosClient => cosmosClient;

        public ExtendedCosmosClient(CosmosClient cosmosClient)
        {
            this.cosmosClient = cosmosClient;
        }

        public async Task UpsertItemAsync(T item)
        {
            var container = this
                .cosmosClient
                .GetDatabase(DatabaseResolver.Resolve(item.partitionKey))
                .GetContainer(ContainerResolver.Resolve(item.partitionKey));
         
            ItemResponse<T> response = await container.UpsertItemAsync<T>(
                item: item,
                partitionKey: new PartitionKey(item.partitionKey)
            );

            Console.WriteLine($"ExtendedCosmosClient:UpsertItemAsync: {response.RequestCharge} RU");
        }

        public async Task<T?> ReadItemAsync(string partitionKey, string id)
        {
            var container = this
                .cosmosClient
                .GetDatabase(DatabaseResolver.Resolve(partitionKey))
                .GetContainer(ContainerResolver.Resolve(partitionKey));

            try
            {
                ItemResponse<T> response = await container.ReadItemAsync<T>(
                    id: id,
                    partitionKey: new PartitionKey(partitionKey)
                );

                Console.WriteLine($"ExtendedCosmosClient:ReadItemAsync: {response.RequestCharge} RU");

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<T[]> GetAllResources(string partitionKey)
        {
            var totalRUs = 0.0;

            var container = this
                .cosmosClient
                .GetDatabase(DatabaseResolver.Resolve(partitionKey))
                .GetContainer(ContainerResolver.Resolve(partitionKey));

            string query = "SELECT * FROM products p WHERE p.partitionKey = @pkey";

            var queryDefinition = new QueryDefinition(query)
              .WithParameter("@pkey", partitionKey);

            using FeedIterator<T> feed = container.GetItemQueryIterator<T>(
                queryDefinition: queryDefinition
            );

            List<T> items = new();
            while (feed.HasMoreResults)
            {
                FeedResponse<T> response = await feed.ReadNextAsync();

                totalRUs += response.Diagnostics.GetQueryMetrics().TotalRequestCharge;

                foreach (var item in response)
                {
                    items.Add(item);
                }
            }

            Console.WriteLine($"ExtendedCosmosClient:GetAllResources: {totalRUs} RU, returned {items.Count} items");

            return items.ToArray();
        }

        public async Task<T[]?> GetDiff(T[] items, string partitionKey)
        {
            var allDBItems = await this.GetAllResources(partitionKey);

            var itemsHash = items.ToDictionary(i => i.id, i => i);

            return allDBItems.Where(i => itemsHash.ContainsKey(i.id) == false).ToArray();
        }

        public async Task DeleteItemAsync(string partitionKey, string id)
        {
            var container = this
                .cosmosClient
                .GetDatabase(DatabaseResolver.Resolve(partitionKey))
                .GetContainer(ContainerResolver.Resolve(partitionKey));

            try
            {
                var response = await container.DeleteItemAsync<T>(
                    id: id,
                    partitionKey: new PartitionKey(partitionKey)
                );

                Console.WriteLine($"ExtendedCosmosClient:DeleteItemAsync: {response.RequestCharge} RU");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return;
            }
        }
    }
}
