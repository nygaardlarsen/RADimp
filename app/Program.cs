using System.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;


public static class HashFunctions
{
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

        // Define p = 2^89 - 1, so q = 89
        int q = 89;
        // Since we can't represent 2^89 directly, we'll use BigInteger
        var p = (BigInteger.One << q) - 1;

        // // Compute hash = (a * x + b) mod p
        // BigInteger bigA = new BigInteger(a);
        // BigInteger bigX = new BigInteger(x);
        // BigInteger bigB = new BigInteger(b);

        BigInteger result = a * x + b;

        // BigInteger result = (bigA * bigX + bigB);

        // Efficient mod p where p = 2^q - 1
        // result = result % p;
        result = (result & p) + (result >> q);
        if (result >= p)
            result -= p;

        // Final hash = result mod 2^l
        ulong final = (ulong)(result & ((1UL << l) - 1));
        return final;
    }
}

public class HashTableChaining
{
    private readonly int l;
    private readonly int tableSize;
    private readonly List<(ulong key, long value)>[] table;
    private readonly Func<ulong, ulong> hashFunction;
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
        var stream1 = StreamGenerator.CreateStream(n, l);

        // MultiplyShift
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        ulong sumShift = 0;
        foreach (var (key, _) in stream1)
        {
            sumShift += HashFunctions.MultiplyShift(key, a, l);
        }
        sw1.Stop();

        // Generate datastream again
        var stream2 = StreamGenerator.CreateStream(n, l);

        // MultiplyModP
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        ulong sumModP = 0;
        foreach (var (key, _) in stream2)
        {
            sumModP += HashFunctions.MultiplyModP(key, a, b, l);
        }
        sw2.Stop();

        // Print resultater
        Console.WriteLine($"n = {n}, l = {l}");
        Console.WriteLine($"MultiplyShift:   Sum = {sumShift}, Time = {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"MultiplyModP:    Sum = {sumModP}, Time = {sw2.ElapsedMilliseconds} ms");
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

    }
}
