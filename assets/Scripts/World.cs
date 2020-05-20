using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public int seed;
    public BiomeAttributes biome;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;

    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    Queue<ChunkCoord> chunksToCreate = new Queue<ChunkCoord>();
    List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    bool applyingModifications = false;

    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool _inUI = false;

    public GameObject debugScreen;

    private void Start()
    {
        Application.targetFrameRate = 60;
        debugScreen.SetActive(false);
        Random.InitState(seed);

        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
            playerLastChunkCoord = playerChunkCoord;
        }

        if (!applyingModifications) ApplyModifications();

        if (chunksToCreate.Count > 0) CreateChunk();

        if (chunksToUpdate.Count > 0) UpdateChunks();

        if (chunksToDraw.Count > 0)
        {
            lock(chunksToDraw)
            {
                if (chunksToDraw.Peek().isEditable)
                {
                    chunksToDraw.Dequeue().CreateMesh();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            debugScreen.SetActive(!debugScreen.activeSelf);
        }
    }

    private void GenerateWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++)
            {
                var coord = new ChunkCoord(x, z);
                chunks[x, z] = new Chunk(coord, this, true);
                activeChunks.Add(coord);
            }
        }

        lock (modifications)
        {
            while (modifications.Count > 0)
            {
                var queue = modifications.Dequeue();

                while (queue != null && queue.Count > 0)
                {
                    var v = queue.Dequeue();
                    var coord = GetChunkCoordFromVector3(v.position);
                    if (chunks[coord.x, coord.z] == null)
                    {
                        chunks[coord.x, coord.z] = new Chunk(coord, this, true);
                        activeChunks.Add(coord);
                    }

                    chunks[coord.x, coord.z].modifications.Enqueue(v);

                    if (!chunksToUpdate.Contains(chunks[coord.x, coord.z])) chunksToUpdate.Add(chunks[coord.x, coord.z]);
                }
            }
        }

        for (var i = chunksToUpdate.Count - 1; i >= 0; i--)
        {
            chunksToUpdate[i].UpdateChunk();
            chunksToUpdate.RemoveAt(i);
        }

        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks / 2f) * VoxelData.ChunkWidth, VoxelData.ChunkHeight - 50f, (VoxelData.WorldSizeInChunks / 2f) * VoxelData.ChunkWidth);
        player.position = spawnPosition;
    }

    void CreateChunk()
    {
        var c = chunksToCreate.Dequeue();
        activeChunks.Add(c);
        chunks[c.x, c.z].Init();
    }

    void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        while (!updated && index < chunksToUpdate.Count - 1)
        {
            if (chunksToUpdate[index].isEditable)
            {
                chunksToUpdate[index].UpdateChunk();
                chunksToUpdate.RemoveAt(index);
                updated = true;
            }
            else index++;
        }
    }

    void ApplyModifications()
    {
        applyingModifications = true;

        lock (modifications)
        {
            while (modifications.Count > 0)
            {
                var queue = modifications.Dequeue();

                while (queue != null && queue.Count > 0)
                {
                    var v = queue.Dequeue();
                    var coord = GetChunkCoordFromVector3(v.position);

                    if (chunks[coord.x, coord.z] == null)
                    {
                        chunks[coord.x, coord.z] = new Chunk(coord, this, true);
                        activeChunks.Add(coord);
                    }

                    chunks[coord.x, coord.z].modifications.Enqueue(v);

                    if (!chunksToUpdate.Contains(chunks[coord.x, coord.z])) chunksToUpdate.Add(chunks[coord.x, coord.z]);
                }
            }
        }

        applyingModifications = false;
    }

    private ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        var x = (int)pos.x / VoxelData.ChunkWidth;
        var z = (int)pos.z / VoxelData.ChunkWidth;
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        var x = (int)pos.x / VoxelData.ChunkWidth;
        var z = (int)pos.z / VoxelData.ChunkWidth;
        return chunks[x, z];
    }

    private void CheckViewDistance()
    {
        var coord = GetChunkCoordFromVector3(player.position);

        var previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = coord.x - VoxelData.ViewDistanceInChunks; x < coord.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++)
            {
                var current = new ChunkCoord(x, z);
                if (!IsChunkInWorld(current)) continue;

                if (chunks[x, z] == null)
                {
                    chunks[x, z] = new Chunk(current, this, false);
                    chunksToCreate.Enqueue(current);
                }
                else if (!chunks[x, z].IsActive)
                {
                    chunks[x, z].IsActive = true;
                }
                activeChunks.Add(current);

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(current))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (var c in previouslyActiveChunks)
        {
            chunks[c.x, c.z].IsActive = false;
        }
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        var thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight) return false;
        //if (IsVoxelInWorld(pos)) return false; // TODO: fix this

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
        {
            return blocktypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blocktypes[GetVoxel(pos)].isSolid;
    }

    public bool CheckIfVoxelTransparent(Vector3 pos)
    {
        var thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight) return false;
        //if (IsVoxelInWorld(pos)) return false; // TODO: fix this

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
        {
            return blocktypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;
        }

        return blocktypes[GetVoxel(pos)].isTransparent;
    }

    public bool inUI
    {
        get => _inUI;
        set
        {
            _inUI = value;

        }
    }

    public byte GetVoxel(Vector3 pos)
    {
        int yPos = (int)pos.y;

        // IMMUTABLE PASS
        if (!IsVoxelInWorld(pos) || yPos == 0) return 1; // BEDROCK

        // BASIC TERRAIN PASS
        int terrainHeight = (int)((biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.solidGroundHeight);

        byte voxelValue;

        if (yPos == terrainHeight) voxelValue = 3; // GRASS
        else if (yPos < terrainHeight && yPos > terrainHeight - 4) voxelValue = 5; // DIRT
        else if (yPos > terrainHeight) return 0; // AIR
        else voxelValue = 2; // STONE

        // SECOND PASS
        if (voxelValue == 2) // STONE
        {
            foreach (var lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                {
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold)) voxelValue = lode.blockID;
                }
            }
        }

        // TREE PASS
        if (yPos == terrainHeight)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treeZoneScale) > biome.treeZoneThreshold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treePlacementScale) > biome.treePlacementThreshold)
                {
                    lock (modifications)
                    {
                        modifications.Enqueue(Structure.MakeTree(pos, biome.minTreeHeight, biome.maxTreeHeight));
                    }
                }
            }
        }

        return voxelValue;
    }

    bool IsChunkInWorld(ChunkCoord coord)
    {
        return (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1);
    }

    bool IsVoxelInWorld(Vector3 pos)
    {
        return (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels);
    }

}

[System.Serializable]
public class BlockType
{
    public string blockName = "Default";
    public bool isSolid = true;
    public bool isTransparent = false;
    public Sprite icon;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right

    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return -1;
        }
    }
}

public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}