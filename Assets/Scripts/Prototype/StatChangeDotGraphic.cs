using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Stat Change Dot Graphic")]
[RequireComponent(typeof(CanvasRenderer))]
public class StatChangeDotGraphic : MaskableGraphic
{
    [SerializeField] private bool isVisible;
    [SerializeField] private bool isLarge;
    [SerializeField] private Color dotColor = Color.clear;
    [SerializeField] private int segments = 32;

    public void Show(int amount, Color positiveColor, Color negativeColor)
    {
        if (amount == 0)
        {
            Hide();
            return;
        }

        isVisible = true;
        isLarge = Mathf.Abs(amount) > 14;
        dotColor = amount > 0 ? positiveColor : negativeColor;
        SetVerticesDirty();
    }

    public void ShowMagnitude(int amount, Color neutralColor)
    {
        if (amount == 0)
        {
            Hide();
            return;
        }

        isVisible = true;
        isLarge = Mathf.Abs(amount) > 14;
        dotColor = neutralColor;
        SetVerticesDirty();
    }

    public void Hide()
    {
        isVisible = false;
        dotColor = Color.clear;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        if (!isVisible || dotColor.a <= 0f)
        {
            return;
        }

        Rect rect = GetPixelAdjustedRect();
        float size = Mathf.Min(rect.width, rect.height);
        if (size <= 0f)
        {
            return;
        }

        float radius = size * (isLarge ? 0.38f : 0.26f);
        if (isLarge)
        {
            AddCircle(vertexHelper, rect.center, radius, dotColor);
            return;
        }

        AddRing(vertexHelper, rect.center, radius, Mathf.Max(2f, size * 0.08f), dotColor);
    }

    private void AddCircle(VertexHelper vertexHelper, Vector2 center, float radius, Color color)
    {
        int safeSegments = Mathf.Max(12, segments);
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

    private void AddRing(VertexHelper vertexHelper, Vector2 center, float outerRadius, float width, Color color)
    {
        int safeSegments = Mathf.Max(12, segments);
        float innerRadius = Mathf.Max(0f, outerRadius - width);
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        for (int i = 0; i < safeSegments; i++)
        {
            float angleA = i / (float)safeSegments * Mathf.PI * 2f;
            float angleB = (i + 1) / (float)safeSegments * Mathf.PI * 2f;
            Vector2 outerA = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * outerRadius;
            Vector2 outerB = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * outerRadius;
            Vector2 innerB = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * innerRadius;
            Vector2 innerA = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * innerRadius;
            int startIndex = vertexHelper.currentVertCount;

            vertex.position = outerA;
            vertexHelper.AddVert(vertex);
            vertex.position = outerB;
            vertexHelper.AddVert(vertex);
            vertex.position = innerB;
            vertexHelper.AddVert(vertex);
            vertex.position = innerA;
            vertexHelper.AddVert(vertex);

            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }
    }
}
