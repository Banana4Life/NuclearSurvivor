using System.Collections.Generic;

public class WeightedRandom
{
    public static float[] CumulativeDensity(IReadOnlyList<float> weights)
    {
        var cdf = new float[weights.Count];
        var prev = 0f;
        for (var i = 0; i < weights.Count; i++)
        {
            prev += weights[i];
            cdf[i] = prev;
        }

        return cdf;
    }
    
    private static T BinaryFindSelectedValue<T>(IReadOnlyList<T> values, IReadOnlyList<float> cdf, int lower, int upper, float selection)
    {
        var mid = (lower + upper) / 2;

        float lowerEdge;
        if (mid == 0)
        {
            lowerEdge = 0f;
        }
        else
        {
            lowerEdge = cdf[mid - 1];
        }
        var upperEdge = cdf[mid];

        if (selection < lowerEdge)
        {
            return BinaryFindSelectedValue(values, cdf, lower, mid, selection);
        }

        if (selection >= upperEdge)
        {
            return BinaryFindSelectedValue(values, cdf, mid, upper, selection);
        }
        
        return values[mid];
    }

    public static T ChooseWeighted<T>(IReadOnlyList<float> weights, IReadOnlyList<T> values, float selection)
    {
        var cdf = CumulativeDensity(weights);
        var sum = cdf[cdf.Length - 1];


        return BinaryFindSelectedValue(values, cdf, 0, values.Count, selection * sum);
    }

    public static T ChooseWeighted<T>(IReadOnlyList<float> weights, IReadOnlyList<T> values)
    {
        return ChooseWeighted(weights, values, UnityEngine.Random.value);
    }
}