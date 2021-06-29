using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMesh : MonoBehaviour
{
    public GameObject Mesh;
    Mesh mesh;

    Vector3[] vertices;
    int[] indices;

    public bool isInfinite = false;

    public int width = 20;
    public int height = 20;

    public int octaves = 8;
    public float persistence = 0.5f;
    public float heightMultiplier = 8.0f;
    public float lacunarity = 2.0f;
    public float scale = 20.0f;

    public float xOffset = 100f;
    public float zOffset = 100f;
    
    // Start is called before the first frame update
    void Start()
    {
        xOffset = Random.Range(0.0f, 9999.0f);
        zOffset = Random.Range(0.0f, 9999.0f);

        mesh = new Mesh();
        Mesh.GetComponent<MeshFilter>().mesh = mesh;

        CreateMesh();
        UpdateMesh();
    }

    private void Update()
    {
        if (isInfinite)
        {
            CreateMesh();
            UpdateMesh();

            xOffset += Time.deltaTime / 2.0f;
            //zOffset += Time.deltaTime / 2.0f;
        }
    }

    private void CreateMesh()
    {
        vertices = new Vector3[(width + 1) * (height + 1)];

        int index = 0;
        for(int z = 0; z <=height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float y = PerlinNoiseData(x, z);

                vertices[index] = new Vector3(x, y * heightMultiplier, z);
                               
                index++;
            }
        }

        indices = new int[width * height * 6];

        int vert = 0;
        int tris = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                indices[tris + 0] = vert;
                indices[tris + 1] = vert + width + 1;
                indices[tris + 2] = vert + 1;
                indices[tris + 3] = vert + 1;
                indices[tris + 4] = vert + width + 1;
                indices[tris + 5] = vert + width + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    private float PerlinNoiseData(int x, int z)
    {
        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;
        float total = 0;

        float maxNoiseHeight = float.MaxValue;
        float minNoiseHeight = float.MinValue;

        for (int i = 0; i < octaves; i++)
        {
            float xCoord = ((float)x / scale * frequency) + xOffset;
            float zCoord = ((float)z / scale * frequency) + zOffset;
            float sample = Mathf.PerlinNoise(xCoord, zCoord) * 2 - 1; // * 2 - 1 allows for negative values
            noiseHeight += sample * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        if (noiseHeight > maxNoiseHeight)
            maxNoiseHeight = noiseHeight;
        else if (noiseHeight < minNoiseHeight)
            minNoiseHeight = noiseHeight;

        total = noiseHeight;
        return total;
    }
           
    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = indices;

        mesh.RecalculateNormals();
    }
}
