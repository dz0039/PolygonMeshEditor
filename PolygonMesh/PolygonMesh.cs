using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

// TODO: ugly toggle
// TODO: allow disable up/down faces

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class PolygonMesh : MonoBehaviour {
    [HideInInspector]
    [SerializeField]
    public List<Vector3> points = new List<Vector3>();
    public float y = 0f;
    public bool showMesh = false;
    public bool enableCollidor = false;
    public bool enableHeight = false;
    [ConditionalHide("enableHeight", true)]
    public float height = 1.0f;
    public Material material;

    MeshRenderer meshRenderer;
    [HideInInspector]
    public MeshFilter meshFilter;
    MeshCollider meshCollider;
    Vector3[] localVerts;
    int[] tris;

    void Start() {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        ToggleMeshRender();
        UpdatePointsPosition();
    }

    void OnValidate() {
        // y or height
        UpdatePointsPosition();
    }

    public void ToggleMeshRender() {
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (!showMesh) {
            if (meshRenderer != null) DestroyImmediate(meshRenderer);
            meshRenderer = null;
        } else {
            if (meshRenderer == null) {
                gameObject.AddComponent<MeshRenderer>();
                meshRenderer = gameObject.GetComponent<MeshRenderer>();
            }
            meshRenderer.material = material;
        }
    }

    public void ToggleMeshCollider() {
        meshCollider = gameObject.GetComponent<MeshCollider>();
        if (!enableCollidor) {
            if (meshCollider != null) DestroyImmediate(meshCollider);
            meshCollider = null;
        } else {
            if (meshCollider == null) {
                gameObject.AddComponent<MeshCollider>();
                meshCollider = gameObject.GetComponent<MeshCollider>();
            }
            UpdatePointsPosition();
        }
    }

    public void UpdateMesh() {
        localVerts = points.Select(p => transform.InverseTransformPoint(p)).ToArray();
        Vector2[] points2 = localVerts.Select(p => new Vector2(p.x, p.z)).ToArray();
        tris = new Triangulator(points2).Triangulate();

        if (enableHeight) {
            int count = points.Count;
            localVerts = localVerts.Concat(localVerts).ToArray();
            var trisList = tris.Concat(tris.Select(i => i + count)).ToList();
            for (int i = 0; i < count; i++) {
                trisList.Add(i + count);
                trisList.Add((i + 1) % count);
                trisList.Add(i);

                trisList.Add(((i + 1) % count + count) % (2 * count));
                trisList.Add((i + 1) % count);
                trisList.Add(i + count);
            }
            tris = trisList.ToArray();
        }
        UpdatePointsPosition();
    }

    void UpdatePointsPosition() {
        points = points.Select(p => new Vector3(p.x, y, p.z)).ToList();
        var localY = transform.InverseTransformPoint(new Vector3(0, y, 0)).y;

        if (localVerts == null || localVerts.Length == 0) return;
        for (int i = 0; i < points.Count; i++) {
            localVerts[i] = new Vector3(localVerts[i].x, localY, localVerts[i].z);
        }
        if (enableHeight && localVerts.Length == points.Count * 2) {
            for (int i = points.Count; i < points.Count * 2; i++) {
                localVerts[i] = new Vector3(localVerts[i].x, localY + height, localVerts[i].z);
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = localVerts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        if (meshCollider != null) meshCollider.sharedMesh = mesh;
    }
}
