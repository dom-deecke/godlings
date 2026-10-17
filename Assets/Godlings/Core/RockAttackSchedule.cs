using System;
using System.Collections.Generic;
using UnityEngine;

namespace Godlings
{
    /// <summary>
    /// Separates boss timing from presentation. WarningStarted supplies the red
    /// cells to display; Impact supplies the identical cells to resolve damage.
    /// Legal target centers must be provided by the level, not invented here.
    /// </summary>
    public sealed class RockAttackSchedule
    {
        private sealed class PendingRock
        {
            public IReadOnlyList<Vector2Int> Cells;
            public double ImpactAt;
        }

        private readonly List<PendingRock> pending = new();
        private Vector2Int[] centers;
        private Vector2Int[][] footprints;
        private readonly System.Random random;
        private double minimumInterval, maximumInterval;
        private readonly double warningSeconds;
        private double nextWarning;
        private double lastTime;
        private bool stopped;
        public bool IsWarningDue(double now) => !stopped && now >= nextWarning;
        public event Action<IReadOnlyList<Vector2Int>> WarningStarted;
        public event Action<IReadOnlyList<Vector2Int>> Impact;
        public event Action WarningsCleared;

        public RockAttackSchedule(Vector2Int[] allowedCenters, double startTime,
            double intervalSeconds, double warningDurationSeconds, System.Random randomSource)
            : this(allowedCenters,startTime,intervalSeconds,intervalSeconds,warningDurationSeconds,randomSource) { }

        public RockAttackSchedule(Vector2Int[] allowedCenters, double startTime,
            double minimumIntervalSeconds, double maximumIntervalSeconds,
            double warningDurationSeconds, System.Random randomSource)
        {
            if (allowedCenters == null || allowedCenters.Length == 0)
                throw new ArgumentException("The level must supply legal rock target centers.");
            if (minimumIntervalSeconds <= 0 || maximumIntervalSeconds < minimumIntervalSeconds || warningDurationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(warningDurationSeconds));
            centers = (Vector2Int[])allowedCenters.Clone();
            random = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            minimumInterval = minimumIntervalSeconds;
            maximumInterval = maximumIntervalSeconds;
            warningSeconds = warningDurationSeconds;
            lastTime = startTime;
            nextWarning = startTime + NextInterval();
        }

        /// <summary>Pass elapsed gameplay time so pausing can pause boss attacks.</summary>
        public void Advance(double now)
        {
            if (stopped) return;
            if (now < lastTime) throw new ArgumentException("Gameplay time cannot move backwards.");
            lastTime = now;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].ImpactAt > now) continue;
                var rock = pending[i];
                pending.RemoveAt(i);
                Impact?.Invoke(rock.Cells);
                if (stopped) return;
            }
            if (now < nextWarning) return;

            // Start a full warning window even after a long frame. Never catch
            // up by spawning overdue rocks that strike before they can be seen.
            nextWarning = now + NextInterval();
            if (footprints != null && footprints.Length == 0) return;
            var chosen = footprints == null ? AttackPatterns.Rock(centers[random.Next(centers.Length)])
                : footprints[random.Next(footprints.Length)];
            var cells = Array.AsReadOnly((Vector2Int[])chosen.Clone());
            pending.Add(new PendingRock { Cells = cells, ImpactAt = now + warningSeconds });
            WarningStarted?.Invoke(cells);
        }

        /// <summary>Future warnings use these exact footprints; empty means safely skip this drop.</summary>
        public void SetAllowedFootprints(Vector2Int[][] allowed)
        {
            if (allowed == null) throw new ArgumentNullException(nameof(allowed));
            footprints = new Vector2Int[allowed.Length][];
            for (int i=0;i<allowed.Length;i++) footprints[i] = (Vector2Int[])allowed[i].Clone();
        }

        private double NextInterval() => minimumInterval + random.NextDouble()*(maximumInterval-minimumInterval);

        /// <summary>Future warnings use current visible targets; pending impacts stay fixed.</summary>
        public void SetAllowedCenters(Vector2Int[] allowedCenters)
        {
            if (allowedCenters == null || allowedCenters.Length == 0)
                throw new ArgumentException("At least one legal meteor target is required.");
            centers = (Vector2Int[])allowedCenters.Clone();
        }

        public void SetIntervalRange(double minimum, double maximum)
        {
            if (minimum < warningSeconds || maximum < minimum)
                throw new ArgumentOutOfRangeException(nameof(minimum));
            if (minimum == minimumInterval && maximum == maximumInterval) return;
            minimumInterval = minimum; maximumInterval = maximum;
            // Editing the sliders changes the next wait, never a live warning.
            nextWarning = lastTime+NextInterval();
        }

        /// <summary>Begin the encounter with a warning; later waits remain randomized.</summary>
        public void TriggerFirstWarningNow() { if (!stopped && pending.Count == 0) nextWarning = lastTime; }

        public void Stop()
        {
            stopped = true;
            pending.Clear();
            WarningsCleared?.Invoke();
        }
    }
}
