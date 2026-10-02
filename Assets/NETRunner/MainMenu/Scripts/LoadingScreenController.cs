using System.Collections;
using UnityEngine;

namespace NetRunner.MenuPrototype
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        public MenuPrototypeSettings settings;
        public SystemLogController logs;
        public float Progress { get; private set; }
        public int CompletedSequences { get; private set; }

        public IEnumerator Run(MenuView view, SurveillanceCameraController feeds, string entry)
        {
            Progress = 0;
            yield return MenuView.Fade(view.Menu, 1, 0, settings.fadeDuration);
            MenuView.SetVisible(view.Menu, false);
            MenuView.SetVisible(view.Terminal, true);
            view.Terminal.style.opacity = 0.82f;
            view.BootText.text = "ESTABLISHING CONNECTION...\n" + entry;
            yield return new WaitForSecondsRealtime(0.65f);
            yield return feeds.InterruptSignal();
            MenuView.SetVisible(view.Terminal, false);
            MenuView.SetVisible(view.Loading, true);
            view.Loading.style.opacity = 1;
            view.ArchiveLog.text = logs.ChooseArchive();
            float start = Time.unscaledTime;
            int previousPercent = -1;
            do
            {
                float elapsed = (Time.unscaledTime - start) / Mathf.Max(1f, settings.loadingDuration);
                // Uneven transfer speed without fake stalls or backward progress.
                Progress = Mathf.Clamp01(elapsed - Mathf.Sin(elapsed * Mathf.PI * 2) * 0.045f);
                int percent = Mathf.FloorToInt(Progress * 100);
                if (percent != previousPercent)
                {
                    view.SetProgress(Progress);
                    view.LoadingLog.text = logs.AccessLog(Progress);
                    previousPercent = percent;
                }
                yield return null;
            } while (Progress < 1);
            view.SetProgress(1);
            yield return new WaitForSecondsRealtime(0.45f);
            MenuView.SetVisible(view.Terminal, true);
            view.Terminal.style.opacity = 1;
            view.BootText.text = "EXTERNAL CONNECTION TERMINATED\nLOCAL INTERFACE ACTIVE\n\nOBSERVATION CHANNEL RESUMING...";
            yield return new WaitForSecondsRealtime(settings.terminalHoldDuration);
            MenuView.SetVisible(view.Loading, false);
            CompletedSequences++;
            view.ShowMenu();
        }
    }
}
