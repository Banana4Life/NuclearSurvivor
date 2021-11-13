using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Area
{
    public readonly HashSet<CubeCoord> Coords;
    public readonly TileArea TileArea;
    
    public Area(HashSet<CubeCoord> coords, TileArea area)
    {
        Coords = coords;
        TileArea = area;
    }
}

public class Hallway : Area
{
    public readonly Room From;
    public readonly Room To;
    public readonly List<CubeCoord> Intersecting;

    public Hallway(Room from, Room to, ILookup<bool, CubeCoord> coords, TileArea tileArea) : base(coords[false].ToHashSet(), tileArea)
    {
        From = from;
        To = to;
        Intersecting = coords[true].ToList();
    }
}

public class Room : Area
{
    public readonly CubeCoord RoomCoord;
    public readonly CubeCoord Origin;
    public readonly (CubeCoord, int)[] Centers;
    public readonly Vector3 WorldCenter;

    public Room(CubeCoord roomCoord, CubeCoord origin, (CubeCoord, int)[] centers, HashSet<CubeCoord> coords,
        TileArea tileArea, Vector3 worldCenter) : base(coords, tileArea)
    {
        RoomCoord = roomCoord;
        Origin = origin;
        Centers = centers;
        WorldCenter = worldCenter;
    }
}

public abstract class CellRole
{
}

public sealed class RoomRole : CellRole
{
    public readonly Room Room;

    public RoomRole(Room room)
    {
        Room = room;
    }
}

public sealed class HallwayRole : CellRole
{
    public readonly List<Hallway> Hallways;

    public HallwayRole(Hallway hallways)
    {
        Hallways = new List<Hallway> { hallways };
    }
}