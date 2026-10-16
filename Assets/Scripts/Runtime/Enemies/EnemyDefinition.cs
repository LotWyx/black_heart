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

#if UNITY_EDITOR
        // Атаки указаны строками — опечатку видно сразу при правке ассета, а не только в тестах или в бою
        void OnValidate()
        {
            foreach (var id in patterns)
                if (!PatternLibrary.Exists(id))
                    Debug.LogError($"[{name}] Нет атаки '{id}'. Доступные: {string.Join(", ", PatternLibrary.AllIds)}", this);
        }
#endif

        public string PickPattern() =>
            patterns.Count > 0 ? patterns[GameRandom.Range(0, patterns.Count)] : StraightPattern.ID;

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
            return GameRandom.value < BlueRatio ? ProjectileType.Blue : ProjectileType.Purple;
        }
    }
}
