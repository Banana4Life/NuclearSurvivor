using System;
using System.Collections.Generic;
using Priority_Queue;
// using UnityEngine;

public static class ShortestPath
{
    public static List<TNode> Search<TNode, TCost>(TNode from, TNode to,
        Func<TNode, IEnumerable<TNode>> neighbors,
        Func<Dictionary<TNode, TNode>, TNode, TNode, TCost> cost,
        Func<TNode, TNode, TCost> estimate,
        TCost min, TCost max, Func<TCost, TCost, TCost> add)
        where TCost : IComparable<TCost>
        where TNode : IEquatable<TNode>
    {
        var closedSet = new HashSet<TNode>();
        var openQueue = new SimplePriorityQueue<TNode, TCost>();
        var costs = new Dictionary<TNode, TCost>
        {
            [from] = min
        };
        var previousNodes = new Dictionary<TNode, TNode>();
        openQueue.Enqueue(from, costs[from]);

        // int nodeCounter = 0;
        // int shorterPathFoundCounter = 0;

        while (openQueue.Count > 0)
        {
            // nodeCounter++;
            var current = openQueue.Dequeue();
            closedSet.Add(current);
            
            var currentCost = costs[current];
            if (current.Equals(to))
            {
                break;
            }

            foreach (var item in neighbors(current))
            {
                if (closedSet.Contains(item))
                {
                    continue;
                }
                var existingItemCost = costs.GetValueOrDefault(item, max);
                var itemCost = add(add(currentCost, cost(previousNodes, current, item)), estimate(item, to));
                if (itemCost.CompareTo(existingItemCost) < 0)
                {
                    // shorterPathFoundCounter++;
                    costs[item] = itemCost;
                    previousNodes[item] = current;
                    if (!openQueue.EnqueueWithoutDuplicates(item, itemCost))
                    {
                        openQueue.UpdatePriority(item, itemCost);
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
        // Debug.LogWarning($"Nodes visited: {nodeCounter}, shorter paths found: {shorterPathFoundCounter}, nodes in path: {path.Count}");
        return path;
    }

    public static List<TItem> Search<TItem>(TItem from, TItem to, Func<TItem, IEnumerable<TItem>> neighbors,
        Func<Dictionary<TItem, TItem>, TItem, TItem, float> cost, Func<TItem, TItem, float> estimate) where TItem : IEquatable<TItem> =>
        Search(from, to, neighbors, cost, estimate, 0, float.PositiveInfinity, (a, b) => a + b);
}