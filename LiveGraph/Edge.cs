using ChecksumCosmosClient;

namespace LiveGraph
{
    public class Edge : PartitionedRecord
    {
        public Edge(Guid uniqueId, string tenant, int slot, string sourceId, string targetId, byte[] affinitizedSlots)
        : base(uniqueId.ToString(), tenant + "_" + slot)
        {
            this.UniqueId = uniqueId;
            this.AffinitizedSlots = affinitizedSlots;
            this.SourceId = sourceId;
            this.TargetId = targetId;
            this.slot = slot;
        }

        public string SourceId { get; set; }
        
        public string TargetId { get; set; }

        public int slot;

        public Guid UniqueId { get; set; }

        public byte[] AffinitizedSlots { get; set; }
    }
}
