using System;
using UnityEngine;

namespace BlackHeart
{
    // Простой покадровый проигрыватель для SpriteRenderer.
    // sequence задаёт порядок кадров (например 0,1,0,2 для пульсации), иначе кадры идут подряд.
    public class SpriteAnimator : MonoBehaviour
    {
        SpriteRenderer sr;
        Sprite[] frames;
        int[] sequence;
        float frameTime, t;
        int step;
        bool loop, playing;
        Action onDone;

        public bool IsPlaying => playing;
        public Sprite[] Frames => frames;   // что сейчас играет (чтобы не перебить чужую анимацию)

        void Awake() => sr = GetComponent<SpriteRenderer>();

        public void Play(Sprite[] frames, float frameTime, bool loop = true, Action onDone = null, int[] sequence = null)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            this.frames = frames;
            this.frameTime = Mathf.Max(0.001f, frameTime);
            this.loop = loop;
            this.onDone = onDone;
            this.sequence = sequence;
            t = 0;
            step = 0;
            playing = frames != null && frames.Length > 0;
            Apply();
        }

        public void Stop() => playing = false;

        int Length => sequence != null ? sequence.Length : frames.Length;

        void Apply()
        {
            if (frames == null || frames.Length == 0) return;
            int index = sequence != null ? sequence[step] : step;
            sr.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        void Update()
        {
            if (!playing) return;
            t += Time.deltaTime;
            while (t >= frameTime && playing)
            {
                t -= frameTime;
                step++;
                if (step >= Length)
                {
                    if (loop) step = 0;
                    else
                    {
                        step = Length - 1;
                        playing = false;
                        Apply();
                        var done = onDone;
                        onDone = null;
                        done?.Invoke();
                        return;
                    }
                }
                Apply();
            }
        }
    }
}
