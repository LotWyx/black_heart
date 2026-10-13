using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BlackHeart.Tests
{
    // Траектории — чистые функции времени, поэтому их правила честности проверяются без запуска игры
    public class MotionTests
    {
        static readonly Rect Box = new Rect(-8.5f, -4.15f, 17f, 8.3f);   // рамка в фазе защиты
        const float HeartHit = 0.35f + 0.45f * 0.6f;                       // радиус сердца + часть радиуса пули

        [Test]
        public void ZigzagDartNeverTouchesHeartWhileRoaming()
        {
            Random.InitState(12345);
            Vector2 heart = Box.center;
            for (int run = 0; run < 200; run++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 start = heart + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 14f;
                var m = new ZigzagDartMotion(start, Box, heart, 3f, 16f, 12f, 7f);
                for (float t = 0f; m.Roaming(t); t += 0.005f)
                    Assert.Greater(Vector2.Distance(m.Evaluate(t), heart), 1.5f, $"Зигзаг подошёл к сердцу (прогон {run}, t={t:0.00})");
            }
        }

        [Test]
        public void ZigzagDartHeadStartCountsFromEnteringTheBox()
        {
            Random.InitState(7);
            Vector2 heart = Box.center;
            const float headStart = 3f;
            var m = new ZigzagDartMotion(heart + Vector2.left * 14f, Box, heart, headStart, 16f, 12f, 7f);
            float entered = -1f, roamEnd = -1f;
            for (float t = 0f; t < 10f; t += 0.005f)
            {
                if (entered < 0f && Box.Contains(m.Evaluate(t))) entered = t;
                if (!m.Roaming(t)) { roamEnd = t; break; }
            }
            Assert.GreaterOrEqual(entered, 0f, "Пуля так и не вошла в рамку");
            Assert.GreaterOrEqual(roamEnd - entered, headStart - 0.05f, "Фора после входа в рамку меньше заданной");
        }

        [Test]
        public void ZigzagDartFinallyReachesHeart()
        {
            Random.InitState(99);
            Vector2 heart = Box.center;
            var m = new ZigzagDartMotion(heart + Vector2.up * 12f, Box, heart, 2.5f, 16f, 12f, 7f);
            float best = float.MaxValue;
            for (float t = 0f; t < 15f; t += 0.01f) best = Mathf.Min(best, Vector2.Distance(m.Evaluate(t), heart));
            Assert.Less(best, HeartHit, "Зигзаг-пуля так и не долетела до сердца");
        }

        [Test]
        public void StraightMotionHitsTarget()
        {
            var m = new StraightMotion(new Vector2(-10f, 0f), Vector2.zero, 2f);
            Assert.That(Vector2.Distance(m.Evaluate(5f), Vector2.zero), Is.LessThan(0.01f));
        }

        [Test]
        public void BlueGapsLeaveRoomAroundBlue()
        {
            var types = new List<ProjectileType> { ProjectileType.White, ProjectileType.Blue, ProjectileType.White, ProjectileType.White };
            var skip = AttackContext.BlueGaps(types, cyclic: false);
            CollectionAssert.AreEqual(new[] { true, false, true, false }, skip);
        }
    }
}
