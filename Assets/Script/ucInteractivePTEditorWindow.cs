using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

[StructLayout(LayoutKind.Sequential)]
public struct ucUnityRenderOptions
{
    public int width;
    public int height;
    public float[] camera_pos;
    public float[] euler_angle;
    public float fov;

    public int sample_count;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string hdr_texture_path;
}

public class ucInteractivePTEditorWindow : ScriptableWizard
{
    private static ucInteractivePTEditorWindow window_inst = null;

    [MenuItem("Tools/U-Cycles/RayTracing Preview")]
    static void CreateW()
    {
        if(window_inst == null)
        {
            window_inst = ScriptableWizard.DisplayWizard<ucInteractivePTEditorWindow>("RayTracingPreview", "Yes", "Cancel");
        }
        window_inst.Focus();
    }

    //static string fileName = "";
    bool interactive_rendering = false;
    public static bool select_sceneview_active_camera = true;
    public static bool enable_denoise = true;
    public static Camera cam = null;
    //bool pressed = false;
    ucRenderDeviceOptions render_device_op;
    //public static int sample_count = 128;
    ucSampleCountOptions sample_count_op;
    public static float render_progress = 0.0f;

    DateTime start_time = System.DateTime.Now;
    TimeSpan offset_time;

    float save_main_camera_far_clip_value = 0.0f;

    public static Cubemap hdr_texture = null;

    ucDLLFunctionCaller dll_function_caller = null;
    ucThreadDispatcher thread_dispatcher = null;

    void OnGUI()
    {
        if(render_progress < 1.0f && interactive_rendering)
        {
            offset_time = System.DateTime.Now - start_time;            
        }
        String time_offset_string = offset_time.ToString(@"hh\:mm\:ss");
        EditorGUILayout.LabelField("Cost Time:", time_offset_string);

        //GUILayout.BeginHorizontal("Box");
        //if (GUILayout.Button("SetSaveImagePath"))
        //{
        //    string path = EditorUtility.OpenFolderPanel("Load png Textures", "", "");
        //    Debug.Log("path = " + path);
        //    fileName = EditorGUILayout.TextField("File Name:", path);            
        //}
        ////EditorGUILayout.LabelField("Status: ", status);

        ////GUI.enabled = false;
        //if (GUILayout.Button(new GUIContent("SavePTImage", "Save current ray tracing image")))
        //{
        //    Debug.Log("Save image!");

        //}
        //GUILayout.EndHorizontal();

        //Select Camera
        //cam = null;// UnityEditor.SceneView.lastActiveSceneView.camera;
        select_sceneview_active_camera = GUILayout.Toggle(select_sceneview_active_camera, "Select Sceneview Active Camera: ");
        GUI.enabled = !select_sceneview_active_camera;
        cam = (EditorGUILayout.ObjectField("Select Camera: ", cam, typeof(Camera), true)) as Camera;
        GUI.enabled = true;
        if(select_sceneview_active_camera)
        {
            cam = UnityEditor.SceneView.lastActiveSceneView.camera;
        }

        //Render device
        render_device_op = (ucRenderDeviceOptions)EditorGUILayout.EnumPopup("Render Device:", render_device_op);        

        //Sample count
        sample_count_op = (ucSampleCountOptions)EditorGUILayout.EnumPopup("Sample Count:", sample_count_op);
        int select_sample_count = GetSampleCount(sample_count_op);

        //Denoise
        enable_denoise = GUILayout.Toggle(enable_denoise, "Enable Denoise");

        //HDR sky light texture
        hdr_texture = (EditorGUILayout.ObjectField("Select Sky light HDR: ", hdr_texture, typeof(Cubemap), true)) as Cubemap;

        //Add render status bar of sample progress.
        Rect rect = GUILayoutUtility.GetRect(position.width - 6, 20);
        EditorGUI.ProgressBar(rect, render_progress, "Render Status:" + (render_progress * 100).ToString("0.00") + "%");

        ucUnityRenderOptions u3d_render_options = new ucUnityRenderOptions();
        u3d_render_options.width = cam.pixelWidth;
        u3d_render_options.height = cam.pixelHeight;
        u3d_render_options.camera_pos = new float[3];
        u3d_render_options.camera_pos[0] = cam.transform.position.x;
        u3d_render_options.camera_pos[1] = cam.transform.position.y;
        u3d_render_options.camera_pos[2] = cam.transform.position.z;
        u3d_render_options.euler_angle = new float[3];        
        u3d_render_options.euler_angle[0] = cam.transform.rotation.eulerAngles.x;
        u3d_render_options.euler_angle[1] = cam.transform.rotation.eulerAngles.y;
        u3d_render_options.euler_angle[2] = cam.transform.rotation.eulerAngles.z;
        u3d_render_options.fov = cam.fieldOfView;
        u3d_render_options.sample_count = select_sample_count;
        if(hdr_texture != null)
        {
            u3d_render_options.hdr_texture_path = Application.dataPath + "/../" + AssetDatabase.GetAssetPath(hdr_texture);
        }        

        ucCyclesInitOptions cycles_init_op = new ucCyclesInitOptions();
        cycles_init_op.width = cam.pixelWidth;
        cycles_init_op.height = cam.pixelHeight;
        cycles_init_op.render_device = (int)render_device_op;
        string lib_file_folder = Application.dataPath + "/Plugins/";
        cycles_init_op.device_working_folder = lib_file_folder;
        cycles_init_op.enable_denoise = Convert.ToInt32(enable_denoise);

        //Start Btn, needed to add bottom after all parameters have inited.
        if (interactive_rendering != GUILayout.Toggle(interactive_rendering, new GUIContent("Start", "Ray tracing result will be outputed to GameView"), "Button"))
        {
            interactive_rendering = !interactive_rendering;
            if (interactive_rendering)
            {                
                //Record time
                start_time = System.DateTime.Now;

                InteractiveRenderingStart(cycles_init_op, u3d_render_options);
            }
            else
            {
                //Debug.Log("Interactive Stop!");
                InteractiveRenderingEnd();
            }
        }
    }

    int GetSampleCount(ucSampleCountOptions op)
    {
        int sample_count = 4;
        switch(op)
        {
            case ucSampleCountOptions.LOW_4:
                sample_count = 4;
                break;
            case ucSampleCountOptions.MEDIUM_128:
                sample_count = 128;
                break;
            case ucSampleCountOptions.HIGH_512:
                sample_count = 512;
                break;
            case ucSampleCountOptions.VERY_HIGH_2048:
                sample_count = 2048;
                break;
            case ucSampleCountOptions.ULTRA_HIGH_4096:
                sample_count = 4096;
                break;
            case ucSampleCountOptions.GROUND_TRUTH_8192:
                sample_count = 8192;
                break;
        }
        return sample_count;
    }

    void InteractiveRenderingStart(ucCyclesInitOptions cycles_init_op, ucUnityRenderOptions render_options)
    {
        if (dll_function_caller == null)
        {
            if (thread_dispatcher == null)
            {
                thread_dispatcher = ucThreadDispatcher.Initialize();
            }

            dll_function_caller = new ucDLLFunctionCaller(thread_dispatcher);
        }

        dll_function_caller.LoadDLLAndInitCycles(cycles_init_op);
        
        dll_function_caller.SendAllMeshToCycles();

        dll_function_caller.SendLightsToCycles();

        Thread t = new Thread(dll_function_caller.InteractiveRenderStart(render_options));
        t.Start();

        //Create post effect component on camera
        Camera addcomponent_cam = null;
        if(cam != UnityEditor.SceneView.lastActiveSceneView.camera)
        {
            addcomponent_cam = cam;
        }
        else
        {
            addcomponent_cam = Camera.main;
        }
        addcomponent_cam.gameObject.AddComponent<ucRaytracingTexShow>();
        save_main_camera_far_clip_value = addcomponent_cam.farClipPlane;
        addcomponent_cam.farClipPlane = addcomponent_cam.nearClipPlane + 0.01f;
    }

    void InteractiveRenderingEnd()
    {
        if(dll_function_caller != null)
            dll_function_caller.Release();
        dll_function_caller = null;

        Camera addcomponent_cam = null;
        if (cam != UnityEditor.SceneView.lastActiveSceneView.camera)
        {
            addcomponent_cam = cam;
        }
        else
        {
            addcomponent_cam = Camera.main;
        }
        if (addcomponent_cam.gameObject.GetComponent<ucRaytracingTexShow>())
        {
            DestroyImmediate(addcomponent_cam.gameObject.GetComponent<ucRaytracingTexShow>());
            addcomponent_cam.farClipPlane = save_main_camera_far_clip_value;
        }
    }

    //public void Awake()
    //{        
    //}

    void OnDestroy()
    {
        if (dll_function_caller != null)
            dll_function_caller.Release();

        window_inst = null;
    }

    void OnWizardUpdate()
    {
        
    }

    void OnHierarchyChange()
    {
        //Debug.Log("OnHierarchyChange");
    }

    public void Update()
    {
        if (thread_dispatcher != null)
        {
            thread_dispatcher.Update();
        }
    }

}
