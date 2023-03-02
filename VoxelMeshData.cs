using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

[CreateAssetMenu(fileName="New Voxel Mesh Data", menuName="SettlersOfKorpi/Voxel Mesh Data")]
public class VoxelMeshData : ScriptableObject
{
    public string blockName;
    public bool dontRenderSameNeigbour;
    public FaceMeshData[] faces;
}

[System.Serializable]
public struct VertData
{
    public float3 pos;
    public float2 uv;

    public VertData(float3 _pos, float2 _uv)
    {
        pos = _pos;
        uv = _uv;
    }

    public float3 GetRotatedPosition (float3 angles)
    {
        float3 center = new(0.5f, 0.5f, 0.5f);
        float3 direction = pos - center;
        direction = Quaternion.Euler(angles) * direction;
        return direction + center;
    }
}

[System.Serializable]
public class FaceMeshData
{
    [SerializeField] private string direction;
    public VertData[] vertData;
    public int[] triangles;

    public VertData GetVertData(int index)
    {
        return vertData[index];
    }

}