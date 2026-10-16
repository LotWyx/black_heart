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
            [BoomCirclePattern.ID] = () => new BoomCirclePattern(),
            // Скелет
            [BoneWallPattern.ID] = () => new BoneWallPattern(),
            [BoneCagePattern.ID] = () => new BoneCagePattern(),
            [SkullStandardPattern.ID] = () => new SkullStandardPattern(),
            // Глаз
            [EyeBoomPattern.ID] = () => new EyeBoomPattern(),
            [EyeRectPattern.ID] = () => new EyeRectPattern(),
            [EyeStandardPattern.ID] = () => new EyeStandardPattern(),
            // Призрак
            [GhostSpiralPattern.ID] = () => new GhostSpiralPattern(),
            [GhostWavePattern.ID] = () => new GhostWavePattern(),
            [PendulumPattern.ID] = () => new PendulumPattern(),
            [FlyPattern.ID] = () => new FlyPattern(),
            [GhostStandardPattern.ID] = () => new GhostStandardPattern(),
        };

        public static IEnumerable<string> AllIds => registry.Keys;

        public static void Register(string id, Func<AttackPattern> factory) => registry[id] = factory;

        public static bool Exists(string id) => id != null && registry.ContainsKey(id);

        static readonly HashSet<string> reported = new HashSet<string>();

        // Опечатка в id ломает набор атак врага, поэтому это ошибка, а не предупреждение. Чтобы бой не встал,
        // вместо неизвестной атаки летит прямая; в лог пишется один раз на id. Проверка ассетов — EnemyDefinition.OnValidate.
        public static AttackPattern Create(string id)
        {
            if (Exists(id)) return registry[id]();
            if (reported.Add(id ?? ""))
                Debug.LogError($"[PatternLibrary] Неизвестный паттерн '{id}', вместо него — '{StraightPattern.ID}'. Доступные: {string.Join(", ", registry.Keys)}");
            return new StraightPattern();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => reported.Clear();
    }
}
