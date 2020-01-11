using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExportLights// : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    static public Light[] Export()
    {
        Light[] lights = GameObject.FindObjectsOfType(typeof(Light)) as Light[];

        return lights;
    }
}
