using System.Collections.Generic;
using UnityEngine;

namespace BlackHeart
{
    // Половинки пуль, осколки души и пиксельные искры.
    // Объекты берутся из пула: отживший обломок выключается и ждёт следующего Spawn, а не уничтожается —
    // на каждое попадание и разрез их появляется около десятка.
    public class Debris : MonoBehaviour
    {
        public const float OneFrame = 1f / 60f;
        const float RotationStep = 22.5f;   // повороты — только ступеньками

        static readonly Stack<Debris> pool = new Stack<Debris>();
        static Transform root;

        Vector2 vel;
        float angVel, angle, life, maxLife, gravity;
        bool fade;
        Color color;
        SpriteRenderer sr;

        static Debris Take()
        {
            while (pool.Count > 0)
            {
                var pooled = pool.Pop();
                if (pooled != null) return pooled;   // объект мог быть уничтожен вместе со сценой
            }
            if (root == null) root = new GameObject("Debris").transform;
            var go = new GameObject("Debris");
            go.transform.SetParent(root, false);
            var d = go.AddComponent<Debris>();
            d.sr = go.AddComponent<SpriteRenderer>();
            return d;
        }

        public static Debris Spawn(Sprite sprite, Vector2 pos, float rotZ, float scale, Color color,
            Vector2 vel, float angVel, float life, float gravity, bool fade, int order)
        {
            var d = Take();
            d.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, rotZ));
            d.transform.localScale = Vector3.one * scale;
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
            d.gameObject.SetActive(true);
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
            if (life <= 0f)
            {
                gameObject.SetActive(false);
                pool.Push(this);
            }
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

        // При выключенном Domain Reload статика переживает Play Mode, а объекты пула — нет
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pool.Clear();
            root = null;
        }
    }
}
