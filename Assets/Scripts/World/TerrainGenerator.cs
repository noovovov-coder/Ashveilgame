using UnityEngine;

namespace Ashveil.World
{
    /// <summary>
    /// Процедурный рельеф биома на шуме Перлина (GDD §14).
    /// Генерирует меш с коллайдером; высоту в любой точке можно
    /// запросить через GetHeight — для расстановки объектов.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField] private int worldSize = 200;     // метров на сторону
        [SerializeField] private int gridResolution = 120; // квадов на сторону
        [SerializeField] private float heightScale = 5f;
        [SerializeField] private float noiseScale = 0.015f;

        private int _seed;

        public int WorldSize => worldSize;

        public void Generate(int seed)
        {
            _seed = seed;

            int verts = gridResolution + 1;
            float cell = (float)worldSize / gridResolution;
            var vertices = new Vector3[verts * verts];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[gridResolution * gridResolution * 6];

            for (int z = 0; z < verts; z++)
            {
                for (int x = 0; x < verts; x++)
                {
                    float wx = x * cell - worldSize / 2f;
                    float wz = z * cell - worldSize / 2f;
                    vertices[z * verts + x] = new Vector3(wx, SampleHeight(wx, wz), wz);
                    uv[z * verts + x] = new Vector2((float)x / gridResolution, (float)z / gridResolution);
                }
            }

            int t = 0;
            for (int z = 0; z < gridResolution; z++)
            {
                for (int x = 0; x < gridResolution; x++)
                {
                    int i = z * verts + x;
                    triangles[t++] = i;
                    triangles[t++] = i + verts;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + verts;
                    triangles[t++] = i + verts + 1;
                }
            }

            var mesh = new Mesh { name = "BiomeTerrain" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>Высота рельефа в мировой точке (та же функция шума, что и меш).</summary>
        public float GetHeight(float worldX, float worldZ) => SampleHeight(worldX, worldZ);

        private float SampleHeight(float x, float z)
        {
            float offset = _seed * 0.0137f;
            float n = Mathf.PerlinNoise(x * noiseScale + offset, z * noiseScale + offset);
            float detail = Mathf.PerlinNoise(x * noiseScale * 4f + offset, z * noiseScale * 4f + offset);
            return (n * 0.8f + detail * 0.2f) * heightScale;
        }
    }
}
