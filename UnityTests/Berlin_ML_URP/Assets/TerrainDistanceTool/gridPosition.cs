using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gridPosition : MonoBehaviour
{
    public int[] position;

    public void setPosition(int x, int y)
    {
        this.position = new int[]{x,y};
    }
}
