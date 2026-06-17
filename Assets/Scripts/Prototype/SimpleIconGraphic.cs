using UnityEngine;
using UnityEngine.UI;

public enum SimpleIconKind
{
    Health,
    Money,
    Social,
    Academic,
    Status,
    Item,
    Timeline,
    Achievement
}

[AddComponentMenu("UI/Simple Icon Graphic")]
[RequireComponent(typeof(CanvasRenderer))]
public class SimpleIconGraphic : MaskableGraphic
{
    [SerializeField] private SimpleIconKind iconKind;
    [SerializeField] private int circleSegments = 24;

    public void SetIcon(SimpleIconKind kind, Color iconColor)
    {
        iconKind = kind;
        color = iconColor;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
        {
            return;
        }

        switch (iconKind)
        {
            case SimpleIconKind.Health:
                DrawHealth(vertexHelper, rect);
                break;
            case SimpleIconKind.Money:
                DrawMoney(vertexHelper, rect);
                break;
            case SimpleIconKind.Social:
                DrawSocial(vertexHelper, rect);
                break;
            case SimpleIconKind.Academic:
                DrawAcademic(vertexHelper, rect);
                break;
            case SimpleIconKind.Item:
                DrawItem(vertexHelper, rect);
                break;
            case SimpleIconKind.Timeline:
                DrawTimeline(vertexHelper, rect);
                break;
            case SimpleIconKind.Achievement:
                DrawAchievement(vertexHelper, rect);
                break;
            default:
                DrawStatus(vertexHelper, rect);
                break;
        }
    }

    private void DrawHealth(VertexHelper vertexHelper, Rect rect)
    {
        AddRect(vertexHelper, NormalizedRect(rect, 0.42f, 0.12f, 0.16f, 0.76f), color);
        AddRect(vertexHelper, NormalizedRect(rect, 0.12f, 0.42f, 0.76f, 0.16f), color);
    }

    private void DrawMoney(VertexHelper vertexHelper, Rect rect)
    {
        AddCircle(vertexHelper, rect.center, Mathf.Min(rect.width, rect.height) * 0.36f, color);
        AddRect(vertexHelper, NormalizedRect(rect, 0.47f, 0.24f, 0.06f, 0.52f), new Color(1f, 1f, 1f, 0.34f));
        AddRect(vertexHelper, NormalizedRect(rect, 0.34f, 0.36f, 0.32f, 0.08f), new Color(1f, 1f, 1f, 0.34f));
        AddRect(vertexHelper, NormalizedRect(rect, 0.34f, 0.56f, 0.32f, 0.08f), new Color(1f, 1f, 1f, 0.34f));
    }

    private void DrawSocial(VertexHelper vertexHelper, Rect rect)
    {
        AddCircle(vertexHelper, NormalizedPoint(rect, 0.38f, 0.62f), Mathf.Min(rect.width, rect.height) * 0.18f, color);
        AddCircle(vertexHelper, NormalizedPoint(rect, 0.64f, 0.56f), Mathf.Min(rect.width, rect.height) * 0.16f, color);
        AddRect(vertexHelper, NormalizedRect(rect, 0.20f, 0.22f, 0.38f, 0.20f), color);
        AddRect(vertexHelper, NormalizedRect(rect, 0.52f, 0.24f, 0.28f, 0.17f), color);
    }

    private void DrawAcademic(VertexHelper vertexHelper, Rect rect)
    {
        AddRect(vertexHelper, NormalizedRect(rect, 0.17f, 0.22f, 0.30f, 0.58f), color);
        AddRect(vertexHelper, NormalizedRect(rect, 0.53f, 0.22f, 0.30f, 0.58f), color);
        AddRect(vertexHelper, NormalizedRect(rect, 0.48f, 0.18f, 0.04f, 0.66f), new Color(1f, 1f, 1f, 0.45f));
        AddRect(vertexHelper, NormalizedRect(rect, 0.24f, 0.58f, 0.17f, 0.045f), new Color(1f, 1f, 1f, 0.35f));
        AddRect(vertexHelper, NormalizedRect(rect, 0.59f, 0.58f, 0.17f, 0.045f), new Color(1f, 1f, 1f, 0.35f));
    }

    private void DrawStatus(VertexHelper vertexHelper, Rect rect)
    {
        AddCircle(vertexHelper, rect.center, Mathf.Min(rect.width, rect.height) * 0.34f, color);
        AddCircle(vertexHelper, rect.center, Mathf.Min(rect.width, rect.height) * 0.16f, new Color(1f, 1f, 1f, 0.4f));
    }

    private void DrawItem(VertexHelper vertexHelper, Rect rect)
    {
        Vector2[] diamond =
        {
            NormalizedPoint(rect, 0.50f, 0.86f),
            NormalizedPoint(rect, 0.86f, 0.50f),
            NormalizedPoint(rect, 0.50f, 0.14f),
            NormalizedPoint(rect, 0.14f, 0.50f)
        };

        AddPolygon(vertexHelper, diamond, color);
        AddCircle(vertexHelper, NormalizedPoint(rect, 0.39f, 0.62f), Mathf.Min(rect.width, rect.height) * 0.055f, new Color(1f, 1f, 1f, 0.45f));
    }

    private void DrawTimeline(VertexHelper vertexHelper, Rect rect)
    {
        AddRect(vertexHelper, NormalizedRect(rect, 0.16f, 0.47f, 0.68f, 0.06f), color);
        AddCircle(vertexHelper, NormalizedPoint(rect, 0.22f, 0.50f), Mathf.Min(rect.width, rect.height) * 0.11f, color);
        AddCircle(vertexHelper, NormalizedPoint(rect, 0.50f, 0.50f), Mathf.Min(rect.width, rect.height) * 0.11f, color);
        AddCircle(vertexHelper, NormalizedPoint(rect, 0.78f, 0.50f), Mathf.Min(rect.width, rect.height) * 0.11f, color);
    }

    private void DrawAchievement(VertexHelper vertexHelper, Rect rect)
    {
        Vector2 center = rect.center;
        float outerRadius = Mathf.Min(rect.width, rect.height) * 0.38f;
        float innerRadius = outerRadius * 0.46f;
        Vector2[] points = new Vector2[10];

        for (int i = 0; i < points.Length; i++)
        {
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            float angle = (-90f + i * 36f) * Mathf.Deg2Rad;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        AddPolygon(vertexHelper, points, color);
    }

    private Rect NormalizedRect(Rect rect, float x, float y, float width, float height)
    {
        return new Rect(
            rect.xMin + rect.width * x,
            rect.yMin + rect.height * y,
            rect.width * width,
            rect.height * height);
    }

    private Vector2 NormalizedPoint(Rect rect, float x, float y)
    {
        return new Vector2(rect.xMin + rect.width * x, rect.yMin + rect.height * y);
    }

    private static void AddRect(VertexHelper vertexHelper, Rect rect, Color color)
    {
        int startIndex = vertexHelper.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = new Vector2(rect.xMin, rect.yMin);
        vertexHelper.AddVert(vertex);
        vertex.position = new Vector2(rect.xMin, rect.yMax);
        vertexHelper.AddVert(vertex);
        vertex.position = new Vector2(rect.xMax, rect.yMax);
        vertexHelper.AddVert(vertex);
        vertex.position = new Vector2(rect.xMax, rect.yMin);
        vertexHelper.AddVert(vertex);

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    private void AddCircle(VertexHelper vertexHelper, Vector2 center, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        int safeSegments = Mathf.Max(12, circleSegments);
        int centerIndex = vertexHelper.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = center;
        vertexHelper.AddVert(vertex);

        for (int i = 0; i <= safeSegments; i++)
        {
            float angle = i / (float)safeSegments * Mathf.PI * 2f;
            vertex.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vertexHelper.AddVert(vertex);
        }

        for (int i = 1; i <= safeSegments; i++)
        {
            vertexHelper.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
        }
    }

    private static void AddPolygon(VertexHelper vertexHelper, Vector2[] points, Color color)
    {
        if (points == null || points.Length < 3)
        {
            return;
        }

        Vector2 center = Vector2.zero;
        for (int i = 0; i < points.Length; i++)
        {
            center += points[i];
        }

        center /= points.Length;
        int centerIndex = vertexHelper.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = center;
        vertexHelper.AddVert(vertex);

        for (int i = 0; i < points.Length; i++)
        {
            vertex.position = points[i];
            vertexHelper.AddVert(vertex);
        }

        for (int i = 0; i < points.Length; i++)
        {
            int current = centerIndex + 1 + i;
            int next = centerIndex + 1 + ((i + 1) % points.Length);
            vertexHelper.AddTriangle(centerIndex, current, next);
        }
    }
}
