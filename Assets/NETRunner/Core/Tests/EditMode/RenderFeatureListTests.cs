using System.Collections.Generic;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of reading and writing the features a benchmark run
    /// switches off (<c>-net92-without</c>).
    /// </summary>
    public sealed class RenderFeatureListTests
    {
        [Test]
        public void ANameSwitchesOffItsFeature()
        {
            RenderFeature features = RenderFeatureList.Parse("shadows", out IReadOnlyList<string> unknown);

            Assert.That(features, Is.EqualTo(RenderFeature.Shadows));
            Assert.That(unknown, Is.Empty);
        }

        [Test]
        public void CommasSeparateSeveralNames()
        {
            RenderFeature features = RenderFeatureList.Parse("volumetric-fog,fog-lights,occlusion-culling", out IReadOnlyList<string> unknown);

            Assert.That(features, Is.EqualTo(RenderFeature.VolumetricFog | RenderFeature.FogLights | RenderFeature.OcclusionCulling));
            Assert.That(unknown, Is.Empty);
        }

        [Test]
        public void NamesIgnoreLetterCaseAndTheSpacesAroundThem()
        {
            RenderFeature features = RenderFeatureList.Parse(" Shadows , AMBIENT-OCCLUSION ", out IReadOnlyList<string> unknown);

            Assert.That(features, Is.EqualTo(RenderFeature.Shadows | RenderFeature.AmbientOcclusion));
            Assert.That(unknown, Is.Empty);
        }

        [Test]
        public void EveryFeatureHasItsOwnName()
        {
            RenderFeature features = RenderFeatureList.Parse(
                "decals,distant-lights,transparent-objects,ambient-occlusion,shadows,fog-lights,volumetric-fog,occlusion-culling",
                out IReadOnlyList<string> unknown);

            Assert.That(features, Is.EqualTo(RenderFeature.OcclusionCulling | RenderFeature.VolumetricFog | RenderFeature.FogLights
                | RenderFeature.Shadows | RenderFeature.AmbientOcclusion | RenderFeature.DistantLights | RenderFeature.TransparentObjects
                | RenderFeature.Decals));
            Assert.That(unknown, Is.Empty);
        }

        [Test]
        public void AnUnknownNameIsReportedAndSwitchesNothingOff()
        {
            RenderFeature features = RenderFeatureList.Parse("shadow,volumetric-fog", out IReadOnlyList<string> unknown);

            Assert.That(features, Is.EqualTo(RenderFeature.VolumetricFog));
            Assert.That(unknown, Is.EqualTo(new[] { "shadow" }));
        }

        [Test]
        public void NoNamesSwitchNothingOff()
        {
            foreach (string text in new[] { null, "", " , ,", "None" })
            {
                RenderFeature features = RenderFeatureList.Parse(text, out IReadOnlyList<string> unknown);

                Assert.That(features, Is.EqualTo(RenderFeature.None), text);
                Assert.That(unknown, Is.Empty, text);
            }
        }

        [Test]
        public void TheWrittenListKeepsAFixedOrder()
        {
            RenderFeature features = RenderFeature.Decals | RenderFeature.TransparentObjects | RenderFeature.Shadows | RenderFeature.OcclusionCulling;

            Assert.That(RenderFeatureList.Format(features), Is.EqualTo("occlusion-culling,shadows,transparent-objects,decals"));
        }

        [Test]
        public void NoFeaturesAreWrittenAsNone()
        {
            Assert.That(RenderFeatureList.Format(RenderFeature.None), Is.EqualTo("none"));
        }
    }
}
