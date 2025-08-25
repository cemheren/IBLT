using Antlr4.Runtime;
using Azure.Identity;
using ChecksumCosmosClient;
using LiveGraph;
using LiveGraph.ContinuousQuery;
using Microsoft.Azure.Cosmos;
using Tests.LanguageTests;

namespace antlrOpaTest
{
    [TestClass]
    public class QueryPlanTest
    {
        private CosmosClient? client;
        private Database? database;
        private Container? entryContainer;
        private Container? edgeContainer;
        private QueryPlanAwareDataAccessor dataAccessor;
        private IIncrementalCosmosClient<Edge> edgeClient;
        private IIncrementalCosmosClient<Entry> EntryClient;

        [TestInitialize]
        public async Task TestInitialize()
        {
            client = CosmosTestHelper.CreateCosmosClient();
            database = await CosmosTestHelper.CreateDatabaseAsync(client);
            entryContainer = await CosmosTestHelper.CreateContainerAsync(database, CosmosTestHelper.EntryContainerName);
            edgeContainer = await CosmosTestHelper.CreateContainerAsync(database, CosmosTestHelper.EdgeContainerName);
            dataAccessor = new QueryPlanAwareDataAccessor(new EntyDataProvider(this.EntryClient, this.edgeClient));


            this.edgeClient = client
                .WithCosmosClientExtensions<Edge>()
                .WithIBLTExtension();

            this.EntryClient = client
                .WithCosmosClientExtensions<Entry>()
                .WithIBLTExtension();
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            if (client != null)
            {
                await CosmosTestHelper.CleanupAsync(client);
                client.Dispose();
            }
        }

        [TestMethod]
        public async Task ParseCQ()
        {
            var rule = @"
START FROM resource
FILTER resource.type == ""virtualMachine""
WALKTONEIGHBORS :DependencyOf-> neigh                               // fanout to all the affinity nodes 1 
FILTER neigh.type == ""SQLServer""                                    // fan back, or better filter inline on the affinitized node via query plan 
WALKTONEIGHBORS :ServiceGroupMember-> sg                            // fanout to all affinity nodes again  2
EXTEND keys = ANCESTORS(sg.id)                                      // Ancestor lookup for an array of IDS 3
USEINDEXFORARRAY HEALTH_RESOURCES                                   // Array lookup for a bunch of ids  4
FILTER index != null                                                // inline filter based on results
RETURN resource.id, index.healthId                                  // Send notification based on return value, total of at least 4 lookups or out of box calls 
            ";

            var cq = await this.GetParsedQueryPlan(rule);

            Assert.IsNotNull(cq);
        }

        private async Task<FeedForwardQueryEvaluationContext?> GetParsedQueryPlan(string queryText)
        {
            var antlrStream = new AntlrInputStream(queryText);
            var lexer = new CQLexer(antlrStream);

            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CQParser(tokenStream);

            var dummyContext = new FeedForwardQueryEvaluationContext { TenantId = "testId", CurrentEntries = [new Entry(Guid.NewGuid(), tenant: "testId", slot: 4, null, null)] };

            var rootVisitor = new QueryPlanVisitor(dataAccessor, dummyContext);
            var root = parser.root();
            var queryPlan = await root.Accept(rootVisitor);

            return queryPlan;
        }
    }
}