using System.Collections;
using UnityEngine;

namespace NetRunner.MenuPrototype
{
    public sealed class MenuBootSequence : MonoBehaviour
    {
        public MenuPrototypeSettings settings;
        private bool skipRequested;
        public void Skip() => skipRequested = true;

        public IEnumerator Run(MenuView view, SurveillanceCameraController feeds)
        {
            skipRequested = false;
            view.BootText.text = "";
            MenuView.SetVisible(view.Terminal, true);
            view.Terminal.style.opacity = 1;
            view.Focus("skip-boot");
            foreach (string line in settings.bootLines)
            {
                if (skipRequested)
                {
                    break;
                }
                view.BootText.text += line + "\n";
                float until = Time.unscaledTime + settings.bootLineDuration;
                while (!skipRequested && Time.unscaledTime < until)
                {
                    yield return null;
                }
            }
            if (!skipRequested)
            {
                yield return MenuView.Fade(view.Terminal, 1, 0, settings.fadeDuration);
                MenuView.SetVisible(view.Menu, true);
                view.Title.text = "NET ACCESS NODE 92";
                view.Title.style.fontSize = 29;
                yield return new WaitForSecondsRealtime(settings.titleDuration);
                yield return feeds.InterruptSignal();
            }
            view.Title.text = "NET.RUNNER";
            view.Title.style.fontSize = 65;
            view.ShowMenu();
        }
    }
}
