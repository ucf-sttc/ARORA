using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagVisibility : MonoBehaviour
{
    public CollectVisibleObjects.AttributeObject referencedEntry;

    private void OnBecameVisible()
    {
        referencedEntry.SetVisibility(true);
    }

    private void OnBecameInvisible()
    {
        referencedEntry.SetVisibility(false);
    }
}
