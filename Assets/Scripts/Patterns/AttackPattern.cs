using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Базовый паттерн атаки: определяет, откуда и как летят пули.
    // Типы пуль паттерн получает от контекста (ctx.PlanTypes / ctx.NextType),
    // поэтому любой тип может лететь по любому паттерну.
    // Новый паттерн: унаследуйтесь, реализуйте Run и зарегистрируйте в PatternLibrary.
    public abstract class AttackPattern
    {
        public abstract string Id { get; }
        public abstract IEnumerator Run(AttackContext ctx);
    }

    // Всё, что нужно паттерну: сложность, рамка, цель, правила баланса и создание пуль
    public class AttackContext
    {
        public float Difficulty;               // 0 — начало игры, 1 — «середина», дальше сложнее
        public BattleBox Box;
        public Vector2 Target;                 // позиция сердца
        public ProjectileManager Projectiles;
        public EnemyDefinition Enemy;

        public float SpecialShare = 0.25f;     // доля особых пуль — строго от общего числа
        public float MinReactionTime = 1.2f;   // минимум секунд от края рамки до сердца
        public float MinBlueAngle = 50f;       // синие не летят рядом с остальными

        readonly Queue<ProjectileType> deck = new Queue<ProjectileType>();

        // Значение от «начала игры» к «середине» по текущей сложности
        public float Lerp(float easy, float mid) => Mathf.LerpUnclamped(easy, mid, Difficulty);
        public int Count(int easy, int mid) => Mathf.Max(1, Mathf.RoundToInt(Lerp(easy, mid)));

        // ---------- типы пуль ----------

        // Готовит типы для count пуль: особых ровно round(count * SpecialShare).
        // На низкой сложности особые пули в одной атаке — только одного вида.
        public void PlanTypes(int count, bool allowBlue = true)
        {
            deck.Clear();
            var list = new List<ProjectileType>(count);
            int specials = Mathf.RoundToInt(count * SpecialShare);
            bool mixed = Difficulty >= 0.6f;
            ProjectileType single = Enemy.PickSpecialType(allowBlue);
            for (int i = 0; i < specials; i++)
                list.Add(mixed ? Enemy.PickSpecialType(allowBlue) : single);
            while (list.Count < count) list.Add(ProjectileType.White);

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            foreach (var t in list) deck.Enqueue(t);
        }

        public ProjectileType NextType() => deck.Count > 0 ? deck.Dequeue() : ProjectileType.White;

        // Для колец: синее кольцо целиком синее и не смешивается с остальными
        public bool RollBlueRing() => Random.value < SpecialShare * Enemy.BlueRatio;

        public Projectile Spawn(ProjectileType type, IProjectileMotion motion) => Projectiles.Spawn(type, motion);

        // ---------- скорость и направление ----------

        // Медленные пули остаются всегда, но с ростом сложности встречаются реже.
        // Скорость ограничена дистанцией: пуля не может долететь быстрее MinReactionTime.
        public float PickSpeed(float slow, float fast, float distance)
        {
            float bias = Mathf.Lerp(2.5f, 0.6f, Mathf.Clamp01(Difficulty / 1.5f));   // >1 — чаще медленные
            float speed = Mathf.Lerp(slow, fast, Mathf.Pow(Random.value, bias));
            return Mathf.Min(speed, distance / MinReactionTime);
        }

        public Vector2 RandomDirection()
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        // Направление прилёта, разнесённое по углу с пулями «несовместимого» цвета
        // (синие отдельно от белых и фиолетовых)
        public Vector2 DirectionFor(ProjectileType type)
        {
            bool blue = type == ProjectileType.Blue;
            Vector2 best = RandomDirection();
            float bestAngle = -1f;
            for (int i = 0; i < 16; i++)
            {
                Vector2 d = RandomDirection();
                float minAngle = 180f;
                foreach (var p in Projectiles.Active)
                {
                    if (p == null || (p.Type == ProjectileType.Blue) == blue) continue;
                    Vector2 rel = p.Position - Target;
                    if (rel.sqrMagnitude > 0.01f) minAngle = Mathf.Min(minAngle, Vector2.Angle(d, rel));
                }
                if (minAngle >= MinBlueAngle) return d;
                if (minAngle > bestAngle) { bestAngle = minAngle; best = d; }
            }
            return best;
        }

        // Точка за рамкой по направлению от сердца
        public Vector2 PointOutsideBox(Vector2 dir, float margin = 0.6f)
        {
            return Target + dir * (Box.DistanceToEdge(Target, dir) + margin);
        }

        // Случайная точка в рамке не ближе minDistance к сердцу (или самая дальняя из попыток)
        public Vector2 RandomPointInBox(float minDistance, float edgeMargin)
        {
            Rect r = Box.Current;
            Vector2 best = Target;
            float bestDist = -1f;
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector2(Random.Range(r.xMin + edgeMargin, r.xMax - edgeMargin),
                                    Random.Range(r.yMin + edgeMargin, r.yMax - edgeMargin));
                float d = Vector2.Distance(p, Target);
                if (d >= minDistance) return p;
                if (d > bestDist) { bestDist = d; best = p; }
            }
            return best;
        }
    }
}
