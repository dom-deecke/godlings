using System;
using System.Collections.Generic;
using UnityEngine;

namespace Godlings
{
    /// <summary>Locks the swipe footprint at warning time so Fi can evade it.</summary>
    public sealed class BossSwipeSchedule
    {
        private readonly double warningSeconds, cooldownSeconds;
        private IReadOnlyList<Vector2Int> pending;
        private double impactAt, nextReady, lastTime;
        private bool stopped;
        public event Action<IReadOnlyList<Vector2Int>> WarningStarted;
        public event Action<IReadOnlyList<Vector2Int>> Impact;
        public event Action WarningsCleared;

        public BossSwipeSchedule(double startTime, double warningDuration, double cooldown)
        {
            if (warningDuration <= 0 || cooldown < warningDuration)
                throw new ArgumentOutOfRangeException(nameof(cooldown));
            lastTime = nextReady = startTime;
            warningSeconds = warningDuration;
            cooldownSeconds = cooldown;
        }

        public void Advance(double now, Vector2Int boss, Vector2Int fi)
        {
            if (stopped) return;
            if (now < lastTime) throw new ArgumentException("Gameplay time cannot move backwards.");
            lastTime = now;
            if (pending != null && now >= impactAt)
            {
                var cells = pending;
                pending = null;
                Impact?.Invoke(cells);
                if (stopped) return;
            }
            Vector2Int difference = fi-boss;
            bool aligned = difference.x == 0 || difference.y == 0;
            int distance = Math.Abs(difference.x)+Math.Abs(difference.y);
            if (pending != null || now < nextReady || !aligned || distance == 0 || distance > 2) return;
            var direction = new Vector2Int(Math.Sign(difference.x),Math.Sign(difference.y));
            pending = Array.AsReadOnly(AttackPatterns.BossSwipe(boss,direction));
            impactAt = now+warningSeconds;
            nextReady = now+cooldownSeconds;
            WarningStarted?.Invoke(pending);
        }

        public void Stop()
        {
            stopped = true;
            pending = null;
            WarningsCleared?.Invoke();
        }
    }
}
