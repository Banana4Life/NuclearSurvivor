
using System.Collections.Generic;
using UnityEngine;

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
}