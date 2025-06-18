namespace ChecksumCosmosClient
{
    using Azure.Identity;
    using Microsoft.Azure.Cosmos;

    public abstract record PartitionedRecord
    {
        public string id;
        public string partitionKey;

        public PartitionedRecord()
        {
            this.id = string.Empty;
            this.partitionKey = string.Empty;
        }

        public PartitionedRecord(string id, string partitionKey)
        {
            this.id = id;
            this.partitionKey = partitionKey;
        }
    }

    public record Resource : PartitionedRecord
    {
        public string id { get; set; }
        public string subscription { get; set; }
        public string name { get; set; }
        public string location { get; set; }

        public Resource()
        : base()
        {
            this.id = string.Empty;
            this.subscription = string.Empty;
            this.name = string.Empty;
            this.location = string.Empty;
        }

        public Resource(string id, string subscription, string name, string location)
        : base(id, subscription)
        {
            this.id = id;
            this.subscription = subscription;
            this.name = name;
            this.location = location;
        }
    }
}
