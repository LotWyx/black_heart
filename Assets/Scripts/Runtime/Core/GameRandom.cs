using UnityEngine;

namespace BlackHeart
{
    // Случайность игровой логики: выбор врагов и атак, типы пуль, направления, параметры траекторий.
    // У каждого забега свой сид (пишется в лог при старте боя): с тем же сидом бой принимает те же решения,
    // поэтому баг из конкретного забега можно повторить — BattleController.Begin(seed).
    // Косметика (искры, обломки, тряска, звуки) берёт UnityEngine.Random и на эту последовательность не влияет.
    // API повторяет UnityEngine.Random, чтобы код паттернов читался так же.
    public static class GameRandom
    {
        // xorshift32: одинаковый результат на любой платформе и в любом рантайме (Mono, IL2CPP)
        static uint state = 2463534242u;

        public static int Seed { get; private set; }

        public static void Init(int seed)
        {
            Seed = seed;
            state = (uint)seed;
            if (state == 0) state = 2463534242u;   // из нуля xorshift не выходит
            for (int i = 0; i < 8; i++) Next();    // соседние сиды разводим подальше
        }

        // Новый сид для забега
        public static int NewSeed() => System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode();

        static uint Next()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        // [0, 1)
        public static float value => (Next() >> 8) * (1f / 16777216f);

        // [min, max)
        public static float Range(float min, float max) => min + (max - min) * value;

        // [min, max), как у UnityEngine.Random.Range(int, int); при max <= min — min
        public static int Range(int min, int max) => max <= min ? min : min + (int)(Next() % (uint)(max - min));

        public static Vector2 insideUnitCircle
        {
            get
            {
                float a = value * Mathf.PI * 2f, r = Mathf.Sqrt(value);
                return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Init(NewSeed());
    }
}
