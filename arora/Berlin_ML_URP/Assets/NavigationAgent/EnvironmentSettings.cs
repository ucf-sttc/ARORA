using UnityEngine;
using Unity.MLAgents;

public class EnvironmentSettings : MonoBehaviour
{
    [HideInInspector]
    public GameObject[] agents;
    [HideInInspector]
    public NavArea[] listArea;

    StatsRecorder m_Recorder;

    public int fastForward;

    public void Awake()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
        m_Recorder = Academy.Instance.StatsRecorder;
        Application.targetFrameRate = -1;
        Debug.Log("Quality level: " + QualitySettings.GetQualityLevel());
        fastForward = CommandLineArgumentListener.instance.fastForward;
    }

    void EnvironmentReset()
    {
        listArea = FindObjectsOfType<NavArea>();
        Debug.Log("Resetting environment ("+listArea.Length+" areas)");
        while(fastForward > 0)
        {
            foreach (var area in listArea)
            {
                area.ResetArea("fastForwardEpisodes");
            }
            fastForward--;
        }
        foreach (var area in listArea)
        {
            area.ResetArea("environment");
        }
    }
}