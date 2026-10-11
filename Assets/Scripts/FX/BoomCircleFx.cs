using UnityEngine;

namespace BlackHeart
{
    // Круг паттерна «бум-круг». «Полупрозрачность» нарисована шахматкой в самом спрайте (без alpha).
    // Держится hold секунд, затем сжимается до размера пули тремя ступенями.
    public class BoomCircleFx : MonoBehaviour
    {
        const int ShrinkSteps = 3;

        float startSize, endSize, hold, shrink, t;
        float spriteSize;

        public static BoomCircleFx Spawn(Vector2 pos, float startDiameter, float endDiameter, float hold, float shrink)
        {
            var go = new GameObject("BoomCircle");
            go.transform.position = pos;
            var fx = go.AddComponent<BoomCircleFx>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ArtLibrary.Get(ArtIds.BoomCircle);
            sr.sortingOrder = 2;
            fx.spriteSize = Mathf.Max(0.01f, sr.sprite.bounds.size.x);
            fx.startSize = startDiameter;
            fx.endSize = endDiameter;
            fx.hold = hold;
            fx.shrink = shrink;
            fx.Apply(startDiameter);
            return fx;
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t < hold) return;
            float k = Mathf.Clamp01((t - hold) / shrink);
            float stepped = Mathf.Ceil(k * ShrinkSteps) / ShrinkSteps;
            Apply(Mathf.Lerp(startSize, endSize, stepped));
            if (k >= 1f) Destroy(gameObject);
        }

        void Apply(float diameter) => transform.localScale = Vector3.one * (diameter / spriteSize);
    }
}
