using UnityEngine;

namespace BlackHeart
{
    // Фон боя: готический зал с витражным окном (3 кадра: мерцают свечи, рябит отражение на полу).
    // Виден только за рамкой. Растягивается на всю видимую область (целым множителем, если возможно).
    public class BattleBackground : MonoBehaviour
    {
        public float frameTime = 0.18f;

        SpriteRenderer sr;
        SpriteAnimator anim;

        void Awake()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -20;
            anim = gameObject.AddComponent<SpriteAnimator>();
            anim.Play(ArtLibrary.Frames(ArtIds.BackgroundBattle, null, 320), frameTime);
        }

        public void SetVisible(bool visible) => sr.enabled = visible;

        void LateUpdate()
        {
            if (sr.sprite == null) return;
            Rect view = GameManager.I.Pixel.ViewRect;
            Vector2 size = sr.sprite.bounds.size;
            float scale = Mathf.Max(view.width / size.x, view.height / size.y);
            if (scale > 1f && scale - Mathf.Floor(scale) < 0.05f) scale = Mathf.Floor(scale);
            transform.position = new Vector3(view.center.x, view.center.y, 5f);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
