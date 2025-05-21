// h(x) = (a * x) >> (64 - l)
// a: et ulige 64-bit tal
// l: et heltal 0 < l < 64

using System;

public class MultiplyShiftHash
{
    private readonly ulong a;
    private readonly int l;

    public MultiplyShiftHash(ulong a, int l)
    {
        if (l <= 0 || l >= 64)
            throw new ArgumentOutOfRangeException(nameof(l), "Parameter 'l' skal være et positivt heltal mindre end 64.");

        this.a = a | 1UL;
        this.l = l;
    }

    public ulong Hash(ulong x)
    {
        return (a * x) >> (64 - l);
    }
}
public class Program
{
    public static void Main(string[] args)  // <- Important: static + correct signature
    {
        ulong a = 0x7C0E754BD488FCA3UL;
        int l = 16;

        var hasher = new MultiplyShiftHash(a, l);

        ulong input = 9876543210987654321UL;
        ulong hash = hasher.Hash(input);

        Console.WriteLine($"Hashværdi: {hash}");
    }
}
