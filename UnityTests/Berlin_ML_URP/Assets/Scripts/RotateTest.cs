#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class RotateTest : MonoBehaviour
{
    public GameObject m_cube;
    protected Vector3 forward, forward2D;
    public bool update = false;
    protected Quaternion old = Quaternion.identity;

    [ContextMenu("test")]
    void Test()
    {
        Quaternion qt = m_cube.transform.rotation;

        Vector3 e = qt.eulerAngles;

        Debug.Log("Q: " + qt.ToString("F4"));
        Debug.Log("Eul(Unity): " + e.ToString("F4"));

        Quaternion q = new Quaternion(qt.z, qt.x, qt.y, qt.w);

        // Convert Unity quaternion (Y-up) into more common (Z-up)
        // x = agent.rot.z
        // y = agent.rot.x
        // z = agent.rot.y
        // w = agent.rot.w

        float t0 = 2 * (q.w * q.x + q.y * q.z);
        float t1 = 1 - 2 * (q.x * q.x + q.y * q.y);
        float x = Mathf.Atan2(t0, t1);

        float t2 = 2 * (q.w * q.y - q.z * q.x);
        if (t2 > 1)
            t2 = 1;
        if (t2 < -1)
            t2 = -1;
        float y = Mathf.Asin(t2);

        float t3 = 2 * (q.w * q.z + q.x * q.y);
        float t4 = 1 - 2 * (q.y * q.y + q.z * q.z);
        float z = Mathf.Atan2(t3, t4);

        x *= 180 / Mathf.PI; // roll
        y *= 180 / Mathf.PI; // pitch
        z *= 180 / Mathf.PI; // yaw

        // in Unity:areaMask
        // z = roll
        // x = pitch
        // y = yaw
        Vector3 eulerAngles = new Vector3(y, z, x);

        Debug.LogFormat("Eul(calc): {0}", eulerAngles.ToString("F4"));

        forward = qt * Vector3.forward;
        Debug.Log("Direction: "+forward.ToString("F4"));
        Debug.Log("Normalized: " + forward.normalized.ToString("F4"));

        forward2D = Vector3.ProjectOnPlane(forward, Vector3.up);
        Debug.Log("Direction2D: " + forward2D.ToString("F4"));
        Debug.Log("Normalized: " + forward2D.normalized.ToString("F4"));

        Vector2 navmap_rot = unity_to_navmap_rot(new Vector4(qt.x, qt.y, qt.z, qt.w));
        Debug.Log("unitytonavmap: "+ navmap_rot.ToString("F4"));
        //Debug.Log(unity_angle_to_navmap_angle(eulerAngles*Mathf.PI/180f).ToString("F4"));

        /* // test python quat calcs
        Quaternion unity_lookrot = Quaternion.LookRotation(new Vector3(navmap_rot.x, 0, navmap_rot.y), Vector3.up);
        Debug.Log("Rot(LookRotation): "+unity_lookrot.ToString("F4"));

        Vector4 unity_rot = navmap_to_unity_rot(navmap_rot);
        Debug.Log("Rot(calc)" + unity_rot.ToString("F4"));
        m_cube.transform.rotation = new Quaternion(unity_rot.x, unity_rot.y, unity_rot.z, unity_rot.w);

        Vector3 vec = Vector3.Cross(new Vector3(.35f,-.2f,.33f), new Vector3(.1f,0,-.8f));
        Debug.Log("cross: " + vec.ToString("F4"));
        */
    }

    [ContextMenu("Generate 100 random yaw")]
    void LogRandomRotations()
    {
        string filePath = Application.dataPath + "/rotations.txt";
        StreamWriter writer = new StreamWriter(filePath, false);
        string line = "[";
        for (int i = 0; i < 100; i++)
        {
            m_cube.transform.Rotate(new Vector3(0, Random.Range(0, 360), 0));
            Quaternion q = m_cube.transform.rotation;
            line += string.Format("[{0}, {1}, {2}, {3}], ", q.x, q.y, q.z, q.w);
        }
        line += "]";
        Debug.Log("Wrote to: "+filePath);
        writer.WriteLine(line);
        writer.Close();
    }

    Vector2 unity_angle_to_navmap_angle(Vector3 eulerAngles)
    {
        Vector2 v = new Vector2(Mathf.Sin(eulerAngles.y), Mathf.Cos(eulerAngles.y));
        return v;
    }

    Vector2 unity_to_navmap_rot(Vector4 q)
    {
        Vector3 v = qv_mult(q, new Vector3(0, 0, 1));
        return v_normalize(new Vector2(v.x, v.z));
    }

    Vector4 navmap_to_unity_rot(Vector2 v)
    {
        Vector3 v1 = v_normalize(new Vector3(v.x, 0, v.y)); // forward
        Vector3 v2 = v_normalize(Vector3.Cross(new Vector3(0, 1, 0), v1));
        Vector3 v3 = Vector3.Cross(v1, v2);

        float m00 = v2[0];
        float m01 = v2[1];
        float m02 = v2[2];
        float m10 = v3[0];
        float m11 = v3[1];
        float m12 = v3[2];
        float m20 = v1[0];
        float m21 = v1[1];
        float m22 = v1[2];

        float num8 = (m00 + m11) + m22;
        float x, y, z, w;

        if (num8 > 0f)
        {
            float num = Mathf.Sqrt(num8 + 1f);
            w = num * 0.5f;
            num = 0.5f / num;
            x = (m12 - m21) * num;
            y = (m20 - m02) * num;
            z = (m01 - m10) * num;
        }
        else if(m00 >= m11 && m00 >= m22)
        {
            float num7 = Mathf.Sqrt(1f + m00 - m11 - m22);
            float num4 = 0.5f / num7;
            x = 0.5f / num7;
            y = (m01 + m10) * num4;
            z = (m02 + m20) * num4;
            w = (m12 - m21) * num4;
        }
        else if(m11 > m22)
        {
            float num6 = Mathf.Sqrt(1f + m11 - m00 - m22);
            float num3 = 0.5f / num6;
            x = (m10 + m01) * num3;
            y = 0.5f * num6;
            z = (m21 + m12) * num3;
            w = (m20 - m02) * num3;
        }
        else
        {
            float num5 = Mathf.Sqrt(1f + m22 - m00 - m11);
            float num2 = 0.5f / num5;
            x = (m20 + m02) * num2;
            y = (m21 + m12) * num2;
            z = 0.5f * num5;
            w = (m01 - m10) * num2;
        }

        return new Vector4(x, y, z, w);
    }

    Vector4 q_mult(Vector4 q1, Vector4 q2)
    {
        float x1 = q1[0];
        float y1 = q1[1];
        float z1 = q1[2];
        float w1 = q1[3];

        float x2 = q2[0];
        float y2 = q2[1];
        float z2 = q2[2];
        float w2 = q2[3];

        float x = w1*x2 + x1*w2 + y1*z2 - z1*y2;
        float y = w1*y2 + y1*w2 + z1*x2 - x1*z2;
        float z = w1*z2 + z1*w2 + x1*y2 - y1*x2;
        float w = w1*w2 - x1*x2 - y1*y2 - z1*z2;

        return new Vector4(x, y, z, w);
    }

    Vector3 qv_mult(Vector4 q, Vector3 v)
    {
        Vector4 qc = new Vector4(-q.x, -q.y, -q.z, q.w);
        Vector4 d = new Vector4(v.x, v.y, v.z, 0);
        Vector4 result = q_mult(q_mult(q, d), qc);
        return new Vector3(result.x, result.y, result.z);
    }

    Vector2 v_normalize(Vector2 v)
    {
        float magnitude = Mathf.Sqrt(v.x * v.x + v.y * v.y);
        return v/magnitude;
    }

    Vector3 v_normalize(Vector3 v)
    {
        float magnitude = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        return v / magnitude;
    }

    [CustomEditor(typeof(RotateTest))]
    public class RotateTestEditor : Editor
    {
        RotateTest rt;
        void OnSceneGUI()
        {
            
            rt = target as RotateTest;
            Event guiEvent = Event.current;

            if (guiEvent.type == EventType.Repaint)
            {
                if (rt.update)
                {
                    if (rt.old != rt.m_cube.transform.rotation)
                    {
                        rt.old = rt.m_cube.transform.rotation;
                        rt.Test();
                    }
                }
                DebugDraw();
            }
        }

        void DebugDraw()
        {
            if (!rt.m_cube) return;
            Handles.color = Color.blue;
            Handles.DrawLine(rt.m_cube.transform.position, rt.forward * 50f);
            Handles.color = Color.green;
            Handles.DrawLine(rt.m_cube.transform.position, rt.forward2D * 50f);
        }
    }
}
#endif