using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static void MakeTree(Vector3 position, Queue<VoxelMod> queue, int minTrunkHeight, int maxTrunkHeight)
    {
        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 250f, 3f));

        if (height < minTrunkHeight) height = minTrunkHeight;

        for (var i = 1; i < height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 6));
        }

        for (var x = -3; x < 4; x++)
        {
            for(var y = height; y < height + 7; y++)
            {
                for(var z = -3; z < 4; z++)
                {
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + y, position.z + z), 11));
                }
            }
        }
        
    }
}
