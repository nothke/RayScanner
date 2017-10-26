using System.Collections.Generic;
using UnityEngine;

public class Scan : MonoBehaviour
{
    List<Vector3> points = new List<Vector3>();

    public Material material;

    MeshFilter mf;
    MeshRenderer mr;

    public enum Method { FrustumScan, RadialScan, RadialMC, RadialMCBounced };
    public Method method;

    public float verticalAngle = 25;
    public float horizontalAngle = 25;
    public int bounces = 1;

    public float maxRange = Mathf.Infinity;

    void Start()
    {
        GameObject renderGO = new GameObject("RenderGO");
        renderGO.layer = 8;

        mf = renderGO.AddComponent<MeshFilter>();
        mr = renderGO.AddComponent<MeshRenderer>();
        mr.material = material;

        DoScan();

        Meshify();
    }

    public void DoScan()
    {
        switch (method)
        {
            case Method.FrustumScan: ScanFrustum(); break;
            //case Method.FrustumMC: ScanFrustum(); break;
            case Method.RadialScan: ScanRadial(); break;
            case Method.RadialMC: ScanRadialMC(); break;
            case Method.RadialMCBounced: ScanReflection(); break;
        }
    }

    int angularPoints = 255;

    void ScanFrustum()
    {
        points.Clear();

        for (int y = 0; y < angularPoints; y++)
        {
            Vector3 dir = transform.forward;
            float vTheta = verticalAngle / angularPoints;
            dir = Quaternion.AngleAxis(-verticalAngle * 0.5f + vTheta * y, transform.right) * dir;
            dir = Quaternion.AngleAxis(-horizontalAngle * 0.5f, transform.up) * dir;

            for (int x = 0; x < angularPoints; x++)
            {
                float hTheta = horizontalAngle / angularPoints;

                dir = Quaternion.AngleAxis(hTheta, transform.up) * dir;
                Vector3 scanDir = dir;

                RaycastHit hit;
                if (Physics.Raycast(transform.position, scanDir, out hit, maxRange))
                {
                    points.Add(hit.point);
                }
            }
        }
    }

    void ScanReflection()
    {
        points.Clear();

        int num = angularPoints * angularPoints / (bounces + 1);

        for (int y = 0; y < num; y++)
        {
            Vector3 scanDir = Random.onUnitSphere;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, scanDir, out hit, maxRange))
            {
                points.Add(hit.point);

                Vector3 reflectedDir;

                for (int i = 0; i < bounces; i++)
                {
                    reflectedDir = Vector3.Reflect(scanDir, hit.normal);
                    reflectedDir += Random.onUnitSphere * 0.5f;

                    if (Physics.Raycast(hit.point, reflectedDir, out hit, maxRange))
                    {
                        points.Add(hit.point);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Radial Monte Carlo (random) scan
    /// </summary>
    void ScanRadialMC()
    {
        points.Clear();

        int num = angularPoints * angularPoints;

        for (int y = 0; y < num; y++)
        {
            Vector3 scanDir = Random.onUnitSphere;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, scanDir, out hit, maxRange))
            {
                points.Add(hit.point);
            }
        }
    }

    /// <summary>
    /// Scan radial scanline
    /// </summary>
    void ScanRadial()
    {
        points.Clear();

        Vector3 dir = Quaternion.AngleAxis(-verticalAngle * 0.5f, transform.right) * transform.forward;
        float byH = 360.0f / angularPoints;
        float byV = verticalAngle / angularPoints;

        for (int y = 0; y < angularPoints; y++)
        {
            dir = Quaternion.AngleAxis(byV, transform.right) * dir;

            for (int x = 0; x < angularPoints; x++)
            {
                dir = Quaternion.AngleAxis(byH, transform.up) * dir;

                Vector3 scanDir = dir;

                RaycastHit hit;
                if (Physics.Raycast(transform.position, scanDir, out hit, maxRange))
                {
                    points.Add(hit.point);
                }
            }
        }
    }

    /// <summary>
    /// Convert points to mesh
    /// </summary>
    void Meshify()
    {
        if (mf.sharedMesh)
            Destroy(mf.sharedMesh);

        Mesh m = new Mesh();

        int pointsNum = points.Count;

        Vector3[] vertices = new Vector3[pointsNum];
        Color[] colors = new Color[pointsNum];

        int[] triangles = new int[pointsNum * 3];

        for (int i = 0; i < pointsNum; i++)
        {
            vertices[i] = points[i];

            colors[i] = Color.white;

            triangles[i * 3] = i;
            triangles[i * 3 + 1] = i;
            triangles[i * 3 + 2] = i;
        }

        m.vertices = vertices;
        m.colors = colors;
        m.triangles = triangles;

        m.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);

        mf.sharedMesh = m;

    }




#if UNITY_EDITOR
    public Color gizmoColor = Color.white;
    public float gizmoLength = 10;


    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        UnityEditor.Handles.color = gizmoColor;

        if (method == Method.FrustumScan)
        {
            Vector3 dir = transform.forward * gizmoLength;
            dir = Quaternion.AngleAxis(-verticalAngle * 0.5f, transform.right) * dir;
            dir = Quaternion.AngleAxis(-horizontalAngle * 0.5f, transform.up) * dir;
            Gizmos.DrawRay(transform.position, dir);
            dir = Quaternion.AngleAxis(horizontalAngle, transform.up) * dir;
            Gizmos.DrawRay(transform.position, dir);

            dir = transform.forward * gizmoLength;
            dir = Quaternion.AngleAxis(verticalAngle * 0.5f, transform.right) * dir;
            dir = Quaternion.AngleAxis(-horizontalAngle * 0.5f, transform.up) * dir;
            Gizmos.DrawRay(transform.position, dir);
            dir = Quaternion.AngleAxis(horizontalAngle, transform.up) * dir;
            Gizmos.DrawRay(transform.position, dir);
        }

        if (method == Method.RadialScan)
        {
            Vector3 p1 = Quaternion.AngleAxis(-verticalAngle * 0.5f, transform.right) * transform.forward;
            Vector3 p2 = Quaternion.AngleAxis(verticalAngle * 0.5f, transform.right) * transform.forward;

            Gizmos.DrawLine(transform.position, transform.position + p1 * gizmoLength);
            Gizmos.DrawLine(transform.position, transform.position + p2 * gizmoLength);

            Vector3 lp = transform.InverseTransformDirection(p1);
            float h = lp.y;
            float r = lp.z;

            UnityEditor.Handles.CircleHandleCap(-1, transform.position + transform.up * h, Quaternion.LookRotation(transform.up), r, EventType.Repaint);
            UnityEditor.Handles.CircleHandleCap(-1, transform.position - transform.up * h, Quaternion.LookRotation(transform.up), r, EventType.Repaint);
        }

        if (method == Method.RadialMC || method == Method.RadialMCBounced)
        {
            Gizmos.DrawWireSphere(transform.position, gizmoLength);
        }
    }
#endif
}
