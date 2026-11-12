using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// Builds a small five-pointed star prism (the top figure rank's star, progression spec 2026-11-12). The star
    /// stands upright in the XY plane, centred on the origin, <paramref name="depth"/> thick along Z. Vertex colours
    /// carry the smoothed outline normal (direction from the centre, encoded n * 0.5 + 0.5) for the toon shader's
    /// <c>_OutlineSmoothNormals</c> mode, so the outline does not split at the points.
    /// </summary>
    /// <example><code>Mesh star = StarMesh.Create(0.5f, 0.22f, 0.18f); filter.sharedMesh = star;</code></example>
    public static class StarMesh
    {
        private const int Points = 5;

        /// <summary>
        /// Creates the mesh: <paramref name="outer"/> and <paramref name="inner"/> are the tip and notch radii,
        /// <paramref name="depth"/> the thickness. The caller owns the mesh and destroys it.
        /// </summary>
        public static Mesh Create(float outer, float inner, float depth)
        {
            int ring = Points * 2;
            float half = depth * 0.5f;
            var outline = new Vector3[ring];
            for (int i = 0; i < ring; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / Points; // first tip points up
                float r = (i & 1) == 0 ? outer : inner;
                outline[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            }

            // Front and back faces: a fan around a centre vertex. Sides: one quad per outline edge (flat normals).
            int faceVerts = ring + 1;
            var vertices = new Vector3[faceVerts * 2 + ring * 4];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[ring * 3 * 2 + ring * 6];
            int t = 0;
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? -half : half;
                Vector3 n = side == 0 ? Vector3.back : Vector3.forward;
                int start = side * faceVerts;
                vertices[start] = new Vector3(0f, 0f, z);
                normals[start] = n;
                for (int i = 0; i < ring; i++)
                {
                    vertices[start + 1 + i] = outline[i] + new Vector3(0f, 0f, z);
                    normals[start + 1 + i] = n;
                }
                for (int i = 0; i < ring; i++)
                {
                    int a = start + 1 + i;
                    int b = start + 1 + (i + 1) % ring;
                    triangles[t++] = start;
                    // Winding follows the face normal (cross(b - a, c - a) = n), so both faces point outwards.
                    triangles[t++] = side == 0 ? b : a;
                    triangles[t++] = side == 0 ? a : b;
                }
            }
            int v = faceVerts * 2;
            for (int i = 0; i < ring; i++)
            {
                Vector3 p0 = outline[i];
                Vector3 p1 = outline[(i + 1) % ring];
                Vector3 n = Vector3.Cross(p1 - p0, Vector3.forward).normalized;
                vertices[v] = p0 + new Vector3(0f, 0f, -half);
                vertices[v + 1] = p1 + new Vector3(0f, 0f, -half);
                vertices[v + 2] = p1 + new Vector3(0f, 0f, half);
                vertices[v + 3] = p0 + new Vector3(0f, 0f, half);
                for (int k = 0; k < 4; k++) normals[v + k] = n;
                triangles[t++] = v;
                triangles[t++] = v + 1;
                triangles[t++] = v + 2;
                triangles[t++] = v;
                triangles[t++] = v + 2;
                triangles[t++] = v + 3;
                v += 4;
            }

            var colors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 s = vertices[i].sqrMagnitude > 1e-8f ? vertices[i].normalized : normals[i];
                colors[i] = new Color(s.x * 0.5f + 0.5f, s.y * 0.5f + 0.5f, s.z * 0.5f + 0.5f, 1f);
            }

            var mesh = new Mesh { name = "RankStar" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
