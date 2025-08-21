using Antlr4.Runtime;
using LiveGraph;
using LiveGraph.ContinuousQuery;

namespace antlrOpaTest
{
    [TestClass]
    public class CQTranspileTests
    {
        [TestMethod]
        public void ParseCQ()
        {
            var rule = @"
                START FROM resource
                EXTEND key = CSHARP(`match(resource.subscription, ""/subscriptions/{*}/"")`)
                USEINDEX SUBSCRIPTION_TO_HEALTH 
                FILTER index != null
                RETURN resource.id, index.healthId
            ";

            var cq = this.GetParsedContinuousQuery(rule);

            Assert.IsNotNull(cq);
        }

        [TestMethod]
        public void UseIndexWithoutKey()
        {
            var rule = @"
                START FROM resource
                EXTEND somevariable = CSHARP(`match(resource.subscription, ""/subscriptions/{*}/"")`)
                USEINDEX SUBSCRIPTION_TO_HEALTH 
                FILTER index != null
                RETURN resource.id, index.healthId
            ";

            Assert.ThrowsException<ArgumentException>(() => this.GetParsedContinuousQuery(rule));
        }

        private CQProgram? GetParsedContinuousQuery(string queryText)
        {
            var antlrStream = new AntlrInputStream(queryText);
            var lexer = new CQLexer(antlrStream);

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CQParser(tokenStream);

            var rootVisitor = new ValidatingVisitor();
            var root = parser.root();
            var CQProgram = root.Accept(rootVisitor);

            return CQProgram;
        }
    }
}