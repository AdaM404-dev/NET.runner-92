using System;
using UnityEngine;

namespace NETRunner92
{
    public sealed class HospitalAtmosphere : MonoBehaviour
    {
        [SerializeField] Transform listener;
        [SerializeField] EnvironmentalZone[] zones;
        [SerializeField] AudioSource exterior;
        [SerializeField] AudioSource ventilation;
        [SerializeField] AudioSource thunder;
        [SerializeField] AudioSource monitor;
        [SerializeField] AudioSource creak;
        [SerializeField] AudioClip thunderClip;
        [SerializeField] AudioClip monitorClip;
        [SerializeField] AudioClip creakClip;
        [SerializeField] Light lightning;
        [SerializeField] ParticleSystem rain;
        [SerializeField] ParticleSystem splash;
        [SerializeField] ParticleSystem dust;
        [SerializeField] ParticleSystem steam;
        [SerializeField] LayerMask weatherGeometry = 1;
        [SerializeField] float rainfall = 730f;
        [SerializeField] Vector2 wind = new Vector2(1.8f, 0.6f);
        [SerializeField] float thunderDelay = 15f;

        System.Random noise = new System.Random(2087);
        float budget;
        float inside;
        float nextFlash;
        float nextSound = float.PositiveInfinity;
        float nextMonitor;
        float nextCreak;
        bool lightningScheduled;

        public float InteriorBlend => inside;
        public int EmittedRain { get; private set; }
        public int RejectedByCover { get; private set; }

        public void Configure(Transform head, EnvironmentalZone[] areas, AudioSource outside, AudioSource insideSource,
                              AudioSource thunderSource, AudioSource monitorSource, AudioSource creakSource, AudioClip thunderEffect,
                              AudioClip monitorEffect, AudioClip creakEffect, Light lightningLight, ParticleSystem rainSystem,
                              ParticleSystem splashSystem, ParticleSystem dustSystem, ParticleSystem steamSystem)
        {
            listener = head; zones = areas; exterior = outside; ventilation = insideSource;
            thunder = thunderSource; monitor = monitorSource; creak = creakSource; thunderClip = thunderEffect; monitorClip = monitorEffect;
            creakClip = creakEffect; lightning = lightningLight; rain = rainSystem; splash = splashSystem; dust = dustSystem; steam = steamSystem;
        }

        void Start()
        {
            nextFlash = Time.time + thunderDelay;
            nextMonitor = Time.time + 4.5f;
            nextCreak = Time.time + 8f;
        }

        void Update()
        {
            Vector3 p = listener.position;
            float coverWeight = 0f;
            float dustWeight = 0f;
            foreach (var zone in zones)
            {
                if (zone.Floor == 0) continue;
                float weight = zone.WeightAt(p);
                coverWeight = Mathf.Max(coverWeight, weight);
                dustWeight = Mathf.Max(dustWeight, weight * zone.DustIntensity);
            }
            inside = Mathf.MoveTowards(inside, coverWeight, Time.deltaTime * 1.4f);
            exterior.volume = Mathf.Lerp(0.48f, 0.034f, inside);
            ventilation.volume = Mathf.Lerp(0.012f, 0.27f, inside);
            var dustEmission = dust.emission;
            dustEmission.rateOverTime = 29f * dustWeight;
            dust.transform.position = p + listener.forward * 2.2f;
            var steamEmission = steam.emission;
            steamEmission.rateOverTime = Vector3.Distance(p, steam.transform.position) < 11f ? 6 : 0;

            Rain(p);
            Events(p);
        }

        float Next(float low, float high) => low + (high - low) * (float)noise.NextDouble();

        void Rain(Vector3 p)
        {
            // Deep interior views are shielded from the exterior rain field on every level.
            if (inside > .92f && Mathf.Abs(p.x) < 29f && Mathf.Abs(p.z) < 9.5f) return;
            budget = Mathf.Min(90f, budget + Time.deltaTime * rainfall);
            int count = (int)budget;
            budget -= count;
            for (int i = 0; i < count; i++)
            {
                float dx = Next(-14f, 14f);
                float dz = Next(-14f, 14f);
                float x = p.x + dx;
                float z = p.z + dz;
                if (!Physics.Raycast(new Vector3(x, 27f, z), Vector3.down, out var roof, 29f, weatherGeometry,
                                     QueryTriggerInteraction.Ignore)) continue;

                // A short randomized segment starts above the highest physical surface in this column.
                // Keep an inset to cover wind displacement between the roof probe and streak endpoint.
                float top = Mathf.Max(p.y + Next(2.7f, 9.5f), roof.point.y + Next(1.6f, 5.6f));
                float floor = roof.point.y + 0.18f;
                if (roof.point.y > p.y + 0.6f && top > p.y + 12f)
                {
                    RejectedByCover++;
                    continue;
                }
                const float verticalSpeed = 16f;
                float duration = (top - floor) / verticalSpeed;
                Vector3 velocity = new Vector3(wind.x, -verticalSpeed, wind.y);
                Vector3 start = new Vector3(x - wind.x * duration, top, z - wind.y * duration);
                if (Physics.Raycast(start, velocity.normalized, out var impact, velocity.magnitude * duration + 0.24f,
                                    weatherGeometry, QueryTriggerInteraction.Ignore))
                    duration = Mathf.Min(duration, Mathf.Max(0.02f, (impact.distance - 0.07f) / velocity.magnitude));
                rain.Emit(new ParticleSystem.EmitParams {
                    position = start, velocity = velocity, startLifetime = duration, startSize = Next(0.013f, 0.026f)
                }, 1);
                EmittedRain++;
                if (i % 7 == 0 && roof.point.y < 0.35f && inside < 0.90f)
                    splash.Emit(new ParticleSystem.EmitParams {
                        position = roof.point + Vector3.up * 0.026f,
                        velocity = new Vector3(Next(-0.3f, 0.3f), Next(0.35f, 0.75f), Next(-0.3f, 0.3f)),
                        startSize = Next(0.028f, 0.067f), startLifetime = Next(0.14f, 0.22f)
                    }, 1);
            }
        }

        void Events(Vector3 p)
        {
            float now = Time.time;
            float offset = now - nextFlash;
            if (offset >= 0 && offset < 0.7f)
            {
                lightning.intensity = 34f * Mathf.Exp(-22f * offset) + 17f * Mathf.Exp(-65f * Mathf.Abs(offset - 0.19f));
                if (!lightningScheduled)
                {
                    lightningScheduled = true;
                    nextSound = now + Next(0.65f, 1.5f);
                }
            }
            else
            {
                lightning.intensity = 0;
                if (offset >= 0.7f) { nextFlash = now + Next(20f, 39f); lightningScheduled = false; }
            }
            if (now >= nextSound)
            {
                nextSound = float.PositiveInfinity;
                thunder.PlayOneShot(thunderClip, Mathf.Lerp(0.72f, 0.26f, inside));
            }
            if (now >= nextMonitor)
            {
                nextMonitor = now + Next(7f, 16f);
                if (inside > 0.45f)
                {
                    monitor.transform.position = p + listener.right * Next(3.0f, 6.5f) + listener.forward * 3f;
                    monitor.PlayOneShot(monitorClip, 0.22f * inside);
                }
            }
            if (now >= nextCreak)
            {
                nextCreak = now + Next(10f, 23f);
                creak.PlayOneShot(creakClip, Mathf.Lerp(0.34f, 0.15f, inside));
            }
        }
    }
}

