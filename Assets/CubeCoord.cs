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

    public static readonly CubeCoord[] PointyTopNeighbors = { NorthEast, East, SouthEast, SouthWest, West, NorthWest }; // counter-clockwiese
    public static readonly CubeCoord[] FlatTopNeighbors = { NorthEast, East, SouthEast, SouthWest, West, NorthWest }; // clockwise - top first
    
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

    public bool Equals(CubeCoord other) => Q == other.Q && R == other.R && S == other.S;

    public static int CountCellsInRing(int radius) => Math.Max(1, radius * PointyTopNeighbors.Length);

    public static IEnumerable<CubeCoord> Ring(CubeCoord center, int radius)
    {
        if (radius == 0)
        {
            yield return center;
        }
        else
        {
            var cube = (center + (West * radius));
            foreach (var direction in PointyTopNeighbors)
            {
                for (var i = 0; i < radius; ++i)
                {
                    yield return cube;
                    cube += direction;
                }
            }
        }
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
            foreach (var neighbor in PointyTopNeighbors)
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

    public static List<CubeCoord> SearchShortestPath(CubeCoord from, CubeCoord to, Func<Dictionary<CubeCoord, CubeCoord>, CubeCoord, CubeCoord, float> cost)
    {
        return ShortestPath.Search(from, to, item => PointyTopNeighbors.Select(neighbor => item + neighbor), cost, (_, _) => 0);
    }

    public static List<CubeCoord> SearchShortestPath(CubeCoord from, CubeCoord to, Func<Dictionary<CubeCoord, CubeCoord>, CubeCoord, CubeCoord, float> cost, Func<CubeCoord, CubeCoord, float> estimate)
    {
        return ShortestPath.Search(from, to, item => PointyTopNeighbors.Select(neighbor => item + neighbor), cost, estimate);
    }

    public bool IsAdjacent(CubeCoord coord)
    {
        // PointyTop or FlatTopNeighbors does not matter 
        return FlatTopNeighbors.Contains(this - coord);
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
} 
