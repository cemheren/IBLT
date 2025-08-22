using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveGraph.ContinuousQuery
{
    internal class PausingVisitor<T> : AbstractParseTreeVisitor<T>
    {


        ////public override IEnumerable<T> VisitRoot([NotNull] CQParser.RootContext context)
        ////{
        ////    foreach (var statement in context.statement())
        ////    {
        ////        // Visit each child statement individually
        ////        foreach (var action in Visit(statement))
        ////        {
        ////            // Yield the result of visiting the child.
        ////            yield return action;
        ////        }
        ////    }
        ////}
    }
}
