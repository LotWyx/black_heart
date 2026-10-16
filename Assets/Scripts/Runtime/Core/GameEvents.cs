using System;
using UnityEngine;

namespace BlackHeart
{
    // Шина игровых событий. UI, звуки и эффекты подписываются сюда,
    // не трогая игровую логику.
    public static class GameEvents
    {
        // Игра
        public static event Action OnGameStarted;
        public static event Action<int> OnGameOver;                     // итоговый счёт
        public static event Action<BattlePhase> OnPhaseChanged;
        public static event Action<bool> OnPauseChanged;                // true — пауза, false — продолжение

        // Игрок
        public static event Action<int, int> OnPlayerTakeDamage;        // урон, текущее HP
        public static event Action<int, int> OnPlayerHealed;            // сколько вылечено, текущее HP
        public static event Action OnPlayerDied;
        public static event Action OnSoulCracked;
        public static event Action OnSoulShattered;

        // Счёт
        public static event Action<int, int, Vector2> OnScoreChanged;   // изменение, итог, мировая позиция
        public static event Action<Vector2> OnCleanDefense;             // защита без урона; позиция надписи с очками

        // Враги
        public static event Action<Enemy> OnEnemySpawned;
        public static event Action<Enemy> OnEnemyAttack;
        public static event Action<Enemy, int> OnEnemyTakeDamage;
        public static event Action<Enemy> OnEnemyLaughed;
        public static event Action<Enemy> OnEnemyDefeated;

        // Пули
        public static event Action<Projectile> OnProjectileSliced;
        public static event Action<Projectile> OnProjectileSplit;
        public static event Action<Projectile> OnProjectileHitPlayer;

        public static void RaiseGameStarted() => OnGameStarted?.Invoke();
        public static void RaiseGameOver(int score) => OnGameOver?.Invoke(score);
        public static void RaisePhaseChanged(BattlePhase phase) => OnPhaseChanged?.Invoke(phase);
        public static void RaisePauseChanged(bool paused) => OnPauseChanged?.Invoke(paused);

        public static void RaisePlayerTakeDamage(int amount, int hp) => OnPlayerTakeDamage?.Invoke(amount, hp);
        public static void RaisePlayerHealed(int amount, int hp) => OnPlayerHealed?.Invoke(amount, hp);
        public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
        public static void RaiseSoulCracked() => OnSoulCracked?.Invoke();
        public static void RaiseSoulShattered() => OnSoulShattered?.Invoke();

        public static void RaiseScoreChanged(int delta, int total, Vector2 pos) => OnScoreChanged?.Invoke(delta, total, pos);
        public static void RaiseCleanDefense(Vector2 pos) => OnCleanDefense?.Invoke(pos);

        public static void RaiseEnemySpawned(Enemy e) => OnEnemySpawned?.Invoke(e);
        public static void RaiseEnemyAttack(Enemy e) => OnEnemyAttack?.Invoke(e);
        public static void RaiseEnemyTakeDamage(Enemy e, int amount) => OnEnemyTakeDamage?.Invoke(e, amount);
        public static void RaiseEnemyLaughed(Enemy e) => OnEnemyLaughed?.Invoke(e);
        public static void RaiseEnemyDefeated(Enemy e) => OnEnemyDefeated?.Invoke(e);

        public static void RaiseProjectileSliced(Projectile p) => OnProjectileSliced?.Invoke(p);
        public static void RaiseProjectileSplit(Projectile p) => OnProjectileSplit?.Invoke(p);
        public static void RaiseProjectileHitPlayer(Projectile p) => OnProjectileHitPlayer?.Invoke(p);

        // При выключенном Domain Reload статические подписки переживают Play Mode — очищаем их
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSubscriptions()
        {
            OnGameStarted = null; OnGameOver = null; OnPhaseChanged = null; OnPauseChanged = null;
            OnPlayerTakeDamage = null; OnPlayerHealed = null; OnPlayerDied = null;
            OnSoulCracked = null; OnSoulShattered = null;
            OnScoreChanged = null; OnCleanDefense = null;
            OnEnemySpawned = null; OnEnemyAttack = null; OnEnemyTakeDamage = null;
            OnEnemyLaughed = null; OnEnemyDefeated = null;
            OnProjectileSliced = null; OnProjectileSplit = null; OnProjectileHitPlayer = null;
        }
    }
}
