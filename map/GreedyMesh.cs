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
                var w = 0;
                var h = 0;

                var u = (direction + 1) % 3;
                var v = (direction + 2) % 3;

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
                            var x1 = x[0];
                            var y1 = x[1];
                            var z1 = x[2];

                            var x2 = x[0] + q[0];
                            var y2 = x[1] + q[1];
                            var z2 = x[2] + q[2];
                            var vox1 = (int)chunk.GetBlockAt(x[0], x[1], x[2], game);
                            var vox2 = (int)chunk.GetBlockAt(x[0] + q[0], x[1] + q[1], x[2] + q[2], game);

                            if ((vox1 == -1 || vox2 == -1) || (vox1 > 0 && vox2 > 0))
                            {
                                mask[n] = 0;
                                continue;
                            }

                            var a = 0 <= x[direction] ? vox1 : 0;
                            var b = x[direction] < dimensions[direction] - 1 ? vox2 : 0;

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
                            var done = false;
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
                                if (done) break;
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
                                dv[u] = w;
                                du[v] = h;
                            }

                            var v1 = new Vector3(x[0], x[1], x[2]);
                            var v2 = new Vector3(x[0] + du[0], x[1] + du[1], x[2] + du[2]);
                            var v3 = new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2]);
                            var v4 = new Vector3(x[0] + dv[0], x[1] + dv[1], x[2] + dv[2]);

                            var block = game.BlockTypes[(BlockType)Math.Abs(block_type)];

                            // TODO: find a FASTER way to get the direction
                            var d_u = v2 - v1;
                            var d_v = v3 - v1;
                            var normal = Vector3.Cross(d_u, d_v);

                            if (normal.X > 0)
                                chunk.Generator.AddQuad(v4, v1, v2, v3, d_u.Y, d_v.Z, Direction.EAST, block.GetMaterialID(Direction.EAST));
                            else if (normal.X < 0)
                                chunk.Generator.AddQuad(v1, v2, v3, v4, d_v.Y, d_u.Z, Direction.WEST, block.GetMaterialID(Direction.WEST));
                            else if (normal.Y > 0)
                                chunk.Generator.AddQuad(v4, v1, v2, v3, d_u.Z, d_v.X, Direction.UP, block.GetMaterialID(Direction.UP));
                            else if (normal.Y < 0)
                                chunk.Generator.AddQuad(v1, v2, v3, v4, d_u.X, d_v.Z, Direction.DOWN, block.GetMaterialID(Direction.DOWN));
                            else if (normal.Z > 0)
                                chunk.Generator.AddQuad(v1, v2, v3, v4, d_v.Y, d_u.X, Direction.SOUTH, block.GetMaterialID(Direction.SOUTH));
                            else if (normal.Z < 0)
                                chunk.Generator.AddQuad(v4, v1, v2, v3, d_u.Y, d_v.X, Direction.NORTH, block.GetMaterialID(Direction.NORTH));

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
