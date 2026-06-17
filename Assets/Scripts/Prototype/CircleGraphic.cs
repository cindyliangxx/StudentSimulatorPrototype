using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Circle Graphic")]
[RequireComponent(typeof(CanvasRenderer))]
public class CircleGraphic : MaskableGraphic
{
    [SerializeField] private int segments = 32;

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        if (radius <= 0f)
        {
            return;
        }

        int safeSegments = Mathf.Max(12, segments);
        Vector2 center = rect.center;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = center;
        vertexHelper.AddVert(vertex);

        for (int i = 0; i <= safeSegments; i++)
        {
            float angle = (float)i / safeSegments * Mathf.PI * 2f;
            vertex.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vertexHelper.AddVert(vertex);
        }

        for (int i = 1; i <= safeSegments; i++)
        {
            vertexHelper.AddTriangle(0, i, i + 1);
        }
    }
}
