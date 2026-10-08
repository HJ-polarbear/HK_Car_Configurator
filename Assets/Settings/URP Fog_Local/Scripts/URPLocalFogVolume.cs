using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class URPLocalFogVolume : MonoBehaviour
{
    public static readonly List<URPLocalFogVolume> ActiveVolumes = new List<URPLocalFogVolume>();

    [Header("Fog Settings")]
    public Color fogColor = new Color(0.55f, 0.6f, 0.6f, 1.0f);

    [Range(0.0f, 5.0f)]
    public float density = 0.8f;

    [Range(0.0f, 1.0f)]
    public float intensity = 0.85f;

    [Header("Distance Fade")]
    public float startDistance = 0.0f;
    public float endDistance = 25.0f;

    [Header("Height Fade")]
    public bool useHeightFade = false;
    public float heightStart = 0.0f;
    public float heightEnd = 1.8f;

    [Header("Noise Settings")]
    public bool useNoise = true;

    [Range(0.1f, 10.0f)]
    public float noiseScale = 1.8f;

    [Range(0.0f, 1.0f)]
    public float noiseSpeed = 0.06f;

    [Range(0.0f, 1.0f)]
    public float noiseStrength = 0.18f;

    [Header("Debug")]
    public bool drawGizmo = true;

    private void OnEnable()
    {
        if (!ActiveVolumes.Contains(this))
        {
            ActiveVolumes.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveVolumes.Remove(this);
    }

    private void OnDestroy()
    {
        ActiveVolumes.Remove(this);
    }

    public Matrix4x4 WorldToLocalMatrix
    {
        get { return transform.worldToLocalMatrix; }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo)
        {
            return;
        }

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Color fillColor = fogColor;
        fillColor.a = 0.12f;

        Gizmos.color = fillColor;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);

        Color wireColor = fogColor;
        wireColor.a = 0.85f;

        Gizmos.color = wireColor;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        Gizmos.matrix = oldMatrix;
    }
}