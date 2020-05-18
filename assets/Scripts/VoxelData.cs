using UnityEngine;

public static class VoxelData
{
    public static int ChunkWidth = 16;
    public static int ChunkHeight = 128;
    public static int WorldSizeInChunks = 100;

    public static int WorldSizeInVoxels => WorldSizeInChunks * ChunkWidth;

    public static readonly int ViewDistanceInChunks = 5;

    public static readonly int TextureAtlasSizeInBlocks = 4;
    public static float NormalizedBlockTextureSize => 1f / (float)TextureAtlasSizeInBlocks;

    public static readonly Vector3[] voxelVerts = new Vector3[8]
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };

    public static readonly Vector3[] faceChecks = new Vector3[6]
    {
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f)
    };

    public static readonly int[,] voxelTris = new int[6, 4]
    {
        { 0, 3, 1, 2 }, // BACK FACE
        { 5, 6, 4, 7 }, // FRONT FACE
        { 3, 7, 2, 6 }, // TOP FACE
        { 1, 5, 0, 4 }, // BOTTOM FACE
        { 4, 7, 0, 3 }, // LEFT FACE
        { 1, 2, 5, 6 }  // RIGHT FACE
    };

    public static readonly Vector2[] voxelUVs = new Vector2[4]
    {
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f)
    };
}
