using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public static class StructureGeneration
{
    public static StructureData Tree (int3 position, Tree tree)
    {

        int height = (int)(tree.maxHeight * Noise.Get2DPerlin(new int2(position.x, position.z), 250, 3f));

        if(height < tree.minHeight)
        {
            height = tree.minHeight;
        }

        int width = (int)(tree.maxWidth * Noise.Get2DPerlin(new int2(position.x, position.z), 250, 3f));

        if (width < tree.minWidth)
        {
            width = tree.minWidth;
        }


        int xr = RandomFromPerlin(position.x, position.z, tree.minFWidth, tree.maxFWidth, 500, 1f);
        int zr = RandomFromPerlin(position.x, position.z, tree.minFWidth, tree.maxFWidth, 250, 1f);
        int yr = RandomFromPerlin(position.x, position.z, tree.minFHeight, tree.maxFHeight, 750, 1f);

        int3 pos = new int3(position.x - xr, position.y, position.z-zr);
        int3 size = new int3(xr * 2 + width, height + 8, zr * 2 + width);
        NativeParallelHashMap<int3, VoxelState> map = new(size.x*size.y*size.z, Allocator.Persistent);

        //trunk
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < width; z++)
            {
                if (width <= 3 || !Corner(x, z, width, width))
                {
                    byte id = tree.w4;
                    byte orientation = 1;

                    if(width==1)
                    {
                        //Keep default
                    }
                    else if (width>3)
                    {
                        if (Corner(x-1, z, width-1, width) || Corner(x, z - 1, width, width - 1))
                        {
                            id = tree.w2;
                            orientation = CornerOrientation(x, z, width / 2);
                        }
                        else if(Side(x,z, width))
                        {
                            id = tree.w1;
                            orientation = SideOrientation(x, z, width);
                        }
                        else
                        {
                            id = tree.w0;
                        }
                    }
                    else if(Corner(x, z, width, width))
                    {
                        id = tree.w2;
                        orientation = CornerOrientation(x, z, width / 2);
                    }
                    else if(Side(x, z, width))
                    {
                        id = tree.w1;
                        orientation = SideOrientation(x, z, width);
                    }
                    else
                    {
                        id = tree.w0;
                    }
                    

                    for (int y = 0; y < height; y++)
                    {
                        map.Add(new int3(x+xr, y, z+zr),new VoxelState{created=true,id=id, orientation=orientation });
                    }
                }
            }
        }

        Debug.Log(GradientTreshold(0, xr, xr-1, tree.minTreshold)+"|"+ GradientTreshold(xr, xr, xr-1, tree.minTreshold));

        //leaves
        for (int x = 0; x < xr*2+width; x++)
        {
            float xt = GradientTreshold(x, xr, xr-4, tree.minTreshold);
            for (int y = height-yr*2+4; y < height+4; y++)
            {
                float yt= GradientTreshold(y, yr, yr-4, tree.minTreshold);
                for (int z = 0; z < zr*2 + width; z++)
                {
                    float zt = GradientTreshold(z, zr, zr-4, tree.minTreshold);

                    float treshold = xt;
                    if(yt>treshold)
                    {
                        treshold = yt;
                    }
                    if (zt>treshold)
                    {
                        treshold = zt;
                    }


                    bool perlin = Noise.Get3DPerlin(new int3(x, y, z), position.x+position.y+position.z, tree.scale, treshold);

                    if (perlin&&!(z < zr+width+1 && z >= zr-1 && x < xr+width+1 && x >= xr-1&&y<height))
                    {
                        map.Add(new int3(x, y, z), new VoxelState {created=true,id =tree.foliage, orientation=1});
                    }
                }
            }
        }

        StructureData data = new(pos, size, map);

        return data;
    }

    public static bool Corner(int x, int z, int xWidth, int zWidth)
    {
        return (z==zWidth-1||z==0)&&(x==0||x==xWidth-1);
    }

    public static byte CornerOrientation(int x, int z, int center)
    {
        if (z < center && x < center)
        {
            return 0;
        }
        else if(z >= center && x < center)
        {
            return 5;
        }
        else if(z >= center && x >= center)
        {
            return 1;
        }
        else
        {
            return 4;
        }
    }

    public static byte SideOrientation(int x, int z, int width)
    {
        if (x==-width)
        {
            return 4;
        }
        else if (z==width)
        {
            return 0;
        }
        else if (z==width)
        {
            return 5;
        }
        else
        {
            return 1;
        }
    }

    public static bool Side(int x, int z, int width)
    {
        return z == 0 || x == 0 || z == width || x == width;
    }

    static int RandomFromPerlin(int x, int y, int min, int max, int offset, float scale)
    {
        int r = (int)(max * Noise.Get2DPerlin(new int2(x,y), offset, scale));

        if (r < min)
        {
            r = min;
        }

        return r;
    }

    static float GradientTreshold(int value, int maxValue, int startGradient, float minTreshold)
    {
        float treshold = minTreshold + Mathf.Abs(value - maxValue)/(maxValue*10);
        return treshold; //< minTreshold ? minTreshold : treshold;
    }
}
