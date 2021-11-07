using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

// Further reading: https://www.redblobgames.com/grids/hexagons/
public struct CubeCoord : IEquatable<CubeCoord>
{
    // pointy top
    private static readonly float[] PointyCubeToWorldMatrix = {
        1f, 1f / 2f,
        0f, 3f / 4f,
    };
    private static readonly float[] PointyWorldToCubeMatrix = {
        3f / 4f * 4f / 3f, -1f / 2f * 4f / 3f,
        0f               , 4f / 3f           ,
    };
    
    // flat top
    private static readonly float[] FlatCubeToWorldMatrix = {
        3f / 4f, 0,
        1f / 2f, 1f,
    };
    
    private static readonly float[] FlatWorldToCubeMatrix = {
        4f / 3f           , 0,
        -1f / 2f * 4f / 3f, 3f / 4f * 4f / 3f,
    };

    public enum WorldType
    {
        PointyTop, FlatTop
    }
    
    public static readonly CubeCoord Origin = new(0, 0);
    
    public static readonly CubeCoord NorthEast = new( 0, 1);
    public static readonly CubeCoord East      = new( 1,  0);
    public static readonly CubeCoord SouthEast = new( 1,  -1);
    public static readonly CubeCoord SouthWest = new(0,  -1);
    public static readonly CubeCoord West      = new(-1,  0);
    public static readonly CubeCoord NorthWest = new( -1, 1);

    public static readonly CubeCoord[] PointyTopNeighborOffsets = { NorthEast, East, SouthEast, SouthWest, West, NorthWest }; // counter-clockwiese
    public static readonly CubeCoord[] FlatTopNeighborOffsets = { NorthEast, East, SouthEast, SouthWest, West, NorthWest }; // clockwise - top first
    
    public int Q { get; }
    public int R { get; }
    public int S { get; }

    public CubeCoord(int q, int r, int s)
    {
        if (q + r + s != 0)
        {
            throw new Exception("q + r + s must be 0");
        }
        Q = q;
        R = r;
        S = s;
    }

    public CubeCoord(int q, int r) : this(q, r, -q - r)
    {
        
    }

    public float Length => (Math.Abs(Q) + Math.Abs(R) + Math.Abs(S)) / 2.0f;
    public double Distance(CubeCoord b) => (this - b).Length;

    public int ManhattenDistance(CubeCoord b)
    {
        var d = this - b;
        return Math.Abs(d.Q) + Math.Abs(d.R);
    }

    private Vector3 _toWorld(int y, Vector3 size, float[] matrix)
    {
        return new((matrix[0] * Q + matrix[1] * R) * size.x, y, (matrix[2] * Q + matrix[3] * R) * size.z);
    }

    public Vector3 FlatTopToWorld(int y, Vector3 size)
    {
        return _toWorld(y, size, FlatCubeToWorldMatrix);
    }

    public Vector3 PointyTopToWorld(int y, Vector3 size)
    {
        return _toWorld(y, size, PointyCubeToWorldMatrix);
    }

    private static CubeCoord _fromWorld(Vector3 p, Vector3 size, float[] matrix)
    {
        return new(Mathf.RoundToInt((matrix[0] * p.x + matrix[1] * p.z) / size.x), Mathf.RoundToInt((matrix[2] * p.x + matrix[3] * p.z) / size.z));
    }

    public static CubeCoord FlatTopFromWorld(Vector3 p, Vector3 size)
    {
        return _fromWorld(p, size, FlatWorldToCubeMatrix);
    }

    public static CubeCoord PointyTopFromWorld(Vector3 p, Vector3 size)
    {
        return _fromWorld(p, size, PointyWorldToCubeMatrix);
    }

    public static CubeCoord operator +(CubeCoord a, CubeCoord b) => new(a.Q + b.Q, a.R + b.R, a.S + b.S);
    public static CubeCoord operator -(CubeCoord a, CubeCoord b) => new(a.Q - b.Q, a.R - b.R, a.S - b.S);
    public static CubeCoord operator *(CubeCoord a, int b) => new(a.Q * b, a.R * b, a.S * b);

    public override string ToString()
    {
        return $"{nameof(Q)}: {Q}, {nameof(R)}: {R}, {nameof(S)}: {S}";
    }

    public bool Equals(CubeCoord other) => this == other;

    public override bool Equals(object obj) => obj is CubeCoord other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Q, R, S);

    public static bool operator ==(CubeCoord a, CubeCoord b) => a.Q == b.Q && a.R == b.R && a.S == b.S;

    public static bool operator !=(CubeCoord a, CubeCoord b) => a.Q != b.Q || a.R != b.R || a.S != b.S;

    public static int CountCellsInRing(int radius) => Math.Max(1, radius * PointyTopNeighborOffsets.Length);

    private CubeCoord[] Neighbors(CubeCoord[] offsets)
    {
        var output = new CubeCoord[offsets.Length];
        for (var i = 0; i < offsets.Length; i++)
        {
            output[i] = this + offsets[i];
        }

        return output;
    }

    public CubeCoord[] FlatTopNeighbors() => Neighbors(FlatTopNeighborOffsets);
    public CubeCoord[] PointyTopNeighbors() => Neighbors(PointyTopNeighborOffsets);

    public static CubeCoord[] Ring(CubeCoord center, int radius)
    {
        if (radius == 0)
        {
            return Array.Empty<CubeCoord>();
        }

        var cube = (center + (West * radius));
        var neighborCount = PointyTopNeighborOffsets.Length;
        var output = new CubeCoord[neighborCount * radius];
        var outputIndex = 0;
        for (var neighborIndex = 0; neighborIndex < neighborCount; neighborIndex++)
        {
            var direction = PointyTopNeighborOffsets[neighborIndex];
            for (var i = 0; i < radius; i++)
            {
                output[outputIndex++] = cube;
                cube += direction;
            }
        }

        return output;
    }

    public static IEnumerable<CubeCoord> Spiral(CubeCoord center, int startRing = 0, int maxRings = -1)
    {
        for (var i = 0; i < maxRings || maxRings == -1; ++i)
        {
            foreach (var coord in Ring(center, startRing + i))
            {
                yield return coord;
            }
        }
    }

    public static IEnumerable<CubeCoord> Outline(HashSet<CubeCoord> coords)
    {
        var emitted = new HashSet<CubeCoord>();
        foreach (var cubeCoord in coords)
        {
            foreach (var neighbor in PointyTopNeighborOffsets)
            {
                var neighborCoord = cubeCoord + neighbor;
                if (!coords.Contains(neighborCoord) && !emitted.Contains(neighborCoord))
                {
                    emitted.Add(neighborCoord);
                    yield return neighborCoord;
                }
            }
        }
    }

    public static IEnumerable<CubeCoord> ShuffledRings(CubeCoord center, int startRing = 0, int maxRings = -1)
    {
        for (var i = 0; i < maxRings || maxRings == -1; ++i)
        {
            foreach (var coord in Ring(center, startRing + i).ToList().Shuffled())
            {
                yield return coord;
            }
        }
    }

    public static ShortestPath.PathFindingResult<CubeCoord, float> SearchShortestPath(CubeCoord from, CubeCoord to, Func<Dictionary<CubeCoord, CubeCoord>, CubeCoord, CubeCoord, float> cost)
    {
        return ShortestPath.Search(from, to, item => PointyTopNeighborOffsets.Select(neighbor => item + neighbor), cost, (_, _) => 0);
    }

    public static ShortestPath.PathFindingResult<CubeCoord, float> SearchShortestPath(CubeCoord from, CubeCoord to, Func<Dictionary<CubeCoord, CubeCoord>, CubeCoord, CubeCoord, float> cost, Func<CubeCoord, CubeCoord, float> estimate)
    {
        return ShortestPath.Search(from, to, item => item.FlatTopNeighbors(), cost, estimate);
    }

    public bool IsAdjacent(CubeCoord coord)
    {
        // PointyTop or FlatTopNeighbors does not matter 
        return FlatTopNeighborOffsets.Contains(this - coord);
    }
}

public static class EnumeratorExt
{
    public static IList<T> Shuffled<T>(this IList<T> l)
    {
        var copy = new List<T>(l);
        int n = copy.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Range(0, n + 1);
            (copy[k], copy[n]) = (copy[n], copy[k]);
        }
        return copy;
    }
    
    public static void Shuffle<T>(this T[] l)
    {
        int n = l.Length;
        while (n > 1)
        {
            n--;
            var k = Random.Range(0, n + 1);
            (l[k], l[n]) = (l[n], l[k]);
        }
    }
} 
