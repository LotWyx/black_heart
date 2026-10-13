using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BlackHeart.Tests
{
    // Данные игры целы: баланс загружается, у врагов есть атаки, и все они существуют.
    // Именно такие тесты ловят коммит, в котором ассеты ссылаются на забытые файлы.
    public class GameDataTests
    {
        static GameBalance Balance => Resources.Load<GameBalance>("Data/GameBalance");

        [Test]
        public void BalanceAssetLoads() => Assert.IsNotNull(Balance, "Нет Assets/Resources/Data/GameBalance.asset");

        [Test]
        public void EnemiesAreAssigned()
        {
            Assert.IsNotEmpty(Balance.enemies, "В балансе нет врагов");
            CollectionAssert.AllItemsAreNotNull(Balance.enemies, "Пустая ссылка на врага (Missing) в GameBalance");
            Assert.IsNotNull(Balance.tutorialBoss, "Не назначен босс обучения");
        }

        [Test]
        public void EveryEnemyAttackExists()
        {
            var known = PatternLibrary.AllIds.ToHashSet();
            foreach (var enemy in Balance.enemies)
            {
                Assert.IsNotEmpty(enemy.patterns, $"У врага {enemy.displayName} нет атак");
                foreach (var id in enemy.patterns)
                    Assert.IsTrue(known.Contains(id), $"У врага {enemy.displayName} атака '{id}', которой нет в PatternLibrary");
            }
        }

        [Test]
        public void EveryAttackCanBeCreated()
        {
            foreach (var id in PatternLibrary.AllIds)
                Assert.IsNotNull(PatternLibrary.Create(id), $"Атака '{id}' не создаётся");
        }

        [Test]
        public void HighDifficultyTempoIsSane()
        {
            var b = Balance;
            Assert.Less(b.densePace, 1f, "«Много, но медленно» должен быть медленнее обычного");
            Assert.Greater(b.fastPace, 1f, "«Быстро, но мало синих» должен быть быстрее обычного");
            Assert.Greater(b.heartQueueLimit, 0);
            Assert.LessOrEqual(b.zigzagChanceMin, b.zigzagChanceMax);
        }
    }
}
