using System.Collections.Generic;

namespace BlackHeart
{
    // Враги по умолчанию
    public static class EnemyRoster
    {
        // Луноликий — босс обучения, в обычную ротацию не входит
        public static EnemyDefinition CreateMoonface() => new EnemyDefinition
        {
            displayName = "ЛУНОЛИКИЙ",
            spriteId = ArtIds.EnemyMoonface,
            minHp = 5, maxHp = 5,
            patterns = new List<string>(),
            purpleWeight = 0.5f, blueWeight = 0.5f,
            laughText = "",   // вместо надписи — реплики в облачке
        };

        // У каждого врага свой набор атак (паттерны из общей библиотеки); стандартная атака
        // встречается чаще остальных — она указана дважды. 5 HP, бой ~2 минуты.
        public static List<EnemyDefinition> CreateDefaults() => new List<EnemyDefinition>
        {
            new EnemyDefinition
            {
                displayName = "ПРИЗРАК",
                spriteId = ArtIds.EnemyGhost,
                minHp = 5, maxHp = 5,
                patterns = new List<string> { GhostSpiralPattern.ID, GhostWavePattern.ID, PendulumPattern.ID,
                                              GhostStandardPattern.ID, GhostStandardPattern.ID },
                purpleWeight = 0.5f, blueWeight = 0.5f,
                laughText = "ХЕ-ХЕ-ХЕ..."
            },
            new EnemyDefinition
            {
                displayName = "ГЛАЗ",
                spriteId = ArtIds.EnemyEye,
                minHp = 5, maxHp = 5,
                patterns = new List<string> { EyeBoomPattern.ID, EyeRectPattern.ID, EyeStandardPattern.ID, EyeStandardPattern.ID },
                purpleWeight = 0.4f, blueWeight = 0.6f,
                laughText = "ХИ-ХИ-ХИ..."
            },
            new EnemyDefinition
            {
                displayName = "ЧЕРЕП",
                spriteId = ArtIds.EnemySkull,
                minHp = 5, maxHp = 5,
                patterns = new List<string> { BoneWallPattern.ID, BoneCagePattern.ID, SkullStandardPattern.ID, SkullStandardPattern.ID },
                purpleWeight = 0.5f, blueWeight = 0.5f,
                laughText = "ХА-ХА-ХА!"
            },
        };
    }
}
