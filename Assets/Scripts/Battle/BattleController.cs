using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Цикл боя: появление врага → (защита → атака) × N → победа → новый враг.
    // В одной фазе защиты враг проводит несколько атак подряд из своего набора (паттерны общей библиотеки).
    public class BattleController : MonoBehaviour
    {
        // Настройки боя задаются только кодом: [NonSerialized], чтобы устаревшие значения,
        // сохранённые в сцене (старые враги с 2–3 HP и старыми атаками), их не перекрывали.
        [System.NonSerialized] public List<EnemyDefinition> enemies = EnemyRoster.CreateDefaults();

        [System.NonSerialized] public float attackWindow = 3f;          // сколько секунд даётся на удар по врагу
        [System.NonSerialized] public float defenseTimeout = 25f;       // страховка, если пуля где-то застряла
        [System.NonSerialized] public float topHudSpace = 0.4f;         // отступ сверху (надписи HUD в углах, враг по центру)

        [System.NonSerialized] public float difficultyPerTurn = 0.05f;        // за каждую фазу защиты в забеге
        [System.NonSerialized] public float difficultyPerFightTurn = 0.18f;   // за каждую фазу защиты против текущего врага
        [System.NonSerialized] public float maxDifficulty = 2f;

        [System.NonSerialized] public int attacksPerPhase = 2;   // длина фазы защиты не растёт со сложностью
        [System.NonSerialized] public float pauseBetweenAttacks = 0.4f;

        [System.NonSerialized] public float specialShareEasy = 0.2f;
        [System.NonSerialized] public float specialShareMid = 0.3f;
        [System.NonSerialized] public float minReactionTime = 1.5f;

        [System.NonSerialized] public int healOnVictory = 2;

        public BattlePhase Phase { get; private set; }
        public Enemy CurrentEnemy { get; private set; }
        public int Turn { get; private set; }
        public int FightTurn { get; private set; }   // фаз защиты против текущего врага
        public float Difficulty => Mathf.Min(maxDifficulty, Turn * difficultyPerTurn + FightTurn * difficultyPerFightTurn);
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
            if (gm != null && gm.Projectiles != null) gm.Projectiles.BlueRush = false;
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
            FightTurn = 0;

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
            gm.Projectiles.BlueRush = true;
            var ctx = new AttackContext
            {
                Difficulty = Difficulty,
                Box = gm.Box,
                Target = gm.Box.Current.center,
                Projectiles = gm.Projectiles,
                Enemy = CurrentEnemy.Definition,
                SpecialShare = Mathf.Lerp(specialShareEasy, specialShareMid, Mathf.Clamp01(Difficulty)),
                MinReactionTime = minReactionTime,
                Host = this
            };
            // Серия атак: следующая начинается, когда поле почти расчищено
            int attacks = attacksPerPhase;
            string last = null;
            for (int i = 0; i < attacks; i++)
            {
                string id = CurrentEnemy.Definition.PickPattern();
                for (int tries = 0; tries < 4 && id == last; tries++) id = CurrentEnemy.Definition.PickPattern();
                last = id;
                yield return PatternLibrary.Create(id).Run(ctx);
                if (i < attacks - 1)
                {
                    yield return ctx.WaitFieldBelow(3, 2.5f);   // не ждём полной расчистки: фаза защиты не затягивается
                    yield return new WaitForSeconds(pauseBetweenAttacks);
                }
            }

            float timeout = defenseTimeout;
            while (gm.Projectiles.Count > 0 && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            gm.Projectiles.BlueRush = false;
            gm.Projectiles.ClearAll();
            Turn++;
            FightTurn++;

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
