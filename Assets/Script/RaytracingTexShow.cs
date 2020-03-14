using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//#if UNITY_5_4_OR_NEWER
//[ImageEffectAllowedInSceneView]
//#endif
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
        shader = Shader.Find("Unlit/RT_result_draw");
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

    //DateTime last_time;
    //void OnRenderImage(RenderTexture source, RenderTexture destination)
    //{
    //    //Debug.Log("rt_texture = " + rt_texture);

    //    if(rt_texture != null)
    //    {
    //        rt_texture.Apply();
    //        mtl.SetTexture("_RTTex", rt_texture);

    //        Graphics.Blit(source, destination, mtl);
    //    }
    //}

    // Will be called from camera after regular rendering is done.
    public void OnPostRender()
    {
        if (rt_texture)
        {
            mtl.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mtl.SetInt("_ZWrite", 0);
            mtl.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

            rt_texture.Apply();
            mtl.SetTexture("_RTTex", rt_texture);
        }       

        GL.PushMatrix();
        GL.LoadOrtho();

        // activate the first shader pass (in this case we know it is the only pass)
        mtl.SetPass(0);
        // draw a quad over whole screen
        GL.Begin(GL.QUADS);
        GL.TexCoord(new Vector3(0, 0, 0));
        GL.Vertex3(-1, -1, 0);
        GL.TexCoord(new Vector3(1, 0, 0));
        GL.Vertex3(1, -1, 0);
        GL.TexCoord(new Vector3(1, 1, 0));
        GL.Vertex3(1, 1, 0);
        GL.TexCoord(new Vector3(0, 1, 0));
        GL.Vertex3(-1, 1, 0);
        GL.End();

        GL.PopMatrix();
    }
}
