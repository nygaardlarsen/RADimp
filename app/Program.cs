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

        // Compute hash = (a * x + b) mod p
        BigInteger bigA = new BigInteger(a);
        BigInteger bigX = new BigInteger(x);
        BigInteger bigB = new BigInteger(b);

        BigInteger result = (bigA * bigX + bigB);

        // Efficient mod p where p = 2^q - 1
        result = (result & p) + (result >> q);
        if (result >= p)
            result -= p;

        // Final hash = result mod 2^l
        ulong final = (ulong)(result & ((1UL << l) - 1));
        return final;
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
    

    public static void BenchmarkHashFunctions(int n, int l){

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
    public static void Main(string[] args){
    // Demonstrér funktionerne
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
    StreamGenerator.BenchmarkHashFunctions(10000000, 16);
    }
}
