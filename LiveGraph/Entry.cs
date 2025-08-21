using ChecksumCosmosClient;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace LiveGraph
{
    public class Entry : PartitionedRecord
    {
        public Entry(Guid uniqueId, string tenant, int slot, JObject payload, byte[] affinitizedSlots)
        : base(uniqueId.ToString(), GetPartitionKey(tenant, slot))
        {
            this.Data = payload;
            this.UniqueId = uniqueId;
            this.AffinitizedSlots = affinitizedSlots;
            this.slot = slot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetPartitionKey(string tenant, int slot)
        {
            return $"{tenant}_{slot}";
        }

        public JObject? Data { get; set; }

        public Guid UniqueId { get; set; }

        public byte[] AffinitizedSlots { get; set; }

        public int slot;
    }
}
