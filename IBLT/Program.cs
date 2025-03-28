using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Unicode;
using BenchmarkDotNet.Running;
using IBLT;
using Newtonsoft.Json;

//var summary = BenchmarkRunner.Run<EnumerableBenchmark>();


var bigInteger = new BigInteger(Encoding.UTF8.GetBytes("some string"));
var str = Encoding.UTF8.GetString(bigInteger.ToByteArray());



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


var get = iblt.Get(128);
Console.WriteLine(iblt.GetApproximateNotFoundProbability());

//iblt.Delete(128, 8671293);
Console.WriteLine(iblt.GetApproximateNotFoundProbability());








var x = 5;




