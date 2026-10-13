using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Бум-круги общего вида: кольцо либо целиком синее, либо бело-фиолетовое
    public class BoomCirclePattern : AttackPattern
    {
        public override string Summary =>
            "Бум-круги: метка в случайном месте рамки сжимается и выпускает кольцо пуль; пули летят от центра, потом " +
            "сворачивают к сердцу. Кольцо либо целиком синее, либо бело-фиолетовое (доля особых — из баланса игры). " +
            "Сейчас ни одному врагу не назначена: целиком синее кольцо противоречит правилу «без полностью синих атак».";

        [Tooltip("Сколько кругов подряд")]
        public Ramp circles = new Ramp(1f, 2f);
        [Tooltip("Пуль в круге")]
        public Ramp count = new Ramp(4f, 8f);
        [Tooltip("Секунд после выстрела до следующего круга")]
        public Ramp pause = new Ramp(2f, 1.4f);
        public BoomBurst burst = new BoomBurst();

        public override IEnumerator Run(AttackContext ctx)
        {
            int rings = Mathf.Max(1, ctx.Int(circles));
            for (int c = 0; c < rings; c++)
            {
                int n = Mathf.Max(1, ctx.Int(count));
                var types = new List<ProjectileType>();
                if (ctx.RollBlueRing())
                {
                    for (int i = 0; i < n; i++) types.Add(ProjectileType.Blue);
                }
                else
                {
                    ctx.PlanTypes(n, allowBlue: false);
                    for (int i = 0; i < n; i++) types.Add(ctx.NextType());
                }
                yield return burst.Run(ctx, burst.RandomCenter(ctx), types);
                yield return new WaitForSeconds(ctx.Value(pause));
            }
        }
    }
}
