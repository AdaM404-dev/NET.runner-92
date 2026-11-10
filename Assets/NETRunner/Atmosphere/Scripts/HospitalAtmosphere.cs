using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Atmosphere
{
    /// <summary>
    /// The hospital's weather and ambience around the player: rain streaks
    /// and splashes, the rain-and-wind and ventilation loops crossfading as
    /// the player goes in and out, dust ahead of the player, steam from a
    /// damaged valve, lightning with thunder, a distant medical monitor and
    /// the creak of the fence and structure. How far inside the player is
    /// comes from the <see cref="EnvironmentalZone"/>s; the numbers come from
    /// <see cref="Ambience"/>, <see cref="RainField"/> and
    /// <see cref="AmbientEvents"/>. This component probes the scene for roofs,
    /// emits the particles, sets the volumes and plays the sounds. The
    /// hospital's scene builder sets it up through <see cref="Configure"/>.
    /// </summary>
    public sealed class HospitalAtmosphere : MonoBehaviour
    {
        // The seed of the Northline scene; the rain and the events share the one random stream.
        private const int Seed = 2087;

        // The roof probe runs straight down from above the hospital's highest roof to 2 m below the ground.
        private const float ProbeTop = 27f;
        private const float ProbeLength = 29f;

        // The probe along a streak reaches this far past its end, so a surface just below the end still stops it.
        private const float ImpactAllowance = 0.24f;

        // Splashes start just above the surface, so they are not hidden in it.
        private const float SplashLift = 0.026f;

        // Dust is emitted this far in front of the listener, where it can be seen.
        private const float DustAhead = 2.2f;

        [SerializeField] private Transform listener;
        [SerializeField] private EnvironmentalZone[] zones;
        [SerializeField] private AudioSource exterior;
        [SerializeField] private AudioSource ventilation;
        [SerializeField] private AudioSource thunder;
        [SerializeField] private AudioSource monitor;
        [SerializeField] private AudioSource creak;
        [SerializeField] private AudioClip thunderClip;
        [SerializeField] private AudioClip monitorClip;
        [SerializeField] private AudioClip creakClip;
        [SerializeField] private Light lightning;
        [SerializeField] private ParticleSystem rain;
        [SerializeField] private ParticleSystem splash;
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private ParticleSystem steam;
        [SerializeField] private LayerMask weatherGeometry = 1;
        [SerializeField] private float rainfall = 730f;
        [SerializeField] private Vector2 wind = new Vector2(1.8f, 0.6f);
        [SerializeField] private float thunderDelay = 15f;

        private readonly System.Random random = new System.Random(Seed);
        private RainField rainField;
        private AmbientEvents events;
        private float inside;

        /// <summary>How far inside the listener is: 0 outdoors, 1 deep inside; it follows the zones at <see cref="Ambience.BlendRate"/>.</summary>
        public float InteriorBlend => inside;

        /// <summary>Rain streaks emitted since the start; the hospital checks count them.</summary>
        public int EmittedRain { get; private set; }

        /// <summary>Streaks left out since the start because they would start far above a roof overhead; the hospital checks count them.</summary>
        public int RejectedByCover { get; private set; }

        private void Start()
        {
            rainField = new RainField(random);
            events = new AmbientEvents(random, Time.time, thunderDelay);
        }

        private void Update()
        {
            var position = listener.position;
            float cover = 0f;
            float dustiness = 0f;
            foreach (var zone in zones)
            {
                // Only the hospital's floors give cover; the builder makes no floor-0 zone today.
                if (zone.Floor == 0)
                {
                    continue;
                }

                float weight = zone.WeightAt(position);
                cover = Mathf.Max(cover, weight);
                dustiness = Mathf.Max(dustiness, weight * zone.DustIntensity);
            }

            inside = Ambience.Follow(inside, cover, Time.deltaTime);
            exterior.volume = Ambience.RainAndWindVolume(inside);
            ventilation.volume = Ambience.VentilationVolume(inside);
            var dustEmission = dust.emission;
            dustEmission.rateOverTime = Ambience.DustRate(dustiness);
            dust.transform.position = position + listener.forward * DustAhead;
            var steamEmission = steam.emission;
            steamEmission.rateOverTime = Ambience.SteamRate(Vector3.Distance(position, steam.transform.position));

            // Rain before the events: they share one random stream, in the Northline code's order.
            Rain(position);
            PlayEvents(position);
        }

        /// <summary>
        /// Sets the atmosphere up from the scene builder: the listener (the
        /// player's eyes), the zones, the five audio sources and three clips,
        /// the lightning light and the four particle systems.
        /// </summary>
        public void Configure(Transform head, EnvironmentalZone[] areas, AudioSource outside, AudioSource insideSource,
                              AudioSource thunderSource, AudioSource monitorSource, AudioSource creakSource, AudioClip thunderEffect,
                              AudioClip monitorEffect, AudioClip creakEffect, Light lightningLight, ParticleSystem rainSystem,
                              ParticleSystem splashSystem, ParticleSystem dustSystem, ParticleSystem steamSystem)
        {
            listener = head;
            zones = areas;
            exterior = outside;
            ventilation = insideSource;
            thunder = thunderSource;
            monitor = monitorSource;
            creak = creakSource;
            thunderClip = thunderEffect;
            monitorClip = monitorEffect;
            creakClip = creakEffect;
            lightning = lightningLight;
            rain = rainSystem;
            splash = splashSystem;
            dust = dustSystem;
            steam = steamSystem;
        }

        // Emits this frame's rain streaks around the listener, and a splash for some of those landing on open ground.
        private void Rain(Vector3 position)
        {
            if (RainField.IsSheltered(inside, position.x, position.z))
            {
                return;
            }

            int drops = rainField.DropsDue(Time.deltaTime, rainfall);
            for (int i = 0; i < drops; i++)
            {
                var (x, z) = rainField.Column(position.x, position.z);
                if (!Physics.Raycast(new Vector3(x, ProbeTop, z), Vector3.down, out var roof, ProbeLength, weatherGeometry,
                                     QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                if (!rainField.TryStreak(position.y, roof.point.y, out float top, out float fallTime))
                {
                    RejectedByCover++;
                    continue;
                }

                // The streak starts upwind, so the wind carries it onto the column that was probed.
                var velocity = new Vector3(wind.x, -RainField.FallSpeed, wind.y);
                var start = new Vector3(x - wind.x * fallTime, top, z - wind.y * fallTime);
                if (Physics.Raycast(start, velocity.normalized, out var impact, velocity.magnitude * fallTime + ImpactAllowance,
                                    weatherGeometry, QueryTriggerInteraction.Ignore))
                {
                    fallTime = RainField.FallTimeToImpact(fallTime, impact.distance, velocity.magnitude);
                }

                rain.Emit(new ParticleSystem.EmitParams
                {
                    position = start,
                    velocity = velocity,
                    startLifetime = fallTime,
                    startSize = rainField.DropSize(),
                }, 1);
                EmittedRain++;
                if (RainField.Splashes(i, roof.point.y, inside))
                {
                    Splash(roof.point);
                }
            }
        }

        private void Splash(Vector3 ground)
        {
            var (x, y, z) = rainField.SplashVelocity();
            splash.Emit(new ParticleSystem.EmitParams
            {
                position = ground + Vector3.up * SplashLift,
                velocity = new Vector3(x, y, z),
                startSize = rainField.SplashSize(),
                startLifetime = rainField.SplashLife(),
            }, 1);
        }

        // Shows this frame's lightning and plays the thunder, monitor beep and creak that start now.
        private void PlayEvents(Vector3 position)
        {
            events.Step(Time.time, inside);
            lightning.intensity = events.Lightning;
            if (events.ThunderStarts)
            {
                thunder.PlayOneShot(thunderClip, Ambience.ThunderVolume(inside));
            }

            if (events.MonitorBeeps)
            {
                monitor.transform.position = position + listener.right * events.MonitorSide
                                             + listener.forward * AmbientEvents.MonitorAhead;
                monitor.PlayOneShot(monitorClip, Ambience.MonitorVolume(inside));
            }

            if (events.CreakSounds)
            {
                creak.PlayOneShot(creakClip, Ambience.CreakVolume(inside));
            }
        }
    }
}
