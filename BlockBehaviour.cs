using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockBehaviour
{
    public static bool Active(VoxelState voxel)
    {
        switch (voxel.id)
        {
            case 2:
                /*if((voxel.neighbours[0] != null && voxel.neighbours[0].id!=5) ||
                  (voxel.neighbours[1] != null && voxel.neighbours[0].id != 5) ||
                  (voxel.neighbours[4] != null && voxel.neighbours[0].id != 5) ||
                  (voxel.neighbours[5] != null && voxel.neighbours[0].id != 5))
                {
                    return true;
                }*/
                break;
        }

        return false;
    }

    public static void Behave(VoxelState voxel)
    {
        switch (voxel.id)
        {
            case 2:
                /*if(voxel.neighbours[2]!=null && voxel.neighbours[2].id != 0)
                {
                    voxel.chunkData.chunk.RemoveActiveVoxel(voxel);
                    voxel.chunkData.ModifyVoxel(voxel.position, 3, 0, true);
                    return;
                }

                List<VoxelState> neigbours = new List<VoxelState>();
                if (voxel.neighbours[0] != null && voxel.neighbours[0].id == 3) neigbours.Add(voxel.neighbours[0]);
                if (voxel.neighbours[0] != null && voxel.neighbours[0].id == 3) neigbours.Add(voxel.neighbours[1]);
                if (voxel.neighbours[0] != null && voxel.neighbours[0].id == 3) neigbours.Add(voxel.neighbours[4]);
                if (voxel.neighbours[0] != null && voxel.neighbours[0].id == 3) neigbours.Add(voxel.neighbours[5]);
                if (neigbours.Count == 0) return;

                int index = Random.Range(0, neigbours.Count);
                neigbours[index].chunkData.ModifyVoxel(neigbours[index].position, 3, 0, true);*/

                break;
        }
    }
}
