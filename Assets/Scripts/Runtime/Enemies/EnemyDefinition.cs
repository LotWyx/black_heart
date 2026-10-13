using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Описание врага — ассет в Assets/Resources/Data/Enemies. Редактируется в инспекторе;
    // чтобы враг появился в бою, добавьте его в список GameBalance.enemies.
    [CreateAssetMenu(fileName = "Enemy", menuName = "Black Heart/Враг")]
    public class EnemyDefinition : ScriptableObject
    {
        public string displayName = "ВРАГ";
        [Tooltip("Путь спрайта внутри Assets/Resources/Art, например Enemies/Ghost")]
        public string spriteId = ArtIds.EnemyGhost;
        public int minHp = 5;
        public int maxHp = 5;

        [Tooltip("Id атак из PatternLibrary. Повтор id делает атаку чаще")]
        public List<string> patterns = new List<string>();

        [Header("Особые пули: соотношение фиолетовых и синих")]
        public float purpleWeight = 0.5f;
        public float blueWeight = 0.5f;

        public string laughText = "ХА-ХА-ХА!";

        public string PickPattern() =>
            patterns.Count > 0 ? patterns[Random.Range(0, patterns.Count)] : StraightPattern.ID;

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
            return Random.value < BlueRatio ? ProjectileType.Blue : ProjectileType.Purple;
        }
    }
}
