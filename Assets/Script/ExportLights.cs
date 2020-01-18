using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExportLights
{
    static public Light[] Export()
    {
        Light[] lights = GameObject.FindObjectsOfType(typeof(Light)) as Light[];

        return lights;
    }
}
