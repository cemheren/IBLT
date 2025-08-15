using Antlr4.Runtime.Misc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveGraph.ContinuousQuery
{
    public class RegoProgram
    {
        public List<string> targets = new List<string>();

        public static JToken Undefined = JValue.CreateNull();
    }

    /// <summary>
    /// This class ties different parts of the rego program together
    /// </summary>
    public class RootVisitor : RegoParserBaseVisitor<RegoProgram>
    {
        private RegoProgram? currentProgram;

        public override RegoProgram VisitRoot([NotNull] RegoParser.RootContext context)
        {
            currentProgram = new RegoProgram();

            base.VisitRoot(context);

            return currentProgram;
        }

        public override RegoProgram VisitTargets([NotNull] RegoParser.TargetsContext context)
        {
            foreach (var targetName in context.Name())
            {
                currentProgram.targets.Add(targetName.GetText());
            }

            if (currentProgram.targets.Count == 0)
            {
                currentProgram.targets.Add("default");
            }

            return base.VisitTargets(context);
        }

        public override RegoProgram VisitRegoRules([NotNull] RegoParser.RegoRulesContext context)
        {
            return currentProgram;
        }

        public override RegoProgram VisitLiteralExpr([NotNull] RegoParser.LiteralExprContext context)
        {
            return currentProgram;
        }
    }
}
