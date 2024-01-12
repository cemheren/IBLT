using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace IBLT
{
    public class EnumerableBenchmark
    {
        private int N = 100000;
        private int[] array;

        public EnumerableBenchmark()
        {
            array = new int[N];
            for (int i = 0; i < N; i++)
            {
                array[i] = i;
            }
        }

        [Benchmark]
        public int NoOP() 
        {
            return array[10];
        }


        [Benchmark]
        public int Enumerable()
        {
            IEnumerable<int> enumerable = array;
            var result = enumerable.ToArray();
            return result[10];
        }

    }
}
