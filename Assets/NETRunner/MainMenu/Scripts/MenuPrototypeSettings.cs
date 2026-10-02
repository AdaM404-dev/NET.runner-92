using UnityEngine;

namespace NetRunner.MenuPrototype
{
    [CreateAssetMenu(menuName = "NET.runner/Menu prototype settings")]
    public sealed class MenuPrototypeSettings : ScriptableObject
    {
        [Header("Timing (unscaled seconds)")]
        [Min(0.05f)] public float bootLineDuration = 0.65f;
        [Min(0.05f)] public float titleDuration = 0.85f;
        [Min(0.05f)] public float fadeDuration = 0.45f;
        [Min(0.02f)] public float signalDuration = 0.14f;
        public Vector2 cameraInterval = new Vector2(18f, 27f);
        public Vector2 eventInterval = new Vector2(38f, 62f);
        [Range(0f, 1f)] public float eventProbability = 0.75f;
        [Min(1f)] public float loadingDuration = 9f;
        [Min(0.2f)] public float terminalHoldDuration = 2.5f;

        [Header("Interface")]
        public Color accent = new Color(0.44f, 0.70f, 0.73f);
        public Color warning = new Color(0.75f, 0.52f, 0.27f);
        public Color text = new Color(0.81f, 0.83f, 0.82f);
        public string facilityStatus = "PARTIAL ACTIVITY DETECTED";
        public string networkStatus = "CONNECTED";
        public string activeCameras = "04 / 18";
        public string powerDraw = "+417%";

        [Header("System text")]
        public string[] bootLines =
        {
            "INITIALIZING NETWORK INTERFACE...",
            "SEARCHING FOR ACTIVE NODES",
            "WAREHOUSE NETWORK FOUND",
            "ESTABLISHING CONNECTION..."
        };
        public string[] loadingLines =
        {
            "establishing secure tunnel",
            "bypassing firewall",
            "reconstructing environment",
            "loading surveillance network",
            "synchronizing local systems"
        };
        [TextArea(4, 9)] public string[] internalLogs =
        {
            "INTERNAL LOG // 03:42:17\nPOWER CONSUMPTION    +417%\nAUTHORIZED PERSONNEL    0\nACTIVE MACHINES        23",
            "INCIDENT BUFFER // CAM_08\n02:14  motion detected\n02:14  camera disabled\n02:15  camera restored\n02:15  camera disabled\n02:16  unknown user connected",
            "MAINTENANCE QUEUE // BAY_04\nVENTILATION     manual override\nLAST SERVICE    unavailable\nRELAY CYCLE     0000092\nOPERATOR        unresolved"
        };

        public float CameraDelay => Random.Range(Mathf.Max(1f, cameraInterval.x), Mathf.Max(cameraInterval.x, cameraInterval.y));
        public float EventDelay => Random.Range(Mathf.Max(1f, eventInterval.x), Mathf.Max(eventInterval.x, eventInterval.y));
    }
}
