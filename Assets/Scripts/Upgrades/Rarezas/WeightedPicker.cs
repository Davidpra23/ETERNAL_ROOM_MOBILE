using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WeightedPicker
{
    // T: item (Upgrade/ShopItem)
    // getRarity: cómo obtener la rareza
    public static T PickOne<T>(IList<T> pool, Func<T, Rarity> getRarity, Dictionary<Rarity, float> rarityWeights, System.Random rng = null)
    {
        if (pool == null || pool.Count == 0) return default;
        rng ??= new System.Random();

        // Asigna peso por rareza, ignora items cuyo peso resultante sea 0
        var weighted = new List<(T item, float w)>();
        foreach (var it in pool)
        {
            if (it == null) continue;
            var r = getRarity(it);
            if (!rarityWeights.TryGetValue(r, out float w)) w = 0f;
            if (w > 0f) weighted.Add((it, w));
        }

        if (weighted.Count == 0) return default;

        float sum = weighted.Sum(x => x.w);
        float roll = (float)(rng.NextDouble()) * sum;

        float acc = 0f;
        foreach (var (item, w) in weighted)
        {
            acc += w;
            if (roll <= acc) return item;
        }
        return weighted.Last().item;
    }

    public static List<T> PickManyDistinct<T>(IList<T> pool, int count, Func<T, Rarity> getRarity, Dictionary<Rarity, float> rarityWeights, System.Random rng = null)
    {
        rng ??= new System.Random();
        var result = new List<T>(count);
        var available = new List<T>(pool.Where(p => p != null));

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            var pick = PickOne(available, getRarity, rarityWeights, rng);
            if (pick == null) break;
            result.Add(pick);
            available.Remove(pick); // sin repetidos
        }
        return result;
    }
}
