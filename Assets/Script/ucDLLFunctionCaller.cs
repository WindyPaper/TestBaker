using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEditor;
using System.Reflection;
using System.Threading;

public class ucDLLFunctionCaller
{
    static IntPtr nativeLibraryPtr;
    ucThreadDispatcher thread_dispatcher = null;

    delegate int bake_scene(int number, int multiplyBy);
    delegate bool init_cycles(ucCyclesInitOptions op);

    delegate int release_cycles();

    delegate int unity_add_mesh(ucCyclesMeshData mesh_data, ucCyclesMtlData[] mtls);

    delegate int bake_lightmap();

    public delegate void RenderImageCb(IntPtr image_array, [MarshalAs(UnmanagedType.I4)]int w, [MarshalAs(UnmanagedType.I4)]int h, int type, float progress);
    delegate int interactive_pt_rendering(ucUnityRenderOptions ops, [MarshalAs(UnmanagedType.FunctionPtr)]RenderImageCb pDelegate);

    //add light to Cycles
    delegate int unity_add_light(ucLightData light_data);

    public ucDLLFunctionCaller(ucThreadDispatcher thread_dispatcher)
    {
        this.thread_dispatcher = thread_dispatcher;
    }    

    public void LoadDLLAndInitCycles(ucCyclesInitOptions op)
    {
        LoadDLL();

        InitCycles(op);
    }

    public void Release()
    {
        if(nativeLibraryPtr == IntPtr.Zero)
        {
            return;
        }

        ucNative.Invoke<int, release_cycles>(nativeLibraryPtr);

        UnloadDLL();
    }    

    void LoadDLL()
    {
        if (nativeLibraryPtr != IntPtr.Zero) return;

        string dll_path = Application.dataPath + "/Plugins/";
        string dll_file_name = "cycles.dll";
        ucNative.LoadLibraryFlags flags = ucNative.LoadLibraryFlags.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS;
        string dll_full_name = dll_path + dll_file_name;
        ucNative.AddDllDirectory(dll_path);
        nativeLibraryPtr = ucNative.LoadLibraryEx(dll_full_name, IntPtr.Zero, flags);

        if (nativeLibraryPtr == IntPtr.Zero)
        {
            Debug.LogError("Failed to load native library. Path = " + dll_full_name + " Last Error Code = " + Marshal.GetLastWin32Error());

            Debug.Log(ucNative.GetErrorMessage(Marshal.GetLastWin32Error()));
        }
    }

    void InitCycles(ucCyclesInitOptions op)
    {        
        //Debug.Log("w = "+op.width+"h = "+op.height);
        bool result = ucNative.Invoke<bool, init_cycles>(nativeLibraryPtr, op);
    }    

    public void SendAllMeshToCycles()
    {
        List<ucCyclesMeshMtlData> mesh_mtl_datas = new List<ucCyclesMeshMtlData>();
        ucExportMesh.ExportCurrSceneMesh(ref mesh_mtl_datas);

        float i = 0;
        foreach (ucCyclesMeshMtlData obj in mesh_mtl_datas)
        {
            EditorUtility.DisplayProgressBar("Sync meshes to Cycles", "Sync meshes to Cycles Progress ", i / mesh_mtl_datas.Count * 100.0f);
            ucNative.Invoke<int, unity_add_mesh>(nativeLibraryPtr, obj.mesh_data, obj.mtl_datas);

            ++i;
        }
        EditorUtility.ClearProgressBar();
    }

    public void BakeLightMap()
    {
        ucNative.Invoke<int, bake_lightmap>(nativeLibraryPtr);
    }

    public void InteractiveRenderCb(IntPtr image_array, [MarshalAs(UnmanagedType.I4)]int w, [MarshalAs(UnmanagedType.I4)]int h, int type, float progress)
    {
        //Debug.Log("Result Interactive Image size = " + (w * h));        
        int image_byte_size = w * h * 2 * 4;
        byte[] native_image_array = new byte[image_byte_size];              

        Marshal.Copy(image_array, native_image_array, 0, image_byte_size);

        for (int i = 0; i < 3000; ++i)
        {            
            native_image_array[i] = 0;
        }

        //Debug.Log("progress = " + progress);
        void local_create_tex_func()
        {
            if (ucPreviewRenderWindow.rt_texture == null ||
                ucPreviewRenderWindow.rt_texture.width != w ||
                ucPreviewRenderWindow.rt_texture.height != h)
            {
                ucPreviewRenderWindow.rt_texture = new Texture2D(w, h, TextureFormat.RGBAHalf, false);
            }
            ucPreviewRenderWindow.rt_texture.SetPixelData(native_image_array, 0, 0);
            ucPreviewRenderWindow.rt_texture.Apply();
            ucInteractivePTEditorWindow.render_progress = progress;
        };

        thread_dispatcher.RunOnMainThread(local_create_tex_func);
    }    

    public ThreadStart InteractiveRenderStart(ucUnityRenderOptions ops)
    {
        return () =>
        {
            RenderImageCb cb = new RenderImageCb(InteractiveRenderCb);
            ucNative.Invoke<int, interactive_pt_rendering>(nativeLibraryPtr, ops, cb);
        };
    }

    public void SendLightsToCycles()
    {
        ucLightData[] light_datas = ucExportLights.Export();

        foreach(ucLightData lds in light_datas)
        {            
            ucNative.Invoke<int, unity_add_light>(nativeLibraryPtr, lds);
        }
    }
    

    public static void UnloadDLL()
    {
        if (nativeLibraryPtr == IntPtr.Zero) return;

        if (ucNative.FreeLibrary(nativeLibraryPtr) == false)
        {
            Debug.LogError("Cycles DLL unloads fail!!");
        }
        else
        {
            nativeLibraryPtr = IntPtr.Zero;
        }
    }    
}
