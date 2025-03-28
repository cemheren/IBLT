using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IBLT
{

    // https://arxiv.org/pdf/1101.2245.pdf
    // https://medium.com/codechain/invertible-bloom-lookup-table-37600927cfbe


    public class FaultTolerantIBLT
    {
        private const int salt = 127;

        protected int k;
        private Func<int, int>[] hashFunctions;

        private int M = 128;
        private Cell[] cells;

        public int totalCount;

        public FaultTolerantIBLT Clone()
        {
            var iblt = new FaultTolerantIBLT(this.k, this.M);

            for (int i = 0; i < cells.Length; i++)
            {
                iblt.cells[i] = cells[i].Clone();
            }

            iblt.totalCount = this.totalCount;

            return iblt;
        }

        /// <param name="duplicateTolerance">MinValue = 1</param>
        public FaultTolerantIBLT(int hashFunctions, int M = 128)
        {
            this.k = hashFunctions;
            this.hashFunctions = Enumerable.Range(1, hashFunctions + 1).Select<int, Func<int, int>>(h => (x) => GenerateHashFunction(x, h)).ToArray();

            // cell count is m/k. 
            var cellcount = M / k;
            this.cells = Enumerable.Range(0, cellcount).Select(c => new Cell()).ToArray();
        }

        public double GetApproximateNotFoundProbability()
        {
            var p0 = Math.Exp(-this.k * this.totalCount / (double)this.M);

            return Math.Pow(1 - p0, this.k);
        }

        public void Insert(int key, BigInteger value)
        {
            totalCount++;

            var g1 = KeyHash(key);

            foreach (var hash in this.hashFunctions)
            {
                var index = hash(key) % this.cells.Length;

                this.cells[index].Count++;
                this.cells[index].KeySum += key;
                this.cells[index].ValueSum += value;
                this.cells[index].HashKeySum += g1;
            }
        }

        public void Delete(int key, BigInteger value)
        {
            totalCount--;
            
            var g1 = KeyHash(key);

            foreach (var hash in this.hashFunctions)
            {
                var index = hash(key) % this.cells.Length;

                this.cells[index].Count--;
                this.cells[index].KeySum -= key;
                this.cells[index].ValueSum -= value;
                this.cells[index].HashKeySum -= g1;
            }
        }

        /// <summary>
        /// Returns null if IBLT is empty, 
        /// throws if something can't be found (it could be because we are over capacity or because it wasn't added in the first place)
        /// </summary>
        public BigInteger? Get(int key, int duplicateTolerance = 5)
        {
            var g1 = KeyHash(key);

            foreach (var hash in hashFunctions)
            {
                var index = hash(key) % this.cells.Length;
                var cell = this.cells[index];

                if (cell.Count == 0 && cell.KeySum == 0 && cell.HashKeySum == 0)
                {
                    return null; 
                }

                for (int j = 1; j <= duplicateTolerance; j++)
                {
                    if (cell.Count == j && (cell.KeySum / j) == key && (cell.HashKeySum / j) == g1)
                    {
                        return cell.ValueSum;
                    }

                    if (cell.Count == -j && (cell.KeySum / j) == -key && (cell.HashKeySum / j) == -g1)
                    {
                        return -cell.ValueSum;
                    }
                }
            }

            throw new Exception("NotFound");
        }

        public void Add(FaultTolerantIBLT other)
        {
            for (int i = 0; i < this.cells.Length; i++)
            {
                this.cells[i].Count += other.cells[i].Count;
                this.cells[i].ValueSum += other.cells[i].ValueSum;
                this.cells[i].KeySum += other.cells[i].KeySum;

                this.cells[i].HashKeySum += other.cells[i].HashKeySum;
            }
        }

        public void Substract(FaultTolerantIBLT other)
        {
            for (int i = 0; i < this.cells.Length; i++)
            {
                this.cells[i].Count -= other.cells[i].Count;
                this.cells[i].ValueSum -= other.cells[i].ValueSum;
                this.cells[i].KeySum -= other.cells[i].KeySum;

                this.cells[i].HashKeySum -= other.cells[i].HashKeySum;
            }
        }

        /// <summary>
        /// This destroys the IBLT, copy it if you want to save it. 
        /// </summary>
        public bool TryGetAllItems(out (int, BigInteger)[] output, out (int, BigInteger)[] extraneousDeletes, int duplicateTolerance = 1)
        {
            var outputList = new List<(int, BigInteger)>();
            var deleteList = new List<(int, BigInteger)>();

            var addRemoveLoopDetector = new HashSet<int>();

            bool Handle_N_OccuranceOfAKey(int n)
            {
                var targetCells = this.cells.Where(cell => cell.Count == n || cell.Count == -n).ToArray();
                if (targetCells.Length == 0)
                {
                    // finished unrolling the datastructure, it's either done, or inconclusive. 
                    return true;
                }

                var changedStructure = false;
                foreach (var cell in targetCells)
                {
                    if (cell.Count == n && (cell.HashKeySum / n) == KeyHash(cell.KeySum / n))
                    {
                        outputList.Add((cell.KeySum / n, cell.ValueSum / n));

                        addRemoveLoopDetector.Add(cell.KeySum / n);

                        var key = cell.KeySum / n;
                        var value = cell.ValueSum / n;
                        // If an item occurs n times in our data structure, we need to remove it that many times. 
                        for (int i = 0; i < n; i++)
                        {
                            this.Delete(key, value);
                            changedStructure = true;
                        }
                    }
                    else if (cell.Count == -n && -(cell.HashKeySum / n) == KeyHash(-(cell.KeySum / n))) // extaneous delete
                    {
                        deleteList.Add((-cell.KeySum / n, -cell.ValueSum / n));

                        if (addRemoveLoopDetector.Contains(-cell.KeySum / n))
                        {
                            throw new Exception("Add/Remove loop detected. Try unrolling the IBLT with lower duplicateTolerance");
                        }

                        var key = -cell.KeySum / n;
                        var value = -cell.ValueSum / n;
                        // If an item was extaneously deleted n times in our data structure, we need to add it that many times. 
                        for (int i = 0; i < n; i++)
                        {
                            this.Insert(key, value);
                            changedStructure = true;
                        }
                    }
                }

                return !changedStructure;
            }

            // Start from large and move to lower integers, I don't think other way around will work. 
            for (int i = duplicateTolerance; i > 0; i--)
            {
                var done = false;
                while (!done)
                {
                    done = Handle_N_OccuranceOfAKey(i);
                }
            }
            
            output = outputList.ToArray();
            extraneousDeletes = deleteList.ToArray();

            if (this.cells.All(cell => cell.Count == 0))
            {
                return true;
            }

            return false; 
        }

        static int GenerateHashFunction(int x, int hashIndex)
        {
            x += salt;

            x *= hashIndex; // just do this for now to generate many hash functions

            x ^= x >> 17;
            x *= 830770091;   // 0xed5ad4bb
            x ^= x >> 3;
            x *= -1404298415; // 0xac4c1b51
            x ^= x >> 7;
            x *= 830770091;   // 0x31848bab
            x ^= x >> 13;

            return x;
        }

        static int KeyHash(int x)
        { 
            // This is the G1(x) from the paper, needs to be uniform, for now trying this out with -1 index.
            // Have not tested whether this hash function is sufficiently random. 
            return GenerateHashFunction(x, -1);
        }

        [DebuggerDisplay("Count = {Count}, KeySum = {KeySum}, ValueSum = {ValueSum}")]
        class Cell
        {
            public int Count { get; set; }

            public int KeySum { get; set; }

            public BigInteger ValueSum { get; set; }


            // Duplicate Deletes 
            public long HashKeySum { get; set; }

            //Duplicate entries (key value same)
            public long HashValueSum { get; set; }

            public Cell Clone()
            {
                return new Cell
                {
                    Count = this.Count,
                    KeySum = this.KeySum,
                    ValueSum = this.ValueSum,
                    HashKeySum = this.HashKeySum,
                    HashValueSum = this.HashValueSum
                };
            }
        }
    }
}
