using System.Collections;
using System.Linq;
using NUnit.Framework;
using NetRunner.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace NetRunner.Hud.Tests
{
    /// <summary>
    /// PlayMode tests of the HUD in the real hospital scene (Phase 11): the
    /// health and stamina bars have 18 segments, and the health bar follows
    /// the player's health, rounding up (75 health lights 14 of 18). The
    /// exploration view needs the captured mouse, which a headless test
    /// does not have; the gameplay acceptance check covers it.
    /// </summary>
    public sealed class HospitalHudTests
    {
        private const string HospitalScene = "Hospital_Exploration";
        private const int StartupFrames = 5;

        [UnityTest]
        public IEnumerator TheBarsHaveEighteenSegmentsAndStartFull()
        {
            var hud = new VisualElement();
            yield return LoadHospital(root => hud = root);

            Assert.That(hud.Q("health-bar").childCount, Is.EqualTo(18));
            Assert.That(hud.Q("stamina-bar").childCount, Is.EqualTo(18));
            Assert.That(Depleted(hud), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator AHitDarkensTheHealthBarBySegment()
        {
            var hud = new VisualElement();
            yield return LoadHospital(root => hud = root);

            Object.FindAnyObjectByType<FirstPersonController>().TakeDamage(25f);
            yield return null;

            Assert.That(Depleted(hud), Is.EqualTo(4));
        }

        private static int Depleted(VisualElement hud)
        {
            return hud.Q("health-bar").Children().Count(segment => segment.ClassListContains("depleted"));
        }

        private static IEnumerator LoadHospital(System.Action<VisualElement> found)
        {
            yield return SceneManager.LoadSceneAsync(HospitalScene, LoadSceneMode.Single);
            for (int i = 0; i < StartupFrames; i++)
            {
                yield return null;
            }

            found(Object.FindAnyObjectByType<HospitalHud>().GetComponent<UIDocument>().rootVisualElement);
        }
    }
}
