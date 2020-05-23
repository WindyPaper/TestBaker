using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ucPreviewRenderWindow : EditorWindow
{
    private static ucPreviewRenderWindow window_instance = null;

    PreviewRenderUtility pre_util;

    public static Texture2D rt_texture = null;

    private static int width, height;

    private static void _CreateWIns(int w, int h)
    {
        if(window_instance != null)
        {
            return;
        }

        width = w;
        height = h;
        Rect r = new Rect(100, 100, w, h);
        window_instance = GetWindowWithRect<ucPreviewRenderWindow>(r, true, "UCRenderPreview", true);
        window_instance.autoRepaintOnSceneChange = true;
    }

    public static void CreatePreviewWindow(int w, int h)
    {
        if(window_instance == null)
        {
            _CreateWIns(w, h);
        }
        else
        {
            if(width != w || height != h)
            {
                window_instance.Close();
                window_instance = null;
                _CreateWIns(w, h);
            }

            window_instance.Focus();
        }        
    }

    void OnGUI()
    {
        if(rt_texture)
        {
            Rect r = new Rect(0, 0, rt_texture.width, rt_texture.height);            
            EditorGUI.DrawPreviewTexture(r, rt_texture);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Repaint();
    }
}
