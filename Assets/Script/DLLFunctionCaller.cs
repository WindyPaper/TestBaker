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

    public delegate void RenderImageCb(IntPtr image_array, [MarshalAs(UnmanagedType.I4)]int w, [MarshalAs(UnmanagedType.I4)]int h, int type);
    delegate int interactive_pt_rendering(UnityRenderOptions ops, [MarshalAs(UnmanagedType.FunctionPtr)]RenderImageCb pDelegate);

    //add light to Cycles
    delegate int unity_add_light([MarshalAs(UnmanagedType.LPStr)]string name, float intensity, float radius, float[] color, float[] dir, float[] pos, int type);

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

    public void InteractiveRenderCb(IntPtr image_array, [MarshalAs(UnmanagedType.I4)]int w, [MarshalAs(UnmanagedType.I4)]int h, int type)
    {
        //Debug.Log("Result Interactive Image size = " + (w * h));
        int image_byte_size = w * h * 2 * 4;
        byte[] native_image_array = new byte[image_byte_size];
        Marshal.Copy(image_array, native_image_array, 0, image_byte_size);        

        void local_create_tex_func()
        {
            if (RaytracingTexShow.rt_texture == null || 
                RaytracingTexShow.rt_texture.width != w || 
                RaytracingTexShow.rt_texture.height != h)
            {
                RaytracingTexShow.rt_texture = new Texture2D(w, h, TextureFormat.RGBAHalf, false);
            }            
            RaytracingTexShow.rt_texture.SetPixelData(native_image_array, 0, 0);
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
        Light[] lights = ExportLights.Export();

        foreach(Light l in lights)
        {
            string name = l.name;
            float intensity = l.intensity;
            float radius = 0.01f;
            if(l.type != LightType.Directional)
            {
                radius = l.range;
            }
            Color color = l.color;
            float[] color_f = new float[4];
            color_f[0] = color.r * 2;
            color_f[1] = color.g * 2;
            color_f[2] = color.b * 2;
            color_f[3] = color.a;
            float[] dir = new float[4];
            dir[0] = l.transform.forward.x;
            dir[1] = l.transform.forward.y;
            dir[2] = l.transform.forward.z;
            dir[3] = 0.0f;
            float[] pos = new float[4];
            pos[0] = l.transform.position.x;
            pos[1] = l.transform.position.y;
            pos[2] = l.transform.position.z;
            pos[3] = 1.0f;

            Native.Invoke<int, unity_add_light>(nativeLibraryPtr, name, intensity, radius, color_f, dir, pos, l.type);
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
