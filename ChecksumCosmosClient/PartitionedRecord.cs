namespace ChecksumCosmosClient
{
    using Azure.Identity;
    using Microsoft.Azure.Cosmos;

    public abstract class PartitionedRecord
    {
        public string id;
        public string partitionKey;

        public PartitionedRecord(string id, string partitionKey)
        {
            this.id = id;
            this.partitionKey = partitionKey;
        }

        public PartitionedRecord()
        {
            this.id = string.Empty;
            this.partitionKey = string.Empty;
        }
    }

    public class Resource : PartitionedRecord
    {
        public string subscription { get; set; }
        public string name { get; set; }
        public string location { get; set; }
        public string properties { get; set; }

        public Resource()
        : base()
        {
            this.subscription = string.Empty;
            this.name = string.Empty;
            this.location = string.Empty;
            this.properties = string.Empty;
        }

        public Resource(string id, string subscription, string name, string location, string properties)
        : base(id, subscription)
        {
            this.id = id;
            this.subscription = subscription;
            this.name = name;
            this.location = location;
            this.properties = properties;
        }
    }
}
