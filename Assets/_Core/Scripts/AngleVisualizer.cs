using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class AngleVisualizer : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject textPrefab;
    public Material arcMaterial;
    public float arcRadius = 0.5f;
    public int arcSegments = 30;

    private List<Vector3> points = new();
    private LineRenderer lineRenderer;
    private GameObject arcObject;
    private TextMeshProUGUI angleText;
    public Button clearButton;
    
    public Transform canvasTransform;

    
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        arcObject = new GameObject("AngleArc");
        arcObject.AddComponent<MeshFilter>();
        var renderer = arcObject.AddComponent<MeshRenderer>();
        renderer.material = arcMaterial;
        GameObject textGO = Instantiate(textPrefab, canvasTransform);
        angleText = textGO.GetComponent<TextMeshProUGUI>();
        angleText.text = "";
        clearButton.onClick.AddListener(Clear);
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && points.Count < 3)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                points.Add(hit.point);
                if (points.Count == 3)
                    DrawAngle();
            }
        }
    }

    void DrawAngle()
    {
        Vector3 A = points[0];
        Vector3 B = points[1];
        Vector3 C = points[2];

        // LineRenderer for AB and AC
        lineRenderer.positionCount = 4;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, A);
        lineRenderer.SetPosition(1, B);
        lineRenderer.SetPosition(2, A);
        lineRenderer.SetPosition(3, C);

        // Calculate angle
        Vector3 dir1 = (B - A).normalized;
        Vector3 dir2 = (C - A).normalized;
        float angle = Vector3.Angle(dir1, dir2);

        // Place text
        Vector3 mid = A + (dir1 + dir2).normalized * (arcRadius + 0.05f);
        
        angleText.text = Mathf.Round(angle) + "°";
        angleText.rectTransform.position = mid;
        angleText.rectTransform.LookAt(mainCamera.transform);
        angleText.rectTransform.Rotate(0, 180, 0); // Face the camera correctly
        angleText.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);

        // Draw arc
        DrawArcMesh(A, dir1, dir2, Vector3.Cross(dir1, dir2).normalized, angle * Mathf.Deg2Rad);
    }

    void DrawArcMesh(Vector3 center, Vector3 startDir, Vector3 endDir, Vector3 normal, float angleRad)
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
        arcObject.GetComponent<MeshFilter>().mesh = mesh;
    }

    public void Clear()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
        arcObject.GetComponent<MeshFilter>().mesh = null;
        angleText.text = "";
    }
    
}
