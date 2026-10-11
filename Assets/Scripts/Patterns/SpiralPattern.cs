using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Кольцо пуль, сходящееся к сердцу по спирали.
    // В начале игры — 3 медленные пули, к середине — 10 пуль и меньше времени на сближение.
    public class SpiralPattern : AttackPattern
    {
        public const string ID = "spiral";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            int count = ctx.Count(3, 10);
            float duration = Mathf.Max(ctx.Lerp(5f, 3.6f), 3f);   // за сколько секунд кольцо сходится
            float angularSpeed = ctx.Lerp(0.9f, 1.6f) * (Random.value < 0.5f ? 1 : -1);
            int waves = ctx.Difficulty >= 0.8f ? 2 : 1;
            bool prevBlue = false;

            for (int w = 0; w < waves; w++)
            {
                // Синее кольцо целиком синее; между кольцами разного цвета пауза больше
                bool blue = ctx.RollBlueRing();
                if (w > 0) yield return new WaitForSeconds(ctx.Lerp(2f, 1.3f) * (blue != prevBlue ? 1.6f : 1f));
                prevBlue = blue;
                if (!blue) ctx.PlanTypes(count, allowBlue: false);

                float offset = Random.Range(0f, Mathf.PI * 2f);
                for (int i = 0; i < count; i++)
                {
                    // Каждая пуля стартует сразу за рамкой; скорость подобрана так,
                    // чтобы всё кольцо сошлось к сердцу одновременно
                    float angle = offset + Mathf.PI * 2f * i / count;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float radius = ctx.Box.DistanceToEdge(ctx.Target, dir) + 0.6f;
                    var type = blue ? ProjectileType.Blue : ctx.NextType();
                    ctx.Spawn(type, new SpiralMotion(ctx.Target, radius, angle, radius / duration, angularSpeed));
                }
            }
        }
    }
}
