using System;
using System.Linq;

namespace Terminus;

public sealed class Key(params object[] parts) : IEquatable<Key>
{
    private readonly object[] _parts = parts;
    private readonly int _hashCode = CombineHashes( 
        parts.Select(p => p.GetHashCode()).ToArray());

    public bool Equals(Key? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _parts.SequenceEqual(other._parts);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Key)obj);
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }
    
    private static int CombineHashes(params int[] hashes)
    {
        const int fnvPrime = 16777619;
        var hash = unchecked((int)2166136261);
    
        foreach (var h in hashes)
        {
            hash ^= h;
            hash *= fnvPrime;
        }
        return hash;
    }
}