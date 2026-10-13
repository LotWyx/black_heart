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

        [Tooltip("Префабы атак из Assets/Prefabs/Patterns. Повтор префаба делает атаку чаще")]
        public List<AttackPattern> attacks = new List<AttackPattern>();

        [Header("Особые пули: соотношение фиолетовых и синих")]
        public float purpleWeight = 0.5f;
        public float blueWeight = 0.5f;

        public string laughText = "ХА-ХА-ХА!";

        // Случайная атака из списка; null — если атак нет
        public AttackPattern PickAttack() =>
            attacks.Count > 0 ? attacks[Random.Range(0, attacks.Count)] : null;

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
