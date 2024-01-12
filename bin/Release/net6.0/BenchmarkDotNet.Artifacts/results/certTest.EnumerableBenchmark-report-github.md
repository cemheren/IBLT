``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.201
  [Host]     : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT
  DefaultJob : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT


```
|     Method |            Mean |         Error |        StdDev |          Median |
|----------- |----------------:|--------------:|--------------:|----------------:|
|       NoOP |       0.0945 ns |     0.0287 ns |     0.0837 ns |       0.0741 ns |
| Enumerable | 148,072.0020 ns | 2,045.7792 ns | 1,913.6232 ns | 147,412.5488 ns |
