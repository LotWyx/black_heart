using UnityEngine;

namespace BlackHeart
{
    // Половинки пуль, осколки души и пиксельные искры.
    public class Debris : MonoBehaviour
    {
        public const float OneFrame = 1f / 60f;
        const float RotationStep = 22.5f;   // повороты — только ступеньками

        Vector2 vel;
        float angVel, angle, life, maxLife, gravity;
        bool fade;
        Color color;
        SpriteRenderer sr;

        public static Debris Spawn(Sprite sprite, Vector2 pos, float rotZ, float scale, Color color,
            Vector2 vel, float angVel, float life, float gravity, bool fade, int order)
        {
            var go = new GameObject("Debris");
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, rotZ));
            go.transform.localScale = Vector3.one * scale;
            var d = go.AddComponent<Debris>();
            d.sr = go.AddComponent<SpriteRenderer>();
            d.sr.sprite = sprite;
            d.sr.color = color;
            d.sr.sortingOrder = order;
            d.vel = vel;
            d.angVel = angVel;
            d.angle = rotZ;
            d.life = d.maxLife = life;
            d.gravity = gravity;
            d.fade = fade;
            d.color = color;
            return d;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            vel.y -= gravity * dt;
            transform.position += (Vector3)(vel * dt);
            angle += angVel * dt;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Round(angle / RotationStep) * RotationStep);
            life -= dt;
            if (fade)
                sr.color = new Color(color.r, color.g, color.b, color.a * Mathf.Clamp01(life / maxLife * 2f));
            if (life <= 0f) Destroy(gameObject);
        }

        // Искры (спрайты 3x3, перекрашиваются в цвет источника), исчезают резко
        public static void Burst(Vector2 pos, Color color, int count, float speed)
        {
            var sparks = ArtLibrary.Frames(ArtIds.Sparks);
            for (int i = 0; i < count; i++)
            {
                Vector2 v = Random.insideUnitCircle.normalized * Random.Range(speed * 0.3f, speed);
                Spawn(sparks[Random.Range(0, sparks.Length)], pos, 0, 1f, color, v, 0, Random.Range(0.2f, 0.4f), 0f, false, 15);
            }
        }
    }
}
