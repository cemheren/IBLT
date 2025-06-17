namespace ChecksumCosmosClient
{
    using Azure.Identity;
    using IBLT;
    using IBLT.Sim;
    using MessagePack;
    using Microsoft.Azure.Cosmos;
    using Newtonsoft.Json;

    public static class CosmosIBLTExtensions
    {
        public static ExtendedCosmosClient<T> WithIBLTExtension<T>(this ExtendedCosmosClient<T> cosmosClient) where T : PartitionedRecord
        {
            return cosmosClient;
        }
    }

    public record IBLTRecord(string id, string partitionKey, byte[] data) : PartitionedRecord(id, partitionKey);

    public class IBLTExtender<T> where T : PartitionedRecord
    {
        private readonly ExtendedCosmosClient<T> cosmosClient;
        private ExtendedCosmosClient<IBLTRecord> IBLTCosmosClient;

        const string databaseName = "test_db";
        const string containerName = "test_iblt_container";

        public IBLTExtender(ExtendedCosmosClient<T> cosmosClient)
        {
            this.cosmosClient = cosmosClient;

            this.IBLTCosmosClient = new ExtendedCosmosClient<IBLTRecord>(cosmosClient.cosmosClient);
            this.IBLTCosmosClient.DatabaseResolver.Add((_) => databaseName);
            this.IBLTCosmosClient.ContainerResolver.Add((_) => containerName);
        }

        public async Task UpsertItemAsync(T item)
        {
            var readItem = await this.ReadItemAsync(item.partitionKey, item.id);

            var ibltTask = new Task(async () => 
            {
                // If this is a new item, we should insert it to the iblt, otherwise it already exists with this id. 
                if (readItem == null)
                {
                    var dbIBLT = await this.IBLTCosmosClient.ReadItemAsync(item.partitionKey, item.partitionKey);
                    var ibltback = MessagePackSerializer.Deserialize<FaultTolerantIBLT>(dbIBLT?.data);

                    if (ibltback == null)
                    {
                        ibltback = new FaultTolerantIBLT(4, 128);
                    }

                    ibltback.InsertString(item.id, item.id); // We should create a smaller footprint IBLT for when the key == value.

                    var msgPackSerialized = MessagePackSerializer.Serialize(ibltback);
                    await this.IBLTCosmosClient.UpsertItemAsync(new IBLTRecord(item.partitionKey, item.partitionKey, msgPackSerialized));
                }
            });

            ibltTask.Start();

            await Task.WhenAll(this.cosmosClient.UpsertItemAsync(item), ibltTask);
        }

        public async Task<T?> ReadItemAsync(string partitionKey, string id)
        {
            return await this.cosmosClient.ReadItemAsync(partitionKey, id);
        }

        public async Task<T[]> GetAllResources(string partitionKey)
        {
            var allResources = await this.cosmosClient.GetAllResources(partitionKey);

            var iblt = new FaultTolerantIBLT(4, 128);
            foreach (var item in allResources)
            {
                iblt.InsertString(item.id, item.id);
            }

            var msgPackSerialized = MessagePackSerializer.Serialize(iblt);
            await this.IBLTCosmosClient.UpsertItemAsync(new IBLTRecord(partitionKey, partitionKey, msgPackSerialized));

            return allResources;
        }

        public async Task<T[]> GetDiff(T[] items, string partitionKey)
        {
            var iblt = new FaultTolerantIBLT(4, 128);
            foreach (var item in items)
            {
                iblt.InsertString(item.id, item.id);
            }

            var dbIBLT = await this.IBLTCosmosClient.ReadItemAsync(partitionKey, partitionKey);
            var ibltback = MessagePackSerializer.Deserialize<FaultTolerantIBLT>(dbIBLT?.data);

            ibltback.Substract(iblt);

            var diffList = ibltback.ListStrings();
        }
    }
}
