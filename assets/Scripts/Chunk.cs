using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System.Threading;

public class Chunk
{
    public ChunkCoord coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();

    public Vector3 position;

    public byte[,,] voxelMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    World world;

    private bool _isActive;
    private bool isVoxelMapPopulated = false;
    private bool threadLocked = false;

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

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth,
                                                    0.0f,
                                                    coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk: " + coord.x + "-" + coord.z;

        position = chunkObject.transform.position;

        var thread = new Thread(new ThreadStart(PopulateVoxelMap));
        thread.Start();
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

    public bool isEditable
    {
        get
        {
            if (!isVoxelMapPopulated || threadLocked) return false;
            return true;
        }
    }

    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;
        return true;
    }

    public void EditVoxel(Vector3 pos, byte newID)
    {
        var x = (int)(pos.x - chunkObject.transform.position.x);
        var y = (int)pos.y;
        var z = (int)(pos.z - chunkObject.transform.position.z);

        voxelMap[x, y, z] = newID;

        UpdateSurroundingVoxels(x, y, z);

        UpdateChunk();
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        var thisVoxel = new Vector3(x, y, z);

        for (var p = 0; p < 6; p++)
        {
            var currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                world.GetChunkFromVector3(currentVoxel + position).UpdateChunk();
            }
        }
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
        _updateChunk();
        isVoxelMapPopulated = true;
    }

    public void UpdateChunk()
    {
        var thread = new Thread(new ThreadStart(_updateChunk));
        thread.Start();
    }

    private void _updateChunk()
    {
        threadLocked = true;

        lock (modifications)
        {
            while (modifications.Count > 0)
            {
                var v = modifications.Dequeue();
                var pos = v.position -= position;
                voxelMap[(int)pos.x, (int)pos.y, (int)pos.z] = v.id;
            }
        }

        ClearMeshData();

        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (!world.blocktypes[voxelMap[x, y, z]].isSolid) continue;
                    UpdateMeshData(new Vector3(x, y, z));
                }
            }
        }

        lock (world.chunksToDraw)
        {
            world.chunksToDraw.Enqueue(this);
        }

        threadLocked = false;
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
            return world.CheckIfVoxelTransparent(pos + position);
        }

        return world.blocktypes[voxelMap[x, y, z]].isTransparent;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos)
    {
        var x = (int)(pos.x - position.x);
        var y = (int)pos.y;
        var z = (int)(pos.z - position.z);
        return voxelMap[x, y, z];
    }

    void UpdateMeshData(Vector3 pos)
    {
        byte blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];
        bool isTransparent = world.blocktypes[blockID].isTransparent;

        for (int p = 0; p < 6; p++)
        {
            if (!CheckVoxel(pos + VoxelData.faceChecks[p])) continue;

            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

            AddTexture(world.blocktypes[blockID].GetTextureID(p));

            if (!isTransparent)
            {
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);
            }
            else
            {
                transparentTriangles.Add(vertexIndex);
                transparentTriangles.Add(vertexIndex + 1);
                transparentTriangles.Add(vertexIndex + 2);
                transparentTriangles.Add(vertexIndex + 2);
                transparentTriangles.Add(vertexIndex + 1);
                transparentTriangles.Add(vertexIndex + 3);
            }

            
            vertexIndex += 4;
        }
    }

    public void CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
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