using System.Numerics;

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


        ulong hash = HashFunctions.MultiplyModP(x, a, b, l);
        Console.WriteLine($"Hash: {hash}");
    }
}
