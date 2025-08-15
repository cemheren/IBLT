using ChecksumCosmosClient;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Perfolizer.Mathematics.Randomization;

namespace LiveGraph
{

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

        // note that reference counting here goes up to 255 connections. 
        private byte[] UpdateByteArray(byte[] original, int[]? add, int[]? remove)
        {
            original ??= new byte[totalSlots];

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

            // akif's thoughts: for a well connected entry, we make a maximum of 1024 collection requests, which would saturate the whole cluster
            // After, we need to remove all the individual edges, this operation can be affinitized to the VM the original request ended up in.

            foreach (var slot in entry.AffinitizedSlots)
            {
                if (slot > 0)
                {
                    var edgesForSlot = await this.FindEdges(tenant, slot, uniqueId, null);
                    edges.AddRange(edgesForSlot);
                }
            }

            foreach (var edge in edges)
            {
                edge.AffinitizedSlots = UpdateByteArray(edge.AffinitizedSlots, add: null, remove: [s]);
                await this.edgeClient.UpsertItemAsync(edge);
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

                await this.cosmosClient.UpsertItemAsync(source);
                await this.cosmosClient.UpsertItemAsync(target);
            }
        }

        public async Task DeleteEdge(string type, string uniqueId, string tenant)
        {
            var s = this.FindSlot(type);

            var edge = await this.edgeClient.ReadItemAsync(partitionKey: tenant + "_" + s, uniqueId);

            if (edge == null)
            {
                return;
            }

            var sourceEntry = await this.cosmosClient.ReadItemAsync(partitionKey: tenant + "_" + edge.slot, edge.SourceId);
            var targetEntry = await this.cosmosClient.ReadItemAsync(partitionKey: tenant + "_" + edge.slot, edge.TargetId);

            if (sourceEntry != null)
            {
                sourceEntry.AffinitizedSlots = UpdateByteArray(sourceEntry.AffinitizedSlots, add: null, remove: [s]);
                await this.cosmosClient.UpsertItemAsync(sourceEntry);
            }

            if (targetEntry != null)
            {
                targetEntry.AffinitizedSlots = UpdateByteArray(targetEntry.AffinitizedSlots, add: null, remove: [s]);
                await this.cosmosClient.UpsertItemAsync(targetEntry);
            }

            await this.edgeClient.DeleteItemAsync(partitionKey: tenant + "_" + s, uniqueId);
        }

        public async Task<Edge[]> FindEdges(string tenantId, int slot, string? sourceId, string? targetId)
        {
            // This can be affinitized and cached 
            var allEdges = await this.edgeClient.GetAllResources(tenantId + "_" + slot);

            var filteredEdges = allEdges
                .Where(e => sourceId != null && e.SourceId == sourceId
                            || targetId != null && e.TargetId == targetId);

            return (Edge[])filteredEdges;
        }

    }
}
