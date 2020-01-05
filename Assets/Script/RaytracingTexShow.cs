using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_5_4_OR_NEWER
[ImageEffectAllowedInSceneView]
#endif
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class RaytracingTexShow : MonoBehaviour
{
    protected Camera m_Camera;
    protected Shader shader;
    protected Material mtl;

    static public Texture2D rt_texture = null;

    RaytracingTexShow()
    {
        
    }

    void Awake()
    {
        shader = Shader.Find("PP/PP_CopyTexture");
        mtl = new Material(shader);
        mtl.hideFlags = HideFlags.HideAndDontSave;
    }

    // Start is called before the first frame update
    void Start()
    {        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        m_Camera = GetComponent<Camera>();
    }

    DateTime last_time;
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Debug.Log("rt_texture = " + rt_texture);

        if(rt_texture != null)
        {
            rt_texture.Apply();
            mtl.SetTexture("_RTTex", rt_texture);

            Graphics.Blit(source, destination, mtl);

            //if(last_time == null)
            //{
            //    last_time = new DateTime();
            //}
            //if (System.DateTime.Now.Second - last_time.Second > 10.0f)
            //{
            //    byte[] png_data = rt_texture.EncodeToPNG();
            //    System.IO.File.WriteAllBytes("./pt_image.png", png_data);
            //    //Debug.Log("Save pt_image.png in main thread!");
            //}
        }
    }     
}
