using UnityEngine;
using System.Collections;

public static class Graphics {

    public class RawMeshData
    {
        public Vector3[] vertices;
        public int[] triangles;

        public RawMeshData(Vector3[] _v, int[] _t)
        {
            this.vertices = _v;
            this.triangles = _t;
        }
    }

    public static RawMeshData CubeRawMesh(Vector3 center, float size)
    {
        float e = size / 2f;
        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(-e, -e, -e) + center,
            new Vector3(-e, e, -e) + center,
            new Vector3(e, e, -e) + center,
            new Vector3(e, -e, -e) + center,
            new Vector3(e, -e, e) + center,
            new Vector3(e, e, e) + center,
            new Vector3(-e, e, e) + center,
            new Vector3(-e, -e, e) + center
        };

        int[] tri = new int[36]
        {
            0, 1, 3,
            1, 2, 3,
            3, 2, 5,
            3, 5, 4,
            5, 2, 1,
            5, 1, 6,
            3, 4, 7,
            3, 7, 0,
            0, 7, 6,
            0, 6, 1,
            4, 5, 6,
            4, 6, 7
        };

        return new RawMeshData(vertices, tri);
    }

    public static GameObject CreateCube(Vector3 center, float size)
    {
        GameObject go = new GameObject();

        RawMeshData raw = CubeRawMesh(center, size);

        Mesh mesh = new Mesh();
        mesh.vertices = raw.vertices;
        mesh.triangles = raw.triangles;
        mesh.RecalculateNormals();
        //mesh.uv = new Vector2[8]
        //{
        //    Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero,
        //    Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero,
        //};
        go.AddComponent<MeshFilter>().mesh = mesh;
        go.AddComponent<MeshRenderer>();
        //go.renderer.material = new Material(Shader.Find("Self-Illumin/VertexLit"));

        return go;
    }
}
