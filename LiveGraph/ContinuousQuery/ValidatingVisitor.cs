using Antlr4.Runtime.Misc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveGraph.ContinuousQuery
{
    public class CQProgram
    {
        public string StartFrom { get; set; }

        public List<CQExpression> DataExpressions { get; set; }

        public List<string> IndexesUsed { get; set; } = [];
    }

    public class CQExpression
    {
        public string Type { get; set; }

        public string[]? Variables { get; set; }
    }

    /// <summary>
    /// This class ties different parts of the CQ program together
    /// </summary>
    public class ValidatingVisitor : CQParserBaseVisitor<CQProgram>
    {
        private CQProgram? currentProgram;

        public override CQProgram VisitRoot([NotNull] CQParser.RootContext context)
        {
            currentProgram = new CQProgram();
            currentProgram.DataExpressions = [];

            base.VisitRoot(context);

            return currentProgram;
        }

        public override CQProgram VisitExpression([NotNull] CQParser.ExpressionContext context)
        {
            return base.VisitExpression(context);
        }

        public override CQProgram VisitExtendClause([NotNull] CQParser.ExtendClauseContext context)
        {
            var assignments = new List<CQExpression>();

            foreach (var assignment in context.assignmentList().assignment())
            {
                assignments.Add(this.ParseAssignment(assignment));
            }

            this.currentProgram.DataExpressions.AddRange(assignments);

            return base.VisitExtendClause(context);
        }

        public override CQProgram VisitUseIndexClause([NotNull] CQParser.UseIndexClauseContext context)
        {
            if (this.currentProgram.DataExpressions.Count(expresion => expresion.Variables.Any(v => v.Equals("key", StringComparison.OrdinalIgnoreCase))) < 1)
            {
                throw new ArgumentException($"Define a variable with name 'key' in order to use it in an index expression like: `{context.GetText()}`");
            }

            this.currentProgram.IndexesUsed.Add(context.IDENTIFIER().ToString());

            return base.VisitUseIndexClause(context);
        }

        public CQExpression ParseAssignment([NotNull] CQParser.AssignmentContext context)
        {
            var expression = new CQExpression() { Type = "Assignment" };

            var variable = context.IDENTIFIER().ToString();

            if (variable == null)
            {
                throw new ArgumentNullException($"IDENTIFIER expected for assignment expression `{context.GetText()}`");
            }

            expression.Variables = [variable];

            return expression;
        }

        public override CQProgram VisitStartFromClause([NotNull] CQParser.StartFromClauseContext context)
        {
            var startingDataSetIdentifier = context.IDENTIFIER().ToString();

            if (startingDataSetIdentifier == null)
            {
                throw new ArgumentNullException("START FROM clause needs a non-null argument");
            }

            this.currentProgram!.StartFrom = startingDataSetIdentifier;

            return base.VisitStartFromClause(context);
        }
    }
}
