namespace BlackHeart
{
    // Идентификаторы спрайтов = пути внутри Assets/Resources/Art/ (без расширения).
    // Чтобы заменить графику, положите свой PNG по тому же пути.
    public static class ArtIds
    {
        public const string Soul = "Player/Soul";
        public const string SoulBroken = "Player/SoulBroken";
        public const string SoulShard = "Player/SoulShard";

        public const string Bullet = "Projectiles/Bullet";
        public const string BulletCrack = "Projectiles/BulletCrack";
        // Пули по типам уже в цвете (Art/Source/gen_projectiles.lua): Projectiles/Bullet<Тип>[_spin];
        // в стиле врага — Projectiles/<Враг>/Bullet<Тип>[_spin] (цвет тот же, меняется только форма)
        public static string BulletOf(ProjectileType type) => "Projectiles/Bullet" + type;
        public static string BulletOf(ProjectileType type, string theme) => "Projectiles/" + theme + "/Bullet" + type;
        // Особый вид пули у паттерна (кости Скелета, призраки маятника): Projectiles/<Враг>/<Вид><Тип>[_spin]
        public static string BulletOf(ProjectileType type, string theme, string variant) =>
            "Projectiles/" + theme + "/" + (variant ?? "Bullet") + type;

        public const string BoomCircle = "FX/BoomCircle";

        public const string BackgroundBattle = "Background/Hall";        // готический зал с витражом, 3 кадра 320x180
        public const string BackgroundMenu = "Background/BloodMoon";     // кровавая луна над морем, 3 кадра 320x180
        public const string BackgroundSunset = "Background/Sunset";      // запасной процедурный фон (PixelArt)

        // Анимации (горизонтальные полоски кадров)
        public const string SoulPulse = "Player/Soul_pulse";
        public const string SoulHit = "Player/Soul_hit";
        public const string SoulShardSet = "Player/SoulShard_set";
        public const string BulletSpin = "Projectiles/Bullet_spin";
        public const string BulletSpawn = "Projectiles/Bullet_spawn";
        public const string BulletDespawn = "Projectiles/Bullet_despawn";
        public const string Sparks = "FX/Sparks";

        // Анимации врага: <spriteId>_idle, _windup, _hurt, _laugh, _defeat
        public static string EnemyAnim(string spriteId, string anim) => spriteId + "_" + anim;

        public const string EnemyGhost = "Enemies/Ghost";
        public const string EnemyEye = "Enemies/Eye";
        public const string EnemySkull = "Enemies/Skull";
        public const string EnemyMoonface = "Enemies/Moonface";   // босс обучения

        // Подсказки обучения
        public const string HintFinger = "UI/HintFinger";   // 2 кадра: палец, касание
        public const string HintMouse = "UI/HintMouse";     // 2 кадра: мышь, нажатая ЛКМ
        public const string HintCross = "UI/HintCross";
        public const string MenuOrnament = "UI/MenuOrnament";   // завиток у рамки выбранного пункта меню

        // Детали рамки боя (Art/Source/gen_box.lua); *Accent красятся в цвет внутренней линии
        public const string BoxCorner = "UI/BoxCorner";
        public const string BoxCornerAccent = "UI/BoxCornerAccent";
        public const string BoxNotchH = "UI/BoxNotchH";
        public const string BoxNotchHAccent = "UI/BoxNotchHAccent";
        public const string BoxNotchV = "UI/BoxNotchV";
        public const string BoxNotchVAccent = "UI/BoxNotchVAccent";
    }
}
