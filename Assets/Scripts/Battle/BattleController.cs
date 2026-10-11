using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Цикл боя: появление врага → (атака → защита) × N → победа → новый враг.
    public class BattleController : MonoBehaviour
    {
        public List<EnemyDefinition> enemies = EnemyRoster.CreateDefaults();

        [Header("Фазы")]
        public float attackWindow = 3f;          // сколько секунд даётся на удар по врагу
        public float defenseTimeout = 25f;       // страховка, если пуля где-то застряла
        public float topHudSpace = 0.4f;         // отступ сверху (надписи HUD в углах, враг по центру)

        [Header("Сложность")]
        public float difficultyPerTurn = 0.085f; // ~12 ходов до «середины»
        public float maxDifficulty = 1.6f;

        [Header("Баланс пуль")]
        [Tooltip("Доля особых (фиолетовых и синих) пуль в атаке: в начале игры и к середине")]
        public float specialShareEasy = 0.2f;
        public float specialShareMid = 0.3f;
        [Tooltip("Минимум секунд от входа пули в рамку до сердца — быстрые пули летят только издалека")]
        public float minReactionTime = 1.2f;
        [Tooltip("Минимальный угол между синими и остальными пулями")]
        public float minBlueAngle = 50f;

        [Header("Победа")]
        public int healOnVictory = 2;

        public BattlePhase Phase { get; private set; }
        public Enemy CurrentEnemy { get; private set; }
        public int Turn { get; private set; }
        public float Difficulty => Mathf.Min(maxDifficulty, Turn * difficultyPerTurn);
        public float AttackTimeLeft { get; private set; }

        bool enemyHitThisPhase;
        int lastEnemyIndex = -1;
        GameManager gm => GameManager.I;

        public void Begin()
        {
            Stop();
            Turn = 0;
            StartCoroutine(BattleLoop());
        }

        public void Stop()
        {
            StopAllCoroutines();
            SetPhase(BattlePhase.None);
        }

        public void DespawnEnemy()
        {
            if (CurrentEnemy != null) Destroy(CurrentEnemy.gameObject);
            CurrentEnemy = null;
        }

        // Для обучения и особых сцен: поставить конкретного врага и управлять фазами вручную
        public Enemy PlaceEnemy(EnemyDefinition def)
        {
            DespawnEnemy();
            CurrentEnemy = Enemy.Create(def, EnemyHome());
            GameEvents.RaiseEnemySpawned(CurrentEnemy);
            return CurrentEnemy;
        }

        public void ForcePhase(BattlePhase phase) => SetPhase(phase);

        void SetPhase(BattlePhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            GameEvents.RaisePhaseChanged(phase);
        }

        IEnumerator BattleLoop()
        {
            while (true)
            {
                yield return SpawnEnemy();

                // Первая фаза атаки — только визуальная, бить нельзя
                SetPhase(BattlePhase.Intro);
                yield return new WaitForSeconds(1.2f);

                while (true)
                {
                    yield return DefensePhase();
                    yield return AttackPhase();
                    if (CurrentEnemy.IsDead) break;
                }

                yield return VictoryPhase();
            }
        }

        // Враг по центру свободного места над рамкой
        Vector2 EnemyHome()
        {
            Rect view = gm.Pixel.ViewRect;
            Rect box = gm.Box.NormalRect();
            float top = view.yMax - topHudSpace, bottom = box.yMax + 0.2f;
            return new Vector2(view.center.x, (top + bottom) / 2f);
        }

        IEnumerator SpawnEnemy()
        {
            DespawnEnemy();
            int index = Random.Range(0, enemies.Count);
            if (enemies.Count > 1 && index == lastEnemyIndex) index = (index + 1) % enemies.Count;
            lastEnemyIndex = index;

            CurrentEnemy = Enemy.Create(enemies[index], EnemyHome());
            GameEvents.RaiseEnemySpawned(CurrentEnemy);
            yield return CurrentEnemy.Appear();
        }

        IEnumerator DefensePhase()
        {
            SetPhase(BattlePhase.ToDefense);
            yield return CurrentEnemy.AttackWindup();

            gm.Box.Expand();
            yield return CurrentEnemy.FlyUp(gm.Pixel.ViewRect.yMax + 3f);
            while (gm.Box.IsAnimating) yield return null;

            SetPhase(BattlePhase.Defense);
            var ctx = new AttackContext
            {
                Difficulty = Difficulty,
                Box = gm.Box,
                Target = gm.Box.Current.center,
                Projectiles = gm.Projectiles,
                Enemy = CurrentEnemy.Definition,
                SpecialShare = Mathf.Lerp(specialShareEasy, specialShareMid, Mathf.Clamp01(Difficulty)),
                MinReactionTime = minReactionTime,
                MinBlueAngle = minBlueAngle
            };
            yield return PatternLibrary.Create(CurrentEnemy.Definition.PickPattern()).Run(ctx);

            float timeout = defenseTimeout;
            while (gm.Projectiles.Count > 0 && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            gm.Projectiles.ClearAll();
            Turn++;

            SetPhase(BattlePhase.ToAttack);
            gm.Box.Shrink();
            yield return CurrentEnemy.Return();
            while (gm.Box.IsAnimating) yield return null;
        }

        IEnumerator AttackPhase()
        {
            SetPhase(BattlePhase.Attack);
            enemyHitThisPhase = false;
            AttackTimeLeft = attackWindow;
            while (AttackTimeLeft > 0 && !enemyHitThisPhase)
            {
                AttackTimeLeft -= Time.deltaTime;
                yield return null;
            }

            if (enemyHitThisPhase)
            {
                yield return new WaitForSeconds(0.7f);
            }
            else
            {
                SetPhase(BattlePhase.Laugh);
                yield return CurrentEnemy.Laugh();
            }
        }

        IEnumerator VictoryPhase()
        {
            SetPhase(BattlePhase.Victory);
            yield return CurrentEnemy.Defeat();
            gm.Score.Add(gm.Score.enemyDefeated, CurrentEnemy.Position);
            gm.Player.Heal(healOnVictory);
            DespawnEnemy();
            yield return new WaitForSeconds(1f);
        }

        // Удар по врагу засчитывается один раз за фазу атаки
        public void HandleSlice(Vector2 a, Vector2 b)
        {
            if (Phase != BattlePhase.Attack || enemyHitThisPhase || CurrentEnemy == null) return;
            if (Geometry.DistancePointSegment(CurrentEnemy.Position, a, b) > CurrentEnemy.HitRadius) return;
            enemyHitThisPhase = true;
            CurrentEnemy.TakeDamage(1);
        }
    }
}
