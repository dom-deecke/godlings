using UnityEngine;

namespace Godlings
{
    /// <summary>
    /// Confirmed demo numbers, kept editable for guided playtesting.
    /// </summary>
    [CreateAssetMenu(menuName = "Godlings/Demo rules")]
    public sealed class DemoRules : ScriptableObject
    {
        [Header("Confirmed by Dominik")]
        [Min(0.01f)] public float PlayerHealth = 10;
        [Min(0.01f)] public float EnemyHealth = 3;
        [Min(0.01f)] public float EnemyDamage = 2;
        [Min(0.01f)] public float RockDamage = 3;
        [Min(0.01f)] public float RockIntervalSeconds = 2.43f;
        [Min(0.01f)] public float RockMinimumIntervalSeconds = 2;
        [Min(0.01f)] public float BossSwipeDamage = 4;
        [Min(0.01f)] public float BossSwipeWarningSeconds = 2f;
        [Min(0.01f)] public float BossSwipeCooldownSeconds = 5;

        [Min(0.01f)] public float SwordDamage = 1.5f;
        [Min(0.01f)] public float BossHealth = 50;
        [Min(0.01f)] public float RockWarningSeconds = 1.5f;

        [Min(0.01f)] public float CraterSeconds = 2;

        [Header("Approved initial playtest timings")]
        [Min(0.01f)] public float StepSeconds = 0.10f;
        [Min(0.01f)] public float HoldDelaySeconds = 0.18f;
        [Min(0.01f)] public float AttackCooldownSeconds = 0.4f;
        public bool ShowRestartOnDefeat = true;

        [Header("Approved enemy playtest tuning")]
        [Min(0.01f)] public float EnemyStepSeconds = 0.45f;
        [Min(0.01f)] public float EnemySenseRadiusTiles = 4;

        [Min(1)] public int PlayerInvincibilityFrames = 10;
        [Min(0.01f)] public float EnemyRecoilSeconds = 0.5f;

        [Header("Temporary playtest panel — disable for production")]
        public bool EnableMotionPanel = true;
        public Vector2 PlayerVisualOffset = new Vector2(-0.15f,-0.05f);
        public Vector2 EnemyVisualOffset = new Vector2(0.04f,-0.17f);

        public bool HasCompleteCombatValues => SwordDamage > 0 && BossHealth > 0
            && RockWarningSeconds > 0 && StepSeconds > 0
            && HoldDelaySeconds > 0 && AttackCooldownSeconds > 0;
    }
}
