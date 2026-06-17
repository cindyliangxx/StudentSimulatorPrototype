using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Rounded Rect Graphic")]
[RequireComponent(typeof(CanvasRenderer))]
public class RoundedRectGraphic : MaskableGraphic
{
    [SerializeField] private Color fillColor = Color.white;
    [SerializeField] private Color strokeColor = Color.clear;
    [SerializeField] private float cornerRadius = 20f;
    [SerializeField] private float strokeWidth = 0f;
    [SerializeField] private int cornerSegments = 8;

    public void SetStyle(Color fill, Color stroke, float radius, float width)
    {
        fillColor = fill;
        strokeColor = stroke;
        cornerRadius = Mathf.Max(0f, radius);
        strokeWidth = Mathf.Max(0f, width);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        float safeRadius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
        List<Vector2> outerPoints = BuildRoundedRectPoints(rect, safeRadius);

        if (fillColor.a > 0f)
        {
            AddFill(vertexHelper, rect.center, outerPoints, fillColor);
        }

        float safeStrokeWidth = Mathf.Min(strokeWidth, Mathf.Min(rect.width, rect.height) * 0.5f);
        if (safeStrokeWidth <= 0f || strokeColor.a <= 0f)
        {
            return;
        }

        Rect innerRect = new Rect(
            rect.xMin + safeStrokeWidth,
            rect.yMin + safeStrokeWidth,
            rect.width - safeStrokeWidth * 2f,
            rect.height - safeStrokeWidth * 2f);

        if (innerRect.width <= 0f || innerRect.height <= 0f)
        {
            return;
        }

        float innerRadius = Mathf.Max(0f, safeRadius - safeStrokeWidth);
        List<Vector2> innerPoints = BuildRoundedRectPoints(innerRect, innerRadius);
        AddStroke(vertexHelper, outerPoints, innerPoints, strokeColor);
    }

    private List<Vector2> BuildRoundedRectPoints(Rect rect, float radius)
    {
        int safeSegments = Mathf.Max(2, cornerSegments);
        List<Vector2> points = new List<Vector2>((safeSegments + 1) * 4);

        AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, safeSegments);
        AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, safeSegments);
        AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, safeSegments);
        AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, safeSegments);

        return points;
    }

    private static void AddCorner(
        List<Vector2> points,
        Vector2 center,
        float radius,
        float startDegrees,
        float endDegrees,
        int segments)
    {
        if (radius <= 0f)
        {
            points.Add(center);
            return;
        }

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(startDegrees, endDegrees, i / (float)segments) * Mathf.Deg2Rad;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    private static void AddFill(VertexHelper vertexHelper, Vector2 center, List<Vector2> points, Color color)
    {
        int centerIndex = vertexHelper.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = center;
        vertexHelper.AddVert(vertex);

        for (int i = 0; i < points.Count; i++)
        {
            vertex.position = points[i];
            vertexHelper.AddVert(vertex);
        }

        for (int i = 0; i < points.Count; i++)
        {
            int current = centerIndex + 1 + i;
            int next = centerIndex + 1 + ((i + 1) % points.Count);
            vertexHelper.AddTriangle(centerIndex, current, next);
        }
    }

    private static void AddStroke(
        VertexHelper vertexHelper,
        List<Vector2> outerPoints,
        List<Vector2> innerPoints,
        Color color)
    {
        int count = Mathf.Min(outerPoints.Count, innerPoints.Count);
        if (count < 2)
        {
            return;
        }

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            int startIndex = vertexHelper.currentVertCount;

            vertex.position = outerPoints[i];
            vertexHelper.AddVert(vertex);
            vertex.position = outerPoints[next];
            vertexHelper.AddVert(vertex);
            vertex.position = innerPoints[next];
            vertexHelper.AddVert(vertex);
            vertex.position = innerPoints[i];
            vertexHelper.AddVert(vertex);

            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }
    }
}
