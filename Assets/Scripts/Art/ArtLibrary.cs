using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Единая точка доступа к графике: спрайты лежат в Resources/Art/<id> и грузятся по id из ArtIds.
    // Спрайты врагов и мелкие спрайты упакованы в атласы (Assets/Art/Atlases). Поэтому везде, где нужна
    // область спрайта в текстуре, берётся textureRect (место в атласе), а не rect (место в исходном PNG).
    public static class ArtLibrary
    {
        public const float PPU = 16f;   // пикселей на юнит у PNG-спрайтов
        const string ResourcesRoot = "Art/";

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<Sprite, Sprite[]> halves = new Dictionary<Sprite, Sprite[]>();
        static readonly Dictionary<string, Sprite[]> frames = new Dictionary<string, Sprite[]>();
        static Sprite pixel;

        public static Sprite Get(string id)
        {
            // Проверка на null нужна при выключенном Domain Reload:
            // созданные в рантайме спрайты уничтожаются при выходе из Play Mode.
            if (cache.TryGetValue(id, out var sprite) && sprite != null) return sprite;

            sprite = Resources.Load<Sprite>(ResourcesRoot + id);
            if (sprite == null)
            {
                Debug.LogWarning($"[ArtLibrary] Нет спрайта '{id}', используется белая заглушка.");
                var tex = Texture2D.whiteTexture;
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PPU);
                sprite.name = id;
            }
            cache[id] = sprite;
            return sprite;
        }

        // Кадры анимации из горизонтальной полоски Resources/Art/<id>.png.
        // По умолчанию кадры квадратные (ширина кадра = высота полоски), иначе задайте frameWidth.
        // Если полоски нет — один кадр из fallbackId.
        public static Sprite[] Frames(string id, string fallbackId = null, int frameWidth = 0)
        {
            if (frames.TryGetValue(id, out var cached) && cached.Length > 0 && cached[0] != null) return cached;

            var strip = Resources.Load<Sprite>(ResourcesRoot + id);
            Sprite[] result;
            if (strip == null)
            {
                result = new[] { Get(fallbackId ?? id) };
            }
            else
            {
                Rect r = strip.textureRect;
                int size = frameWidth > 0 ? frameWidth : Mathf.RoundToInt(r.height);
                int count = Mathf.Max(1, Mathf.RoundToInt(r.width) / size);
                result = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = Sprite.Create(strip.texture, new Rect(r.x + i * size, r.y, size, r.height),
                        new Vector2(0.5f, 0.5f), strip.pixelsPerUnit);
                    result[i].name = id + "_" + i;
                }
            }
            frames[id] = result;
            return result;
        }

        // Есть ли спрайт/полоска в Resources/Art. Ответ запоминается: пуля спрашивает про свой вид при каждом
        // появлении, и повторный Resources.Load (особенно для отсутствующих) давал рывки при залпах
        static readonly Dictionary<string, bool> exists = new Dictionary<string, bool>();
        public static bool Has(string id)
        {
            if (exists.TryGetValue(id, out bool known)) return known;
            bool found = (cache.TryGetValue(id, out var s) && s != null) || Resources.Load<Sprite>(ResourcesRoot + id) != null;
            exists[id] = found;
            return found;
        }

        // Белый квадрат 1x1 юнит — для рамки, полосок и частиц
        public static Sprite Pixel
        {
            get
            {
                if (pixel == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }
                return pixel;
            }
        }

        // Левая и правая половинки спрайта (пивот на линии разреза) — для эффекта разрубания
        public static void Halves(Sprite sprite, out Sprite left, out Sprite right)
        {
            if (!halves.TryGetValue(sprite, out var pair) || pair[0] == null || pair[1] == null)
            {
                Rect r = sprite.textureRect;
                float hw = Mathf.Floor(r.width / 2f);
                pair = new[]
                {
                    Sprite.Create(sprite.texture, new Rect(r.x, r.y, hw, r.height), new Vector2(1f, 0.5f), sprite.pixelsPerUnit),
                    Sprite.Create(sprite.texture, new Rect(r.x + hw, r.y, r.width - hw, r.height), new Vector2(0f, 0.5f), sprite.pixelsPerUnit)
                };
                halves[sprite] = pair;
            }
            left = pair[0];
            right = pair[1];
        }
    }
}
