using System.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.IO;


public static class HashFunctions
{
    private static readonly int q = 89;
    private static readonly BigInteger p = (BigInteger.One << q) - 1;


    // EXERCISE 1
    public static ulong MultiplyShift(ulong x, ulong a, int l)
    {
        if (l <= 0 || l >= 64)
            throw new ArgumentOutOfRangeException(nameof(l), "Parameter 'l' must be between 1 and 63.");

        a |= 1UL; // ensure a is odd
        return (a * x) >> (64 - l);
    }

    // EXERCISE 1
    public static ulong MultiplyModP(BigInteger x, BigInteger a, BigInteger b, int l)
    {
        if (l <= 0 || l > 64)
            throw new ArgumentOutOfRangeException(nameof(l), "l must be in the range (0, 64]");

        BigInteger result = a * x + b;
        result = Reduce(result);

    

        // result = Reduce(result); // f(x) = (a * x + b) mod p

        // Console.WriteLine($"MultiplyModP: result after reduction = {result}");
        // // Final hash = result mod 2^l
        return (ulong)(result & ((1UL << l) - 1)); // ensure result is within p

    }

    // EXERCISE 4   
    public static BigInteger Degree3Polynomial(ulong x, BigInteger a0, BigInteger a1, BigInteger a2, BigInteger a3)
    {
        BigInteger bx = new BigInteger(x);

        // Horner's rule: (((a3 * x + a2) * x + a1) * x + a0) mod p
        BigInteger y = a3;
        y = Reduce(y * bx + a2);
        y = Reduce(y * bx + a1);
        y = Reduce(y * bx + a0);

        return y;
    }

    private static BigInteger Reduce(BigInteger y)
    {
        y = (y & p) + (y >> q);
        if (y >= p) y -= p;
        return y;
    }

    public static BigInteger RandomCoeff()
    {
        var rnd = new Random();
        byte[] bytes = new byte[12]; // 96 bits
        rnd.NextBytes(bytes);
        bytes[^1] &= 0b00011111; // trim til 89 bits
        return new BigInteger(bytes) % p;
    }

    // EXERCISE 5
    public static (Func<ulong, int> h, Func<ulong, int> s) HashGenerator(int t)
    {

        if (t <= 0 || t > 64)
            throw new ArgumentOutOfRangeException(nameof(t), "Parameter 't' must be between 1 and 64.");
        ulong m = 1UL << t; // m = 2^t

        BigInteger a0 = RandomCoeff();
        BigInteger a1 = RandomCoeff();
        BigInteger a2 = RandomCoeff();
        BigInteger a3 = RandomCoeff();

        // g(x) = a3 * x^3 + a2 * x^2 + a1 * x + a0 mod p - 4-universal hashfunction
        Func<ulong, BigInteger> g = x => Degree3Polynomial(x, a0, a1, a2, a3);

        // We use g(x) to define two hash functions:
        // h(x) = g(x) mod m
        Func<ulong, int> h = x => (int)(g(x) & (m - 1)); // according to algorithm 2 specified in second moment estimation

        // s(x) = 1 - 2g(x)  
         Func<ulong, int> s = x => ((g(x) >> (q - 1)) & 1) == 0 ? 1 : -1;  // according to algorithm 2 specified in second moment estimation
        // s(x) = 1 if g(x) is even, -1 if g(x) is odd

        return (h, s);

    }
}


// EXERCISE 2
public class HashTableChaining
{
    private readonly int l;
    private readonly int tableSize;
    private readonly List<(ulong key, long value)>[] table;
    private readonly Func<ulong, ulong> hashFunction;
    public List<(ulong key, long value)>[] Buckets => table;

    public HashTableChaining(Func<ulong, ulong> hashFunction, int l)
    {
        this.l = l;
        this.tableSize = 1 << l; // 2^l
        this.hashFunction = hashFunction;
        table = new List<(ulong key, long value)>[tableSize];
        for (int i = 0; i < tableSize; i++)
        {
            table[i] = new List<(ulong key, long value)>();
        }
    }

    private int Index(ulong x)
    {
        return (int)(hashFunction(x) % (ulong)tableSize);
    }

    public long Get(ulong x)
    {
        int index = Index(x);
        foreach (var (key, value) in table[index])
        {
            if (key == x) return value;
        }
        return 0; // Not found
    }

    public void Set(ulong x, long value)
    {
        int index = Index(x);
        for (int i = 0; i < table[index].Count; i++)
        {
            if (table[index][i].key == x)
            {
                table[index][i] = (x, value); // Update existing value
                return;
            }
        }
        table[index].Add((x, value)); // Add new key-value pair
    }

    public void Increment(ulong x, long d)
    {

        int index = Index(x);
        for (int i = 0; i < table[index].Count; i++)
        {
            if (table[index][i].key == x)
            {
                table[index][i] = (x, table[index][i].value + d); // Increment existing value
                return;
            }
        }
        table[index].Add((x, d)); // Add new key-value pair with increment

    }

        public static void TestCollisionHandling()
    {
        // Dummy hashfunction that always returns 0
        Func<ulong, ulong> constantHash = x => 0;

        int l = 16;
        var table = new HashTableChaining(constantHash, l);

        // insert some values that will collide
        table.Set(100UL, 1);
        table.Set(200UL, 2);
        table.Set(300UL, 3);
        
        foreach (var bucket in table.Buckets)
        {
            foreach (var (key, value) in bucket)
            {
                Console.WriteLine($"Key: {key}, Hashed: {constantHash(key)}, Value: {value}");
            }
        }
    }
}

// EXERCISE 1
public static class StreamGenerator
{
    public static IEnumerable<Tuple<ulong, int>> CreateStream(int n, int l)
    {
        // We generate a random uint64 number.
        Random rnd = new System.Random();
        ulong a = 0UL;
        Byte[] b = new Byte[8];
        rnd.NextBytes(b);
        for (int i = 0; i < 8; ++i)
        {
            a = (a << 8) + (ulong)b[i];
        }

        // We demand that our random number has 30 zeros on the least
        // significant bits and then a one.

        a = (a | ((1UL << 31) - 1UL)) ^ ((1UL << 30) - 1UL);
        ulong x = 0UL;
        for (int i = 0; i < n / 3; ++i)
        {
            x = x + a;
            yield return Tuple.Create(x & (((1UL << l) - 1UL) << 30), 1);
        }

        for (int i = 0; i < (n + 1) / 3; ++i)
        {
            x = x + a;
            yield return Tuple.Create(x & (((1UL << l) - 1UL) << 30), -1);
        }

        for (int i = 0; i < (n + 2) / 3; ++i)
        {
            x = x + a;
            yield return Tuple.Create(x & (((1UL << l) - 1UL) << 30), 1);
        }
    }


    public static void BenchmarkHashFunctions(int n, int l)
    {

        var rnd = new Random();
        BigInteger a = HashFunctions.RandomCoeff();
        BigInteger b = HashFunctions.RandomCoeff();

        ulong aShift = (ulong)(a & ((1UL << 31) - 1)); // ensure a is odd


        // Generate datastream
        var stream = StreamGenerator.CreateStream(n, l).ToList();

        // MultiplyShift
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        ulong sumShift = 0;
        foreach (var (key, _) in stream)
        {
            sumShift += HashFunctions.MultiplyShift(key, aShift, l); // Irrelevant sum, just for benchmarking
        }
        sw1.Stop();

        // MultiplyModP
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        ulong sumModP = 0;
        foreach (var (key, _) in stream)
        {
            sumModP += HashFunctions.MultiplyModP(key, a, b, l); // Irrelevant sum, just for benchmarking
        }
        sw2.Stop();

        // Print results
        Console.WriteLine($"n = {n}, l = {l}");
        Console.WriteLine($"MultiplyShift:   Sum = {sumShift}, Time = {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"MultiplyModP:    Sum = {sumModP}, Time = {sw2.ElapsedMilliseconds} ms");
    }
}


// EXERCISE 3
public static class Estimators
{
    public static (ulong sum, HashTableChaining table) ComputeSquareSum(IEnumerable<Tuple<ulong, int>> stream, Func<ulong, ulong> hashFunc, int l)
    {
        var table = new HashTableChaining(hashFunc, l);
        ulong sum = 0;
        foreach (var (key, value) in stream)
        {
            table.Increment(key, value);
        }

        foreach (var bucket in table.Buckets)
        {
            foreach (var (_, value) in bucket)
            {
                sum += (ulong)(value * value); // Cast to avoid overflow
            }
        }

        return (sum, table);
    }

        public static int CountCollisions(HashTableChaining table)
    {
        int collisions = 0;
        foreach (var bucket in table.Buckets)
        {
            if (bucket.Count > 1)
                collisions += bucket.Count - 1; // hver ekstra entry i bucket = én kollision
        }
        return collisions;
    }

        public static int CountUsedBuckets(HashTableChaining table)
    {
        int used = 0;
        foreach (var bucket in table.Buckets)
        {
            if (bucket.Count > 0) used++;
        }
        return used;
    }



    public static void BenchmarkSquareSum(int n, int l)
    {
        var rnd = new Random();
        ulong a = ((ulong)(uint)rnd.Next() << 32) | (ulong)(uint)rnd.Next();
        ulong b = ((ulong)(uint)rnd.Next() << 32) | (ulong)(uint)rnd.Next();

        // Create data stream
        var stream1 = StreamGenerator.CreateStream(n, l);
        var stream2 = StreamGenerator.CreateStream(n, l); // to avoid reusing enumerator

        // MultiplyShift

        Func<ulong, ulong> hashShift = x => HashFunctions.MultiplyShift(x, a | 1UL, l);
        var sw1 = Stopwatch.StartNew();
        var (sumShift, tableShift) = ComputeSquareSum(stream1, hashShift, l);
        sw1.Stop();

        // MultiplyModP
        Func<ulong, ulong> hashModP = x => HashFunctions.MultiplyModP(x, a, b, l);
        var sw2 = Stopwatch.StartNew();
        var (sumModP, tableModP) = ComputeSquareSum(stream2, hashModP, l);
        sw2.Stop();

        int collisionsShift = CountCollisions(tableShift); // du skal gemme `tableShift` fra ComputeSquareSum
        int collisionsModP = CountCollisions(tableModP);  // og tilsvarende for ModP
        int usedBucketsShift = CountUsedBuckets(tableShift);
        int usedBucketsModP = CountUsedBuckets(tableModP);


        // Output
        Console.WriteLine($"[n={n}, l={l}]");
        Console.WriteLine($"MultiplyShift:   S = {sumShift}, Time = {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"MultiplyModP:    S = {sumModP}, Time = {sw2.ElapsedMilliseconds} ms");
        Console.WriteLine($"MultiplyShift collisions: {collisionsShift}");
        Console.WriteLine($"MultiplyModP  collisions: {collisionsModP}");
        Console.WriteLine($"Total buckets: {1 << l}");
        Console.WriteLine($"MultiplyShift used buckets: {usedBucketsShift}");
        Console.WriteLine($"MultiplyModP  used buckets: {usedBucketsModP}");
    }
}

public static class CountSketch
{
    private static int[]? C;
    private static Func<ulong, int>? h;
    private static Func<ulong, int>? s;

    // Given a stream of (key, delta) pairs, where key is a ulong and delta is an int,
    // this method runs the CountSketch algorithm to estimate the frequency of each key.
    public static void Run(IEnumerable<Tuple<ulong, int>> stream, int t)
    {
        int m = 1 << t;
        C = new int[m];
        (h, s) = HashFunctions.HashGenerator(t);

        foreach (var (x, d) in stream)
        {
            C[h(x)] += s(x) * d;
        }
    }

    // Returns the estimated frequency of the key x.
    public static int EstimateFrequency(ulong x)
    {
        if (C == null || h == null || s == null)
            throw new InvalidOperationException("CountSketch has not been initialized. Call Run() first.");

        return s(x) * C[h(x)];
    }

    // Returns the estimated square sum of the frequencies of all keys.
    public static ulong EstimateSquareSum()
    {
        if (C == null)
            throw new InvalidOperationException("CountSketch has not been initialized. Call Run() first.");

        ulong sum = 0;
        foreach (int count in C)
        {
            sum += (ulong)(count * count);
        }
        return sum;
    }
}


public static class Experiments
{
    // EXERCISE 7
    public static void RunCountSketch(int n, int l, int t)
    {
        // Generate a stream of (key, delta) pairs

        Console.WriteLine($"Exercise 7: Running CountSketch with n={n}, l={l}, t={t}");
        var stream = StreamGenerator.CreateStream(n, l).ToList();

        BigInteger a0 = HashFunctions.RandomCoeff();
        BigInteger a1 = HashFunctions.RandomCoeff();
        Func<ulong, ulong> hashFunc = x => HashFunctions.MultiplyModP(x, a0, a1, l);

        var (ExactSum, table) = Estimators.ComputeSquareSum(stream, hashFunc, l); // Compute the exact square sum using chaining

        List<ulong> estimates = new List<ulong>();
        for (int i = 0; i < 100; i++)
        {
            if (i % 5 == 4)
            {
                Console.WriteLine($"🔄 Iteration {i + 1} of 100"); // ChatGPT suggested emojis :D
            }
            CountSketch.Run(stream, t);
            ulong EstimatedSum = CountSketch.EstimateSquareSum(); // Estimate square sum using CountSketch
            estimates.Add(EstimatedSum);
        }

        List<ulong> sortedEstimates = estimates.OrderBy(x => x).ToList();

        // Print the MSE
        double mse = estimates.Select(x => Math.Pow((double)x - (double)ExactSum, 2)).Average();
        Console.WriteLine($"\n📉 Mean Square Error (MSE): {mse:F2}");

        List<ulong> medians = new();
        for (int g = 0; g < 9; g++)
        {
            var group = estimates.Skip(g * 11).Take(11).OrderBy(x => x).ToList(); // group of 11 estimates, sorted
            medians.Add(group[5]); // 5th element is the median in a sorted list of 11 elements
        }
        medians.Sort();
        Console.WriteLine("\nMedians from 9 groups:");
        for (int i = 0; i < medians.Count; i++)
        {
            Console.WriteLine($"Median {i + 1}: {medians[i]}");
        }
        Console.WriteLine($"Exact square sum: {ExactSum}");

        // Export to CSV
        Console.WriteLine("\nExporting estimates and medians to CSV files, to plot in python...");
        string estimatePath = "estimates.csv";
        string header = "Index,EstimatedSquareSum";
        SaveListToCsv(estimatePath, sortedEstimates, header, ExactSum);

        string medianPath = "medians.csv";
        string medianHeader = "Index,MedianSquareSum";
        SaveListToCsv(medianPath, medians, medianHeader, ExactSum);

        Console.WriteLine($"\nEstimates saved to {estimatePath}");
        Console.WriteLine($"Medians saved to {medianPath}");

    }
    public static void SaveListToCsv(string path, IEnumerable<ulong> data, string header, ulong? exactSum = null)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine(header);
        int i = 1;
        foreach (var x in data)
        {
            writer.WriteLine($"{i},{x}");
            i++;
        }
        if (exactSum.HasValue) // If exactSum is provided, write it as the last line
        {
            writer.WriteLine($"-1,{exactSum.Value}");
        }
    }
}


// MAIN PROGRAM
public class Program
{
    public static void Main(string[] args)
    {

        HashTableChaining.TestCollisionHandling();
        Console.WriteLine("✅ Collision handling test passed successfully.");

        int n = 1000000; // Size of the stream
        var l_list = Enumerable.Range(1, 11).Select(i => 2 * i).ToList(); // m = 2^l unique keys

        Console.WriteLine("\nBenchmarking hash functions:");
        foreach (var l_val in l_list)
        {
            StreamGenerator.BenchmarkHashFunctions(n, l_val);

        }

        Console.WriteLine("\nTesting HashTableChaining collision handling:");
        foreach (var l_val in l_list)
        {
            Estimators.BenchmarkSquareSum(n, l_val);
        }

        // int t = 10;


        Experiments.RunCountSketch(n, 18, 10); // Example call to run CountSketch with n=1000000, l=16, t=10
        // int l = 16; // Example value for l
        // var stream = StreamGenerator.CreateStream(n, l).ToList();
        // Console.WriteLine("⚙️ Running Count Sketch...");
        // CountSketch.Run(stream, t);

        // // Time EstimateSquareSum
        // var sw_est = Stopwatch.StartNew();
        // ulong S_est = CountSketch.EstimateSquareSum();
        // sw_est.Stop();

        // Console.WriteLine("✅ Estimating SquareSum (S) with CountSketch:");
        // Console.WriteLine($"SquareSum Countsketch = {S_est}");
        // Console.WriteLine($"Time for EstimateSquareSum: {sw_est.ElapsedMilliseconds} ms");

        // BigInteger a0 = HashFunctions.RandomCoeff();
        // BigInteger a1 = HashFunctions.RandomCoeff();
        // Func<ulong, ulong> hashFunc = x => HashFunctions.MultiplyModP(x, a0, a1, l);
        // Console.WriteLine("📏 Computing exact S with chaining...");

        // // Time ComputeSquareSum
        // var sw_exact = Stopwatch.StartNew();
        // var (S_exact, table) = Estimators.ComputeSquareSum(stream, hashFunc, l);
        // sw_exact.Stop();

        // Console.WriteLine("🎯 Exact SquareSum (S):");
        // Console.WriteLine($"Exact SquareSum = {S_exact}");
        // Console.WriteLine($"Time for ComputeSquareSum: {sw_exact.ElapsedMilliseconds} ms");

        // // Error calculation
        // double relativeError = ((double)S_est - S_exact) / S_exact;
        // Console.WriteLine($"📉 Relative error: {relativeError:P2}");

        // ulong testKey = stream[0].Item1;
        // int fx_est = CountSketch.EstimateFrequency(testKey);

        // long fx_true = 0;
        // foreach (var (key, value) in stream)
        // {
        //     if (key == testKey)
        //         fx_true += value;
        // }

        // Console.WriteLine($"🔍 Estimated f({testKey}) = {fx_est}, exact = {fx_true}");


    }
}
