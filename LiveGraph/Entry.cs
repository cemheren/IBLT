using ChecksumCosmosClient;
using Newtonsoft.Json.Linq;

namespace LiveGraph
{
    public class Entry : PartitionedRecord
    {
        public Entry(Guid uniqueId, string tenant, int slot, JObject payload, byte[] affinitizedSlots)
        : base(uniqueId.ToString(), tenant + "_" + slot)
        {
            this.Data = payload;
            this.UniqueId = uniqueId;
            this.AffinitizedSlots = affinitizedSlots;
            this.slot = slot;
        }

        public JObject? Data { get; set; }

        public Guid UniqueId { get; set; }

        public byte[] AffinitizedSlots { get; set; }

        public int slot;
    }
}
