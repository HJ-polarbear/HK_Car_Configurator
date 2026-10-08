namespace URPLabStudio
{
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(CanvasRenderer))]
public sealed class Lab_RoundedPanelGraphic : MaskableGraphic
{
    [SerializeField, Min(0f)] float cornerRadius = 28f;
    [SerializeField, Min(0f)] float borderWidth = 2f;
    [SerializeField] Color borderColor = new Color(1f, 1f, 1f, .96f);
    [SerializeField, Range(2, 16)] int cornerSegments = 8;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * .5f);
        List<Vector2> outer = BuildRoundedPath(rect, radius);
        Rect innerRect = new Rect(rect.xMin + borderWidth, rect.yMin + borderWidth,
            Mathf.Max(0f, rect.width - borderWidth * 2f), Mathf.Max(0f, rect.height - borderWidth * 2f));
        List<Vector2> inner = BuildRoundedPath(innerRect, Mathf.Max(0f, radius - borderWidth));

        AddFan(vh, inner, color);
        AddBorder(vh, outer, inner, borderColor);
    }

    List<Vector2> BuildRoundedPath(Rect rect, float radius)
    {
        var points = new List<Vector2>(cornerSegments * 4 + 4);
        Vector2[] centers =
        {
            new Vector2(rect.xMax - radius, rect.yMax - radius),
            new Vector2(rect.xMin + radius, rect.yMax - radius),
            new Vector2(rect.xMin + radius, rect.yMin + radius),
            new Vector2(rect.xMax - radius, rect.yMin + radius)
        };
        float[] starts = { 0f, 90f, 180f, 270f };
        for (int corner = 0; corner < 4; corner++)
            for (int i = 0; i <= cornerSegments; i++)
            {
                float angle = (starts[corner] + i * 90f / cornerSegments) * Mathf.Deg2Rad;
                points.Add(centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        return points;
    }

    static void AddFan(VertexHelper vh, List<Vector2> path, Color32 tint)
    {
        int centerIndex = vh.currentVertCount;
        Vector2 center = Vector2.zero;
        foreach (Vector2 point in path) center += point;
        center /= path.Count;
        vh.AddVert(center, tint, new Vector2(.5f, .5f));
        foreach (Vector2 point in path) vh.AddVert(point, tint, Vector2.zero);
        for (int i = 0; i < path.Count; i++)
            vh.AddTriangle(centerIndex, centerIndex + 1 + i, centerIndex + 1 + (i + 1) % path.Count);
    }

    static void AddBorder(VertexHelper vh, List<Vector2> outer, List<Vector2> inner, Color32 tint)
    {
        int start = vh.currentVertCount;
        int count = Mathf.Min(outer.Count, inner.Count);
        for (int i = 0; i < count; i++)
        {
            vh.AddVert(outer[i], tint, Vector2.zero);
            vh.AddVert(inner[i], tint, Vector2.zero);
        }
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            int outerA = start + i * 2;
            int innerA = outerA + 1;
            int outerB = start + next * 2;
            int innerB = outerB + 1;
            vh.AddTriangle(outerA, outerB, innerB);
            vh.AddTriangle(outerA, innerB, innerA);
        }
    }
}
}
