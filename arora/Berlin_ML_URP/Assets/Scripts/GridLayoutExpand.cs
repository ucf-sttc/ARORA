using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridLayoutExpand : MonoBehaviour
{
    public GridLayoutGroup grid;
    public RectTransform rect;
    public CanvasScaler canvasScaler;
    public int columns = 2;

    void Start()
    {
        ResizeGridLayout();
    }

    void ResizeGridLayout()
    {
        if(grid != null)
        {
            int n = grid.transform.childCount;

            Vector2 scale = canvasScaler.referenceResolution;
            Vector2 spacing = grid.spacing;

            int rows = n / columns;

            float childX = (rect.rect.width - spacing.x * (columns - 1)) / columns;
            float childY = (rect.rect.height - spacing.y * (rows - 1)) / rows;

            //float childX = (scale.x - spacing.x * (columns - 1)) / columns;
            //float childY = (scale.y - spacing.y * (rows - 1)) / rows;

            if (childX < childY)    childY = childX;
            else                    childX = childY;

            grid.cellSize = new Vector2(childX, childY);
        }
    }
}
