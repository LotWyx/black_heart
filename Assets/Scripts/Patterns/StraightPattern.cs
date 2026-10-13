using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Пули вылетают из-за рамки и летят прямо в сердце, вращаясь вокруг своей оси.
    // Быстрые пули прилетают только с дальних сторон рамки.
    public class StraightPattern : AttackPattern
    {
        public const string ID = "straight";
        public override string Id => ID;

        public override IEnumerator Run(AttackContext ctx)
        {
            int count = ctx.Count(4, 10);
            float interval = ctx.Lerp(0.6f, 0.3f);
            ctx.PlanTypes(count);

            for (int i = 0; i < count; i++)
            {
                var type = ctx.NextType();
                Vector2 dir = ctx.DirectionFor(type);
                float distance = ctx.Box.DistanceToEdge(ctx.Target, dir);
                float speed = ctx.PickSpeed(2.2f, 4.5f, distance);
                ctx.Spawn(type, new StraightMotion(ctx.PointOffScreen(dir), ctx.Target, speed));
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
