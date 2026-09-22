using UnityEngine;

namespace PlagueSurvivor
{
    [CreateAssetMenu(menuName = "Neural Lockdown/Boss Arena Config")]
    public sealed class BossArenaConfig : ScriptableObject
    {
        public Vector2 halfSize = new Vector2(9, 6);
        public Vector2 playerSpawn = new Vector2(0, -4.6f);
        public Vector2 startPosition = new Vector2(0, -2.6f);
        public float startRadius = .65f, playerRadius = .38f;
        public float health = 2600, bodyRadius = .9f, hitRadius = 1.1f;
        public float startupSeconds = 2, transitionSeconds = 2;
        [Header("Burst")]
        public float trackSeconds = .6f, lockSeconds = .3f, shotInterval = .15f, shotSpeed = 6, shotDamage = 16;
        public int burstCount = 3;
        public Vector2 muzzleOffset = new Vector2(1.375f, .28125f);
        [Header("Bombardment")]
        public float bombRadius = 1.3f, bombWarning = 1.1f, bombInterval = .65f, bombDamage = 20;
        [Header("Grid: two strips, safe central corridor 4.8 units")]
        public float gridCenter = 3.5f, gridWidth = 2.2f, gridWarning = 1.4f, gridActive = 1, gridDamage = 18;
        public float burstRecovery = 1, bombRecovery = 1, gridRecovery = 1.5f, phaseTwoRecovery = .8f;
        [Header("Supply")]
        public Vector2[] supplyPoints = { new Vector2(-7, 0), new Vector2(7, 0), new Vector2(0, 4.3f), new Vector2(0, -4.3f) };
        public float supplyWarning = .8f, medicalRadius = .8f, medicalHeal = 25;
        [Header("Art")]
        public Material material;
        public Sprite baseSprite, turret, core, damageOverlay, wreck, projectile, bombRing;
        public Sprite gridWarningSprite, gridActiveSprite, aimTracking, aimLocked, medkit, supplyMarker, startMarker;
        public Sprite boundary, corner, healthFrame, healthFill;
        public Sprite[] explosions;
    }
}
