using Microsoft.Azure.Cosmos;
using ChecksumCosmosClient;

namespace Tests.LanguageTests
{
    public static class CosmosTestHelper
    {
        // Cosmos DB Emulator connection details
        public static readonly string EmulatorEndpoint = "https://localhost:8081";
        public static readonly string EmulatorKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        public static readonly string DatabaseName = "TestDatabase";
        public static readonly string EntryContainerName = "EntryContainer";
        public static readonly string EdgeContainerName = "EdgeContainer";

        public static CosmosClient CreateCosmosClient()
        {
            var cosmosClientOptions = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            return new CosmosClient(EmulatorEndpoint, EmulatorKey, cosmosClientOptions);
        }

        public static async Task<Database> CreateDatabaseAsync(CosmosClient cosmosClient)
        {
            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
            return database.Database;
        }

        public static async Task<Container> CreateContainerAsync(Database database, string containerName)
        {
            var containerProperties = new ContainerProperties(containerName, "/partitionKey");
            var container = await database.CreateContainerIfNotExistsAsync(containerProperties);
            return container.Container;
        }

        public static async Task CleanupAsync(CosmosClient cosmosClient)
        {
            try
            {
                await cosmosClient.GetDatabase(DatabaseName).DeleteAsync();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Database doesn't exist, nothing to clean up
            }
        }
    }

    public class TestResource : PartitionedRecord
    {
        public TestResource() : base("", "") { }
        public TestResource(string id, string partitionKey) : base(id, partitionKey) { }
        
        public string? Name { get; set; }
        public string? Type { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
        public string? Subscription { get; set; }
    }
}