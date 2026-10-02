using UnityEngine;

namespace NetRunner.MenuPrototype
{
    public sealed class SystemLogController : MonoBehaviour
    {
        public MenuPrototypeSettings settings;
        private int previous = -1;

        public string ChooseArchive()
        {
            if (settings.internalLogs.Length == 0)
            {
                return "INTERNAL ARCHIVE / UNAVAILABLE";
            }
            int index = Random.Range(0, settings.internalLogs.Length);
            if (settings.internalLogs.Length > 1 && index == previous)
            {
                index = (index + 1) % settings.internalLogs.Length;
            }
            previous = index;
            return settings.internalLogs[index];
        }

        public string AccessLog(float progress)
        {
            int count = Mathf.Min(settings.loadingLines.Length, 1 + Mathf.FloorToInt(progress * settings.loadingLines.Length));
            var output = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                output.Append("> ").Append(settings.loadingLines[i]).Append('\n');
            }
            return output.ToString();
        }
    }
}
