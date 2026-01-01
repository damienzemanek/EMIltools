using UnityEngine;

public static class Hashing
{
    public static ulong Fnv1a64(string text)
    {
        const ulong offset = 1469598103934665603UL;
        const ulong prime  = 1099511628211UL;

        ulong hash = offset;
        for (int i = 0; i < text.Length; i++)
        {
            hash ^= text[i];
            hash *= prime;
        }
        return hash;
    }
}
