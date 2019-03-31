using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class PolygonMesh : MonoBehaviour {
    [HideInInspector]
    [SerializeField]
    public List<Vector3> points;
    public float y = 0f;
    public bool showMesh = false;
    public bool enableHeight = false;
    [ConditionalHide("enableHeight", true)]
    public float height = 1.0f;
    public Material material;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    void Start() {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        UpdateShowMesh();
        UpdateMesh();
        UpdatePointsPosition();
    }

    void OnValidate() {
        // y or height
        UpdatePointsPosition();
    }

    public void UpdateShowMesh() {
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

    public void UpdateMesh() {
        Mesh mesh = new Mesh();
        Vector2[] points2 = points.Select(p => new Vector2(p.x, p.z)).ToArray();
        int[] tris = new Triangulator(points2).Triangulate();
        if (!enableHeight) {
            mesh.vertices = points.Select(p => transform.InverseTransformPoint(p)).ToArray();
            mesh.triangles = tris;
        } else {

        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;
    }

    void UpdatePointsPosition() {
        if (!enableHeight) {
            points = points.Select(p => new Vector3(p.x, y, p.z)).ToList();
        } else {
            points = points.GetRange(0, points.Count / 2)
                        .Select(p => new Vector3(p.x, y, p.z))
                        .Concat(
                            points.GetRange(points.Count / 2, points.Count / 2)
                                .Select(p => new Vector3(p.x, y + height, p.z))
                        )
                        .ToList();
        }
    }
}
