using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMapCoords : MonoBehaviour
{
    public float unity_x = 0;
    public float unity_z = 0;

    public int navmap_x = 0;
    public int navmap_y = 0;

    public float unity_max_x = 2048;
    public float unity_max_z = 1024;

    public int navmap_max_x = 256;
    public int navmap_max_y = 256;


    [ContextMenu("test")]
    void Test()
    {
        Debug.Log("RatioX: " + (Mathf.Floor(unity_max_x) / navmap_max_x));
        Debug.Log("RatioY: " + (Mathf.Floor(unity_max_z) / navmap_max_y));

        // input: 0<= unity_x < floor(unity_max_x) && 0 <= unity_z < floor(unity_max_z)

        int navmap_x = Mathf.FloorToInt(unity_x / (Mathf.Floor(unity_max_x) / navmap_max_x));
        int navmap_y = (navmap_max_y - 1) - Mathf.FloorToInt(unity_z / (Mathf.Floor(unity_max_z) / navmap_max_y));

        Debug.LogFormat("Navmap: ({0}, {1})", navmap_x, navmap_y);

        navmap_loc_to_unity_loc(navmap_x, navmap_y, navmap_max_x, navmap_max_y, false);
        navmap_loc_to_unity_loc(navmap_x, navmap_y, navmap_max_x, navmap_max_y, true);


    }

    [ContextMenu("testnavmap")]
    void TestNavMap()
    {
        navmap_loc_to_unity_loc(navmap_x, navmap_y, navmap_max_x, navmap_max_y, false);
    }

    void navmap_loc_to_unity_loc(int navmap_x, int navmap_y, int navmap_max_x, int navmap_max_y, bool center=true)
    {
        // input: 0 <= navmap_x < navmap_max_x && 0<= navmap_y < navmap_max_y

        float unity_x = navmap_x * (Mathf.Floor(unity_max_x) / navmap_max_x);
        float unity_z = Mathf.Floor(unity_max_z) - (navmap_y + 1) * (Mathf.Floor(unity_max_z) / navmap_max_y);

        if (center)
        {
            unity_x += (Mathf.Floor(unity_max_x) / navmap_max_x) / 2;
            unity_z += (Mathf.Floor(unity_max_z) / navmap_max_y) / 2;
        }

        Debug.LogFormat("Unity: ({0}, {1})", unity_x, unity_z);

    }


}
