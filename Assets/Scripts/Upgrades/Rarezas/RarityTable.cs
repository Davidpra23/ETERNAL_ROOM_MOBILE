using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Rarity Table")]
public class RarityTable : ScriptableObject
{
    [Tooltip("Curvas de peso por rareza según número de oleada. X=Wave, Y=Peso relativo")]
    public AnimationCurve commonCurve    = AnimationCurve.Linear(1, 1f, 100, 0.5f);
    public AnimationCurve rareCurve      = AnimationCurve.Linear(1, 0.0f, 100, 0.7f);
    public AnimationCurve epicCurve      = AnimationCurve.Linear(1, 0.0f, 100, 0.4f);
    public AnimationCurve legendaryCurve = AnimationCurve.Linear(1, 0.0f, 100, 0.2f);

    [Min(1)] public int clampMinWave = 1;
    [Min(1)] public int clampMaxWave = 100;

    public Dictionary<Rarity, float> GetWeights(int wave)
    {
        wave = Mathf.Clamp(wave, clampMinWave, clampMaxWave);
        float wC = Mathf.Max(0f, commonCurve.Evaluate(wave));
        float wR = Mathf.Max(0f, rareCurve.Evaluate(wave));
        float wE = Mathf.Max(0f, epicCurve.Evaluate(wave));
        float wL = Mathf.Max(0f, legendaryCurve.Evaluate(wave));

        // Normaliza para obtener proporciones
        float sum = wC + wR + wE + wL;
        if (sum <= 0f) { wC = 1f; sum = 1f; }

        return new Dictionary<Rarity, float>
        {
            { Rarity.Common,    wC / sum },
            { Rarity.Rare,      wR / sum },
            { Rarity.Epic,      wE / sum },
            { Rarity.Legendary, wL / sum }
        };
    }
}
