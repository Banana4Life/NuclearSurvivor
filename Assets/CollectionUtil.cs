
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public static class CollectionExt
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
    
    public static void Shuffle<T>(this IList<T> l)
    {
        int n = l.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Range(0, n + 1);
            (l[k], l[n]) = (l[n], l[k]);
        }
    }

    public static IEnumerable<(T1, T2)> Zip<T1, T2>(this IEnumerable<T1> a, IEnumerable<T2> b)
    {
        return a.Zip(b, (x, y) => (x, y));
    }

    public static IEnumerable<(T, int)> ZipWithIndex<T>(this IEnumerable<T> a)
    {
        return a.Select((x, i) => (x, i));
    }

    public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> f) where TKey : IComparable<TKey>
    {
        var e = source.GetEnumerator();
        if (!e.MoveNext())
        {
            throw new ArgumentNullException();
        }

        TSource maxValue = e.Current;
        TKey max = f(maxValue);
        while (e.MoveNext())
        {
            var nextValue = e.Current;
            var next = f(nextValue);
            if (next.CompareTo(max) > 0)
            {
                maxValue = maxValue;
                max = next;
            }
        }

        return maxValue;
    }
}