using System;
using System.Numerics;
using Viking.Blocks;

namespace Viking.Map
{
    public static class GreedyMesh
    {
        public static void ReduceMesh(Chunk chunk, Game game)
        {
            int[] dimensions = { Chunk.WIDTH, Chunk.HEIGHT, Chunk.WIDTH };

            //Sweep over 3-axes
            for (var direction = 0; direction < 3; direction++)
            {
                int w = 0;
                int h = 0;

                int u = (direction + 1) % 3;
                int v = (direction + 2) % 3;

                int[] x = { 0, 0, 0 };
                int[] q = { 0, 0, 0 };
                int[] mask = new int[(dimensions[u] + 1) * (dimensions[v] + 1)];


                q[direction] = 1;

                for (x[direction] = -1; x[direction] < dimensions[direction];)
                {
                    // Compute the mask
                    int n = 0;
                    for (x[v] = 0; x[v] < dimensions[v]; ++x[v])
                    {
                        for (x[u] = 0; x[u] < dimensions[u]; ++x[u], ++n)
                        {
                            int vox1 = (int)chunk.GetBlockAt(x[0], x[1], x[2], game);
                            int vox2 = (int)chunk.GetBlockAt(x[0] + q[0], x[1] + q[1], x[2] + q[2], game);

                            int a = 0 <= x[direction] ? vox1 : 0;
                            int b = x[direction] < dimensions[direction] - 1 ? vox2 : 0;

                            if ((a != 0) == (b != 0)) mask[n] = 0;
                            else if (a != 0) mask[n] = a;
                            else mask[n] = -b;
                        }
                    }

                    ++x[direction];

                    // Generate mesh for mask using lexicographic ordering
                    n = 0;
                    for (var j = 0; j < dimensions[v]; ++j)
                    {
                        for (var i = 0; i < dimensions[u];)
                        {
                            var block_type = mask[n];

                            if (block_type == 0)
                            {
                                ++i;
                                ++n;
                                continue;
                            }

                            // compute width
                            for (w = 1; mask[n + w] == block_type && (i + w) < dimensions[u]; ++w) { }

                            // compute height
                            bool done = false;
                            for (h = 1; (j + h) < dimensions[v]; ++h)
                            {
                                for (var k = 0; k < w; ++k)
                                {
                                    if (mask[n + k + h * dimensions[u]] != block_type)
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                if (done)
                                {
                                    break;
                                }
                            }

                            // add quad
                            x[u] = i;
                            x[v] = j;

                            int[] du = { 0, 0, 0 };
                            int[] dv = { 0, 0, 0 };

                            if (block_type > 0)
                            {
                                dv[v] = h;
                                du[u] = w;
                            }
                            else
                            {
                                du[v] = h;
                                dv[u] = w;
                            }

                            Vector3 v1 = new Vector3(x[0], x[1], x[2]);
                            Vector3 v2 = new Vector3(x[0] + du[0], x[1] + du[1], x[2] + du[2]);
                            Vector3 v3 = new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2]);
                            Vector3 v4 = new Vector3(x[0] + dv[0], x[1] + dv[1], x[2] + dv[2]);

                            var d_u = 0;
                            var d_v = 0;
                            for (var r = 0; r < 3; r++)
                            {
                                if (du[r] > d_u) d_u = du[r];
                                if (dv[r] > d_v) d_v = dv[r];
                            }

                            var dir = direction;
                            if (block_type < 0)
                            {
                                dir = Math.Abs(block_type) + 3;
                            }

                            var block = game.BlockTypes[(BlockType)Math.Abs(block_type)];
                            chunk.Generator.AddQuad(v1, v2, v3, v4, d_u, d_v, block.GetMaterialID((Direction)dir));

                            for (var l = 0; l < h; ++l)
                            {
                                for (var k = 0; k < w; ++k)
                                {
                                    mask[n + k + l * dimensions[u]] = 0;
                                }
                            }

                            i += w;
                            n += w;
                        }
                    }
                }
            }
        }
    }
}
