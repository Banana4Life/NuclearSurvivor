using System;
using System.Collections.Generic;
using Priority_Queue;

public static class Graph
{
    public struct PathFindingResult<TNode, TCost>
    {
        public TNode From { get; }
        public TNode To { get; }
        public HashSet<TNode> ClosedSet { get; }
        public SimplePriorityQueue<TNode, TCost> OpenQueue { get; }
        public Dictionary<TNode, TCost> CostLookup { get; }
        public Dictionary<TNode, TNode> PreviousLookup { get; }
        public List<TNode> Path { get; }
        public TCost TotalCost { get; }

        public PathFindingResult(TNode from, TNode to, HashSet<TNode> closedSet,
            SimplePriorityQueue<TNode, TCost> openQueue, Dictionary<TNode, TCost> costLookup,
            Dictionary<TNode, TNode> previousLookup, List<TNode> path, TCost totalCost)
        {
            From = from;
            To = to;
            ClosedSet = closedSet;
            OpenQueue = openQueue;
            CostLookup = costLookup;
            PreviousLookup = previousLookup;
            Path = path;
            TotalCost = totalCost;
        }
    }
    
    public static PathFindingResult<TNode, TCost> FindPath<TNode, TCost>(TNode from, TNode to,
        Func<TNode, IEnumerable<TNode>> neighbors,
        Func<Dictionary<TNode, TNode>, TNode, TNode, TCost> cost,
        Func<TNode, TNode, TCost> estimate,
        TCost min, TCost max, Func<TCost, TCost, TCost> add)
        where TCost : IComparable<TCost>
        where TNode : IEquatable<TNode>
    {
        var closedSet = new HashSet<TNode>();
        var openQueue = new SimplePriorityQueue<TNode, TCost>();
        var exactCosts = new Dictionary<TNode, TCost>
        {
            [from] = min
        };
        var previousNodes = new Dictionary<TNode, TNode>();
        openQueue.Enqueue(from, exactCosts[from]);

        while (openQueue.Count > 0)
        {
            var current = openQueue.Dequeue();
            closedSet.Add(current);
            
            if (current.Equals(to))
            {
                break;
            }
            var currentCost = exactCosts[current];
            foreach (var item in neighbors(current))
            {
                if (closedSet.Contains(item))
                {
                    continue;
                }
                var knownExactCost = exactCosts.GetValueOrDefault(item, max);
                var newExactCost = add(currentCost, cost(previousNodes, current, item));
                if (newExactCost.CompareTo(knownExactCost) < 0)
                {
                    var estimatedCost = add(newExactCost, estimate(item, to));
                    previousNodes[item] = current;
                    exactCosts[item] = newExactCost;
                    if (!openQueue.EnqueueWithoutDuplicates(item, estimatedCost))
                    {
                        openQueue.UpdatePriority(item, estimatedCost);
                    }
                }
            }
        }

        var totalCost = exactCosts[to];
        var path = new List<TNode>();
        var end = to;
        while (true)
        {
            path.Add(end);
            if (!previousNodes.TryGetValue(end, out end))
            {
                break;
            }
        }
        path.Reverse();

        return new PathFindingResult<TNode, TCost>(from, to, closedSet, openQueue, exactCosts, previousNodes, path, totalCost);
    }

    public static PathFindingResult<TNode, float> FindPath<TNode>(TNode from, TNode to, Func<TNode, IEnumerable<TNode>> neighbors,
        Func<Dictionary<TNode, TNode>, TNode, TNode, float> cost, Func<TNode, TNode, float> estimate) where TNode : IEquatable<TNode> =>
        FindPath(from, to, neighbors, cost, estimate, 0, float.PositiveInfinity, (a, b) => a + b);

    public static IEnumerable<TNode> TraverseNodes<TNode>(TNode from, Func<TNode, IEnumerable<TNode>> neighbors) where TNode : IEquatable<TNode>
    {
        var seen = new HashSet<TNode>();
        var next = new Queue<TNode>();
        next.Enqueue(from);

        while (next.Count > 0)
        {
            var current = next.Dequeue();
            seen.Add(current);

            yield return current;
            
            foreach (var newNeighbor in neighbors(current))
            {
                if (!seen.Contains(newNeighbor))
                {
                    next.Enqueue(newNeighbor);
                }
            }
        }
    }

    public static IEnumerable<(TNode, TNode)> TraverseEdges<TNode>(TNode from, Func<TNode, IEnumerable<TNode>> neighbors) where TNode : IEquatable<TNode>
    {
        var seenEdges = new HashSet<(TNode, TNode)>();
        var seenNodes = new HashSet<TNode>();
        var next = new Queue<TNode>();
        next.Enqueue(from);

        while (next.Count > 0)
        {
            var current = next.Dequeue();
            seenNodes.Add(current);
            
            foreach (var newNeighbor in neighbors(current))
            {
                if (!seenNodes.Contains(newNeighbor))
                {
                    next.Enqueue(newNeighbor);
                    var edge = (current, newNeighbor);
                    if (!seenEdges.Contains(edge) && !seenEdges.Contains((edge.Item2, edge.Item1)))
                    {
                        yield return edge;
                    }
                }
            }
        }
    }
}