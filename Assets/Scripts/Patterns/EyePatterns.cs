using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // ================= ГЛАЗ =================

    // Серия бум-кругов из глаз: в каждом круге ровно один синий. Пули медленные.
    // Сложность добавляет круги и сокращает паузы между ними.
    public class EyeBoomPattern : AttackPattern
    {
        public const string ID = "eye_boom";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            int circles = ctx.RampInt(2f, 5f, 0f, 1.5f);
            float pause = ctx.Ramp(1.7f, 0.7f, 0f, 1.5f);
            for (int c = 0; c < circles; c++)
            {
                var types = ctx.Types(ctx.RampInt(6, 9, 0f, 1.5f), 1, ctx.Ramp(0.1f, 0.3f));
                ctx.Host.StartCoroutine(BoomCirclePattern.Burst(ctx, BoomCirclePattern.RandomCenter(ctx), types, 1.4f, 2f));
                yield return new WaitForSeconds(pause);
            }
            yield return new WaitForSeconds(BoomCirclePattern.HoldTime + BoomCirclePattern.ShrinkTime);
        }
    }

    // Пятиглаз: синий глаз в центре, вокруг крутятся 4 белых/фиолетовых; вся группа летит «пьяно».
    // Отдельной атакой не бывает — появляется внутри стандартной.
    public static class FiveEye
    {
        public static void Spawn(AttackContext ctx)
        {
            Vector2 dir = ctx.RandomDirection();
            Vector2 start = ctx.PointOutsideBox(dir, 1.6f);
            float speed = ctx.Ramp(1.3f, 1.9f, 0f, 1.5f);
            float arc = Random.Range(1.2f, 2.5f) * (Random.value < 0.5f ? 1 : -1);
            var center = new DrunkMotion(start, ctx.Target, speed, arc, 0.6f);
            ctx.Spawn(ProjectileType.Blue, center);

            float purpleShare = ctx.Ramp(0.1f, 0.4f, 0f, 1.5f);
            float spin = 2.4f * (Random.value < 0.5f ? 1 : -1);
            float a0 = Random.Range(0f, Mathf.PI * 2f);
            for (int i = 0; i < 4; i++)
            {
                var type = Random.value < purpleShare ? ProjectileType.Purple : ProjectileType.White;
                ctx.Spawn(type, new OrbitMotion(center, ctx.Target, 1.05f, a0 + i * Mathf.PI / 2f, spin));
            }
        }
    }

    // Сужающийся крутящийся прямоугольник из глаз вокруг сердца. Фиолетовых нет, синих 1–2 —
    // они мешают просто изрешетить всё подряд. Сужение медленное, вращение ускоряется со сложностью.
    public class EyeRectPattern : AttackPattern
    {
        public const string ID = "eye_rect";
        public override string Id => ID;
        const float Spacing = 1.0f;

        public override IEnumerator Run(AttackContext ctx)
        {
            Rect r = ctx.Box.Current;
            float hw = Mathf.Min(5.5f, r.width / 2f - 1.2f), hh = Mathf.Min(3.1f, r.height / 2f - 0.6f);
            float shrinkTime = ctx.Ramp(11f, 7.5f, 0f, 1.5f);
            float spin = ctx.Ramp(0.3f, 1.3f, 0f, 1.6f) * (Random.value < 0.5f ? 1 : -1);

            // точки по контуру прямоугольника с равным шагом
            var points = new List<Vector2>();
            var corners = new[] { new Vector2(-hw, -hh), new Vector2(hw, -hh), new Vector2(hw, hh), new Vector2(-hw, hh) };
            for (int side = 0; side < 4; side++)
            {
                Vector2 a = corners[side], b = corners[(side + 1) % 4];
                int steps = Mathf.Max(1, Mathf.RoundToInt(Vector2.Distance(a, b) / Spacing));
                for (int j = 0; j < steps; j++) points.Add(Vector2.Lerp(a, b, (float)j / steps));
            }
            var types = ctx.Types(points.Count, Random.Range(1, 3), 0f);
            for (int i = 0; i < points.Count; i++)
                ctx.Spawn(types[i], new RectRingMotion(ctx.Target, points[i], 0f, spin, shrinkTime));
            yield return new WaitForSeconds(shrinkTime * 0.8f);
        }
    }

    // Стандартная атака Глаза: пятиглазы (не больше двух в секунду), глаза по 1–3 по спирали, «пьяные» глаза.
    // Со сложностью пятиглазы появляются чаще.
    public class EyeStandardPattern : AttackPattern
    {
        public const string ID = "eye_std";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            float duration = ctx.Ramp(8f, 12f, 0f, 1.5f);
            float interval = ctx.Ramp(1.3f, 0.7f, 0f, 1.5f);
            float fiveChance = ctx.Ramp(0.25f, 0.55f, 0f, 1.5f);
            float special = ctx.Ramp(0.2f, 0.35f);
            float lastFive = -10f;

            for (float t = 0f; t < duration; t += interval)
            {
                float roll = Random.value;
                if (roll < fiveChance && t - lastFive >= 0.5f)
                {
                    FiveEye.Spawn(ctx);
                    lastFive = t;
                }
                else if (roll < fiveChance + (1f - fiveChance) * 0.5f)
                {
                    // 1–3 глаза по спирали с соседних углов
                    int n = Random.Range(1, 4);
                    float a0 = Random.Range(0f, Mathf.PI * 2f);
                    float spin = ctx.Ramp(0.9f, 1.5f) * (Random.value < 0.5f ? 1 : -1);
                    float dur = ctx.Ramp(4.5f, 3.4f, 0f, 1.5f);
                    for (int i = 0; i < n; i++)
                    {
                        float a = a0 + i * 0.45f;
                        float radius = ctx.Box.DistanceToEdge(ctx.Target, ctx.Dir(a)) + 0.6f;
                        ctx.Spawn(ctx.RollType(special), new SpiralMotion(ctx.Target, radius, a, radius / dur, spin));
                    }
                }
                else
                {
                    var type = ctx.RollType(special);
                    Vector2 dir = ctx.DirectionFor(type);
                    float distance = ctx.Box.DistanceToEdge(ctx.Target, dir) * 1.15f;
                    float speed = ctx.PickSpeed(1.6f, 3f, distance);
                    ctx.Spawn(type, new DrunkMotion(ctx.PointOutsideBox(dir), ctx.Target, speed,
                        Random.Range(1.5f, 3f) * (Random.value < 0.5f ? 1 : -1), ctx.Ramp(0.5f, 0.8f)));
                }
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
