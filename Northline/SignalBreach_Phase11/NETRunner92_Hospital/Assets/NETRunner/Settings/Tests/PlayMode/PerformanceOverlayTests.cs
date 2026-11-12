using System.Collections;
using NetRunner.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace NetRunner.Settings.Tests
{
    /// <summary>Exercises the performance panel and the reserved F4 shortcut.</summary>
    public sealed class PerformanceOverlayTests
    {
        [UnityTest]
        public IEnumerator F4OpensAndClosesThePanelCreatedAtStartup()
        {
            yield return null;

            var panel = Object.FindAnyObjectByType<PerformanceOverlay>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.IsVisible, Is.False);

            InputSettings settings = InputSystem.settings;
            InputSettings.BackgroundBehavior background = settings.backgroundBehavior;
            InputSettings.EditorInputBehaviorInPlayMode routing = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Send(keyboard, new KeyboardState());
                Send(keyboard, new KeyboardState(Key.F4));
                Assert.That(panel.IsVisible, Is.True);
                Send(keyboard, new KeyboardState());
                Assert.That(panel.IsVisible, Is.True);
                Send(keyboard, new KeyboardState(Key.F4));
                Assert.That(panel.IsVisible, Is.False);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                settings.editorInputBehaviorInPlayMode = routing;
                settings.backgroundBehavior = background;
            }
        }

        [Test]
        public void AnOldF4BindingMovesToItsDefaultAndKeepsOtherBindings()
        {
            var preferences = new GamePreferences();
            preferences.Bind(GameAction.Interact, Key.F4);
            preferences.Bind(GameAction.Cctv, Key.F);

            preferences.Validate();

            Assert.That(GamePreferences.AllowedKey(Key.F4), Is.False);
            Assert.That(preferences.KeyFor(GameAction.Interact), Is.EqualTo(Key.E));
            Assert.That(preferences.KeyFor(GameAction.Cctv), Is.EqualTo(Key.F));
        }

        [Test]
        public void AnOldF4BindingUsesAnUnusedDefaultWhenItsDefaultIsTaken()
        {
            var preferences = new GamePreferences();
            preferences.Bind(GameAction.Interact, Key.F4);
            preferences.Bind(GameAction.Cctv, Key.E);

            preferences.Validate();

            Assert.That(preferences.KeyFor(GameAction.Interact), Is.EqualTo(Key.Digit1));
            Assert.That(preferences.KeyFor(GameAction.Cctv), Is.EqualTo(Key.E));
        }

        private static void Send(Keyboard keyboard, KeyboardState state)
        {
            InputSystem.QueueStateEvent(keyboard, state);
            InputSystem.Update();
        }
    }
}
