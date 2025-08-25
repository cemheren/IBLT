using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveGraph.ContinuousQuery
{
    public class FeedForwardQueryEvaluationContext
    { 
        /// <summary>
        /// 
        /// </summary>
        public required string TenantId { get; set; }
        
        /// <summary>
        /// Can be the current entry we are working on, or the starter entry 
        /// </summary>
        public required Entry[] CurrentEntries { get; set; }
    }

    public class QueryPlanVisitor : CQParserBaseVisitor<Task<FeedForwardQueryEvaluationContext>>
    {
        public QueryPlanAwareDataAccessor dataAccessor { get; }

        public FeedForwardQueryEvaluationContext existingContext { get; set; }

        public bool Paused { get; private set; }
        private readonly Stack<(IRuleNode, int)> visitStack = new();

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="tenantId">passed in here vs the query since we use this as a high level partition id, it could be abstracted as a partition key too.</param>
        /// <param name="dataAccessor"></param>
        public QueryPlanVisitor(QueryPlanAwareDataAccessor dataAccessor, FeedForwardQueryEvaluationContext existingContext)
        {
            this.dataAccessor = dataAccessor;
            this.existingContext = existingContext;
        }

        #region pause/resume implementation

        public void Resume(FeedForwardQueryEvaluationContext? nextContext = null)
        {
            if (nextContext != null)
            {
                this.existingContext = nextContext;
            }

            Paused = false;
            while (!Paused && visitStack.TryPop(out var state))
            {
                var (node, childIndex) = state;
                VisitChildrenFromIndex(node, childIndex);
            }
        }

        public override async Task<FeedForwardQueryEvaluationContext> VisitChildren([NotNull] IRuleNode node)
        {
            return VisitChildrenFromIndex(node, 0);
        }

        private FeedForwardQueryEvaluationContext VisitChildrenFromIndex(IRuleNode node, int startIndex)
        {
            for (int i = startIndex; i < node.ChildCount; i++)
            {
                if (Paused)
                {
                    visitStack.Push((node, i));
                    return default;
                }
                Visit(node.GetChild(i));
            }
            return default;
        }

        #endregion

        public override async Task<FeedForwardQueryEvaluationContext> VisitRoot([NotNull] CQParser.RootContext context)
        {
            return await base.VisitRoot(context);
        }

        public override async Task<FeedForwardQueryEvaluationContext> VisitWalkToNeighborsClause([NotNull] CQParser.WalkToNeighborsClauseContext context)
        {
            Paused = true;

            // note: don't handle syntax errors here, handle them in validating visitor
            var edgeType = context.relationshipPattern().IDENTIFIER().ToString();

            var result = new List<bool>();
            foreach (var entry in this.existingContext!.CurrentEntries)
            {
                // Walk to neighbor needs a current entries and and edge type. Fail if not provided. 
                var results = await this.dataAccessor.WalkToNeighbors(this.existingContext.TenantId, entry, edgeType: edgeType!, this);
                result.AddRange(results);
            }

            if (result.Any(e => !e))
            {
                // Let's assume these don't throw but return false when there is an exception
                throw new Exception($"Query execution partially failed : {result.Count(e => e == false)}");
            }

            return await base.VisitWalkToNeighborsClause(context);
        }
    }
}
