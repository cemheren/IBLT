using LiveGraph.ContinuousQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveGraph
{
    /// <summary>
    /// Design cue here will be that none of these methods should return a large payload to the caller, everything here will be affinitized. 
    /// They should be "pass forward" i.e. if an action is taken expectation is to pass the calls forward to keep evaluating the query. 
    /// </summary>
    public class QueryPlanAwareDataAccessor
    {
        private readonly EntyDataProvider entyDataProvider;

        public QueryPlanAwareDataAccessor(EntyDataProvider entyDataProvider)
        {
            this.entyDataProvider = entyDataProvider;
        }

        public async Task<bool> WalkToNeighbors(string tenantId, Entry start, string edgeType, QueryPlanVisitor visitorInstance)
        {
            var edges = new List<Edge>();

            foreach (var slot in start.AffinitizedSlots)
            {
                if (slot > 0)
                {
                    var edgesForSlot = await this.FindEdges(tenantId, slot, sourceId: start.UniqueId.ToString(), null, edgeType, visitorInstance);
                    edges.AddRange(edgesForSlot);
                }
            }

            var entries = new List<Entry>();

            foreach (var edge in edges)
            {
                var entry = await this.entyDataProvider.GetEntry(tenantId, (int)edge.TargetAffinity!, edge.TargetId);
                if (entry != null)
                {
                    entries.Add(entry!);
                }
            }

            visitorInstance.existingContext.CurrentEntries = entries.ToArray();

            visitorInstance.Resume();

            return true;
        }

        /// <summary>
        /// This version may need to be feedforward. Design cue here is to duplicate functionality for either need for FF version and regular version of the function. 
        /// <returns></returns>
        public async Task<Edge[]> FindEdges(string tenantId, int slot, string? sourceId, string? targetId, string? edgeType, QueryPlanVisitor visitorInstance)
        {
            // This can be affinitized and cached 
            var allEdges = await this.entyDataProvider.GetAllEdges(tenantId + "_" + slot);

            var filteredEdges = allEdges
                .Where(e => sourceId != null && e.SourceId == sourceId
                            || targetId != null && e.TargetId == targetId);

            if (edgeType != null)
            {
                filteredEdges.Where(edge => edge.type.Equals(edgeType, StringComparison.OrdinalIgnoreCase));
            }

            return filteredEdges.ToArray();
        }
    }
}
