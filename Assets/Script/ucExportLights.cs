using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

//name, intensity, radius, angle, l.areaSize.x, l.areaSize.y, color_f, dir, pos, l.type);
public struct ucLightData
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string name;

    public float intensity;
    public float radius;
    public float angle;
    public float sizex, sizey;
    public float[] color;
    public float[] dir;
    public float[] pos;
    public int type;
}

public class ucExportLights
{

    static public ucLightData[] Export()
    {
        Light[] lights = GameObject.FindObjectsOfType(typeof(Light)) as Light[];

        ucLightData[] light_datas = new ucLightData[lights.Length];

        int index = 0;
        foreach (Light l in lights)
        {
            string name = l.name;
            //Debug.Log("light name = " + name);
            float radius = 0.01f;
            float light_value_scale = 1.0f;
            float angle = 0.0f;
            if (l.type == LightType.Point)
            {
                radius = 0.3f;// l.range;
                light_value_scale = l.range * 1.5f;
            }
            else if (l.type == LightType.Spot)
            {
                radius = 0.1f;
                light_value_scale = l.range * 10;
                angle = l.spotAngle;
                //Debug.Log("light angle = " + angle);
            }
            else if (l.type == LightType.Area)
            {
                light_value_scale = l.areaSize.x * l.areaSize.y;
            }
            float intensity = l.intensity * l.bounceIntensity * light_value_scale;
            //Debug.Log("light intensity = " + intensity);

            Color color = l.color;
            float[] color_f = new float[4];
            color_f[0] = color.r;
            color_f[1] = color.g;
            color_f[2] = color.b;
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

            light_datas[index].name = name;
            light_datas[index].intensity = intensity;
            light_datas[index].radius = radius;
            light_datas[index].angle = angle;
            light_datas[index].sizex = l.areaSize.x;
            light_datas[index].sizey = l.areaSize.y;
            light_datas[index].color = color_f;
            light_datas[index].dir = dir;
            light_datas[index].pos = pos;
            light_datas[index].type = (int)l.type;

            ++index;
        }

        return light_datas;
    }
}
