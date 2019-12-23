using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEditor;
using System.Reflection;
using System.Threading;

public class DLLFunctionCaller : MonoBehaviour
{
    static IntPtr nativeLibraryPtr;

    delegate int bake_scene(int number, int multiplyBy);
    delegate bool init_cycles(int w, int h, [MarshalAs(UnmanagedType.LPStr)]string core_type);

    delegate int release_cycles();

    delegate int unity_add_mesh(float[] vertex_array, float[] uvs_array, float[] lightmapuvs_array, float[] normal_array, int vertex_num,
        int[] index_array, int[] mat_index, int triangle_num,
        [MarshalAs(UnmanagedType.LPStr)]string[] mat_name, [MarshalAs(UnmanagedType.LPStr)]string[] diffuse_tex, int mat_num);

    delegate int bake_lightmap();

    public delegate void RenderImageCb(IntPtr image_array, [MarshalAs(UnmanagedType.I4)]int w, [MarshalAs(UnmanagedType.I4)]int h, int type);
    delegate int interactive_pt_rendering([MarshalAs(UnmanagedType.FunctionPtr)]RenderImageCb pDelegate);

    public void Init()
    {
        Debug.Log("Start...");

        LoadDLL();

        InitCycles();

        //SendAllMeshToCycles();

        //BakeLightMap();

        Debug.Log("Finish...");
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



    void Awake()
    {
        //Debug.Log("Start...");

        //LoadDLL();

        //InitCycles();

        //SendAllMeshToCycles();

        ////BakeLightMap();
        //InteractiveRenderStart();

        //Debug.Log("Finish...");
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

    void InitCycles()
    {

        //try
        //{
        Debug.Log("w = "+Screen.width+"h = "+Screen.height);
        bool result = Native.Invoke<bool, init_cycles>(nativeLibraryPtr, Screen.width, Screen.height, "CPU"); // Should return the number 15.
        Debug.Log(result);
        //}
        //catch (System.Exception e)
        //{
        //Debug.Log(e.Message);            
        //}
    }

    private static List<MeshFilter> GetAllObjectsInScene()
    {
        List<MeshFilter> objectsInScene = new List<MeshFilter>();

        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            MeshFilter mf = go.transform.GetComponent<MeshFilter>();
            if (mf && go.active == true)
            {
                Debug.Log(go.name);
                objectsInScene.Add(mf);
            }
        }
        return objectsInScene;
    }

    public void SendAllMeshToCycles()
    {
        List<MeshFilter> objs = GetAllObjectsInScene();

        foreach (MeshFilter mf in objs)
        {
            Transform t = mf.transform;
            Vector3 s = t.localScale;
            Vector3 p = t.localPosition;
            Quaternion r = t.localRotation;


            int numVertices = 0;
            Mesh m = mf.sharedMesh;
            if (!m)
            {
                Debug.LogError("No mesh!");
                continue;
            }

            float[] vertex_array = new float[m.vertices.Length * 4];
            foreach (Vector3 vv in m.vertices)
            {
                Vector3 v = t.TransformPoint(vv);
                Debug.Log("pos = " + v.x + "  " + v.y + "  " + v.z);
                //Vector3 v = (vv);
                vertex_array[numVertices * 4] = -v.x;
                vertex_array[numVertices * 4 + 1] = v.y;
                vertex_array[numVertices * 4 + 2] = -v.z;
                vertex_array[numVertices * 4 + 3] = 1.0f;

                numVertices++;
            }
            //sb.Append("\n");
            int numNormal = 0;
            float[] normal_array = new float[m.normals.Length * 4];
            foreach (Vector3 nn in m.normals)
            {
                Vector3 v = r * nn;
                //sb.Append(string.Format("vn {0} {1} {2}\n", -v.x, -v.y, v.z));
                normal_array[numNormal * 4] = -v.x;
                normal_array[numNormal * 4 + 1] = v.y;
                normal_array[numNormal * 4 + 2] = -v.z;
                normal_array[numNormal * 4 + 3] = 0.0f;

                numNormal++;
            }
            //sb.Append("\n");
            int numUVs = 0;
            float[] uvs_array = new float[m.uv.Length * 2];
            foreach (Vector3 v in m.uv)
            {
                //sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
                uvs_array[numUVs * 2] = v.x;
                uvs_array[numUVs * 2 + 1] = v.y;

                numUVs++;
            }

            float[] lightmapuv_array = new float[m.uv.Length * 2]; //hack 防止没有lightmap崩溃
            int numLightmapuv = 0;
            if (m.uv2.Length > 0)
            {
                foreach (Vector3 v in m.uv2)
                {
                    //sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
                    if (v.x > 1.0f || v.x < -0.0001f)
                    {
                        Debug.LogError("Uv error!");
                    }

                    lightmapuv_array[numLightmapuv * 2] = v.x;
                    lightmapuv_array[numLightmapuv * 2 + 1] = v.y;

                    numLightmapuv++;
                }
            }
            //Debug.Log("Lightmap uv num = " + numLightmapuv);
            Debug.Log("vertice num = " + numVertices);
            if (numLightmapuv != m.uv.Length)
            {
                Debug.LogError("numLightmapuv != m.uv.Length");
            }

            Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
            int mat_num = mats.Length;
            string[] mat_name = new string[mat_num];
            string[] diffuse_tex_name = new string[mat_num];
            for (int i = 0; i < mat_num; ++i)
            {
                mat_name[i] = mats[i].name;
                diffuse_tex_name[i] = Application.dataPath + "/../" + AssetDatabase.GetAssetPath(mats[i].mainTexture);
                Debug.Log("texture full path = " + Application.dataPath + "/../" + AssetDatabase.GetAssetPath(mats[i].mainTexture));
            }

            //int mat_num = m.subMeshCount;
            int triangle_num = 0;
            for (int material = 0; material < m.subMeshCount; material++)
            {
                int[] triangles = m.GetTriangles(material);
                triangle_num += triangles.Length;
            }

            int[] index_array = new int[triangle_num * 3];
            int[] index_mat_array = new int[triangle_num];
            Debug.Log("triangle_num num = " + triangle_num);
            int index_i = 0;
            for (int material = 0; material < m.subMeshCount; material++)
            {
                int[] triangles = m.GetTriangles(material);

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    //revert wind
                    index_array[index_i * 3] = triangles[i + 1];
                    index_array[index_i * 3 + 1] = triangles[i + 2];
                    index_array[index_i * 3 + 2] = triangles[i + 0];
                    index_mat_array[index_i] = material;

                    ++index_i;
                }
            }

            Native.Invoke<int, unity_add_mesh>(nativeLibraryPtr,
                vertex_array, uvs_array, lightmapuv_array, normal_array, numVertices,
                index_array, index_mat_array, triangle_num, mat_name, diffuse_tex_name, mat_num);
        }
    }

    public void BakeLightMap()
    {
        Native.Invoke<int, bake_lightmap>(nativeLibraryPtr);
    }

    public void InteractiveRenderCb(IntPtr image_array, [MarshalAs(UnmanagedType.I4)]int w, [MarshalAs(UnmanagedType.I4)]int h, int type)
    {
        Debug.Log("Result Interactive Image size = " + (w * h));        
        float[] native_image_array = new float[w * h * 4];
        Marshal.Copy(image_array, native_image_array, 0, w * h * 4);
        //Byte[] b_data = (Byte[])Convert.ChangeType(image_array, typeof(Byte[]));
        //Marshal.Copy(b_data, 0, native_image_array, w * h * 4);
        //for(int i = 0; i < w * h * 4; i += 40 * 4)
        //{
        //    Debug.Log("r = " + native_image_array[i] + " g = " + native_image_array[i + 1] + " b = " + native_image_array[i + 2] + " a = " + native_image_array[i + 3]);
        //}
        Debug.Log("r = " + native_image_array[0] + " g = " + native_image_array[1] + " b = " + native_image_array[2] + " a = " + native_image_array[3]);
    }

    public void InteractiveRenderStart()
    {
        RenderImageCb cb = new RenderImageCb(InteractiveRenderCb);
        Native.Invoke<int, interactive_pt_rendering>(nativeLibraryPtr, cb);
    }

    void Update()
    {
       
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

    void OnApplicationQuit()
    {
        
    }
}
