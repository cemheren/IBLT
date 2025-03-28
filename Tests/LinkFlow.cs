using IBLT.Sim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public sealed class LinkFlow
    {
        [TestMethod]
        public void TestAddingLinks()
        {
            var lp = new LinkProcessor();
            var subscriptions = Enumerable.Range(0, 5).Select(g => Guid.NewGuid().ToString()).ToArray();

            var SGs = Enumerable.Range(0, 10).Select(g => Guid.NewGuid().ToString()).ToArray();

            for (int i = 0; i < 100_000; i++)
            {
                var sub = subscriptions[i % subscriptions.Length];
                var sg = SGs[i % SGs.Length];

                lp.AddLink("NA", sub, sg, i.ToString("x"));
            }

            var b = 5;
        }

        [TestMethod]
        public void TestAddingLinks_With_ED()
        {
            var lp = new LinkProcessor();
            var subscriptions = Enumerable.Range(0, 5).Select(g => Guid.NewGuid().ToString()).ToArray();

            var SGs = Enumerable.Range(0, 10).Select(g => Guid.NewGuid().ToString()).ToArray();

            for (int i = 0; i < 100_000; i++)
            {
                var sub = subscriptions[i % subscriptions.Length];
                var sg = SGs[i % SGs.Length];

                lp.AddLinkWithErrorDetection("NA", sub, sg, i.ToString("x"));
            }

            var b = 5;
        }
    }
}
