using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Пули наворачивают петли, но в итоге летят прямо в сердце
    public class ZigzagPattern : AttackPattern
    {
        public const string ID = "zigzag";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            int count = ctx.Count(3, 7);
            float interval = ctx.Lerp(0.8f, 0.4f);
            ctx.PlanTypes(count);

            for (int i = 0; i < count; i++)
            {
                var type = ctx.NextType();
                Vector2 dir = ctx.DirectionFor(type);
                // Петли удлиняют путь — дистанция больше прямой
                float distance = ctx.Box.DistanceToEdge(ctx.Target, dir) * 1.3f;
                float speed = ctx.PickSpeed(1.6f, 3f, distance);
                ctx.Spawn(type, new ZigzagMotion(ctx.PointOutsideBox(dir), ctx.Target, speed, 0.7f, 6f));
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
