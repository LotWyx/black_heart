using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlackHeart.Tests
{
    // Сид забега (GameRandom) и проверка ссылок на атаки по строкам
    public class DeterminismTests
    {
        [Test]
        public void SameSeedGivesSameSequence()
        {
            GameRandom.Init(2026);
            var first = new float[50];
            for (int i = 0; i < first.Length; i++) first[i] = GameRandom.value;
            GameRandom.Init(2026);
            for (int i = 0; i < first.Length; i++)
                Assert.AreEqual(first[i], GameRandom.value, $"Сид 2026 дал другое число на шаге {i}");
        }

        [Test]
        public void DifferentSeedsGiveDifferentSequences()
        {
            GameRandom.Init(1);
            float a = GameRandom.value + GameRandom.value + GameRandom.value;
            GameRandom.Init(2);
            float b = GameRandom.value + GameRandom.value + GameRandom.value;
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void RangesStayInBounds()
        {
            GameRandom.Init(7);
            for (int i = 0; i < 10000; i++)
            {
                float f = GameRandom.Range(-2f, 3f);
                Assert.That(f, Is.GreaterThanOrEqualTo(-2f).And.LessThan(3f));
                int n = GameRandom.Range(0, 4);
                Assert.That(n, Is.InRange(0, 3));
                Assert.LessOrEqual(GameRandom.insideUnitCircle.magnitude, 1f + 1e-5f);
            }
            Assert.AreEqual(5, GameRandom.Range(5, 5), "Пустой диапазон возвращает min, как UnityEngine.Random");
        }

        // Набор типов пуль атаки (сколько синих и фиолетовых и в каком порядке) повторяется с тем же сидом
        [Test]
        public void AttackTypesRepeatWithSeed()
        {
            var ctx = new AttackContext();
            GameRandom.Init(42);
            var first = ctx.Types(20, 3, 0.3f);
            GameRandom.Init(42);
            var second = ctx.Types(20, 3, 0.3f);
            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void UnknownPatternIsReportedAsError()
        {
            string id = "no_such_attack_" + System.Guid.NewGuid().ToString("N");
            LogAssert.Expect(LogType.Error, new Regex("Неизвестный паттерн '" + id + "'"));
            var pattern = PatternLibrary.Create(id);
            Assert.AreEqual(StraightPattern.ID, pattern.Id, "Вместо неизвестной атаки — прямая, чтобы бой не встал");
        }
    }
}
