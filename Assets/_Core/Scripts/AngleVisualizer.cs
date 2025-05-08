/*  AngleMeasurementTool.cs
 *  Drop this component on an empty GameObject.
 *  ▸ Requires:LineRenderer, Collider on the target mesh, a world‑space Canvas.
 *  ▸ Public fields are exposed in the Inspector for easy drag‑and‑drop.
 *
 *  Author: 2025‑05‑07 Nianzhi
 */
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class AngleMeasurementTool : MonoBehaviour
{

    #region Inspector
    [Header("Scene references")]
    [Tooltip("Camera used to ray‑cast taps.")]
    [SerializeField] private Camera mainCamera;
    
    [Tooltip("Canvas used to display the angle label.")]
    [SerializeField] private Transform canvasRoot;          // A world‑space canvas
    
    [Tooltip("Prefab for the angle label.")]
    [SerializeField] private GameObject textPrefab;         // A TextMeshProUGUI prefab (world‑space)
    
    [Tooltip("Material used to draw the filled arc.")]
    [SerializeField] private Material filledArcMaterial;    // Unlit + transparent material works best
    
    [Header("Interaction")]
    [SerializeField] private Button clearButton;

    [Header("Visual settings")]
    [Range(0.05f, 5f)] [SerializeField] private float arcRadius = 0.5f;
    [Range(4, 128)]   [SerializeField] private int   arcSegments = 40;
    #endregion
    // ──────────────────────────────────────────────────────────
    #region Private fields
    private readonly List<Vector3> _points = new();   // Selected positions
    private LineRenderer           _line;             // Draws the two sides
    private GameObject             _arcGo;            // Holds the arc mesh
    private Mesh                   _arcMesh;          // Re‑used each frame
    private TextMeshProUGUI        _angleLabel;       // Angle value
    #endregion
    // ──────────────────────────────────────────────────────────
    #region Unity Life Cycle
    
    private void Awake ()
    {
        InitializeTool();
    }
    
    // ──────────────────────────────────────────────────────────
    private void Update ()
    {
        // 1. Handle mouse clicks until we have three points -------------------
        HandlePointerDown();

        // 2. If three points exist, update every frame so live‑moving works ---
        if (_points.Count == 3)
        {
            UpdateVisuals();
        }
    }
    #endregion
    // ──────────────────────────────────────────────────────────
    #region Private methods
    private void InitializeTool()
    {
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 0;
        _line.useWorldSpace = true;

        // Arc mesh container ---------------------------------------------------
        _arcGo = new GameObject("AngleArc", typeof(MeshFilter), typeof(MeshRenderer));
        _arcGo.transform.parent = transform;
        _arcMesh = new Mesh { name = "AngleArcMesh" };
        
        _arcGo.GetComponent<MeshFilter>().sharedMesh = _arcMesh;
        _arcGo.GetComponent<MeshRenderer>().material = filledArcMaterial;

        // Label ---------------------------------------------------------------
        _angleLabel = Instantiate(textPrefab, canvasRoot).GetComponent<TextMeshProUGUI>();
        _angleLabel.text = string.Empty;

        clearButton.onClick.AddListener(Clear);
    }
    
    private void HandlePointerDown()
    {
        if (Input.GetMouseButtonDown(0) && _points.Count < 3)
        {
            if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition),
                                out RaycastHit hit, Mathf.Infinity))
            {
                _points.Add(hit.point);
                if (_points.Count == 3) BuildLineRenderer();
            }
        }
    }
    
    private void BuildLineRenderer ()
    {
        _line.positionCount = 4; // A‑B, A‑C (A duplicated)
        _line.widthMultiplier = 0.01f;
    }
    private void UpdateVisuals()
    {
        Vector3 A = _points[0];
        Vector3 B = _points[1];
        Vector3 C = _points[2];

        // Update line positions ----------------------------------------------
        _line.SetPosition(0, A);
        _line.SetPosition(1, B);
        _line.SetPosition(2, A);
        _line.SetPosition(3, C);

        // Calculate angle -----------------------------------------------------
        Vector3 dir1 = (B - A).normalized;
        Vector3 dir2 = (C - A).normalized;
        float angleDeg = Vector3.Angle(dir1, dir2);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        // Label position (a bit outside the arc) -----------------------------
        Vector3 bisector = (dir1 + dir2).normalized;
        
        _angleLabel.text = $"{angleDeg:F1}°";
        // World point where we want the label
        Vector3 worldPos  = A + bisector * (arcRadius + 0.05f);

        // Convert to screen point and feed it to the RectTransform
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(mainCamera, worldPos);
        _angleLabel.rectTransform.position = screenPos;
        // Draw filled arc ------------------------------------------------------
        DrawFilledArc(A, dir1, Vector3.Cross(dir1, dir2).normalized, angleRad);
    }
    private void DrawFilledArc(Vector3 center, Vector3 startDir, Vector3 normal, float angleRad)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new() { center };
        List<int> triangles = new();
        
        
        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float currentAngle = t * angleRad;
            Quaternion rot = Quaternion.AngleAxis(Mathf.Rad2Deg * currentAngle, normal);
            Vector3 point = center + rot * startDir * arcRadius;
            vertices.Add(point);
            if (i > 0)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        _arcGo.GetComponent<MeshFilter>().mesh = mesh;
        
    }
    private void Clear ()
    {
        _points.Clear();
        _line.positionCount = 0;
        _arcMesh.Clear();
        _angleLabel.text = string.Empty;
        _arcGo.GetComponent<MeshFilter>().mesh = null;
    }
    #endregion
}
