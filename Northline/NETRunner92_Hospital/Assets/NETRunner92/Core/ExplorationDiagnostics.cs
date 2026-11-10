#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NETRunner92
{
    public sealed partial class ExplorationDiagnostics : MonoBehaviour
    {
        public Action<bool> Complete;
        public string Output;
        public bool CaptureImages;
        FirstPersonController player;
        HospitalAtmosphere weather;
        readonly List<string> log = new();
        bool failed;
        float maxStep;
        int renderedFrames;

        void OnEnable() => RenderPipelineManager.beginCameraRendering += CountFrames;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= CountFrames;
        void CountFrames(ScriptableRenderContext _, Camera camera)
        {
            if (player && player.Eyes == camera) renderedFrames++;
        }

        void Check(bool passed, string text)
        {
            string line = (passed ? "PASS " : "FAIL ") + text;
            Debug.Log("NET92 QA | " + line);
            log.Add(line);
            if (!passed) failed = true;
        }

        IEnumerator Start()
        {
            player = FindAnyObjectByType<FirstPersonController>();
            weather = FindAnyObjectByType<HospitalAtmosphere>();
            Check(player && weather, "Player and atmosphere present in Play Mode.");
            if (!player || !weather) { Complete?.Invoke(false); yield break; }
            Check(player.GetComponentsInChildren<Renderer>().Length == 0, "Player has no renderable body.");
            Directory.CreateDirectory(Output);
            yield return null;
            if (!Application.isBatchMode) yield return InputTest();
            else { log.Add("Focused keyboard and mouse require a foreground Game or Player window."); SprintTest(); }

            player.enabled = false;
            if (player.TryGetComponent<LatchViewmodel>(out var latch)) yield return TestLatch(latch);
            if (latch && FindAnyObjectByType<SecurityCctv>() is SecurityCctv network) yield return TestCctv(latch, network);
            var access = FindAnyObjectByType<StairAccess>();
            Coroutine residualPower = null;
            if (access)
            {
                TestClosedSystems();
                residualPower = StartCoroutine(TrackResidualPower());
                yield return TestPatrols();
            }
            player.Warp(new Vector3(0,.04f,-36.45f), Quaternion.identity);
            yield return Capture("01_PlayerSpawn", new Vector3(0,.04f,-36.45f), 0);
            Check(Travel("Public entrance to reception", new[]{P(0,-24),P(0,-19.0f),P(0,-16.0f),P(0,-5.0f),P(0,0)}),
                "Continuous exterior-to-reception route.");
            player.Warp(new Vector3(-22,.04f,23.6f), Quaternion.Euler(0,180,0));
            Check(Travel("Ambulance vestibule", new[]{P(-22,20.4f),P(-22,18.3f),P(-22,16.25f),P(-22,15.10f)}),
                "Ambulance apron to emergency department.");
            player.Warp(new Vector3(0,.03f,-38.4f), Quaternion.identity);
            for (int i=0;i<160;i++) player.Move(Vector3.back,true,1f/90f);
            Check(player.transform.position.z > -40.08f && player.IsGrounded, "Closed gate blocks outward sprint.");
            player.Warp(new Vector3(48,.02f,8), Quaternion.identity);
            for (int i=0;i<185;i++) player.Move(Vector3.right,true,1f/90f);
            Check(player.transform.position.x < 52.02f && player.IsGrounded, "Perimeter blocks lateral sprint.");
            player.Warp(new Vector3(-6,.72f,0), Quaternion.identity);
            for (int i=0;i<90;i++) player.Move(Vector3.zero,false,1f/90f);
            Check(player.IsGrounded && Mathf.Abs(player.transform.position.y)<.09f, "Gravity settles onto the original ground slab.");

            if (access) yield return TestStairsAndLift(access);
            else
            {
                player.Warp(new Vector3(0,.03f,0), Quaternion.identity);
                Check(Circuit(-32), "Walked levels 1→4→1 using west/east/west, without jumping.");
                player.Warp(new Vector3(0,.03f,0), Quaternion.identity);
                Check(Circuit(32), "Walked the complementary east/west/east circuit in both directions.");
            }
            Check(maxStep < .23f, $"Largest vertical delta per 1/90-second motion step {maxStep:F3} m.");

            player.Warp(new Vector3(-41,.03f,-31),Quaternion.identity);
            Physics.SyncTransforms();
            int floorHits=0;
            for(int f=0;f<4;f++)
            {
                if (Physics.Raycast(new Vector3(0,f*4.2f+2f,0),Vector3.down,out var hit,2.6f,1,QueryTriggerInteraction.Ignore)
                    && Mathf.Abs(hit.point.y-f*4.2f)<.05f) floorHits++;
                else Debug.LogWarning($"NET92 FLOOR {f+1}: {hit.collider?.name} at {hit.point.y}");
            }
            Check(floorHits==4, "Four concourse slabs align at 0.0, 4.2, 8.4 and 12.6 m.");
            player.Warp(new Vector3(-41,.03f,-31),Quaternion.identity);
            if (access) OpenRoomDoorsForSweep();
            string rooms = RoomReachability.Probe();
            Check(!rooms.Contains("Missing:"), "Room sweep: " + rooms);
            var transparent = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials)
                .Distinct().Count(m=>m && m.HasProperty("_SurfaceType") && m.GetFloat("_SurfaceType")>.5f);
            bool desk = Physics.Raycast(new Vector3(-6.39f,.73f,-8.8f),Vector3.forward,out var deskHit,3.5f,1,QueryTriggerInteraction.Ignore);
            Check(desk && deskHit.collider.name.Contains("Reception_Counter"), "Major reception desk blocks chest-height movement.");
            bool wall = Physics.Raycast(new Vector3(-8f,1.05f,0),Vector3.back,out var wallHit,3.1f,1,QueryTriggerInteraction.Ignore);
            Check(wall && wallHit.collider.name.Contains("_Wall_"),"Concourse wall blocks a lateral route.");
            Check(transparent>=2, $"{transparent} distinct transparent window/water materials.");
            int rainBefore = weather.EmittedRain;
            int coverBefore = weather.RejectedByCover;
            yield return Capture("02_ExteriorWeather", new Vector3(0,.03f,-25.6f), 0);
            Check(weather.EmittedRain>rainBefore, $"Rain emitted outdoors ({weather.EmittedRain-rainBefore} new drops).");
            Check(weather.RejectedByCover>coverBefore, $"Roof-height rejection registered ({weather.RejectedByCover-coverBefore} probes).");
            yield return Capture("03_Reception", new Vector3(-1,.03f,-13.4f), 337);
            Check(weather.InteriorBlend>.80f, $"Interior ambience blend reached {weather.InteriorBlend:F2}.");
            yield return Capture("04_Patient_Ward", new Vector3(-24,4.23f,0), 90);
            yield return Capture("05_Surgery", new Vector3(7,8.43f,0), 270);
            yield return Capture("06_Research", new Vector3(6,12.63f,0), 90);
            if (residualPower != null) yield return residualPower;

            log.Add($"Graphics {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsDeviceType}, VRAM reported {SystemInfo.graphicsMemorySize} MB.");
            log.Add($"Player-camera HDRP begin-render callbacks: {renderedFrames}.");
            File.WriteAllLines(Path.Combine(Output,"playmode_checks.txt"),log);
            Complete?.Invoke(!failed);
        }

        IEnumerator InputTest()
        {
            var k = InputSystem.AddDevice<Keyboard>();
            var m = InputSystem.AddDevice<Mouse>();
            k.MakeCurrent(); m.MakeCurrent();
            player.CaptureCursor(true);
            yield return null;
            var start = player.transform.position;
            for(int i=0;i<22;i++)
            {
                InputSystem.QueueStateEvent(k,new KeyboardState(Key.W));
                yield return null;
            }
            Check(player.transform.position.z > start.z + .06f, $"WASD forward Input System event advanced {player.transform.position.z-start.z:F2} m.");
            InputSystem.QueueStateEvent(k,new KeyboardState());
            var yaw = player.transform.eulerAngles.y;
            InputSystem.QueueDeltaStateEvent(m.delta,new Vector2(55,-12));
            yield return null;
            yield return null;
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw, player.transform.eulerAngles.y)) > .1f,"Mouse delta rotated the first-person view.");
            InputSystem.QueueStateEvent(k,new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            Check(Cursor.lockState==CursorLockMode.None, "Escape released the cursor.");
            InputSystem.QueueStateEvent(m,new MouseState().WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            Check(Cursor.lockState==CursorLockMode.Locked, "Left click captured the cursor.");
            InputSystem.RemoveDevice(k);
            InputSystem.RemoveDevice(m);
            player.CaptureCursor(false);
            SprintTest();
        }

        void SprintTest()
        {
            player.Warp(new Vector3(-10,.02f,-35.65f), Quaternion.identity);
            var original = player.transform.position;
            for(int i=0;i<90;i++) player.Move(Vector3.right,false,1f/90f);
            float walk = player.transform.position.x - original.x;
            player.Warp(original, Quaternion.identity);
            for(int i=0;i<90;i++) player.Move(Vector3.right,true,1f/90f);
            float run = player.transform.position.x - original.x;
            Check(run > walk + 0.85f, $"Sprint/normal travel in one second {run:F2}/{walk:F2} m.");
        }

        static Vector2 P(float x,float z) => new(x,z);

        bool Circuit(int first)
        {
            var order=new[]{first,-first,first};
            for(int i=0;i<3;i++) if(!Connect(order[i],i,true)) return false;
            for(int i=2;i>=0;i--) if(!Connect(order[i],i,false)) return false;
            return true;
        }

        bool Connect(int core,int baseLevel,bool up)
        {
            var path = ExplorationRoute.StairLeg(core, up);
            string name=$"{(core<0?"West":"East")} core L{baseLevel+(up?1:2)} to L{baseLevel+(up?2:1)}";
            bool traveled=Travel(name,path);
            float expected=(baseLevel+(up?1:0))*4.2f;
            bool level=Mathf.Abs(player.transform.position.y-expected)<.16f && player.IsGrounded;
            Check(traveled && level,$"{name}; settled Y={player.transform.position.y:F3}, expected {expected:F2}.");
            return traveled && level;
        }

        bool Travel(string label,IEnumerable<Vector2> path)
        {
            foreach (var target in path)
            {
                var start = player.transform.position;
                int cap = Mathf.CeilToInt((Vector2.Distance(target,new Vector2(start.x,start.z))+4f)*80f);
                bool arrived = false;
                for (int step=0;step<cap;step++)
                {
                    var p=player.transform.position;
                    Vector2 d=target-new Vector2(p.x,p.z);
                    if (d.magnitude<.074f) { arrived=true; break; }
                    if ((step & 7) == 0) HospitalDoor.OpenOnDiagnosticPath(player, new Vector3(d.x,0,d.y), true);
                    player.Move(new Vector3(d.x,0,d.y)*7f,false,1f/90f);
                    maxStep = Mathf.Max(maxStep,Mathf.Abs(player.transform.position.y-p.y));
                    if(player.transform.position.y<-1.5f)break;
                }
                if (!arrived)
                {
                    Debug.LogWarning($"NET92 STUCK {label}: target {target}; player {player.transform.position}");
                    return false;
                }
            }
            return true;
        }

        IEnumerator Capture(string label,Vector3 position,float yaw,bool still = false)
        {
            player.Warp(position,Quaternion.Euler(0,yaw,0));
            float until=Time.realtimeSinceStartup+1.08f;
            while(Time.realtimeSinceStartup<until)
            {
                player.Move(Vector3.zero,false,1f/90f);
                yield return null;
            }
            CaptureCamera(label, still);
        }

        void CaptureCamera(string label, bool still = false)
        {
            if (!CaptureImages) return;
            var camera = player.Eyes;
            var hdrp = camera.GetComponent<HDAdditionalCameraData>();
            var antialiasing = hdrp.antialiasing;
            if (still)
            {
                hdrp.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                HDCamera.GetOrCreate(camera).Reset();
            }
            var rt = new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var request=new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(camera,request))
            {
                RenderPipeline.SubmitRenderRequest(camera,request);
                var previous=RenderTexture.active;
                RenderTexture.active=rt;
                var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                tex.ReadPixels(new Rect(0,0,1920,1080),0,0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(Output,label+".png"),tex.EncodeToPNG());
                RenderTexture.active=previous;
                Destroy(tex);
            }
            rt.Release();
            Destroy(rt);
            hdrp.antialiasing = antialiasing;
        }
    }
}
#endif



