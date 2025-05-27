using System.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;


public static class HashFunctions
{
    private static readonly int q = 89;
    private static readonly BigInteger p = (BigInteger.One << q) - 1;

    public static ulong MultiplyShift(ulong x, ulong a, int l)
    {
        if (l <= 0 || l >= 64)
            throw new ArgumentOutOfRangeException(nameof(l), "Parameter 'l' must be between 1 and 63.");

        a |= 1UL; // ensure a is odd
        return (a * x) >> (64 - l);
    }
    public static ulong MultiplyModP(ulong x, ulong a, ulong b, int l)
    {
        if (l <= 0 || l > 64)
            throw new ArgumentOutOfRangeException(nameof(l), "l must be in the range (0, 64]");

        BigInteger result = a * x + b;

        result = (result & p) + (result >> q); // f(x) = (a * x + b) mod p
        if (result >= p)
            result -= p;

        // Final hash = result mod 2^l
        return (ulong)(result & ((1UL << l) - 1)); // f(x) = ((a * x + b) mod p) mod 2^l
    }
    public static BigInteger GHash(ulong x, BigInteger a0, BigInteger a1, BigInteger a2, BigInteger a3)
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
}

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
}

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
        ulong a = ((ulong)(uint)rnd.Next() << 32) | (ulong)(uint)rnd.Next();
        ulong b = ((ulong)(uint)rnd.Next() << 32) | (ulong)(uint)rnd.Next();


        // Generate datastream
        var stream = StreamGenerator.CreateStream(n, l).ToList();

        // MultiplyShift
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        ulong sumShift = 0;
        foreach (var (key, _) in stream)
        {
            sumShift += HashFunctions.MultiplyShift(key, a, l);
        }
        sw1.Stop();

        // MultiplyModP
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        ulong sumModP = 0;
        foreach (var (key, _) in stream)
        {
            sumModP += HashFunctions.MultiplyModP(key, a, b, l);
        }
        sw2.Stop();

        // Print results
        Console.WriteLine($"n = {n}, l = {l}");
        Console.WriteLine($"MultiplyShift:   Sum = {sumShift}, Time = {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"MultiplyModP:    Sum = {sumModP}, Time = {sw2.ElapsedMilliseconds} ms");
    }
}

public static class Estimators
{
    public static ulong ComputeSquareSum(IEnumerable<Tuple<ulong, int>> stream, Func<ulong, ulong> hashFunc, int l)
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
                sum += (ulong)(value * value); // OBS: cast for at undgå overflow
            }
        }

        return sum;
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
        ulong sumShift = ComputeSquareSum(stream1, hashShift, l);
        sw1.Stop();

        // MultiplyModP
        Func<ulong, ulong> hashModP = x => HashFunctions.MultiplyModP(x, a, b, l);
        var sw2 = Stopwatch.StartNew();
        ulong sumModP = ComputeSquareSum(stream2, hashModP, l);
        sw2.Stop();

        // Output
        Console.WriteLine($"[n={n}, l={l}]");
        Console.WriteLine($"MultiplyShift:   S = {sumShift}, Time = {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"MultiplyModP:    S = {sumModP}, Time = {sw2.ElapsedMilliseconds} ms");
    }   
}


public class Program
{
    public static void Main(string[] args)
    {
        ulong x = 12345678901234567890UL;
        ulong a = 1234567890123UL;
        ulong b = 987654321098UL;
        int l = 16;

        ulong hash1 = HashFunctions.MultiplyShift(x, a, l);
        Console.WriteLine($"MultiplyShift Hash: {hash1}");

        ulong hash2 = HashFunctions.MultiplyModP(x, a, b, l);
        Console.WriteLine($"MultiplyModP Hash: {hash2}");

        // Benchmark
        Console.WriteLine("\nBenchmarking hash functions:");
        StreamGenerator.BenchmarkHashFunctions(1000000, 16);


        Console.WriteLine("\nTesting HashTableChaining (basic test):");

        // Use multiplyShift hash function with a random odd number
        ulong aHash = ((ulong)(uint)new Random().Next() << 32) | (ulong)(uint)new Random().Next();
        Func<ulong, ulong> hashFunc = x => HashFunctions.MultiplyShift(x, aHash | 1UL, l);

        // Create HashTableChaining instance
        var table = new HashTableChaining(hashFunc, l);

        // Simpel test: tilføj og hent nøgler
        table.Increment(42UL, 5);
        table.Increment(42UL, 3);
        table.Increment(13UL, 1);
        // Test Set method

        Console.WriteLine($"Value for key 42: {table.Get(42UL)} (Expected: 8)");
        Console.WriteLine($"Value for key 13: {table.Get(13UL)} (Expected: 1)");

        table.Set(42UL, 100);
        table.Set(13UL, 50);


        Console.WriteLine($"Value for unknown key 99: {table.Get(99UL)} (Expected: 0)");
        Console.WriteLine($"Value for key 42 after Set: {table.Get(42UL)} (Expected: 100)");

        foreach (int l_value in new int[] { 2, 4, 6 })
        {
            Estimators.BenchmarkSquareSum(1000000, l_value);
        }

        BigInteger a0 = HashFunctions.RandomCoeff();
        BigInteger a1 = HashFunctions.RandomCoeff();
        BigInteger a2 = HashFunctions.RandomCoeff();
        BigInteger a3 = HashFunctions.RandomCoeff();

        for (ulong c = 0; c < 10; c++)
        {
            
            BigInteger gx = HashFunctions.GHash(c, a0, a1, a2, a3);
            Console.WriteLine($"g({c}) = {gx}");
        }
    }
}
