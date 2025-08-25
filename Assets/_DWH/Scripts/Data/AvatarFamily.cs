using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AvatarFamily
{
    public string familyName;
    [Range(0f, 100f)]
    public float spawnChance;
    public List<AvatarVariant> variants = new List<AvatarVariant>();
}