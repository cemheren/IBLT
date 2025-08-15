using Antlr4.Runtime;
using LiveGraph;
using LiveGraph.ContinuousQuery;

namespace antlrOpaTest
{
    [TestClass]
    public class RegoTranspileTests
    {
        [TestMethod]
        public void ParseRego()
        {
            var rule = @"

                allow {
                    input.location == ""eastus""
                }
            ";

            var cq = this.GetParsedContinuousQuery(rule);

            Assert.IsNotNull(cq);
        }

        private RegoProgram? GetParsedContinuousQuery(string rule, string head = "allow")
        {
            var antlrStream = new AntlrInputStream(rule);
            var lexer = new RegoLexer(antlrStream);

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new RegoParser(tokenStream);

            var rootVisitor = new RootVisitor();
            var root = parser.root();
            var regoProgram = root.Accept(rootVisitor);

            return regoProgram;
        }
    }
}