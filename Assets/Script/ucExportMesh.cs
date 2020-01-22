using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

public struct ucCyclesMeshData
{
    public float[] vertex_array;
    public float[] uvs_array;
    public float[] lightmapuvs_array;
    public float[] normal_array;
    public int vertex_num;
    public int[] index_array;
    public int[] index_mat_array;
    public int triangle_num;
    public int mtl_num;
}

public struct ucCyclesMtlData
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string mat_name;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string diffuse_tex_name;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string mtl_tex_name;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string normal_tex_name;

    public int is_transparent;
    public float tiling_x, tiling_y;
    public float offset_x, offset_y;
    public float[] diffuse_color;
}

public struct ucCyclesMeshMtlData
{
    public ucCyclesMeshData mesh_data;
    public ucCyclesMtlData[] mtl_datas;
}

public class ucExportMesh
{
    private static List<MeshFilter> GetAllObjectsInScene()
    {
        List<MeshFilter> objectsInScene = new List<MeshFilter>();

        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            MeshFilter mf = go.transform.GetComponent<MeshFilter>();
            if (mf && go.active == true)
            {
                //Debug.Log(go.name);
                objectsInScene.Add(mf);
            }
        }
        return objectsInScene;
    }

    private static void StandardToCyclesMtl(ref ucCyclesMtlData cycles_mtl, Material standard_mtl)
    {
        cycles_mtl.mat_name = standard_mtl.name;

        cycles_mtl.diffuse_tex_name = Application.dataPath + "/../" + AssetDatabase.GetAssetPath(standard_mtl.mainTexture);
        cycles_mtl.diffuse_color = new float[3];
        if (standard_mtl.HasProperty("_Color"))
        {
            cycles_mtl.diffuse_color[0] = standard_mtl.color.r;
            cycles_mtl.diffuse_color[1] = standard_mtl.color.g;
            cycles_mtl.diffuse_color[2] = standard_mtl.color.b;
        }


        cycles_mtl.tiling_x = standard_mtl.mainTextureScale.x;
        cycles_mtl.tiling_y = standard_mtl.mainTextureScale.y;
        cycles_mtl.offset_x = standard_mtl.mainTextureOffset.x;
        cycles_mtl.offset_y = standard_mtl.mainTextureOffset.y;

        cycles_mtl.normal_tex_name = Application.dataPath + "/../" + AssetDatabase.GetAssetPath(standard_mtl.GetTexture("_BumpMap"));
        cycles_mtl.mtl_tex_name = Application.dataPath + "/../" + AssetDatabase.GetAssetPath(standard_mtl.GetTexture("_MetallicGlossMap"));
    }

    private static void GetObjectMtls(MeshFilter mf, ref ucCyclesMtlData[] mtls)
    {
        Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
        int mat_num = mats.Length;

        mtls = new ucCyclesMtlData[mat_num];

        //string[] mat_name = new string[mat_num];
        //string[] diffuse_tex_name = new string[mat_num];
        for (int i = 0; i < mat_num; ++i)
        {
            //mtls[i].mat_name = mats[i].name;
            //diffuse_tex_name[i] = Application.dataPath + "/../" + AssetDatabase.GetAssetPath(mats[i].mainTexture);
            //Debug.Log("texture full path = " + Application.dataPath + "/../" + AssetDatabase.GetAssetPath(mats[i].mainTexture));
            StandardToCyclesMtl(ref mtls[i], mats[i]);
        }
    }

    public static void ExportCurrSceneMesh(ref List<ucCyclesMeshMtlData> mesh_data_list)
    {
        //mesh_data_list = new List<ucCyclesMeshMtlData>();

        List<MeshFilter> objs = GetAllObjectsInScene();

        foreach (MeshFilter mf in objs)
        {
            ucCyclesMeshMtlData mesh_mtl_data = new ucCyclesMeshMtlData();
            mesh_mtl_data.mesh_data = new ucCyclesMeshData();
            ref ucCyclesMeshData mesh_data = ref mesh_mtl_data.mesh_data;
            ref ucCyclesMtlData[] mtl_datas = ref mesh_mtl_data.mtl_datas;

            Transform t = mf.transform;

            Vector3 final_scale = t.localScale;
            Transform parent = t.parent;
            while (parent != null)
            {
                final_scale = Vector3.Scale(final_scale, parent.localScale);
                parent = parent.parent;
            }

            //Vector3 local_scale = t.localToWorldMatrix.lossyScale;
            //Vector3 p = t.localPosition;
            Quaternion r = t.rotation;


            int numVertices = 0;
            Mesh m = mf.sharedMesh;
            if (!m)
            {
                Debug.LogError("No mesh!");
                continue;
            }

            mesh_data.vertex_array = new float[m.vertices.Length * 4];
            foreach (Vector3 vv in m.vertices)
            {
                Vector3 v = t.TransformPoint(vv);
                mesh_data.vertex_array[numVertices * 4] = v.x;
                mesh_data.vertex_array[numVertices * 4 + 1] = v.y;
                mesh_data.vertex_array[numVertices * 4 + 2] = v.z;
                mesh_data.vertex_array[numVertices * 4 + 3] = 1.0f;

                numVertices++;
            }
            mesh_data.vertex_num = numVertices;
            
            int numNormal = 0;
            mesh_data.normal_array = new float[m.normals.Length * 4];
            foreach (Vector3 nn in m.normals)
            {
                //Vector3 v = r * nn;
                //v = Vector3.Scale(v, final_scale);
                //v = Vector3.Normalize(v);
                //Vector4 v = new Vector4(nn.x, nn.y, nn.z, 0.0f);
                Vector3 v = Matrix4x4.Transpose(t.transform.worldToLocalMatrix).MultiplyVector(nn);

                mesh_data.normal_array[numNormal * 4] = v.x;
                mesh_data.normal_array[numNormal * 4 + 1] = v.y;
                mesh_data.normal_array[numNormal * 4 + 2] = v.z;
                mesh_data.normal_array[numNormal * 4 + 3] = 0.0f;

                numNormal++;
            }
            
            int numUVs = 0;
            mesh_data.uvs_array = new float[m.uv.Length * 2];
            foreach (Vector2 v in m.uv)
            {
                mesh_data.uvs_array[numUVs * 2] = v.x;
                mesh_data.uvs_array[numUVs * 2 + 1] = v.y;

                numUVs++;
            }

            mesh_data.lightmapuvs_array = new float[m.uv.Length * 2]; //hack 防止没有lightmap崩溃
            int numLightmapuv = 0;
            if (m.uv2.Length > 0)
            {
                foreach (Vector2 v in m.uv2)
                {
                    //if (v.x > 1.0f || v.x < -0.0001f)
                    //{
                    //    Debug.LogError("Uv error!");
                    //}

                    mesh_data.lightmapuvs_array[numLightmapuv * 2] = v.x;
                    mesh_data.lightmapuvs_array[numLightmapuv * 2 + 1] = v.y;

                    numLightmapuv++;
                }
            }

            //Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
            //int mat_num = mats.Length;
            //string[] mat_name = new string[mat_num];
            //string[] diffuse_tex_name = new string[mat_num];
            //for (int i = 0; i < mat_num; ++i)
            //{
            //    mat_name[i] = mats[i].name;
            //    diffuse_tex_name[i] = Application.dataPath + "/../" + AssetDatabase.GetAssetPath(mats[i].mainTexture);
            //    //Debug.Log("texture full path = " + Application.dataPath + "/../" + AssetDatabase.GetAssetPath(mats[i].mainTexture));
            //}
            Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
            mesh_data.mtl_num = mats.Length;
            GetObjectMtls(mf, ref mtl_datas);


            mesh_data.triangle_num = 0;
            for (int material = 0; material < m.subMeshCount; material++)
            {
                int[] triangles = m.GetTriangles(material);
                mesh_data.triangle_num += triangles.Length;
            }

            mesh_data.index_array = new int[mesh_data.triangle_num * 3];
            mesh_data.index_mat_array = new int[mesh_data.triangle_num];
            //Debug.Log("triangle_num num = " + mesh_data.triangle_num);
            //Debug.Log(string.Format("s.x = {0}, y = {1}, z = {2} ", final_scale.x, final_scale.y, final_scale.z));
            int index_i = 0;
            for (int material = 0; material < m.subMeshCount; material++)
            {
                int[] triangles = m.GetTriangles(material);

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (final_scale.x > 0 && final_scale.y > 0 && final_scale.z > 0)
                    {
                        //revert wind
                        mesh_data.index_array[index_i * 3] = triangles[i + 1];
                        mesh_data.index_array[index_i * 3 + 1] = triangles[i + 2];
                        mesh_data.index_array[index_i * 3 + 2] = triangles[i + 0];
                    }
                    else
                    {
                        //for negative scale value, revert triangle order.
                        mesh_data.index_array[index_i * 3] = triangles[i + 0];
                        mesh_data.index_array[index_i * 3 + 1] = triangles[i + 2];
                        mesh_data.index_array[index_i * 3 + 2] = triangles[i + 1];
                    }
                    mesh_data.index_mat_array[index_i] = material;

                    ++index_i;
                }
            }

            mesh_data_list.Add(mesh_mtl_data);
        }

        //return mesh_data_list;
    }    
}
