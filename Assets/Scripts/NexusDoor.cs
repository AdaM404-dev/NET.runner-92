using UnityEngine;
public sealed class NexusDoor : MonoBehaviour
{
    public bool loadingDoor;
    public float angle=95, lift=3.8f;
    public bool IsOpen {get;private set;}
    Quaternion closedRotation;Vector3 closedPosition;float progress;
    void Awake(){closedRotation=transform.rotation;closedPosition=transform.position;}
    public void Toggle(){IsOpen=!IsOpen;}
    void Update()
    {
        progress=Mathf.MoveTowards(progress,IsOpen?1:0,Time.deltaTime*(loadingDoor ? 0.45f : 1.3f));float eased=progress*progress*(3-2*progress);
        if(loadingDoor)transform.position=closedPosition+Vector3.up*(lift*eased);
        else transform.rotation=Quaternion.AngleAxis(angle*eased,Vector3.up)*closedRotation;
    }
}
