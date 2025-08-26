using System;
using UnityEngine;

public class Arrow3DPointer : MonoBehaviour
{
    [Header("Target Settings")]
    public Vector3 from; // Vector3 kezdőpont
    public Camera mainCamera; // Referencia a kamerához

    [Header("Arrow Components")]
    public SpriteRenderer arrowHeadSprite;
    public LineRenderer lineRenderer;

    [Header("Arrow Settings")]
    public float arrowHeadSize = 1f;
    public float heightOffset = 2f;
    public int curveSegments = 20;

    [Header("Animation")]
    public bool animateArrow = true;
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;
    public float rotationSpeed = 100f;

    [Header("Materials")]
    public Material lineMaterial;
    public Sprite arrowSprite;

    private float animationTime = 0f;
    private Vector3[] curvePoints;
    private Vector3 currentTargetPosition; // Egérpozíció a 3D térben
    public static Arrow3DPointer instance;
    void Start()
    {
        instance = this;
        TurnOff();
        SetupComponents();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        SetArrow(new Vector3(0, 0, 0));
    }

    internal void TurnOff()
    {
        on = false;
        SetArrowVisible(false);
    }
    void Select()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            ILiveTarget target = hit.collider.GetComponent<ILiveTarget>();
            if (target != null) { 
                
                
            }
            else
            {
                GameManager.instance.CancelAttack();
            }
        }
        else
        {
            GameManager.instance.CancelAttack();
        }

    }
    void SetupComponents()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.useWorldSpace = true;

        if (lineMaterial != null)
            lineRenderer.material = lineMaterial;

        if (arrowHeadSprite == null && arrowSprite != null)
        {
            GameObject spriteObj = new GameObject("Arrow Head Sprite");
            spriteObj.transform.SetParent(transform);
            arrowHeadSprite = spriteObj.AddComponent<SpriteRenderer>();
            arrowHeadSprite.sprite = arrowSprite;
        }

        if (arrowHeadSprite != null)
        {
            arrowHeadSprite.transform.localScale = Vector3.one * arrowHeadSize;
        }
    }
    bool on = true;
    void Update()
    {
        if (mainCamera == null||!on) return;

        UpdateTargetPosition();
        animationTime += Time.deltaTime;
        CalculateCurvePoints();
        DrawCurve();
        UpdateArrowHead();
        if (Input.GetMouseButtonDown(0))
        {
            Select();
        }
    }

    // Frissíti a célpontot az egér pozíciója alapján
    void UpdateTargetPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            currentTargetPosition = hit.point;
            
        }
        else
        {
            // Alapértelmezett távolság ha nincs találat
            currentTargetPosition = ray.GetPoint(10f);
        }
    }

    void CalculateCurvePoints()
    {
        Vector3 startPos = from;
        Vector3 endPos = currentTargetPosition;

        Vector3 midPoint = (startPos + endPos) * 0.5f;
        midPoint.y += heightOffset;

        if (animateArrow)
        {
            float bobOffset = Mathf.Sin(animationTime * bobSpeed) * bobHeight;
            midPoint.y += bobOffset;
        }

        curvePoints = new Vector3[curveSegments + 1];

        for (int i = 0; i <= curveSegments; i++)
        {
            float t = (float)i / curveSegments;
            curvePoints[i] = CalculateQuadraticBezierPoint(startPos, midPoint, endPos, t);
        }
    }

    Vector3 CalculateQuadraticBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 point = uu * p0;
        point += 2 * u * t * p1;
        point += tt * p2;

        return point;
    }

    // Egyszerű folytonos görbe rajzolása
    void DrawCurve()
    {
        if (curvePoints == null) return;

        lineRenderer.positionCount = curvePoints.Length;
        lineRenderer.SetPositions(curvePoints);
    }

    void UpdateArrowHead()
    {
        if (arrowHeadSprite == null || curvePoints == null || curvePoints.Length < 2) return;

        Vector3 direction = (curvePoints[curvePoints.Length - 1] - curvePoints[curvePoints.Length - 2]).normalized;
        arrowHeadSprite.transform.position = curvePoints[curvePoints.Length - 1];

        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

            if (animateArrow)
            {
                angle += Mathf.Sin(animationTime * rotationSpeed * 0.1f) * 10f;
            }

            arrowHeadSprite.transform.rotation = Quaternion.Euler(0, 0, -angle);
        }
    }

    public void SetArrowVisible(bool visible)
    {
        if (lineRenderer) lineRenderer.enabled = visible;
        if (arrowHeadSprite) arrowHeadSprite.enabled = visible;
    }
    public void SetArrow(Vector3 position)
    {
        from= position;
        SetArrowVisible(true);
        on = true;
    }

    public void SetArrowColor(Color color)
    {
        if (lineRenderer && lineRenderer.material)
            lineRenderer.material.color = color;

        if (arrowHeadSprite)
            arrowHeadSprite.color = color;
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(from, currentTargetPosition);
        Gizmos.DrawWireSphere(currentTargetPosition, 0.2f);

        if (curvePoints != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < curvePoints.Length - 1; i++)
            {
                Gizmos.DrawLine(curvePoints[i], curvePoints[i + 1]);
            }
        }
    }
}