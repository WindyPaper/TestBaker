using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEditor;
using System.Reflection;
using System.Threading;

public class DLLFunctionCaller
{
    static IntPtr nativeLibraryPtr;
    ThreadDispatcher thread_dispatcher = null;

    delegate int bake_scene(int number, int multiplyBy);
    delegate bool init_cycles(CyclesInitOptions op);

    delegate int release_cycles();

    delegate int unity_add_mesh(ucCyclesMeshData mesh_data, ucCyclesMtlData[] mtls);

    delegate int bake_lightmap();

    public delegate void RenderImageCb(IntPtr image_array, [MarshalAs(UnmanagedType.I4)]int w, [MarshalAs(UnmanagedType.I4)]int h, int type, float progress);
    delegate int interactive_pt_rendering(UnityRenderOptions ops, [MarshalAs(UnmanagedType.FunctionPtr)]RenderImageCb pDelegate);

    //add light to Cycles
    delegate int unity_add_light(ucLightData light_data);

    public DLLFunctionCaller(ThreadDispatcher thread_dispatcher)
    {
        this.thread_dispatcher = thread_dispatcher;
    }    

    public void LoadDLLAndInitCycles(CyclesInitOptions op)
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

        Native.Invoke<int, release_cycles>(nativeLibraryPtr);

        UnloadDLL();
    }    

    void LoadDLL()
    {
        if (nativeLibraryPtr != IntPtr.Zero) return;

        string dll_path = Application.dataPath + "/Plugins/";
        string dll_file_name = "cycles.dll";
        Native.LoadLibraryFlags flags = Native.LoadLibraryFlags.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS;
        string dll_full_name = dll_path + dll_file_name;
        Native.AddDllDirectory(dll_path);
        nativeLibraryPtr = Native.LoadLibraryEx(dll_full_name, IntPtr.Zero, flags);

        if (nativeLibraryPtr == IntPtr.Zero)
        {
            Debug.LogError("Failed to load native library. Path = " + dll_full_name + " Last Error Code = " + Marshal.GetLastWin32Error());

            Debug.Log(Native.GetErrorMessage(Marshal.GetLastWin32Error()));
        }
    }

    void InitCycles(CyclesInitOptions op)
    {        
        Debug.Log("w = "+op.width+"h = "+op.height);
        bool result = Native.Invoke<bool, init_cycles>(nativeLibraryPtr, op);
    }    

    public void SendAllMeshToCycles()
    {
        List<ucCyclesMeshMtlData> mesh_mtl_datas = new List<ucCyclesMeshMtlData>();
        ucExportMesh.ExportCurrSceneMesh(ref mesh_mtl_datas);        

        foreach(ucCyclesMeshMtlData obj in mesh_mtl_datas)
        {
            Native.Invoke<int, unity_add_mesh>(nativeLibraryPtr, obj.mesh_data, obj.mtl_datas);
        }                
    }

    public void BakeLightMap()
    {
        Native.Invoke<int, bake_lightmap>(nativeLibraryPtr);
    }

    public void InteractiveRenderCb(IntPtr image_array, [MarshalAs(UnmanagedType.I4)]int w, [MarshalAs(UnmanagedType.I4)]int h, int type, float progress)
    {
        //Debug.Log("Result Interactive Image size = " + (w * h));        
        int image_byte_size = w * h * 2 * 4;
        byte[] native_image_array = new byte[image_byte_size];
        Marshal.Copy(image_array, native_image_array, 0, image_byte_size);

        //Debug.Log("progress = " + progress);
        void local_create_tex_func()
        {
            if (RaytracingTexShow.rt_texture == null || 
                RaytracingTexShow.rt_texture.width != w || 
                RaytracingTexShow.rt_texture.height != h)
            {
                RaytracingTexShow.rt_texture = new Texture2D(w, h, TextureFormat.RGBAHalf, false);
            }            
            RaytracingTexShow.rt_texture.SetPixelData(native_image_array, 0, 0);
            InteractivePTEditorWindow.render_progress = progress;
        };

        thread_dispatcher.RunOnMainThread(local_create_tex_func);
    }    

    public ThreadStart InteractiveRenderStart(UnityRenderOptions ops)
    {
        return () =>
        {
            RenderImageCb cb = new RenderImageCb(InteractiveRenderCb);
            Native.Invoke<int, interactive_pt_rendering>(nativeLibraryPtr, ops, cb);
        };
    }

    public void SendLightsToCycles()
    {
        ucLightData[] light_datas = ucExportLights.Export();

        foreach(ucLightData lds in light_datas)
        {            
            Native.Invoke<int, unity_add_light>(nativeLibraryPtr, lds);
        }
    }
    

    public static void UnloadDLL()
    {
        if (nativeLibraryPtr == IntPtr.Zero) return;

        if (Native.FreeLibrary(nativeLibraryPtr) == false)
        {
            Debug.LogError("Cycles DLL unloads fail!!");
        }
        else
        {
            nativeLibraryPtr = IntPtr.Zero;
        }
    }    
}
