﻿using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    public byte[,,] voxelMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    World world;

    private bool _isActive;
    public bool isVoxelMapPopulated = false;

    // Start is called before the first frame update
    public Chunk(ChunkCoord _coord, World _world, bool generateOnLoad)
    {
        coord = _coord;
        world = _world;
        IsActive = true;

        if (generateOnLoad) Init();
    }

    public void Init()
    {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = world.material;
        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth,
                                                    0.0f,
                                                    coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk: " + coord.x + "-" + coord.z;

        PopulateVoxelMap();
        CreateMeshData();
        CreateMesh();
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            if (chunkObject != null) chunkObject.SetActive(value);
        }
    }

    public Vector3 position => chunkObject.transform.position;

    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;
        return true;
    }

    void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + position);
                }
            }
        }
        isVoxelMapPopulated = true;
    }

    void CreateMeshData()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (!world.blocktypes[voxelMap[x, y, z]].isSolid) continue;
                    AddVoxelDataToChunk(new Vector3(x, y, z));
                }
            }
        }
    }

    public byte GetVoxelFromMap(Vector3 pos)
    {
        pos -= position;
        return voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];
    }

    bool CheckVoxel(Vector3 pos)
    {
        int x = (int)pos.x;
        int y = (int)pos.y;
        int z = (int)pos.z;

        if (!IsVoxelInChunk(x, y, z))
        {
            return world.CheckForVoxel(pos + position);
        }

        return world.blocktypes[voxelMap[x, y, z]].isSolid;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos)
    {
        var x = (int)(pos.x - chunkObject.transform.position.x);
        var y = (int)pos.y;
        var z = (int)(pos.z - chunkObject.transform.position.z);
        return voxelMap[x, y, z];
    }

    void AddVoxelDataToChunk(Vector3 pos)
    {
        for (int p = 0; p < 6; p++)
        {
            if (CheckVoxel(pos + VoxelData.faceChecks[p])) continue;

            byte blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];

            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

            AddTexture(world.blocktypes[blockID].GetTextureID(p));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 3);
            vertexIndex += 4;
        }
    }

    void CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }
}

public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public ChunkCoord(Vector3 pos)
    {
        int xCheck = (int)pos.x;
        int zCheck = (int)pos.z;

        x = xCheck / VoxelData.ChunkWidth;
        z = zCheck / VoxelData.ChunkWidth;
    }

    public bool Equals(ChunkCoord other)
    {
        return (other != null && other.x == x && other.z == z);
    }
}