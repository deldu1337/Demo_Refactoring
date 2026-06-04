using System.Collections.Generic;
using UnityEngine;

public class AStarPathVisualizer : MonoBehaviour
{
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color pathColor = Color.red;
    [SerializeField] private float lineWidth = 0.18f;
    [SerializeField] private float yOffset = 0.08f;

    private bool isPreviewEnabled;
    private PlayerMove subscribedPlayerMove;

    private void Awake()
    {
        ResolvePlayerMove();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        SetupLineRenderer();
        HidePath();
    }

    private void OnEnable()
    {
        ResolvePlayerMove();
    }

    private void OnDisable()
    {
        UnsubscribePlayerMove();
    }

    public void TogglePathPreview()
    {
        SetPathPreviewEnabled(!isPreviewEnabled);
    }

    public void SetPathPreviewEnabled(bool enabled)
    {
        ResolvePlayerMove();
        isPreviewEnabled = enabled;

        if (!isPreviewEnabled)
        {
            HidePath();
            return;
        }

        if (playerMove != null)
            DrawPath(playerMove.GetCurrentPathSnapshot());
    }

    public void ShowPathPreview()
    {
        SetPathPreviewEnabled(true);
    }

    public void HidePathPreview()
    {
        SetPathPreviewEnabled(false);
    }

    private void HandlePathUpdated(IReadOnlyList<Vector3> path)
    {
        if (!isPreviewEnabled)
            return;

        DrawPath(path);
    }

    private void DrawPath(IReadOnlyList<Vector3> path)
    {
        if (path == null || path.Count < 2)
        {
            HidePath();
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.positionCount = path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 point = path[i];
            point.y += yOffset;
            lineRenderer.SetPosition(i, point);
        }
    }

    private void HidePath()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }

    private void SetupLineRenderer()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.positionCount = 0;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.alignment = LineAlignment.View;

        var material = new Material(Shader.Find("Sprites/Default"));
        material.color = pathColor;
        lineRenderer.material = material;
        lineRenderer.startColor = pathColor;
        lineRenderer.endColor = pathColor;
    }

    private void ResolvePlayerMove()
    {
        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();

        if (playerMove == null)
            playerMove = FindAnyObjectByType<PlayerMove>();

        if (subscribedPlayerMove == playerMove)
            return;

        UnsubscribePlayerMove();

        if (playerMove != null)
        {
            playerMove.PathUpdated += HandlePathUpdated;
            subscribedPlayerMove = playerMove;
        }
    }

    private void UnsubscribePlayerMove()
    {
        if (subscribedPlayerMove == null)
            return;

        subscribedPlayerMove.PathUpdated -= HandlePathUpdated;
        subscribedPlayerMove = null;
    }
}
