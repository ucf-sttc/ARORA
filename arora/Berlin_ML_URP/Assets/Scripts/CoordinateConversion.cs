using UnityEngine;

// converts Unity to nonnegative coordinates by adding an offset
// nonnegative coords are used by the python trainer
// dependency: GetNavMeshMap runs first to set conversion value based on terrain
public static class CoordinateConversion
{
    public static Vector3Int m_offset = Vector3Int.zero;

    public static Vector2 ToUnity(Vector2 a)
    {
        return new Vector2(a.x + m_offset.x, a.y + m_offset.z);
    }

    public static Vector3 ToUnity(Vector3 a)
    {
        return a + m_offset;
    }

    public static Vector2 ToNonNegative(Vector2 a)
    {
        return new Vector2(a.x - m_offset.x, a.y - m_offset.z);
    }

    public static Vector3 ToNonNegative(Vector3 a)
    {
        return a - m_offset;
    }
}
