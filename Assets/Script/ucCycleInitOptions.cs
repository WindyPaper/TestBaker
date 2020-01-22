using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

public enum SampleCountOptions
{
    LOW_4 = 0,
    MEDIUM_128 = 1,
    HIGH_512 = 2,
    VERY_HIGH_2048 = 3,
    ULTRA_HIGH_4096 = 4,
    GROUND_TRUTH_8192 = 5
}

public enum RenderDeviceOptions
{
    CUDA = 0,
    CPU = 1
}

public enum WorkType
{
    RENDER = 0,
    BAKDER = 1
}

[StructLayout(LayoutKind.Sequential)]
public struct CyclesInitOptions
{
    public int width;
    public int height;

    public int sample_count;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string device_working_folder;
    public int render_device;

    public int work_type; //RENDER / BAKDER
    public int enable_denoise;

}
