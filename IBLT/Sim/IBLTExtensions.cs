using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IBLT.Sim
{
    public static class IBLTExtensions
    {
        public static void InsertString(this FaultTolerantIBLT iblt, string key, string value)
        {
            var k = new BigInteger(Encoding.UTF8.GetBytes(key));
            var v = new BigInteger(Encoding.UTF8.GetBytes(value));

            iblt.Insert(k, v);
        }


        public static void DeleteString(this FaultTolerantIBLT iblt, string key, string value)
        {
            var k = new BigInteger(Encoding.UTF8.GetBytes(key));
            var v = new BigInteger(Encoding.UTF8.GetBytes(value));

            iblt.Delete(k, v);
        }

        public static List<(string, string)>? ListStrings(this FaultTolerantIBLT iblt)
        {
            //var scopeClone = iblt.Clone();

            var gotAllItems = iblt.TryGetAllItems(out var values, out var deletes, 1);

            var result = values.Select(item => (Encoding.UTF8.GetString(item.Item1.ToByteArray()), Encoding.UTF8.GetString(item.Item2.ToByteArray()))).ToList();

            return result;
        }
    }
}
