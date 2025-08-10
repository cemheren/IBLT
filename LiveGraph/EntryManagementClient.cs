using ChecksumCosmosClient;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Perfolizer.Mathematics.Randomization;

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

    public class EntryManagementClient
    {
        private IIncrementalCosmosClient<Entry> cosmosClient;
        private readonly IIncrementalCosmosClient<Edge> edgeClient;

        public EntryManagementClient(IIncrementalCosmosClient<Entry> nodeClient, IIncrementalCosmosClient<Edge> edgeClient)
        {
            this.cosmosClient = nodeClient;
            this.edgeClient = edgeClient;
        }

        const int totalSlots = 1024;

        private byte[] UpdateByteArray(byte[] original, int[]? add, int[]? remove)
        {
            for (int i = 0; i < add?.Length; i++)
            {
                var index = add[i];
                original[index] += 1;
            }

            for (int i = 0; i < remove?.Length; i++)
            {
                var index = remove[i];
                original[index] -= 1;
            }

            return original;
        }

        private int FindSlot(string type)
        { 
            return type.Sum(c => c) % totalSlots;
        }

        public async Task CreateEntry(string type, string tenant, JObject payload)
        { 
            var s = this.FindSlot(type);

            var entry = new Entry(Guid.NewGuid(), tenant, s, payload, []);

            await this.cosmosClient.UpsertItemAsync(entry);
        }

        public async Task DeleteEntry(string type, string uniqueId, string tenant)
        {
            var s = this.FindSlot(type);

            var entry = await this.cosmosClient.ReadItemAsync(partitionKey: tenant + "_" + s, uniqueId);

            if (entry == null)
            {
                return;
            }

            var edges = new List<Edge>();

            foreach (var slot in entry.AffinitizedSlots)
            {
                if (slot > 0)
                {
                    var edge = this.FindEdges(tenant, slot, uniqueId, null);
                }
            }
        }

        public async Task CreateEdge(string type, string tenant, Entry source, Entry target)
        { 
            var s = this.FindSlot(type);

            var edge = new Edge(Guid.NewGuid(), tenant, s, source.UniqueId.ToString(), target.UniqueId.ToString(), UpdateByteArray([], [source.slot], [target.slot]));

            source.AffinitizedSlots = UpdateByteArray(source.AffinitizedSlots, [s], null);
            target.AffinitizedSlots = UpdateByteArray(target.AffinitizedSlots, [s], null);

            // Fake transaction 
            { 
                await this.edgeClient.UpsertItemAsync(edge);

                await this.edgeClient.UpsertItemAsync(edge);
                await this.edgeClient.UpsertItemAsync(edge);
            }
        }

        public async Task<Edge[]> FindEdges(string tenantId, int slot, string? sourceId, string? targetId)
        {
            // This can be affinitized and cached 
            var allEdges = await this.edgeClient.GetAllResources(tenantId + "_" + slot);

            var filteredEdges = allEdges
                .Where(e => sourceId != null && e.SourceId == sourceId)
                .Where(e => targetId != null && e.TargetId == targetId);

            return (Edge[])filteredEdges;
        }
    }
}
