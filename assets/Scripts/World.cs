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
    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    Queue<ChunkCoord> chunksToCreate = new Queue<ChunkCoord>();
    private bool isCreatingChunks;

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

        if (chunksToCreate.Count > 0 && !isCreatingChunks)
        {
            StartCoroutine("CreateChunks");
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

        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks / 2f) * VoxelData.ChunkWidth, VoxelData.ChunkHeight - 50f, (VoxelData.WorldSizeInChunks / 2f) * VoxelData.ChunkWidth);
        player.position = spawnPosition;
    }

    private IEnumerator CreateChunks()
    {
        isCreatingChunks = true;
        while (chunksToCreate.Count > 0)
        {
            var coord = chunksToCreate.Dequeue();
            chunks[coord.x, coord.z].Init();
            yield return null;
        }
        isCreatingChunks = false;
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

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
        {
            return blocktypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blocktypes[GetVoxel(pos)].isSolid;
    }

    public byte GetVoxel(Vector3 pos)
    {
        int yPos = (int)pos.y;

        // IMMUTABLE PASS
        if (!IsVoxelInWorld(pos) || yPos == 0) return 1; // BEDROCK 

        // BASIC TERRAIN PASS
        int terrainHeight = (int)((biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.solidGroundHeight);

        byte voxelValue;

        if (yPos == terrainHeight) voxelValue = 3;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4) voxelValue = 5;
        else if (yPos > terrainHeight) return 0;
        else voxelValue = 2;

        // SECOND PASS
        if (voxelValue != 2) return voxelValue;
        
        foreach(var lode in biome.lodes)
        {
            if (yPos > lode.minHeight && yPos < lode.maxHeight)
            {
                if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold)) voxelValue = lode.blockID;
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
