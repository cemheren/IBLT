using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using LiveGraph.ContinuousQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveGraph
{
    public class QueryPlan
    { 
        
    }

    public class QueryPlanVisitor : CQParserBaseVisitor<QueryPlan>
    {
        public EntryManagementClient EntryManagementClient { get; }

        public bool Paused { get; private set; }
        private readonly Stack<(IRuleNode, int)> visitStack = new();

        public QueryPlanVisitor(EntryManagementClient entryManagementClient)
        {
            EntryManagementClient = entryManagementClient;
        }

        #region pause/resume implementation

        public void Resume()
        {
            Paused = false;
            while (!Paused && visitStack.TryPop(out var state))
            {
                var (node, childIndex) = state;
                VisitChildrenFromIndex(node, childIndex);
            }
        }

        public override QueryPlan VisitChildren([NotNull] IRuleNode node)
        {
            return VisitChildrenFromIndex(node, 0);
        }

        private QueryPlan VisitChildrenFromIndex(IRuleNode node, int startIndex)
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

        public override QueryPlan VisitRoot([NotNull] CQParser.RootContext context)
        {
            return base.VisitRoot(context);
        }

        public override QueryPlan VisitWalkToNeighborsClause([NotNull] CQParser.WalkToNeighborsClauseContext context)
        {
            this.Paused = true;


            return base.VisitWalkToNeighborsClause(context);
        }
    }
}
