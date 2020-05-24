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
    List<Color> colors = new List<Color>();

    public Vector3 position;

    public VoxelState[,,] voxelMap = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

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

        //materials[0] = world.material;
        //materials[1] = world.transparentMaterial;
        meshRenderer.material = world.material;

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

        voxelMap[x, y, z].id = newID;

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
                    voxelMap[x, y, z] = new VoxelState(world.GetVoxel(new Vector3(x, y, z) + position));
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
                if (v == null) continue;
                var pos = v.position -= position;
                voxelMap[(int)pos.x, (int)pos.y, (int)pos.z] = new VoxelState(v.id);
            }
        }

        ClearMeshData();

        lock (world.chunksToDraw)
        {
            CalculateLight();
        }

        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (!world.blocktypes[voxelMap[x, y, z].id].isSolid) continue;
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

    void CalculateLight()
    {
        var litVoxels = new Queue<Vector3Int>();

        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int z = 0; z < VoxelData.ChunkWidth; z++)
            {
                var lightRay = 1f;

                for (int y = VoxelData.ChunkHeight - 1; y >= 0; y--)
                {
                    var thisVoxel = voxelMap[x, y, z];
                    if (thisVoxel.id > 0 && world.blocktypes[thisVoxel.id].transparency < lightRay) lightRay = world.blocktypes[thisVoxel.id].transparency;

                    thisVoxel.globalLightPercent = lightRay;
                    voxelMap[x, y, z] = thisVoxel;

                    if (lightRay > VoxelData.lightFalloff)
                    {
                        litVoxels.Enqueue(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        while (litVoxels.Count > 0)
        {
            var v = litVoxels.Dequeue();

            for (var p = 0; p < 6; p++)
            {
                var currentVoxel = v + VoxelData.faceChecks[p];
                var neighbor = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                if (IsVoxelInChunk(neighbor.x, neighbor.y, neighbor.z))
                {
                    if (voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff)
                    {
                        voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;

                        if (voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff)
                        {
                            litVoxels.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
    }

    public byte GetVoxelFromMap(Vector3 pos)
    {
        pos -= position;
        return voxelMap[(int)pos.x, (int)pos.y, (int)pos.z].id;
    }

    VoxelState CheckVoxel(Vector3 pos)
    {
        int x = (int)pos.x;
        int y = (int)pos.y;
        int z = (int)pos.z;

        if (!IsVoxelInChunk(x, y, z))
        {
            return world.GetVoxelState(pos + position);
        }

        return voxelMap[x, y, z];
    }

    public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
    {
        var x = (int)(pos.x - position.x);
        var y = (int)pos.y;
        var z = (int)(pos.z - position.z);
        return voxelMap[x, y, z];
    }

    void UpdateMeshData(Vector3 pos)
    {
        var x = (int)(pos.x);
        var y = (int)pos.y;
        var z = (int)(pos.z);

        byte blockID = voxelMap[x, y, z].id;
        //bool isTransparent = world.blocktypes[blockID].renderNeighborFaces;

        for (int p = 0; p < 6; p++)
        {
            var neighbor = CheckVoxel(pos + VoxelData.faceChecks[p]);

            if (neighbor == null || !world.blocktypes[neighbor.id].renderNeighborFaces) continue;

            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
            vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

            AddTexture(world.blocktypes[blockID].GetTextureID(p));

            var lightLevel = neighbor.globalLightPercent;

            colors.Add(new Color(0, 0, 0, lightLevel));
            colors.Add(new Color(0, 0, 0, lightLevel));
            colors.Add(new Color(0, 0, 0, lightLevel));
            colors.Add(new Color(0, 0, 0, lightLevel));

            //if (!isTransparent)
            //{
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 3);
            //}
            //else
            //{
            //    transparentTriangles.Add(vertexIndex);
            //    transparentTriangles.Add(vertexIndex + 1);
            //    transparentTriangles.Add(vertexIndex + 2);
            //    transparentTriangles.Add(vertexIndex + 2);
            //    transparentTriangles.Add(vertexIndex + 1);
            //    transparentTriangles.Add(vertexIndex + 3);
            //}

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
        //mesh.subMeshCount = 2;
        //mesh.SetTriangles(triangles.ToArray(), 0);
        //mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
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
        colors.Clear();
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

public class VoxelState
{
    public byte id;
    public float globalLightPercent;

    public VoxelState(byte _id)
    {
        id = _id;
    }
}