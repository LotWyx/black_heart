using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // ================= ПРИЗРАК =================

    // Спираль: пули вылетают друг за другом с поворачивающегося угла и сходятся к сердцу —
    // вместе они рисуют закрученный рукав. На низкой сложности в рукаве есть дыры, потом нет.
    // Синих и фиолетовых всё больше, но не больше половины.
    public class GhostSpiralPattern : AttackPattern
    {
        public const string ID = "ghost_spiral";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            int arms = ctx.Difficulty >= 1f ? 2 : 1;
            int count = ctx.RampInt(18f, 30f, 0f, 1.5f);
            float dt = ctx.Ramp(0.17f, 0.13f, 0f, 1.5f);
            float step = 0.42f * (Random.value < 0.5f ? 1 : -1);
            float duration = ctx.Ramp(3.8f, 2.9f, 0f, 1.5f);
            float spin = 0.5f * Mathf.Sign(step);
            bool holes = ctx.Difficulty < 0.6f;
            float special = Mathf.Min(0.5f, ctx.Ramp(0.15f, 0.5f, 0f, 1.5f));
            float a0 = Random.Range(0f, Mathf.PI * 2f);
            // типы по рукаву заранее: соседи синих в рукаве пропускаются
            var types = new List<ProjectileType>();
            for (int i = 0; i < count; i++) types.Add(ctx.RollType(special));
            var skip = AttackContext.BlueGaps(types, false);

            for (int i = 0; i < count; i++)
            {
                if (!(holes && i % 4 == 3) && !skip[i])   // дыра в рукаве
                {
                    for (int arm = 0; arm < arms; arm++)
                    {
                        float a = a0 + i * step + arm * Mathf.PI;
                        float radius = ctx.Box.DistanceToEdge(ctx.Target, ctx.Dir(a)) + 0.6f;
                        ctx.Spawn(types[i], new SpiralMotion(ctx.Target, radius, a, radius / duration, spin));
                    }
                }
                yield return new WaitForSeconds(dt);
            }
            yield return new WaitForSeconds(duration * 0.5f);
        }
    }

    // Волна: пули вылетают из угла (слева сверху или справа снизу) и идут вдоль рамки синусоидой.
    // Голова доходит до стенки — волна замирает, после постоянной паузы пули по очереди бросаются в сердце.
    // Синие разбросаны по волне. Со сложностью волна быстрее.
    public class GhostWavePattern : AttackPattern
    {
        public const string ID = "ghost_wave";
        public override string Id => ID;
        const float Pause = 0.9f, SpawnStep = 0.26f, DashSpeed = 6.5f;

        public override IEnumerator Run(AttackContext ctx)
        {
            Rect r = ctx.Box.Current;
            bool topLeft = Random.value < 0.5f;
            Vector2 dir = topLeft ? Vector2.right : Vector2.left;
            Vector2 start = topLeft ? new Vector2(r.xMin - 0.5f, r.yMax - 1.3f) : new Vector2(r.xMax + 0.5f, r.yMin + 1.3f);
            float speed = ctx.Ramp(2.5f, 5.5f, 0f, 1.5f);
            float travel = r.width - 0.2f;               // голова доходит до противоположной стенки
            float headTime = travel / speed;
            int count = Mathf.Min(ctx.RampInt(10f, 16f, 0f, 1.5f), Mathf.FloorToInt(headTime / SpawnStep));
            var types = ctx.Types(count, Random.Range(3, 6), ctx.Ramp(0f, 0.3f, 0.2f, 1.4f));

            var skip = AttackContext.BlueGaps(types, false);   // в волне по бокам от синих — пусто
            for (int i = 0; i < count; i++)
            {
                if (skip[i]) { yield return new WaitForSeconds(SpawnStep); continue; }
                float spawnTime = i * SpawnStep;
                float stopAt = headTime - spawnTime;                     // все замирают в один момент
                float releaseAt = stopAt + Pause + i * 0.07f             // и бросаются по очереди,
                                  + (types[i] == ProjectileType.Blue ? 0.5f : 0f); // синие — с задержкой
                // бросок от точки остановки к сердцу — не быстрее времени реакции
                Vector2 stopPoint = start + dir * speed * stopAt;
                float dash = ctx.CapSpeed(DashSpeed, Vector2.Distance(stopPoint, ctx.Target));
                ctx.Spawn(types[i], new WaveMotion(start, dir, speed, 0.9f, 3.5f, stopAt, releaseAt, ctx.Target, dash));
                yield return new WaitForSeconds(SpawnStep);
            }
            yield return new WaitForSeconds(Mathf.Max(0f, headTime - count * SpawnStep) + Pause);
        }
    }

    // Маятник: призраки вылетают с любой стороны и спускаются к сердцу, раскачиваясь поперёк.
    // Сложность сначала увеличивает размах, потом скорость раскачки; спуск постоянный.
    // Сначала по одному, потом случайно 2–4; если их 2 и больше — один обязательно синий.
    public class PendulumPattern : AttackPattern
    {
        public const string ID = "pendulum";
        public override string Id => ID;
        public const string Spirit = "Spirit";

        public override IEnumerator Run(AttackContext ctx)
        {
            int volleys = ctx.Difficulty >= 1f ? 2 : 1;
            for (int v = 0; v < volleys; v++)
            {
                int count = ctx.Difficulty < 0.5f ? 1 : Random.Range(2, 2 + 1 + ctx.RampInt(0f, 2f, 0.5f, 1.5f));
                var types = ctx.Types(count, count >= 2 ? 1 : 0, ctx.Ramp(0.1f, 0.35f));
                types.Sort((a, b) => (a == ProjectileType.Blue).CompareTo(b == ProjectileType.Blue));   // синий — последним
                for (int i = 0; i < count; i++)
                {
                    if (types[i] == ProjectileType.Blue && i > 0) yield return new WaitForSeconds(0.6f);
                    SpawnOne(ctx, types[i]);
                    yield return new WaitForSeconds(0.5f);
                }
                yield return new WaitForSeconds(ctx.Ramp(3.5f, 2.5f));
            }
        }

        public static void SpawnOne(AttackContext ctx, ProjectileType type)
        {
            Vector2[] sides = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            Vector2 dir = sides[Random.Range(0, sides.Length)];
            Vector2 start = ctx.PointOutsideBox(dir, 0.8f);
            float amplitude = ctx.Ramp(1.0f, 3.0f, 0f, 0.8f);
            float frequency = ctx.Ramp(2.0f, 5.0f, 0.8f, 1.8f);
            // по вертикали размах ограничен шириной рамки, по горизонтали — её высотой
            if (dir.x != 0f) amplitude = Mathf.Min(amplitude, ctx.Box.Current.height / 2f - 0.8f);
            ctx.Spawn(type, new PendulumMotion(start, ctx.Target, 1.3f, amplitude, frequency, Random.Range(0f, Mathf.PI * 2f)), Spirit);
        }
    }

    // Муха: пули мечутся короткими резкими бросками с зависаниями, постепенно приближаясь к сердцу
    public class FlyPattern : AttackPattern
    {
        public const string ID = "fly";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            int count = ctx.RampInt(3f, 7f, 0f, 1.5f);
            float interval = ctx.Ramp(0.9f, 0.5f, 0f, 1.5f);
            ctx.PlanTypes(count);
            for (int i = 0; i < count; i++)
            {
                SpawnOne(ctx, ctx.NextType());
                yield return new WaitForSeconds(interval);
            }
        }

        public static void SpawnOne(AttackContext ctx, ProjectileType type)
        {
            Vector2 dir = ctx.DirectionFor(type);
            float hop = ctx.Ramp(0.65f, 0.45f, 0f, 1.5f);   // такт броска: на последнем рывке к сердцу должно хватить реакции
            ctx.Spawn(type, new FlyMotion(ctx.PointOutsideBox(dir), ctx.Target, 6, hop, 2f));
        }
    }

    // Стандартная атака Призрака: короткие спирали, маятники и мухи вперемешку
    public class GhostStandardPattern : AttackPattern
    {
        public const string ID = "ghost_std";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            float duration = ctx.Ramp(8f, 12f, 0f, 1.5f);
            float interval = ctx.Ramp(1.5f, 0.8f, 0f, 1.5f);
            float special = Mathf.Min(0.5f, ctx.Ramp(0.2f, 0.4f));
            for (float t = 0f; t < duration; t += interval)
            {
                float roll = Random.value;
                if (roll < 0.35f)
                {
                    // короткий рукав спирали
                    int n = ctx.RampInt(5f, 9f);
                    float a0 = Random.Range(0f, Mathf.PI * 2f), step = 0.45f * (Random.value < 0.5f ? 1 : -1);
                    float dur = ctx.Ramp(3.8f, 3f);
                    var armType = ctx.RollType(special);   // рукав одного цвета: синие не вплотную к белым
                    for (int i = 0; i < n; i++)
                    {
                        float a = a0 + i * step;
                        float radius = ctx.Box.DistanceToEdge(ctx.Target, ctx.Dir(a)) + 0.6f;
                        ctx.Spawn(armType, new SpiralMotion(ctx.Target, radius, a, radius / dur, 0.5f * Mathf.Sign(step)));
                        yield return new WaitForSeconds(0.12f);
                    }
                }
                else if (roll < 0.65f) PendulumPattern.SpawnOne(ctx, ctx.RollType(special));
                else
                {
                    int n = Random.value < 0.5f ? 1 : 2;
                    for (int i = 0; i < n; i++) FlyPattern.SpawnOne(ctx, ctx.RollType(special));
                }
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
