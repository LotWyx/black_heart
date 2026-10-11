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

        public static List<EnemyDefinition> CreateDefaults() => new List<EnemyDefinition>
        {
            new EnemyDefinition
            {
                displayName = "ПРИЗРАК",
                spriteId = ArtIds.EnemyGhost,
                patterns = new List<string> { StraightPattern.ID, ZigzagPattern.ID, DrunkPattern.ID },
                purpleWeight = 0.7f, blueWeight = 0.3f,
                laughText = "ХЕ-ХЕ-ХЕ..."
            },
            new EnemyDefinition
            {
                displayName = "ГЛАЗ",
                spriteId = ArtIds.EnemyEye,
                patterns = new List<string> { SpiralPattern.ID, BoomCirclePattern.ID, StraightPattern.ID },
                purpleWeight = 0.35f, blueWeight = 0.65f,
                laughText = "ХИ-ХИ-ХИ..."
            },
            new EnemyDefinition
            {
                displayName = "ЧЕРЕП",
                spriteId = ArtIds.EnemySkull,
                patterns = new List<string> { ZigzagPattern.ID, SpiralPattern.ID, BoomCirclePattern.ID, DrunkPattern.ID },
                purpleWeight = 0.6f, blueWeight = 0.4f,
                laughText = "ХА-ХА-ХА!"
            },
        };
    }
}
