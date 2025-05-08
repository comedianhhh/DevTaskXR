using UnityEngine;
using UnityEngine.EventSystems;

/*  DynamicPivotTool.cs
 *  Drop this component on an empty GameObject.
 *  ▸ Requires: Camera, LayerMask, Collider on the target mesh, a prefab for the pivot marker.
 *  ▸ SerializeField fields are exposed in the Inspector for easy drag‑and‑drop.
 *
 *  Author: 2025‑05‑08 Nianzhi
 */
public class DynamicPivotTool : MonoBehaviour
{

    #region Inspector Fields
    [Header("Scene References")]
    [Tooltip("Camera used to ray‑cast taps / drags.")]
    [SerializeField] private Camera cam;

    [Tooltip("Root that will be moved to the new pivot. Place all visual meshes under this object.")]
    [SerializeField] private Transform modelRoot;

    [Header("Interaction")]
    [Tooltip("Layers considered selectable as part of the model.")]
    [SerializeField] private LayerMask hitMask = ~0; // Everything by default

    [Tooltip("Prefab for the small pivot marker.")]
    [SerializeField] private GameObject pivotMarkerPrefab;

    [Tooltip("Degrees per pixel while dragging.")]
    [SerializeField] private float rotationSpeed = 0.25f;
    #endregion
    // ────────────────────────────────────────────────────────────────
    #region Private Fields
    private GameObject _markerInstance;
    private bool _isDragging;
    private Vector3 _prevPointerPos;
    #endregion
    // ────────────────────────────────────────────────────────────────
    #region Unity Life Cycle
    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }
    void Update()
    {
        HandlePointerDown();
        HandlePointerUp();
        HandleDrag();
    }
    #endregion
    // ────────────────────────────────────────────────────────────────
    #region Private Methods
    
    private void HandlePointerDown()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            _prevPointerPos = Input.mousePosition;
            TryPickNewPivot(_prevPointerPos);
            _isDragging = true;
        }
    }

    private void HandlePointerUp()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }
    }

    private void HandleDrag()
    {
        if (_isDragging && Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - _prevPointerPos;

            // Horizontal drag → yaw (around world‑up)
            modelRoot.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);

            // Vertical drag → pitch (around camera’s right)
            modelRoot.Rotate(cam.transform.right, delta.y * rotationSpeed, Space.World);

            _prevPointerPos = Input.mousePosition;
        }
    }

    // ────────────────────────────────────────────────────────────────
    private void TryPickNewPivot(Vector3 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, hitMask))
        {
            SetPivot(hit.point);
        }
    }

    private void SetPivot(Vector3 worldPoint)
    {
        // Calculate world‑space offset between current and new pivot.
        Vector3 delta = worldPoint - modelRoot.position;

        // Move the root to the desired pivot location.
        modelRoot.position = worldPoint;

        // Counter‑translate children so their world positions stay put.
        foreach (Transform child in modelRoot)
        {
            child.position -= delta;
        }

        PlaceMarker(worldPoint);
    }

    private void PlaceMarker(Vector3 worldPos)
    {
        if (_markerInstance == null && pivotMarkerPrefab != null)
        {
            _markerInstance = Instantiate(pivotMarkerPrefab,null);
        }

        if (_markerInstance != null)
        {
            _markerInstance.transform.position = worldPos;
        }
    }
    
    #endregion
}
