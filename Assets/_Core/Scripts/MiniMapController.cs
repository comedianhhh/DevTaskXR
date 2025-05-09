/*  MiniMapController.cs
 *  Single component that: 
 *    • Draws a wireframe bounding box around a model (shown only in the mini‑map)
 *    • Lets the player click the mini‑map to move the main camera, clamped to the box
 *  ▸ Put this script on the UI RawImage that shows the mini‑map RenderTexture.
 *  ▸ Create one material for the LineRenderer (Unlit/Color).
 *  ▸ Create a  LineRenderer GO at runtime.
 *
 *  Author: 2025‑05‑08  Nianzhi
 */
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniMapController : MonoBehaviour, IPointerClickHandler
{
    #region Inspector
    [Header("Camera References")]
    [Tooltip("Orthographic top‑down camera that renders the mini‑map view.")]
    [SerializeField] private Camera miniMapCam;

    [Tooltip("Main gameplay camera to move.")]
    [SerializeField] private  Camera mainCam;

    [Header("Model + Overlay")]
    [Tooltip("Root whose children’s Renderers define the bounds.")]
    [SerializeField] private  Transform modelRoot;

    [Tooltip("Material used for the wireframe box.")]
    [SerializeField] private  Material wireMaterial;

    [Range(0.01f, 0.3f)]
    [SerializeField] private  float wireWidth = 0.05f;

    [Header("Movement")]
    [Tooltip("Meters outside bounds the camera is allowed to move.")]
    [SerializeField] private  float clampMargin = 0.5f;

    [Tooltip("Seconds for camera to slide (0 = instant).")]
    [SerializeField] private  float transitionTime = 0.5f;
    #endregion

    #region Private Fields
    private Bounds       _bounds;
    private LineRenderer _line;
    private Coroutine    _moveRoutine;
    #endregion
    
    #region Unity Life Cycle
    private void Awake()
    {
        BuildLineRenderer();
        RecalculateBounds();
    }
    
    #endregion
    
    #region Bounding Box
    private void BuildLineRenderer()
    {
        GameObject go = new("MiniMap_BoundsWire", typeof(LineRenderer));
        go.transform.SetParent(null);
        _line = go.GetComponent<LineRenderer>();
        _line.widthMultiplier   = wireWidth;
        _line.loop              = false;
        _line.useWorldSpace     = true;
        _line.material          = wireMaterial;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows    = false;
    }

    private void RecalculateBounds()
    {
        _bounds = CalculateBounds();
        DrawWireBox();
    }

    private Bounds CalculateBounds()
    {
        Renderer[] rends = modelRoot.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(modelRoot.position, Vector3.one);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }

    private void DrawWireBox()
    {
        Vector3 c = _bounds.center;
        Vector3 e = _bounds.extents;
        float y  = _bounds.max.y + 0.02f;          // draw just above roof
        Vector3[] v =
        {
            c + new Vector3(-e.x, y - c.y, -e.z),
            c + new Vector3( e.x, y - c.y, -e.z),
            c + new Vector3( e.x, y - c.y,  e.z),
            c + new Vector3(-e.x, y - c.y,  e.z),
            
            c + new Vector3(-e.x, y - c.y, -e.z),
            c + new Vector3(-e.x, -e.y, -e.z),
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3( e.x, y - c.y, -e.z),
            
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3( e.x, y - c.y,  e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x, y - c.y,  e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x, -e.y, -e.z)
        };
        _line.positionCount = v.Length;
        _line.SetPositions(v);
    }
    #endregion
    
    #region Click‑to‑Move
    public void OnPointerClick(PointerEventData data)
    {
        RectTransform rt = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, data.position, data.pressEventCamera, out Vector2 local)) return;
        Vector2 uv = new(local.x / rt.rect.width + 0.5f, local.y / rt.rect.height + 0.5f);

        Ray ray = miniMapCam.ViewportPointToRay(new Vector3(uv.x, uv.y, 0));
        Plane ground = new Plane(Vector3.up, new Vector3(0, _bounds.center.y, 0));
        if (!ground.Raycast(ray, out float enter)) return;
        Vector3 hit = ray.GetPoint(enter);

        hit.x = Mathf.Clamp(hit.x, _bounds.min.x - clampMargin, _bounds.max.x + clampMargin);
        hit.z = Mathf.Clamp(hit.z, _bounds.min.z - clampMargin, _bounds.max.z + clampMargin);

        Vector3 camOffset = mainCam.transform.position - GetLookPoint();
        Vector3 destPos = hit + camOffset;

        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveCamera(destPos, hit));
    }

    private Vector3 GetLookPoint()
    {
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
        Plane ground = new Plane(Vector3.up, new Vector3(0, _bounds.center.y, 0));
        return ground.Raycast(ray, out float enter) ? ray.GetPoint(enter) : _bounds.center;
    }

    private IEnumerator MoveCamera(Vector3 destPos, Vector3 lookAt)
    {
        if (transitionTime <= 0f)
        {
            mainCam.transform.position = destPos;
            mainCam.transform.rotation = Quaternion.LookRotation(lookAt - destPos, Vector3.up);
            yield break;
        }
        Vector3    startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;
        Quaternion destRot  = Quaternion.LookRotation(lookAt - destPos, Vector3.up);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionTime;
            float s = Mathf.SmoothStep(0f, 1f, t);
            mainCam.transform.position = Vector3.Lerp(startPos, destPos, s);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, destRot, s);
            yield return null;
        }
    }
    #endregion
}