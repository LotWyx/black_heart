using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Цикл боя: появление врага → (защита → атака) × N → победа → новый враг.
    // В одной фазе защиты враг проводит несколько атак подряд из своего набора (паттерны общей библиотеки).
    public class BattleController : MonoBehaviour
    {
        // Все настройки боя лежат в ассете GameBalance (Resources/Data), здесь — только доступ к ним
        static GameBalance B => GameBalance.Current;

        public List<EnemyDefinition> enemies => B.enemies;
        public float attackWindow => B.attackWindow;
        public int missedAttackDamage => B.missedAttackDamage;
        public float defenseTimeout => B.defenseTimeout;
        public float topHudSpace => B.topHudSpace;
        public float difficultyPerTurn => B.difficultyPerTurn;
        public float difficultyPerFightTurn => B.difficultyPerFightTurn;
        public float maxDifficulty => B.maxDifficulty;
        public int attacksPerPhase => B.attacksPerPhase;
        public float pauseBetweenAttacks => B.pauseBetweenAttacks;
        public float specialShareEasy => B.specialShareEasy;
        public float specialShareMid => B.specialShareMid;
        public float minReactionTime => B.minReactionTime;
        public int healOnVictory => B.healOnVictory;

        public BattlePhase Phase { get; private set; }
        public Enemy CurrentEnemy { get; private set; }
        public int Turn { get; private set; }
        public int FightTurn { get; private set; }   // фаз защиты против текущего врага
        public float Difficulty => Mathf.Min(maxDifficulty, Turn * difficultyPerTurn + FightTurn * difficultyPerFightTurn);
        public float AttackTimeLeft { get; private set; }

        bool enemyHitThisPhase;
        int lastEnemyIndex = -1;
        readonly List<int> opening = new List<int>();   // первые бои забега: каждый враг по разу, в случайном порядке
        GameManager gm => GameManager.I;

        public void Begin()
        {
            Stop();
            Turn = 0;
            opening.Clear();
            for (int i = 0; i < enemies.Count; i++) opening.Insert(Random.Range(0, opening.Count + 1), i);
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
            // Сначала игроку показывают всех врагов по одному разу, дальше — случайный, но не тот же, что только что
            int index;
            if (opening.Count > 0)
            {
                index = opening[0];
                opening.RemoveAt(0);
            }
            else
            {
                index = Random.Range(0, enemies.Count);
                if (enemies.Count > 1 && index == lastEnemyIndex) index = (index + 1) % enemies.Count;
            }
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
            int hpBefore = gm.Player.Hp;
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
                    // не ждём полной расчистки, чтобы фаза защиты не затягивалась; но на низкой сложности
                    // следующая атака не наслаивается на хвост предыдущей
                    float d01 = Mathf.Clamp01(Difficulty);
                    yield return ctx.WaitFieldBelow(d01 < 0.6f ? 1 : 3, Mathf.Lerp(4f, 2.5f, d01));
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
            // Бонус за защиту без урона: «ЧИСТО!» и «+50» рядом с сердцем
            if (gm.Player.Hp >= hpBefore)
            {
                Vector2 at = gm.Player.Position + new Vector2(1.6f, 0.2f);
                GameEvents.RaiseCleanDefense(at);
                gm.Score.Add(gm.Score.cleanDefense, at);
            }
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
                // не атаковал — враг смеётся и сам наносит урон
                SetPhase(BattlePhase.Laugh);
                gm.Player.TakeDamage(missedAttackDamage);
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
