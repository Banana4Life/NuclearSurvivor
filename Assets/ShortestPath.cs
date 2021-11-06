using System;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;

public static class ShortestPath
{
    public static List<TItem> Search<TItem, TCost>(TItem from, TItem to,
        Func<TItem, IEnumerable<TItem>> neighbors,
        Func<Dictionary<TItem, TItem>, TItem, TItem, TCost> cost,
        Func<TItem, TItem, TCost> estimate,
        TCost min, TCost max, Func<TCost, TCost, TCost> add)
        where TCost : IComparable<TCost>
        where TItem : IEquatable<TItem>
    {
        var seenElements = new HashSet<TItem>();
        var nextElements = new SimplePriorityQueue<TItem, TCost>();
        var costs = new Dictionary<TItem, TCost>
        {
            [from] = min
        };
        var prev = new Dictionary<TItem, TItem>();
        nextElements.Enqueue(from, costs[from]);

        while (nextElements.Count > 0)
        {
            var current = nextElements.Dequeue();
            var currentCost = costs[current];
            seenElements.Add(current);
            if (current.Equals(to))
            {
                break;
            }

            var neighborItems = neighbors(current)
                .Where(item => !seenElements.Contains(item));
            foreach (var item in neighborItems)
            {
                var existingItemCost = costs.GetValueOrDefault(item, max);
                var itemCost = add(add(currentCost, cost(prev, current, item)), estimate(item, to));
                if (itemCost.CompareTo(existingItemCost) < 0)
                {
                    costs[item] = itemCost;
                    prev[item] = current;
                    nextElements.Enqueue(item, itemCost);
                }
            }

        }

        var path = new List<TItem>();
        var end = to;
        while (true)
        {
            path.Add(end);
            if (!prev.TryGetValue(end, out end))
            {
                break;
            }
        }
        path.Reverse();
        return path;
    }

    public static List<TItem> Search<TItem>(TItem from, TItem to, Func<TItem, IEnumerable<TItem>> neighbors,
        Func<Dictionary<TItem, TItem>, TItem, TItem, float> cost, Func<TItem, TItem, float> estimate) where TItem : IEquatable<TItem> =>
        Search(from, to, neighbors, cost, estimate, 0, float.PositiveInfinity, (a, b) => a + b);
}