namespace ChecksumCosmosClient
{
    using Azure.Identity;
    using Microsoft.Azure.Cosmos;

    public abstract record PartitionedRecord(string id, string partitionKey);

    public record Resource(
        string subscription,
        string id,
        string name
,
        string location) : PartitionedRecord(id, subscription);
}
