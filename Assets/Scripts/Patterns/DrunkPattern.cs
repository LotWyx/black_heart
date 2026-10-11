using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Пули летят по дуге и хаотично качаются вверх-вниз
    public class DrunkPattern : AttackPattern
    {
        public const string ID = "drunk";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            int count = ctx.Count(3, 8);
            float interval = ctx.Lerp(0.75f, 0.35f);
            float amplitude = ctx.Lerp(0.5f, 0.8f);
            ctx.PlanTypes(count);

            for (int i = 0; i < count; i++)
            {
                var type = ctx.NextType();
                Vector2 dir = ctx.DirectionFor(type);
                float distance = ctx.Box.DistanceToEdge(ctx.Target, dir) * 1.15f;
                float speed = ctx.PickSpeed(1.6f, 3f, distance);
                float arc = Random.Range(1.5f, 3f) * (Random.value < 0.5f ? 1 : -1);
                ctx.Spawn(type, new DrunkMotion(ctx.PointOutsideBox(dir), ctx.Target, speed, arc, amplitude));
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
