using ChecksumCosmosClient;

namespace LiveGraph
{
    public class Edge : PartitionedRecord
    {
        public Edge() { }

        public Edge(Guid uniqueId, string tenant, int slot, string type, string sourceId, string targetId, int sourceAffinity, int targetAffinity)
        : base(uniqueId.ToString(), tenant + "_" + slot)
        {
            this.UniqueId = uniqueId;
            
            this.SourceAffinity = sourceAffinity;
            this.TargetAffinity = targetAffinity;

            this.SourceId = sourceId;
            this.TargetId = targetId;
            this.slot = slot;
            this.type = type;
        }

        public string SourceId { get; set; }
        
        public string TargetId { get; set; }

        public int slot;

        public Guid UniqueId { get; set; }

        public string type { get; set; }

        public int? SourceAffinity { get; set; }
        
        public int? TargetAffinity { get; set; }
    }
}
