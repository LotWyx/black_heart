using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Цикл боя: появление врага → (защита → атака) × N → победа → новый враг.
    // В одной фазе защиты враг проводит несколько атак подряд из своего набора (префабы атак в EnemyDefinition.attacks).
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
        public float Difficulty => Testing ? testDifficulty
            : Mathf.Min(maxDifficulty, Turn * difficultyPerTurn + FightTurn * difficultyPerFightTurn);
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
            if (Testing)
            {
                Testing = false;
                if (gm != null && gm.Player != null) gm.Player.CannotDie = false;
            }
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
            var ctx = NewContext(CurrentEnemy.Definition);
            // Серия атак: следующая начинается, когда поле почти расчищено
            int attacks = attacksPerPhase;
            AttackPattern last = null;
            for (int i = 0; i < attacks; i++)
            {
                AttackPattern attack = CurrentEnemy.Definition.PickAttack();
                for (int tries = 0; tries < 4 && attack == last; tries++) attack = CurrentEnemy.Definition.PickAttack();
                last = attack;
                if (attack != null) yield return attack.Run(ctx);
                else
                {
                    Debug.LogWarning($"[BattleController] У врага «{CurrentEnemy.Definition.name}» нет атак (список attacks пуст).");
                    yield return new WaitForSeconds(1f);
                }
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

        AttackContext NewContext(EnemyDefinition enemy) => new AttackContext
        {
            Difficulty = Difficulty,
            Box = gm.Box,
            Target = gm.Box.Current.center,
            Projectiles = gm.Projectiles,
            Enemy = enemy,
            SpecialShare = Mathf.Lerp(specialShareEasy, specialShareMid, Mathf.Clamp01(Difficulty)),
            MinReactionTime = minReactionTime,
            Host = this
        };

        // ---------- проверка одной атаки (кнопка в инспекторе префаба атаки) ----------

        public bool Testing { get; private set; }
        float testDifficulty;

        // Гоняет одну атаку по кругу на заданной сложности: без врага, без фазы удара, сердце не умирает.
        // Обычный бой при этом останавливается; вернуться — через паузу в главное меню.
        public void TestAttack(AttackPattern attack, float difficulty)
        {
            if (attack == null) return;
            if (gm.CurrentState != GameManager.State.Playing || gm.Tutorial.IsRunning) gm.StartGame(false);
            Stop();
            gm.Projectiles.ClearAll();
            DespawnEnemy();
            Testing = true;
            testDifficulty = difficulty;
            StartCoroutine(TestLoop(attack));
        }

        IEnumerator TestLoop(AttackPattern attack)
        {
            // враг нужен только как источник соотношения синих и фиолетовых: берём того, у кого эта атака есть
            EnemyDefinition owner = enemies.Find(e => e != null && e.attacks.Contains(attack));
            if (owner == null) owner = enemies.Find(e => e != null);
            if (owner == null)
            {
                Debug.LogWarning("[BattleController] В GameBalance нет врагов — атаку не на ком проверить.");
                Stop();
                yield break;
            }
            gm.Player.CannotDie = true;
            while (gm.Box.IsAnimating) yield return null;
            gm.Box.Expand();
            while (gm.Box.IsAnimating) yield return null;
            SetPhase(BattlePhase.Defense);
            gm.Projectiles.BlueRush = true;
            while (true)
            {
                yield return attack.Run(NewContext(owner));
                float timeout = defenseTimeout;
                while (gm.Projectiles.Count > 0 && timeout > 0)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                gm.Projectiles.ClearAll();
                yield return new WaitForSeconds(1f);
            }
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
