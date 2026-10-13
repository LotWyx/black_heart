using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Баланс игры одним ассетом: Assets/Resources/Data/GameBalance.asset. Меняется в инспекторе, без правки кода.
    // Враги — отдельные ассеты EnemyDefinition в Assets/Resources/Data/Enemies, здесь только их список.
    // Числа внутри самих атак (скорости, количество пуль) — в префабах атак: Assets/Prefabs/Patterns.
    [CreateAssetMenu(fileName = "GameBalance", menuName = "Black Heart/Баланс игры")]
    public class GameBalance : ScriptableObject
    {
        const string ResourcePath = "Data/GameBalance";

        [Header("Враги")]
        [Tooltip("Враги обычного боя. Первые бои забега показывают каждого по разу")]
        public List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        [Tooltip("Враг обучения, в обычный бой не выходит")]
        public EnemyDefinition tutorialBoss;

        [Header("Игрок")]
        public int playerMaxHp = 10;
        public float playerHitRadius = 0.35f;
        [Tooltip("Секунды неуязвимости после удара")]
        public float playerInvulnTime = 0.4f;

        [Header("Бой")]
        [Tooltip("Сколько секунд даётся на удар по врагу")]
        public float attackWindow = 3f;
        [Tooltip("Урон игроку, если за фазу атаки он не ударил врага")]
        public int missedAttackDamage = 1;
        [Tooltip("Сколько атак враг проводит за одну фазу защиты")]
        public int attacksPerPhase = 2;
        public float pauseBetweenAttacks = 0.4f;
        [Tooltip("Страховка: фаза защиты не длится дольше, даже если пуля где-то застряла")]
        public float defenseTimeout = 25f;
        public int healOnVictory = 2;
        [Tooltip("Отступ сверху экрана до врага (место под надписи HUD)")]
        public float topHudSpace = 0.4f;

        [Header("Сложность")]
        [Tooltip("Прирост за каждую фазу защиты в забеге")]
        public float difficultyPerTurn = 0.05f;
        [Tooltip("Прирост за каждую фазу защиты против текущего врага")]
        public float difficultyPerFightTurn = 0.18f;
        public float maxDifficulty = 2f;
        [Tooltip("Доля особых пуль в начале игры и на сложности 1")]
        public float specialShareEasy = 0.2f;
        public float specialShareMid = 0.3f;
        [Tooltip("Минимум секунд от края рамки до сердца")]
        public float minReactionTime = 1.5f;

        [Header("Очки")]
        public int whiteBullet = 10;
        public int purpleFragment = 15;
        public int blueBulletPenalty = -25;
        public int enemyDefeated = 200;
        [Tooltip("Фаза защиты пройдена без урона")]
        public int cleanDefense = 50;

        [Header("Пули")]
        [Tooltip("Насколько шире пули засчитывается разрез (юниты; 1/16 = 1 пиксель)")]
        public float sliceTolerance = 0.33f;
        [Tooltip("Сколько секунд оставшиеся одни синие пули ждут перед рывком в сердце")]
        public float blueRushDelay = 1f;
        [Tooltip("Сколько пуль создаётся заранее. Если не хватит, пул дорастёт сам")]
        public int projectilePoolSize = 96;

        [Header("Музыка")]
        [Tooltip("За сколько секунд затихает трек, когда его сменяет другой")]
        [Min(0f)] public float musicFadeOut = 0.6f;
        [Tooltip("За сколько секунд набирает громкость новый трек")]
        [Min(0f)] public float musicFadeIn = 1.2f;

        static GameBalance current;

        public static GameBalance Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<GameBalance>(ResourcePath);
                if (current == null)
                {
                    Debug.LogError($"[GameBalance] Нет ассета Resources/{ResourcePath}: используются значения по умолчанию, врагов нет.");
                    current = CreateInstance<GameBalance>();
                }
                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => current = null;
    }
}
