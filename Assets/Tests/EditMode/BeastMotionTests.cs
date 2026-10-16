using NUnit.Framework;
using UnityEngine;

namespace BlackHeart.Tests
{
    // Траектории Ненасытного: засада и прыжок, «мыши» на краю рамки
    public class BeastMotionTests
    {
        static readonly Rect Box = new Rect(-8.5f, -4.15f, 17f, 8.3f);   // рамка в фазе защиты
        const float HeartHit = 0.35f + 0.45f * 0.6f;

        [Test]
        public void PounceWaitsInAmbushThenHitsHeart()
        {
            Vector2 heart = Box.center, stand = heart + new Vector2(3f, 1f);
            var m = new PounceMotion(stand + Vector2.right * 6f, stand, 1f, 2f, heart, 5f);
            Assert.Less(Vector2.Distance(m.Evaluate(1.2f), stand), 0.1f, "В засаде пуля стоит на месте (дрожь — меньше пикселя)");
            Assert.Greater(Vector2.Distance(m.Evaluate(1.9f), heart), 3f, "До прыжка к сердцу не приближается");
            float reach = 2f + Vector2.Distance(stand, heart) / 5f;
            Assert.Less(Vector2.Distance(m.Evaluate(reach), heart), HeartHit, "Прыжок приходит в сердце");
        }

        [Test]
        public void MiceStayOnTheEdgeWhileRoaming()
        {
            GameRandom.Init(5);
            Vector2 heart = Box.center;
            for (int run = 0; run < 100; run++)
            {
                var reversals = new[] { GameRandom.Range(0.3f, 1f), GameRandom.Range(1.2f, 2f) };
                var m = new PerimeterRunMotion(Box, 0.7f, GameRandom.value, GameRandom.value < 0.5f ? 1f : -1f,
                                               GameRandom.Range(3f, 5f), reversals, 2.5f, heart, 5f);
                for (float t = 0f; m.Roaming(t); t += 0.01f)
                {
                    Vector2 p = m.Evaluate(t);
                    Assert.Greater(Vector2.Distance(p, heart), 3f, $"Мышь подбежала к сердцу (прогон {run}, t={t:0.00})");
                    Assert.IsTrue(Box.Contains(p), $"Мышь выбежала из рамки (прогон {run}, t={t:0.00})");
                }
                // за время броска мышь проходит путь до сердца
                Vector2 from = m.Evaluate(2.5f);
                Assert.Less(Vector2.Distance(m.Evaluate(2.5f + Vector2.Distance(from, heart) / 5f), heart), HeartHit);
            }
        }

        [Test]
        public void MicePathIsContinuousAcrossReversals()
        {
            var m = new PerimeterRunMotion(Box, 0.7f, 0.1f, 1f, 4f, new[] { 0.5f, 1.1f }, 2f, Box.center, 5f);
            for (float t = 0f; t < 1.98f; t += 0.01f)   // до броска в 2 с: дальше скорость другая
                Assert.Less(Vector2.Distance(m.Evaluate(t), m.Evaluate(t + 0.01f)), 4f * 0.01f + 1e-3f, $"Скачок пути на t={t:0.00}");
        }
    }
}
