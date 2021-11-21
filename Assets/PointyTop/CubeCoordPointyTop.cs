using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PointyTop
{
    public static class CubeCoordPointyTop
    {
        private static readonly float[] CubeToWorldMatrix = {
            1f, 1f / 2f,
            0f, 3f / 4f,
        };
        private static readonly float[] WorldToCubeMatrix = {
            3f / 4f * 4f / 3f, -1f / 2f * 4f / 3f,
            0f               , 4f / 3f           ,
        };

        // counter-clockwiese
        public static readonly CubeCoord[] NeighborOffsets =
        {
            CubeCoord.NorthEast,
            CubeCoord.East,
            CubeCoord.SouthEast,
            CubeCoord.SouthWest,
            CubeCoord.West,
            CubeCoord.NorthWest
        };

        public static CubeCoord FromWorld(Vector3 p, Vector3 size)
        {
            return CubeCoord.FromWorld(p, size, WorldToCubeMatrix);
        }

        public static Vector3 ToWorld(this CubeCoord self, int y, Vector3 size)
        {
            return self.ToWorld(y, size, CubeToWorldMatrix);
        }

        public static CubeCoord[] Neighbors(this CubeCoord coord) => coord.Neighbors(NeighborOffsets);

        public static Graph.PathFindingResult<CubeCoord, float> SearchShortestPath(CubeCoord from, CubeCoord to,
            Func<Dictionary<CubeCoord, CubeCoord>, CubeCoord, CubeCoord, float> cost)
        {
            return Graph.FindPath(from, to, item => NeighborOffsets.Select(neighbor => item + neighbor), cost,
                (_, _) => 0);
        }

        public static Graph.PathFindingResult<CubeCoord, float> SearchShortestPath(CubeCoord from, CubeCoord to,
            Func<Dictionary<CubeCoord, CubeCoord>, CubeCoord, CubeCoord, float> cost,
            Func<CubeCoord, CubeCoord, float> estimate)
        {
            return Graph.FindPath(from, to, item => item.Neighbors(), cost, estimate);
        }

        public static bool IsAdjacent(this CubeCoord self, CubeCoord coord)
        {
            // PointyTop or FlatTopNeighbors does not matter 
            return NeighborOffsets.Contains(self - coord);
        }
    }
}