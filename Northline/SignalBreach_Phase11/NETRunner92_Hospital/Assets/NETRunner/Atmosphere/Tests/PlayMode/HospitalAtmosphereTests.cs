using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetRunner.Atmosphere.Tests
{
    /// <summary>
    /// PlayMode tests of the hospital's weather and ambience on a stand-in
    /// site the test builds far from anything else: open ground, one indoor
    /// zone and a roof high above the ground. Outdoors it rains around the
    /// listener and the rain and wind is loud; indoors the interior blend rises
    /// at 1.4 per second, the sound crossfades to the ventilation, dust drifts
    /// ahead of the listener and a distant monitor beeps; under a roof far
    /// overhead no streaks are drawn. The real hospital is covered by
    /// HospitalChecksTests. Expected values come from docs/systems/atmosphere.md.
    /// </summary>
    public sealed class HospitalAtmosphereTests
    {
        private const float Frame = 1f / 60f;
        private const float ZoneDust = 0.5f;

        // 500 m from the origin, so a hospital scene left loaded by another test is out of reach of the rain probes.
        private static readonly Vector3 Site = new Vector3(0f, 0f, 500f);

        // Places on the site, at eye height.
        private static readonly Vector3 Outdoors = new Vector3(60f, 1.6f, 0f);
        private static readonly Vector3 Indoors = new Vector3(0f, 1.6f, 60f);
        private static readonly Vector3 UnderHighRoof = new Vector3(-60f, 1.6f, 0f);

        private static readonly Vector3 ZoneCentre = new Vector3(0f, 2f, 60f);
        private static readonly Vector3 SteamValve = new Vector3(65f, 0.7f, 0f);

        private GameObject rig;
        private AudioClip tone;
        private Transform listener;
        private AudioSource exterior;
        private AudioSource ventilation;
        private AudioSource monitor;
        private ParticleSystem rain;
        private ParticleSystem dust;
        private ParticleSystem steam;

        [SetUp]
        public void UseEvenFrames()
        {
            // Every frame lasts exactly 1/60 s, however fast the machine runs the test.
            Time.captureDeltaTime = Frame;
        }

        [TearDown]
        public void RemoveSite()
        {
            Time.captureDeltaTime = 0f;
            Object.Destroy(rig);
            Object.Destroy(tone);
        }

        [UnityTest]
        public IEnumerator OutdoorsItRainsAroundTheListener()
        {
            var atmosphere = Build(Outdoors);

            yield return Frames(30);

            Assert.That(atmosphere.EmittedRain, Is.GreaterThan(0));
            Assert.That(rain.particleCount, Is.GreaterThan(0));
            Assert.That(atmosphere.RejectedByCover, Is.Zero, "Open sky counted as cover");
        }

        [UnityTest]
        public IEnumerator OutdoorsTheRainAndWindIsLoudAndTheVentilationFaint()
        {
            var atmosphere = Build(Outdoors);

            yield return Frames(30);

            Assert.That(atmosphere.InteriorBlend, Is.Zero);
            Assert.That(exterior.volume, Is.EqualTo(0.48f).Within(1e-5f));
            Assert.That(ventilation.volume, Is.EqualTo(0.012f).Within(1e-5f));
        }

        [UnityTest]
        public IEnumerator UnderARoofFarAboveTheListenerNoStreakIsDrawn()
        {
            // The roof is 12 m up, so every streak would start more than 12 m above the listener.
            var atmosphere = Build(UnderHighRoof);

            yield return Frames(30);

            Assert.That(atmosphere.EmittedRain, Is.Zero);
            Assert.That(atmosphere.RejectedByCover, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator IndoorsTheBlendRisesAtOnePointFourPerSecondAndTheSoundCrossfades()
        {
            var atmosphere = Build(Indoors);

            // A quarter of a second in: 0.25 × 1.4 = 0.35 of the way inside.
            yield return Frames(15);

            Assert.That(atmosphere.InteriorBlend, Is.EqualTo(0.35f).Within(1e-4f));
            Assert.That(exterior.volume, Is.EqualTo(0.48f + (0.034f - 0.48f) * 0.35f).Within(1e-4f));
            Assert.That(ventilation.volume, Is.EqualTo(0.012f + (0.27f - 0.012f) * 0.35f).Within(1e-4f));

            // Fully inside after 1 / 1.4 = 0.71 s.
            yield return Frames(45);

            Assert.That(atmosphere.InteriorBlend, Is.EqualTo(1f));
            Assert.That(exterior.volume, Is.EqualTo(0.034f).Within(1e-5f));
            Assert.That(ventilation.volume, Is.EqualTo(0.27f).Within(1e-5f));
        }

        [UnityTest]
        public IEnumerator IndoorsTheDustDriftsAheadOfTheListenerAtTheZonesDensity()
        {
            Build(Indoors);

            yield return Frames(1);

            // 29 motes a second in a zone of full dust, this zone has half.
            Assert.That(dust.emission.rateOverTime.constant, Is.EqualTo(29f * ZoneDust).Within(1e-4f));
            var ahead = listener.position + listener.forward * 2.2f;
            Assert.That(Vector3.Distance(dust.transform.position, ahead), Is.LessThan(1e-4f));
        }

        [UnityTest]
        public IEnumerator TheValveSteamsOnlyWhileTheListenerIsNear()
        {
            // The valve is about 5 m from the outdoor spot, well within 11 m.
            Build(Outdoors);
            yield return Frames(1);

            Assert.That(steam.emission.rateOverTime.constant, Is.EqualTo(6f));

            listener.position = Site + Indoors;
            yield return Frames(1);

            Assert.That(steam.emission.rateOverTime.constant, Is.Zero);
        }

        [UnityTest]
        public IEnumerator IndoorsADistantMonitorBeepsToTheListenersRightAhead()
        {
            // The first beep comes 4.5 s in; the listener faces +z, so its right is +x.
            Build(Indoors);

            yield return Frames(300);

            var offset = monitor.transform.position - listener.position;
            Assert.That(offset.x, Is.InRange(3f, 6.5f));
            Assert.That(offset.y, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(offset.z, Is.EqualTo(3f).Within(1e-4f));
        }

        [UnityTest]
        public IEnumerator OutdoorsTheMonitorStaysSilent()
        {
            Build(Outdoors);
            var before = monitor.transform.position;

            yield return Frames(300);

            Assert.That(monitor.transform.position, Is.EqualTo(before));
        }

        private HospitalAtmosphere Build(Vector3 listenerAt)
        {
            rig = new GameObject("Test atmosphere site");
            rig.transform.position = Site;
            Box("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(200f, 1f, 200f));
            Box("High roof", new Vector3(-60f, 11.5f, 0f), new Vector3(40f, 1f, 40f));

            listener = Part("Listener").transform;
            listener.position = Site + listenerAt;
            var zone = Part("Zone").AddComponent<EnvironmentalZone>();
            zone.transform.position = Site + ZoneCentre;
            zone.Configure(new Vector3(20f, 4f, 20f), 1.35f, 1, ZoneDust);

            tone = AudioClip.Create("Test tone", 441, 1, 44100, false);
            exterior = Part("Rain and wind").AddComponent<AudioSource>();
            ventilation = Part("Ventilation").AddComponent<AudioSource>();
            var thunder = Part("Thunder").AddComponent<AudioSource>();
            monitor = Part("Monitor").AddComponent<AudioSource>();
            var creak = Part("Creak").AddComponent<AudioSource>();

            // Only the intensity number is used; the lightning need not light anything.
            var lightning = Part("Lightning").AddComponent<Light>();
            lightning.enabled = false;

            rain = Particles("Rain");
            var splash = Particles("Splash");
            dust = Particles("Dust");
            steam = Particles("Steam");
            steam.transform.position = Site + SteamValve;

            Physics.SyncTransforms();
            var atmosphere = rig.AddComponent<HospitalAtmosphere>();
            atmosphere.Configure(listener, new[] { zone }, exterior, ventilation, thunder, monitor, creak, tone, tone,
                tone, lightning, rain, splash, dust, steam);
            return atmosphere;
        }

        private GameObject Part(string name)
        {
            var part = new GameObject(name);
            part.transform.SetParent(rig.transform, false);
            return part;
        }

        private void Box(string name, Vector3 centre, Vector3 size)
        {
            var box = Part(name);
            box.transform.position = Site + centre;
            box.AddComponent<BoxCollider>().size = size;
        }

        // A particle system that emits only what the atmosphere emits, in world space, and draws nothing.
        private ParticleSystem Particles(string name)
        {
            var particles = Part(name).AddComponent<ParticleSystem>();
            var main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            particles.GetComponent<ParticleSystemRenderer>().enabled = false;
            return particles;
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }
    }
}
