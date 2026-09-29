using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController))]
public sealed class NexusPlayer : MonoBehaviour
{
    public Camera viewCamera;
    public Animator animator;
    public LODGroup lodGroup;
    public Transform visual;
    public bool firstPerson;
    public bool previewMode;
    public float walkSpeed=1.65f, runSpeed=3.25f, sensitivity=2f;
    CharacterController motor;
    Renderer[] renderers;
    float yaw, pitch=8f, verticalSpeed;
    Vector3 spawn;
    bool cursorCaptured=true, automated;
    string notice="";
    float noticeUntil;
    public bool Grounded => motor && motor.isGrounded;
    public Vector3 Position => transform.position;

    void Awake()
    {
        motor=GetComponent<CharacterController>();spawn=transform.position;
        renderers=visual.GetComponentsInChildren<Renderer>(true);
        automated=Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-nexus-autotest");
        Application.runInBackground=true;
        yaw=transform.eulerAngles.y;
        SetView(firstPerson);
        if(!automated) CaptureCursor(true);
    }
    void Start() { if(automated) StartCoroutine(AutoTest()); }
    void CaptureCursor(bool value) { cursorCaptured=value; Cursor.lockState=value?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!value; }
    public void SetView(bool value)
    {
        firstPerson=value;
        if(animator && animator.layerCount>1)animator.SetLayerWeight(1,value?1:0);
        if(lodGroup)lodGroup.ForceLOD(value?0:-1);
        foreach(var r in renderers)
        {
            string n=r.name;
            bool head=n.Contains("Head_Neck")||n.Contains("Hair_")||n.Contains("Eyebrow")||n.Contains("Eyeball")||n.Contains("_Eye")||n.Contains("Iris")||n.Contains("Pupil");
            r.shadowCastingMode=value&&head?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
        }
    }
    void Update()
    {
        if(automated)return;
        if(Input.GetKeyDown(KeyCode.Escape))CaptureCursor(!cursorCaptured);
        if(Input.GetMouseButtonDown(0)&&!cursorCaptured)CaptureCursor(true);
        if(Input.GetKeyDown(KeyCode.Tab))SetView(!firstPerson);
        if(Input.GetKeyDown(KeyCode.F1))UnityEngine.SceneManagement.SceneManager.LoadScene(previewMode?"MainTest":"CharacterPreview");
        if(Input.GetKeyDown(KeyCode.R))Respawn();
        if(Input.GetKeyDown(KeyCode.E))Interact();
        if(cursorCaptured){yaw+=Input.GetAxisRaw("Mouse X")*sensitivity;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*sensitivity,-78,78);}
        Vector2 input=cursorCaptured?new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")):Vector2.zero;
        Simulate(input,Input.GetKey(KeyCode.LeftShift),Input.GetKeyDown(KeyCode.Space),Time.deltaTime);
    }
    public void Simulate(Vector2 input,bool run,bool jump,float dt)
    {
        if(previewMode)input=Vector2.zero;
        input=Vector2.ClampMagnitude(input,1);Vector3 direction=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
        if(firstPerson)transform.rotation=Quaternion.Euler(0,yaw,0);
        else if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-12*dt));
        float speed=(run?runSpeed:walkSpeed)*input.magnitude;
        if(motor.isGrounded && verticalSpeed<0)verticalSpeed=-2;
        if(jump && motor.isGrounded && !previewMode)verticalSpeed=4.4f;
        verticalSpeed-=14*dt;motor.Move((direction*(run?runSpeed:walkSpeed)+Vector3.up*verticalSpeed)*dt);
        if(animator)animator.SetFloat("Speed",speed,.15f,dt);
        if(transform.position.y<-12)Respawn();
    }
    public void Respawn(){motor.enabled=false;transform.position=spawn;motor.enabled=true;verticalSpeed=0;}
    void LateUpdate()
    {
        if(!viewCamera)return;
        Quaternion rotation=Quaternion.Euler(pitch,yaw,0);
        if(firstPerson){viewCamera.transform.position=transform.position+Vector3.up*1.66f+transform.forward*.17f;viewCamera.transform.rotation=rotation;viewCamera.fieldOfView=75;}
        else
        {
            Vector3 focus=transform.position+Vector3.up*1.34f;Vector3 offset=rotation*new Vector3(.36f,.14f,-3.15f);float distance=offset.magnitude;
            if(Physics.SphereCast(focus,.14f,offset.normalized,out var hit,distance,1<<9,QueryTriggerInteraction.Ignore))distance=Mathf.Max(.45f,hit.distance-.08f);
            viewCamera.transform.position=focus+offset.normalized*distance;viewCamera.transform.LookAt(focus);viewCamera.fieldOfView=53;
        }
    }
    void Interact()
    {
        if(Physics.Raycast(viewCamera.transform.position,viewCamera.transform.forward,out var hit,3.5f,~(1<<8)))
        {var door=hit.collider.GetComponentInParent<NexusDoor>();if(door){door.Toggle();notice=door.IsOpen?"Access granted":"Door closed";noticeUntil=Time.time+2;}}
    }
    void OnGUI()
    {
        if(automated)return;
        GUI.color=new Color(.8f,.9f,.93f);GUI.Label(new Rect(24,18,700,28),previewMode?"NEXUS  /  CHARACTER STUDY":"NEXUS  /  NEAR-FUTURE WAREHOUSE");
        GUI.color=new Color(.72f,.78f,.8f);GUI.Label(new Rect(24,46,1100,26),previewMode?"Mouse: orbit    Tab: first person    F1: warehouse    Esc: release cursor":"WASD: move    Mouse: look    Shift: run    Space: jump    E: door    Tab: change view    F1: character preview    R: reset    Esc: cursor");
        if(firstPerson){GUI.color=new Color(.65f,.82f,.86f,.55f);GUI.Label(new Rect(Screen.width*.5f-4,Screen.height*.5f-8,16,20),"·");}
        if(Time.time<noticeUntil){GUI.color=Color.white;GUI.Label(new Rect(Screen.width*.5f-90,Screen.height-80,300,30),notice);}
    }
    [Serializable] class TestResult { public bool grounded, moved, jumpReturned, firstPersonEnabled; public float movedMetres; public int doors; public string scene; }
    void CaptureView(string path)
    {
        var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);rt.Create();
        var previous=RenderTexture.active;viewCamera.targetTexture=rt;viewCamera.Render();RenderTexture.active=rt;
        var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());viewCamera.targetTexture=null;RenderTexture.active=previous;rt.Release();Destroy(rt);Destroy(texture);
    }
    IEnumerator AutoTest()
    {
        string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../QA/Runtime"));Directory.CreateDirectory(directory);
        yield return new WaitForSeconds(1);
        if(previewMode)
        {
            yaw=155;pitch=4;yield return new WaitForSeconds(.3f);CaptureView(Path.Combine(directory,"04_CharacterPreview.png"));
            Application.Quit(0);yield break;
        }
        for(int i=0;i<90;i++){Simulate(Vector2.zero,false,false,1f/60);yield return null;}
        bool grounded=Grounded;Vector3 start=Position;
        for(int i=0;i<120;i++){Simulate(new Vector2(0,1),false,false,1f/60);yield return null;}
        float moved=Vector3.Distance(start,Position);
        CaptureView(Path.Combine(directory,"01_ThirdPerson.png"));yield return new WaitForSeconds(.3f);
        Simulate(Vector2.zero,false,true,1f/60);
        for(int i=0;i<150;i++){Simulate(Vector2.zero,false,false,1f/60);yield return null;}
        bool landed=Grounded;SetView(true);pitch=12;yield return new WaitForSeconds(.5f);
        CaptureView(Path.Combine(directory,"02_FirstPerson.png"));yield return new WaitForSeconds(.3f);
        pitch=70;yield return new WaitForSeconds(.3f);CaptureView(Path.Combine(directory,"03_LookDown.png"));yield return new WaitForSeconds(.3f);
        var doors=FindObjectsByType<NexusDoor>();if(doors.Length>0)doors[0].Toggle();
        var result=new TestResult{grounded=grounded,moved=moved>2,jumpReturned=landed,firstPersonEnabled=firstPerson,movedMetres=moved,doors=doors.Length,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name};
        File.WriteAllText(Path.Combine(directory,"RuntimeValidation.json"),JsonUtility.ToJson(result,true));
        if(grounded&&moved>2&&landed)UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterPreview");else Application.Quit(2);
    }
}
