using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.World.Editor
{
    public static partial class HospitalSceneBuilder
    {
        public static void RefineReadability()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var root=GameObject.Find("Atmosphere").transform;
            var old=root.Find("Low-output clinical fill");
            if(old) Object.DestroyImmediate(old.gameObject);
            var fills=Node("Low-output clinical fill",root);
            for(int f=0;f<4;f++)
            {
                float elevation=f*4.2f;
                for(int x=-28;x<=28;x+=8)
                    Fill(fills,new Vector3(x,elevation+2.86f,.05f), f==1?190f:320f, 6.6f,
                        f==3?new Color(.78f,.93f,1f):new Color(.78f,.86f,1f));
                foreach(int x in new[]{-32,32})
                {
                    Fill(fills,new Vector3(x,elevation+2.86f,-4.15f),230f,6.2f,new Color(.74f,.84f,.94f));
                    if(f<3) Fill(fills,new Vector3(x,elevation+3.35f,-9.09f),195f,5.2f,new Color(.77f,.85f,.98f));
                }
            }
            foreach(var p in new[]{new Vector3(-4.5f,2.93f,-10.15f),new Vector3(4.2f,2.95f,-9.4f),new Vector3(-21.8f,2.95f,8.6f)})
                Fill(fills,p,440f,7.2f,new Color(.77f,.87f,.99f));
            foreach(var p in AssetDatabase.FindAssets("t:VolumeProfile",new[]{Atmos+"/Fog"}))
            {
                var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetDatabase.GUIDToAssetPath(p));
                if(profile.name.Contains("Storm"))continue;
                if(profile.TryGet<Exposure>(out var e))
                {
                    e.fixedExposure.Override(profile.name.Contains("Patient")?2.65f:profile.name.Contains("Research")?2.80f:2.5f);
                    EditorUtility.SetDirty(e);
                }
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        static void Fill(Transform root,Vector3 at,float power,float distance,Color color)
        {
            var o=Node("Reflected clinical practical",root);
            o.position=at;
            var light=o.gameObject.AddComponent<Light>();
            light.type=LightType.Point;light.color=color;light.intensity=power;light.range=distance;
            light.shadows=LightShadows.None;
            o.gameObject.AddComponent<HDAdditionalLightData>().volumetricDimmer=.08f;
        }
    }
}

