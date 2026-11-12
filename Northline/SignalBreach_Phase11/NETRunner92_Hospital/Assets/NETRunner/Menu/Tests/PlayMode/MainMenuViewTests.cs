using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using NetRunner.Saves;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace NetRunner.Menu.Tests
{
    /// <summary>
    /// PlayMode tests of the main menu in its own scene (Phase 11): it offers
    /// New Game, Load Game, Settings and Exit, and Continue only when a valid
    /// save exists. Saves go to Logs/Tests only.
    /// </summary>
    public sealed class MainMenuViewTests
    {
        private const string SessionPath = "Assets/NETRunner/Menu/Data/RunSession.asset";
        private const int StartupFrames = 5;

        private string folder;
        private RunSession session;

        [SetUp]
        public void UseAnEmptySaveFolder()
        {
            folder = Path.Combine(Application.dataPath, "..", "Logs", "Tests", "Menu-" + Guid.NewGuid().ToString("N"));
#if UNITY_EDITOR
            session = UnityEditor.AssetDatabase.LoadAssetAtPath<RunSession>(SessionPath);
#endif
            session.UseSaveFolder(folder);
        }

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [UnityTest]
        public IEnumerator WithoutSavesItOffersEverythingButContinue()
        {
            var menu = new VisualElement();
            yield return LoadMenu(root => menu = root);

            Assert.That(menu.Q<Button>("new-game"), Is.Not.Null);
            Assert.That(menu.Q<Button>("load-game"), Is.Not.Null);
            Assert.That(menu.Q<Button>("settings"), Is.Not.Null);
            Assert.That(menu.Q<Button>("exit"), Is.Not.Null);
            Assert.That(menu.Q<Button>("continue"), Is.Null);
        }

        [UnityTest]
        public IEnumerator AValidSaveAddsContinue()
        {
            session.Store.Save(1, new HospitalCheckpoint(DateTime.UtcNow, new Vector3(0f, .04f, -34.3f), 0f, 0f, 0, false, false,
                "1. szint · Előtér", new string[0]));
            var menu = new VisualElement();

            yield return LoadMenu(root => menu = root);

            Assert.That(menu.Q<Button>("continue"), Is.Not.Null);
        }

        private static IEnumerator LoadMenu(Action<VisualElement> found)
        {
            yield return SceneManager.LoadSceneAsync(HospitalRun.MenuScene, LoadSceneMode.Single);
            for (int i = 0; i < StartupFrames; i++)
            {
                yield return null;
            }

            found(UnityEngine.Object.FindAnyObjectByType<MainMenuView>().GetComponent<UIDocument>().rootVisualElement);
        }
    }
}
