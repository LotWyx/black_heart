using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Как выглядит пуля: кадры анимации и признаки для отрисовки. Подбор спрайта по типу, врагу и виду
    // требует собирать строки путей, поэтому результат считается один раз на сочетание и дальше берётся
    // из кэша — появление пули не создаёт ни одной строки.
    public sealed class ProjectileLook
    {
        public Sprite[] frames;
        public bool typed;   // спрайт уже в цвете своего типа (иначе общий серый, перекрашенный кодом)
        public bool half;    // это половинка фиолетовой пули (а не копия целой с трещиной)

        readonly struct Key : IEquatable<Key>
        {
            readonly ProjectileType type;
            readonly string enemySprite, variant;
            readonly bool fragment;

            public Key(ProjectileType type, string enemySprite, string variant, bool fragment)
            {
                this.type = type;
                this.enemySprite = enemySprite;
                this.variant = variant;
                this.fragment = fragment;
            }

            public bool Equals(Key o) => type == o.type && fragment == o.fragment && enemySprite == o.enemySprite && variant == o.variant;
            public override bool Equals(object obj) => obj is Key o && Equals(o);
            public override int GetHashCode() =>
                ((int)type * 397) ^ (fragment ? 7919 : 0) ^ (enemySprite != null ? enemySprite.GetHashCode() : 0) ^
                ((variant != null ? variant.GetHashCode() : 0) * 31);
        }

        static readonly Dictionary<Key, ProjectileLook> cache = new Dictionary<Key, ProjectileLook>();

        // enemySprite — spriteId врага, выпустившего пулю (Enemies/Ghost…), или null;
        // variant — особый вид пули у паттерна (Bone, Spirit…) или null
        public static ProjectileLook For(ProjectileType type, string enemySprite, string variant, bool fragment)
        {
            var key = new Key(type, enemySprite, variant, fragment);
            if (cache.TryGetValue(key, out var look) && look.frames.Length > 0 && look.frames[0] != null) return look;
            look = Resolve(type, enemySprite, variant, fragment);
            cache[key] = look;
            return look;
        }

        static ProjectileLook Resolve(ProjectileType type, string enemySprite, string variant, bool fragment)
        {
            string theme = enemySprite != null ? enemySprite.Substring(enemySprite.LastIndexOf('/') + 1) : null;

            // Осколок фиолетовой распадается на половинки своего врага; если их нет — копия с трещиной
            string halfId = null;
            if (fragment && type == ProjectileType.Purple)
            {
                string half = (theme != null ? ArtIds.BulletOf(type, theme, variant) : ArtIds.BulletOf(type)) + "Half";
                if (!ArtLibrary.Has(half + "_spin") && theme != null) half = ArtIds.BulletOf(type, theme) + "Half";
                if (!ArtLibrary.Has(half + "_spin")) half = ArtIds.BulletOf(type) + "Half";
                if (ArtLibrary.Has(half + "_spin")) halfId = half;
            }

            // Форма врага → цветная по типу → общая серая, перекрашенная в цвет типа
            string id = halfId ?? (theme != null ? ArtIds.BulletOf(type, theme, variant) : null);
            if (id != null && halfId == null && variant != null && !ArtLibrary.Has(id + "_spin")) id = ArtIds.BulletOf(type, theme);
            if (id == null || !ArtLibrary.Has(id + "_spin")) id = ArtIds.BulletOf(type);
            bool typed = ArtLibrary.Has(id + "_spin");

            return new ProjectileLook
            {
                frames = typed ? ArtLibrary.Frames(id + "_spin", id) : ArtLibrary.Frames(ArtIds.BulletSpin, ArtIds.Bullet),
                typed = typed,
                half = halfId != null
            };
        }

        // При выключенном Domain Reload созданные в игре спрайты уничтожаются — кэш собирается заново
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();
    }
}
