using IBLT;
using MessagePack;
using Newtonsoft.Json;
using System.Numerics;
using System.Text;

namespace Tests
{
    /// <summary>
    /// This class is not in the spirit of regular unit testing, this is testing different scenarios and checking if they are feasible. 
    /// </summary>
    [TestClass]
    public sealed class FaultTolerance
    {
        [TestMethod]
        public void AddThrice()
        {
            var hashCount = 4;
            var cells = 128;

            var iblt = new FaultTolerantIBLT(hashCount, cells);

            var totalIntegers = 3;

            for (int i = 0; i < totalIntegers; i++)
            {
                var value = Encoding.UTF8.GetBytes($"This is a string representing first number");
                iblt.Insert(1, new BigInteger(value));
            }

            // this will work as long has hashSums are not overflowing, overflow wasn't a huge problem before.
            // However, in case of dupes those M cannot be recovered if overflown. 
            var v = iblt.Get(1);
            Assert.IsNotNull(v);

            var success = iblt.TryGetAllItems(out var items, out var _, duplicateTolerance: 1);
            Assert.IsFalse(success);

            success = iblt.TryGetAllItems(out items, out var _, duplicateTolerance: 3);
            Assert.IsTrue(success);

            var result = items.Select(item => (item.Item1, Encoding.UTF8.GetString(item.Item2.ToByteArray()))).ToArray();

            Console.WriteLine(String.Join("\n", result));

            Assert.AreEqual(1, result.Length);
        }

        [TestMethod]
        public void AddAndDelete()
        {
            var hashCount = 4;
            var cells = 128;

            var iblt = new FaultTolerantIBLT(hashCount, cells);

            var totalIntegers = 10;

            for (int i = 0; i < totalIntegers; i++)
            {
                var value = Encoding.UTF8.GetBytes($"This is a string representing {i}th number");
                iblt.Insert(i, new BigInteger(value));
            }

            // Delete a non existent number from the 1st iblt, this would normally corrupt the data structure. 
            iblt.Delete(13, new BigInteger(Encoding.UTF8.GetBytes($"This is a string representing {13}th number")));

            // Clone first since that destroys the M. 
            Assert.ThrowsException<Exception>(() => iblt.Clone().TryGetAllItems(out var items, out var extraneousDeletes, duplicateTolerance: 2));

            var success = iblt.TryGetAllItems(out var items, out var extraneousDeletes, duplicateTolerance: 1);
            Assert.IsTrue(success);

            var result = items.Select(item => (item.Item1, Encoding.UTF8.GetString(item.Item2.ToByteArray()))).ToArray();

            Console.WriteLine(String.Join("\n", result));

            Assert.AreEqual(10, result.Length);
            Assert.AreEqual(1, extraneousDeletes.Length);
        }

        [TestMethod]
        public void ExtraDelete()
        {
            var hashCount = 4;
            var M = 64;

            var iblt = new FaultTolerantIBLT(hashCount, M);
            var iblt2 = new FaultTolerantIBLT(hashCount, M);

            Console.WriteLine(iblt.GetApproximateNotFoundProbability());

            var totalIntegers = 100_000;

            for (int i = 0; i < totalIntegers; i++)
            {
                var value = Encoding.UTF8.GetBytes($"This is a string representing {i}th number");

                iblt.Insert(i, new  BigInteger(value));

                var p = iblt.GetApproximateNotFoundProbability();
                if (p < 0.5) Console.WriteLine(p);

                if (i >= 29 && i <= 32)
                {
                    continue;
                }

                iblt2.Insert(i, new BigInteger(value));
            }

            // Delete a non existent number from the 1st iblt, this would normally corrupt the data structure. 
            iblt.Delete(200_000, new BigInteger(Encoding.UTF8.GetBytes($"This is a string representing {200_000}th number")));

            var msgPackSerialized = MessagePackSerializer.Serialize(iblt);
            var ibltback = MessagePackSerializer.Deserialize<FaultTolerantIBLT>(msgPackSerialized);

            var serialized = JsonConvert.SerializeObject(iblt);

            Console.WriteLine($"Serialized string length = {serialized.Length} for {totalIntegers * 40} chars in the original data"); // 2990 
            Console.WriteLine($"Serialized message pack = {msgPackSerialized.Length} for {totalIntegers * 40} chars in the original data"); // 1227 bytes

            iblt.Substract(iblt2);

            Console.WriteLine(iblt.GetApproximateNotFoundProbability());

            var success = iblt.TryGetAllItems(out var items, out var _);
            var result = items.Select(item => (item.Item1, Encoding.UTF8.GetString(item.Item2.ToByteArray()))).ToArray();

            Console.WriteLine(String.Join("\n", result));

            Assert.IsTrue(success, "iblt failed to unroll.");

            // 200_000 item is in the extra deleted bucket. 
            Assert.AreEqual(4, result.Length);

            var get = iblt.Get(128);
            Console.WriteLine(iblt.GetApproximateNotFoundProbability());

            //iblt.Delete(128, 8671293);
            Console.WriteLine(iblt.GetApproximateNotFoundProbability());
        }
    }
}
