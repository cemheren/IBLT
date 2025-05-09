﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IBLT
{

    // https://arxiv.org/pdf/1101.2245.pdf
    // https://medium.com/codechain/invertible-bloom-lookup-table-37600927cfbe

    [Serializable]
    public class InvertibleBloomLookupTableXOR
    {
        [JsonProperty]
        private int k;

        private Func<int, int>[] hashFunctions;

        [JsonProperty]
        private int M;
        
        [JsonProperty]
        private Cell[] cells;

        [JsonProperty]
        public int totalCount;

        public InvertibleBloomLookupTableXOR(int hashFunctions, int m)
        {
            this.k = hashFunctions;
            this.hashFunctions = Enumerable.Range(0, hashFunctions).Select<int, Func<int, int>>(h => (x) => GenerateHashFunction(x, h)).ToArray();
                        
            this.M = m;
            this.cells = Enumerable.Range(0, M).Select(c => new Cell()).ToArray();
        }

        public double GetApproximateNotFoundProbability()
        {
            var p0 = Math.Exp(-this.k * this.totalCount / (double)this.M);

            return Math.Pow(1 - p0, this.k);
        }

        public void Insert(int key, byte[] value)
        {
            totalCount++;

            foreach (var hash in this.hashFunctions)
            {
                var index = hash(key) % this.cells.Length;

                this.cells[index].Count++;
                this.cells[index].KeySum ^= key;
                this.cells[index].ValueSum = xOR(this.cells[index].ValueSum, value);
            }
        }

        public void Delete(int key, byte[] value)
        {
            totalCount--;

            foreach (var hash in this.hashFunctions)
            {
                var index = hash(key) % this.cells.Length;

                this.cells[index].Count--;
                this.cells[index].KeySum ^= key;
                this.cells[index].ValueSum = xOR(this.cells[index].ValueSum, value);
            }
        }

        public byte[]? Get(int key)
        {
            foreach (var hash in hashFunctions)
            {
                var index = hash(key) % this.cells.Length;

                if (this.cells[index].Count == 0)
                {
                    return null;
                }

                if (this.cells[index].Count == 1)
                {
                    if (this.cells[index].KeySum == key)
                    {
                        return this.cells[index].ValueSum;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            throw new Exception("NotFound");
        }

        public void Substract(InvertibleBloomLookupTableXOR other)
        {
            totalCount -= other.totalCount;

            for (int i = 0; i < this.cells.Length; i++)
            {
                this.cells[i].Count -= other.cells[i].Count;
                this.cells[i].ValueSum = xOR(this.cells[i].ValueSum, other.cells[i].ValueSum);
                this.cells[i].KeySum ^= other.cells[i].KeySum;
            }
        }

        public (int, byte[])[] GetAllItems()
        {
            var outputList = new List<(int, byte[])>();

            while (true)
            {
                var cell = this.cells.FirstOrDefault(cell => cell.Count == 1);
                if (cell == null)
                {
                    break;
                }

                outputList.Add((cell.KeySum, cell.ValueSum));
                this.Delete(cell.KeySum, cell.ValueSum);
            }

            if (this.cells.All(cell => cell.Count == 0))
            {
                return outputList.ToArray();
            }

            throw new Exception("The list is partial");
        }

        static int GenerateHashFunction(int x, int hashIndex)
        {
            x *= hashIndex; // just do this for now to generate many hash functions

            x ^= x >> 17;
            x *= 830770091;   // 0xed5ad4bb
            x ^= x >> 11;
            x *= -1404298415; // 0xac4c1b51
            x ^= x >> 15;
            x *= 830770091;   // 0x31848bab
            x ^= x >> 14;

            return x;
        }

        //public static byte[] operator ^(byte[] f, byte[] s) => xOR(f, s);

        private static byte[] xOR(byte[] first, byte[] second)
        {
            if (first == null)
            {
                first = new byte[0];
            }

            if (second == null)
            {
                second = new byte[0];
            }

            var longer = first.Length > second.Length ? first : second;
            var shorter = first.Length > second.Length ? second : first;
            var result = new byte[longer.Length];

            for (int i = 0; i < longer.Length; i++)
            {
                result[i] = longer[i];
            }

            for (int i = 0; i < shorter.Length; i++)
            {
                result[i] ^= shorter[i];
            }

            return result;
        }

        [Serializable]
        [DebuggerDisplay("Count = {Count}, KeySum = {KeySum}, ValueSum = {ValueSum}")]
        class Cell
        {
            [JsonProperty]
            public int Count { get; set; }

            [JsonProperty]
            public int KeySum { get; set; }

            [JsonProperty]
            public byte[]? ValueSum { get; set; }
        }
    }
}
