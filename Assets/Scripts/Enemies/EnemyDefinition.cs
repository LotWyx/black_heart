using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Описание врага. Редактируется в инспекторе (BattleController.enemies).
    [Serializable]
    public class EnemyDefinition
    {
        public string displayName = "ВРАГ";
        [Tooltip("Путь спрайта внутри Assets/Resources/Art, например Enemies/Ghost")]
        public string spriteId = ArtIds.EnemyGhost;
        public int minHp = 2;
        public int maxHp = 3;

        [Tooltip("Id паттернов из PatternLibrary: straight, zigzag, spiral, drunk, boom")]
        public List<string> patterns = new List<string> { StraightPattern.ID };

        [Header("Особые пули: соотношение фиолетовых и синих")]
        public float purpleWeight = 0.6f;
        public float blueWeight = 0.4f;

        public string laughText = "ХА-ХА-ХА!";

        public string PickPattern() =>
            patterns.Count > 0 ? patterns[UnityEngine.Random.Range(0, patterns.Count)] : StraightPattern.ID;

        public float BlueRatio
        {
            get
            {
                float total = Mathf.Max(0, purpleWeight) + Mathf.Max(0, blueWeight);
                return total > 0 ? Mathf.Max(0, blueWeight) / total : 0f;
            }
        }

        public ProjectileType PickSpecialType(bool allowBlue = true)
        {
            if (!allowBlue) return ProjectileType.Purple;
            return UnityEngine.Random.value < BlueRatio ? ProjectileType.Blue : ProjectileType.Purple;
        }
    }
}
