using System;
using System.Collections.Generic;
using Priority_Queue;

public static class ShortestPath
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

        public PathFindingResult(TNode from, TNode to, HashSet<TNode> closedSet,
            SimplePriorityQueue<TNode, TCost> openQueue, Dictionary<TNode, TCost> costLookup,
            Dictionary<TNode, TNode> previousLookup, List<TNode> path)
        {
            From = from;
            To = to;
            ClosedSet = closedSet;
            OpenQueue = openQueue;
            CostLookup = costLookup;
            PreviousLookup = previousLookup;
            Path = path;
        }
    }
    
    public static PathFindingResult<TNode, TCost> Search<TNode, TCost>(TNode from, TNode to,
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

        return new PathFindingResult<TNode, TCost>(from, to, closedSet, openQueue, exactCosts, previousNodes, path);
    }

    public static PathFindingResult<TNode, float> Search<TNode>(TNode from, TNode to, Func<TNode, IEnumerable<TNode>> neighbors,
        Func<Dictionary<TNode, TNode>, TNode, TNode, float> cost, Func<TNode, TNode, float> estimate) where TNode : IEquatable<TNode> =>
        Search(from, to, neighbors, cost, estimate, 0, float.PositiveInfinity, (a, b) => a + b);
}