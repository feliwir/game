using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    private World world;
    private Text text;

    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
    }

    void Update()
    {
        var x = (int)world.player.transform.position.x - halfWorldSizeInVoxels;
        var y = (int)world.player.transform.position.y;
        var z = (int)world.player.transform.position.z - halfWorldSizeInVoxels;

        string debugText = "Viking";
        debugText += "\n";
        debugText += frameRate + " FPS";
        debugText += "\n\n";
        debugText += "XYZ: " + x + " | " + y + " | " + z;
        debugText += "\n";
        debugText += "Chunk: " + (world.playerChunkCoord.x - halfWorldSizeInChunks) + " | " + (world.playerChunkCoord.z - halfWorldSizeInChunks);
        text.text = debugText;
        
        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
        {
            timer += Time.deltaTime;
        }
    }
}
