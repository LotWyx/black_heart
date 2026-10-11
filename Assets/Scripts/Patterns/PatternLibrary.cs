using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Реестр паттернов по id. Враги ссылаются на паттерны строками (EnemyDefinition.patterns).
    public static class PatternLibrary
    {
        static readonly Dictionary<string, Func<AttackPattern>> registry = new Dictionary<string, Func<AttackPattern>>
        {
            [StraightPattern.ID] = () => new StraightPattern(),
            [ZigzagPattern.ID] = () => new ZigzagPattern(),
            [SpiralPattern.ID] = () => new SpiralPattern(),
            [DrunkPattern.ID] = () => new DrunkPattern(),
            [BoomCirclePattern.ID] = () => new BoomCirclePattern(),
        };

        public static IEnumerable<string> AllIds => registry.Keys;

        public static void Register(string id, Func<AttackPattern> factory) => registry[id] = factory;

        public static AttackPattern Create(string id)
        {
            if (id != null && registry.TryGetValue(id, out var factory)) return factory();
            Debug.LogWarning($"[PatternLibrary] Неизвестный паттерн '{id}', используется '{StraightPattern.ID}'.");
            return new StraightPattern();
        }
    }
}
