using NetRunner.Core;
using UnityEngine;

namespace NetRunner.Diagnostics
{
    /// <summary>
    /// The first line of every benchmark CSV: the graphics card and API, the
    /// output resolution, the quality level and the rendering features this
    /// run switched off, so results from different machines and runs can be
    /// told apart.
    /// </summary>
    public static class BenchmarkReport
    {
        /// <summary>The run's context line.</summary>
        public static string Context()
        {
            return $"{SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}; {Screen.width}x{Screen.height}; "
                + $"{QualitySettings.names[QualitySettings.GetQualityLevel()]}; without: {RenderFeatureList.Format(RenderFeatureSwitch.FromLaunchOptions())}";
        }
    }
}
