using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Зигзаг-пуля — сюрприз средней сложности: раз за фазу защиты (с шансом) посреди атаки из-за экрана
    // врывается пуля, мечется по рамке очень быстро и через несколько секунд форы бросается в сердце.
    // Белая, на высокой сложности иногда фиолетовая. Синей не бывает: от неё нечего было бы ждать.
    public static class ZigzagDart
    {
        // Все числа — в GameBalance.asset (раздел «Зигзаг-пуля»)
        static GameBalance B => GameBalance.Current;

        // Шанс появиться в фазе защиты: с середины сложности, растёт к высокой
        public static float Chance(float difficulty) =>
            difficulty < B.zigzagFrom ? 0f
            : Mathf.Lerp(B.zigzagChanceMin, B.zigzagChanceMax, Mathf.InverseLerp(B.zigzagFrom, B.zigzagFrom + 1f, difficulty));

        public static IEnumerator SpawnAfter(AttackContext ctx, float delay)
        {
            yield return new WaitForSeconds(delay);
            while (ctx.Box.IsAnimating) yield return null;   // зигзаг строится по рамке — ждём, пока она раскроется
            if (GameManager.I == null || !GameManager.I.IsPlaying || GameManager.I.Battle.Phase != BattlePhase.Defense) yield break;
            Vector2 dir = ctx.RandomDirection();
            Vector2 start = ctx.PointOffScreen(dir, 1.5f);
            float from = B.zigzagFrom, to = B.zigzagFrom + 1f;
            float headStart = ctx.Ramp(B.zigzagHeadStartEasy, B.zigzagHeadStartHard, from, to);   // секунды форы после входа в рамку
            var motion = new ZigzagDartMotion(start, ctx.Box.Current, ctx.Target, headStart,
                                              B.zigzagEntrySpeed, ctx.Ramp(B.zigzagSpeedEasy, B.zigzagSpeedHard, from, to), B.zigzagDashSpeed);
            var type = ctx.Difficulty >= B.highDifficulty && GameRandom.value < B.zigzagPurpleChance ? ProjectileType.Purple : ProjectileType.White;
            // мимо ограничений темпа: сюрприз приходит всегда и в своём темпе
            ctx.Projectiles.Spawn(type, motion);
        }
    }
}
