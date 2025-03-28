using IBLT;
using MessagePack;
using Newtonsoft.Json;
using System.Text;

namespace Tests
{
    /// <summary>
    /// This class is not in the spirit of regular unit testing, this is testing different scenarios and checking if they are feasible. 
    /// </summary>
    [TestClass]
    public sealed class IBLT_Scenarios
    {
        [TestMethod]
        public void Four_In_100000()
        {
            var hashCount = 4;
            var cells = 8;

            var iblt = new InvertibleBloomLookupTableXOR(hashCount, cells);
            var iblt2 = new InvertibleBloomLookupTableXOR(hashCount, cells);

            Console.WriteLine(iblt.GetApproximateNotFoundProbability());

            var totalIntegers = 100000;

            for (int i = 0; i < totalIntegers; i++)
            {
                var value = Encoding.UTF8.GetBytes($"This is a string representing {i}th number");

                iblt.Insert(i, value);

                var p = iblt.GetApproximateNotFoundProbability();
                if (p != 1) Console.WriteLine(p);

                if (i >= 29 && i <= 32)
                {
                    continue;
                }

                iblt2.Insert(i, value);
            }

            var serialized = JsonConvert.SerializeObject(iblt);
            Console.WriteLine($"Serialized string length = {serialized.Length} for {totalIntegers * 40} chars in the original data");

            iblt.Substract(iblt2);

            Console.WriteLine(iblt.GetApproximateNotFoundProbability());

            var items = iblt.GetAllItems().Select(item => (item.Item1, Encoding.UTF8.GetString(item.Item2))).ToArray();

            Console.WriteLine(String.Join("\n", items));

            Assert.AreEqual(4, items.Length);

            var get = iblt.Get(128);
            Console.WriteLine(iblt.GetApproximateNotFoundProbability());

            //iblt.Delete(128, 8671293);
            Console.WriteLine(iblt.GetApproximateNotFoundProbability());

        }
    }
}
