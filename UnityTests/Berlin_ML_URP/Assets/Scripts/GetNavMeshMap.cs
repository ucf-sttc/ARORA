/*
 * Handles creating, reading, converting the accessibility map of the environment
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

public class GetNavMeshMap : MonoBehaviour
{
    NavMeshQueryFilter queryFilter;
    Bounds b, bMap;
    bool[,] map, newMap, zoomMap;
    float delta = 1f; // generating map using 1m grid size
    public int m_lengthX, m_lengthZ; // set when map is acquired either from CreateMap() or ImportMapPng()
    public int m_newX = 328, m_newZ = 266; // used for testing in Editor
    public int zoomX = 21, zoomZ = 20;
    public float m_threshold = 0.5f;
    public string areaName = "ShortestPath";
    public string nameMap = "Assets/Resources/map";
    public string nameSmallMap = "Assets/smallmap";
    public string nameZoomMap = "Assets/zoommap";
    float prevY = 0;

    void Awake()
    {
        Setup();
    }

    void Setup()
    {
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();
        b.SetMinMax(navMeshData.vertices[0], navMeshData.vertices[0]);
        foreach (Vector3 v in navMeshData.vertices)
            b.Encapsulate(v);

        Debug.Log(b.min + " " + b.max);

        Vector3 mapMin = new Vector3((float)Math.Ceiling(b.min.x) - 1, b.min.y, (float)Math.Ceiling(b.min.z) - 1);
        Vector3 mapMax = new Vector3((float)Math.Floor(b.max.x) + 1, b.max.y, (float)Math.Floor(b.max.z) + 1);

        bMap.SetMinMax(mapMin, mapMax);

        /* // TODO: deactivated now to avoid having to regenerate binary map, but will revisit for LARGE EXTENTS
        m_lengthX = (int)(bMap.max.x - bMap.min.x);
        m_lengthZ = (int)(bMap.max.z - bMap.min.z);

        CoordinateConversion.m_offset = new Vector3Int((int)bMap.min.x, 0, (int)bMap.min.z);
        */

        queryFilter = new NavMeshQueryFilter
        {
            agentTypeID = -1372625422, // vehicle navmesh
            areaMask = (1 << NavMesh.GetAreaFromName(areaName))
        };

        Debug.Log("Initialized navmesh data");
    }

#if UNITY_EDITOR
    [ContextMenu("EditorMode Init")]
    void EditInit()
    {
        Setup();
    }
#endif

    // create map from vehicle navmesh
    [ContextMenu("Generate Map")]
    void CreateMap()
    {
        DateTime startTime = DateTime.Now;
        float r = delta / 2; // center of cell

        Debug.Log("x,z: " + m_lengthX + "," + m_lengthZ);

        map = new bool[m_lengthX, m_lengthZ];

        for (int i = (int)bMap.min.x; i < (int)bMap.max.x; i++)
            for (int j = (int)bMap.min.z; j < bMap.max.z; j++)
                map[i - (int)bMap.min.x, j - (int)bMap.min.z] = CheckPoint(i, j, r);

        Debug.Log("Finished getting map " + (DateTime.Now - startTime).TotalMilliseconds);
    }

#if UNITY_EDITOR
    [ContextMenu("Get Zoom Map")]
    void GetZoomMap()
    {
        CreateZoomMap(zoomX, zoomZ);
    }
#endif

    // creates 100 x 100 navigable map at 1cm scale
    public byte[] CreateZoomMap(int x, int z)
    {
        //DateTime startTime = DateTime.Now;
        Vector2 cc = CoordinateConversion.ToUnity(new Vector2(x, z));

        if (cc.x >= m_lengthX || cc.y >= m_lengthZ)
        {
            Debug.LogError("Point for CreateZoomMap exceeds map bounds");
            return new byte[] { };
        }

        if (b == null)
        {
            Debug.LogError("Bounds not built");
            return null;
        }

        int lengthX = 100;
        int lengthZ = 100;
        float r = 0.5f / lengthX; // half the width of the cm cell

        zoomMap = new bool[lengthX, lengthZ];
        byte[] resultBytes = new byte[lengthX * lengthZ];

        for (int i = 0; i < lengthX; i++)
            for (int j = 0; j < lengthZ; j++)
            {
                zoomMap[i, j] = CheckPoint(cc.x + i / (float)lengthX, cc.y + j / (float)lengthZ, r);
                resultBytes[j * lengthX + i] = (byte)(zoomMap[i, j] ? 1 : 0); // store in 1D byte array
            }

        //Debug.Log("Finished getting zoomed map " + (DateTime.Now - startTime).TotalMilliseconds);                

        return BytesToBinary(resultBytes); // convert from each byte storing one pixel to each byte storing 8 pixels
    }

    // go up y-axis checking for navmesh
    bool CheckPoint(float x, float z, float offset)
    {
        NavMeshHit hit;
        // check at previous y position prevY initially, if no hit then check else where 
        if (NavMesh.SamplePosition(new Vector3(x + offset, prevY, z + offset), out hit, 0.5f, queryFilter))
        {
            prevY = hit.position.y;
            return true;
        }

        // the logic can be improved for the search to start at prevY and alternate +/-
        for (float i = b.min.y; i < b.max.y; i += 1f)
        {
            if (NavMesh.SamplePosition(new Vector3(x + offset, i, z + offset), out hit, 0.5f, queryFilter))
            {
                prevY = hit.position.y;
                return true;
            }
        }
        return false;
    }

    // return full binary navmap, no downsampling
    public byte[] GetMap()
    {
        byte[] resultBytes = new byte[m_lengthX * m_lengthZ];
        for (int x = 0; x < m_lengthX; x++)
            for (int z = 0; z < m_lengthZ; z++)
                resultBytes[z * m_lengthX + x] = (byte)(map[x, z] ? 1 : 0); // store in 1D byte array

        return BytesToBinary(resultBytes); // convert from each byte storing one pixel to each byte storing 8 pixels
    }

#if UNITY_EDITOR
    [ContextMenu("Downsample")]
    void GetDownsampledMap()
    {
        byte[] test = DownsampleMap(m_newX, m_newZ, m_threshold);
    }
#endif

    // create smaller map where new pixels are based on threshold of old pixels
    // returns 1D byte array in row-major order
    public byte[] DownsampleMap(int sizeX, int sizeZ, float threshold)
    {
        if (map == null) { Debug.LogError("No map created"); return null; }
        if (sizeX > m_lengthX || sizeZ > m_lengthZ) { Debug.LogError("Desired dimensions can't be larger than master"); return null; }

        byte[] resultBytes, resultBinary;
        float dx = 1.0f * m_lengthX / sizeX;
        float dz = 1.0f * m_lengthZ / sizeZ;

        newMap = new bool[sizeX, sizeZ];
        resultBytes = new byte[sizeX * sizeZ];

        for (int x = 0; x < sizeX; x++)
            for (int z = 0; z < sizeZ; z++)
            {
                newMap[x, z] = CalculatePixel(x, z, dx, dz, threshold); // store in 2D bool array
                resultBytes[z * sizeX + x] = (byte)(newMap[x, z] ? 1 : 0); // store in 1D byte array
                //resultBytes[(sizeZ - z - 1) * sizeX + x] = (byte)(newMap[x, z] ? 1 : 0); // store in 1D byte array with origin at top-left (for img format)
            }

        resultBinary = BytesToBinary(resultBytes); // convert from each byte storing one pixel to each byte storing 8 pixels

        return resultBinary;
    }

    // byte[0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1] -> byte[01010000, 10000100] (written without '0b' prefix for brevity)
    public byte[] BytesToBinary(byte[] input)
    {
        int numBytes = input.Length / 8;
        if (input.Length % 8 != 0)
            numBytes++;

        byte[] result = new byte[numBytes];

        for(int i = 0; i < input.Length; i++)
        {
            result[i / 8] = (byte)(result[i / 8] << 1);
            result[i / 8] |= input[i];
        }

        if (input.Length % 8 != 0)
            result[result.Length - 1] = (byte)(result[result.Length - 1] << (8 - input.Length % 8)); // shift last byte if not full

        /*
        // test print
        string str = "";
        foreach (byte it in input)
            str += it;
        Debug.Log(str);
        str = "";
        foreach (byte it in result)
            str += Convert.ToString(it, 2).PadLeft(8, '0');
        Debug.Log(str);
        */

        return result;
    }

#if UNITY_EDITOR
    // byte[01010000, 10000000] -> byte[0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0] (written without '0b' prefix for brevity)
    // only used for testing
    public byte[] BinaryToBytes(byte[] input, int x, int z)
    {
        byte[] result = new byte[x * z];

        for(int i = 0; i < result.Length; i++)
        {
            if ((input[i / 8] & (1 << 7 - (i % 8))) != 0)
                result[i] = 1;
            else
                result[i] = 0;
        }

        return result;
    }
#endif

    // calculate new larger pixels based on 'parent' pixels and value threshold
    bool CalculatePixel(int x, int z, float dx, float dz, float threshold)
    {
        float startX = dx * x;
        float endX = startX + dx;
        float startZ = dz * z;
        float endZ = startZ + dz;
        float startRemainX = (float)(Math.Ceiling(startX) - startX);
        float endRemainX = (float)(endX - Math.Floor(endX));
        float startRemainZ = (float)(Math.Ceiling(startZ) - startZ);
        float endRemainZ = (float)(endZ - Math.Floor(endZ));
        // calculate total area while subtracting corners
        float total = dx * dz - startRemainX * (startRemainZ + endRemainZ) - endRemainX * (startRemainZ + endRemainZ);
        //Debug.Log(startX + ", " + endX + "), ( " + startZ + ", " + endZ + " ) total: " + total);

        if (endX > m_lengthX && Math.Abs(endX - m_lengthX) < 0.01) endX = m_lengthX; // correct rounding errors when reaching bounds
        if (endZ > m_lengthZ && Math.Abs(endZ - m_lengthZ) < 0.01) endZ = m_lengthZ;

        float sum = 0;
        for (int i = (int)Math.Ceiling(startX); i < endX; i++)
        {
            for (int j = (int)Math.Ceiling(startZ); j < endZ; j++)
            {
                if (map[i, j])
                    sum += 1f;
            }
        }
        if (startX < Math.Ceiling(startX)) // get partial cells before leading element on X-axis
        {
            for (int j = (int)Math.Ceiling(startZ); j < endZ; j++)
                if (map[(int)Math.Floor(startX), j])
                    sum += startRemainX;
        }
        if (endX > Math.Floor(endX) && Math.Ceiling(endX) < m_lengthX) // get partial cells after ending element on X-axis
        {
            for (int j = (int)Math.Ceiling(startZ); j < endZ; j++)
                if (map[(int)Math.Ceiling(endX), j])
                    sum += endRemainX;
        }
        if (startZ < Math.Ceiling(startZ)) // get partial cells before leading element on Z-axis
        {
            for (int i = (int)Math.Ceiling(startX); i < endX; i++)
                if (map[i, (int)Math.Floor(startZ)])
                    sum += startRemainZ;
        }
        if (endZ > Math.Floor(endZ) && Math.Ceiling(endZ) < m_lengthZ) // get partial cells after ending element on Z-axis
        {
            for (int i = (int)Math.Ceiling(startX); i < endX; i++)
                if (map[i, (int)Math.Ceiling(endZ)])
                    sum += endRemainZ;
        }

        return sum / total >= threshold;
    }

    // read map from .png image file
    [ContextMenu("Import Map")]
    public void ImportMapPng()
    {
        TextAsset text = Resources.Load("map") as TextAsset;
        byte[] bytes = text.bytes;

        Texture2D img = new Texture2D(2, 2); // dummy size
        img.LoadImage(bytes);

        m_lengthX = img.width;
        m_lengthZ = img.height;
        map = new bool[m_lengthX, m_lengthZ];

        for(int x = 0; x < m_lengthX; x++)
            for(int z = 0; z < m_lengthZ; z++)
                map[x, z] = img.GetPixel(x, z) == Color.white;

        Debug.Log("Finished importing map from image (" + m_lengthX + "," + m_lengthZ + ")");
    }

#if UNITY_EDITOR
    [ContextMenu("Save Map Image")]
    void SaveMap()
    {
        if (map == null) { Debug.LogError("No map created"); return; }
        MakeImage(map, m_lengthX, m_lengthZ, nameMap);
    }

    [ContextMenu("Save Small Map Image")]
    void SaveSmallMap()
    {
        if (newMap == null) { Debug.LogError("No small map created"); return; }
        MakeImage(newMap, m_newX, m_newZ, nameSmallMap);
    }

    [ContextMenu("Save Zoomed Map Image")]
    void SaveZoomMap()
    {
        if (zoomMap == null) { Debug.LogError("No zoom map created"); return; }
        MakeImage(zoomMap, 100, 100, nameZoomMap);
    }

    void MakeImage(bool[,] arr, int x, int z, string name)
    {
        Texture2D img = new Texture2D(x, z);
        for(int i = 0; i < x; i++)
        {
            for(int j = 0; j < z; j++)
            {
                if (arr[i, j])
                    img.SetPixel(i, j, UnityEngine.Color.white);
                else
                    img.SetPixel(i, j, UnityEngine.Color.black);
            }
        }
        File.WriteAllBytes(name + ".png", img.EncodeToPNG());
        Debug.Log("Wrote: " + name + ".png");
    }
#endif
}