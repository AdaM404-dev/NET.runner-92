using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NETRunner92.Editor
{
    [InitializeOnLoad]
    public static class PlayModeValidation
    {
        const string Session = "NET92.PlayModeValidation";
        static PlayModeValidation() => EditorApplication.playModeStateChanged += Changed;

        public static void RefineAndRun()
        {
            HospitalSceneBuilder.RefineReadability();
            Run();
        }

        public static void RunAndBuild()
        {
            SessionState.SetBool(Session+".build",true);
            Run();
        }

        public static void InstallAndRun()
        {
            HospitalSceneBuilder.InstallHospitalSystems();
            Run();
        }

        public static void InstallLatchAndRun()
        {
            HospitalSceneBuilder.InstallLatch07();
            Run();
        }

        public static void InstallCctvAndRun()
        {
            HospitalSceneBuilder.InstallCctv();
            Run();
        }

        public static void Run()
        {
            if (!File.Exists(HospitalSceneBuilder.ScenePath))
                throw new FileNotFoundException(HospitalSceneBuilder.ScenePath);
            EditorSceneManager.OpenScene(HospitalSceneBuilder.ScenePath);
            SessionState.SetBool(Session,true);
            EditorApplication.isPlaying=true;
        }

        static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Session,false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var o=new GameObject("Transient validation");
                var d=o.AddComponent<ExplorationDiagnostics>();
                d.Output=Path.GetFullPath(Object.FindAnyObjectByType<SecurityCctv>() ? "../Renders/Phase_07_CCTV"
                    : Object.FindAnyObjectByType<LatchViewmodel>() ? "../Renders/Phase_06_Latch07"
                    : Object.FindAnyObjectByType<StairAccess>() ? "../Renders/Phase_05_Unity/Interactive" : "../Renders/Phase_05_Unity");
                d.CaptureImages=SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null;
                d.Complete=ok=>{
                    SessionState.SetInt(Session+".result",ok?0:2);
                    EditorApplication.isPlaying=false;
                };
            }
            else if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Session,false);
                int result=SessionState.GetInt(Session+".result",2);
                if(result==0 && SessionState.GetBool(Session+".build",false))
                {
                    SessionState.SetBool(Session+".build",false);
                    try { HospitalSceneBuilder.BuildWindows(); }
                    catch (System.Exception ex) { Debug.LogException(ex); result=3; }
                }
                EditorApplication.Exit(result);
            }
        }
    }
}


