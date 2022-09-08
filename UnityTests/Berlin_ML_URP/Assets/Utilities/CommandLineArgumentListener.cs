using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CommandLineArgumentListener : MonoBehaviour
{
    public string[] args;
    public static CommandLineArgumentListener instance;
    public int observationWidth = 64, observationHeight = 64;
    public RenderTexture[] observationTextures;
    public int fastForward;

    public GameObject canvas;
    public bool useTestArgs;
    public string[] testArgs;

    bool playback = false;
    float[] trainingArea = new float[] {0,0,500,3000}, testingArea = new float[] {500,0,4000,3000 };

    void Awake()
    {
        instance = this;
        args = Environment.GetCommandLineArgs();
#if UNITY_EDITOR
        if(useTestArgs)
            args = testArgs;
#endif

        int width = -1, height = -1, agentAllowedAreaIndex = -1, qualityIndex = -1;

        for (int i = 0; i < args.Length; i++)
        {
            Debug.Log("ARG " + i + ": " + args[i]);
            if (args[i] == "-observationWidth")
            {
                width = ++i;
            }
            else if (args[i] == "-observationHeight")
            {
                height = ++i;
            }
            else if (args[i] == "-showVisualObservations")
            {
                ShowVisualObservations();
            }
            else if (args[i] == "-playback")
                playback = true;
            else if (args[i] == "-fastForward")
                SetupFastForward(++i);
            else if (args[i] == "-allowedArea")
                agentAllowedAreaIndex = ++i;
            else if (args[i] == "-quality")
                qualityIndex = ++i;
            else if (args[i] == "-terrainLoadDistance" && int.TryParse(args[i+1], out int loadDistance))
            {
                DynamicSceneLoader.instance.range = loadDistance;
                Debug.Log("Set dynamic scene loader range:" + DynamicSceneLoader.instance.range);
            }
                
            /*** other env args ***/
            else if (args[i].StartsWith("-") && float.TryParse(args[i + 1], out float envVal))
            {
                CommandLineArgs.Instance.Parameters.Add(args[i].Substring(1), envVal);
                i++;
            }
        }

        SetupRenderTextures(width, height);
        SetupQuality(qualityIndex);
        
        if(playback) // playback and training area setup are mutually exclusive
            SetupPlaybackMode();
        else
            SetupAvailableArea(agentAllowedAreaIndex);
    }

    void SetupRenderTextures(int width, int height)
    {
        observationWidth = 64; 
        observationHeight = 64;

        if (width != -1 && height != -1)
        {
            try
            {
                observationWidth = int.Parse(args[width]);
                observationHeight = int.Parse(args[height]);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to read observation width and height. Defaulting to 64x64. Error:" + e);
            }
        }
        else
        {
            Debug.LogWarning("Observation width and height not set in command line arguments. Defaulting to 64x64");
        }

        foreach (RenderTexture r in observationTextures)
        {
            r.Release();
            r.width = observationWidth;
            r.height = observationHeight;
            if(playback && r.format == RenderTextureFormat.R8)
                r.format = RenderTextureFormat.ARGB32; // show depth as grayscale in playback mode
            r.Create();
        }
    }

    void SetupQuality(int qualityIndex)
    {
        if (qualityIndex == -1)
            return;

        int quality = 0;
        try
        {
            quality = int.Parse(args[qualityIndex]);
        }
        catch(Exception e)
        {
            Debug.LogError("Failed to read quality setting from command line args: " + e);
        }
        if (quality >= 0 && quality < QualitySettings.names.Length)
            QualitySettings.SetQualityLevel(quality);
    }

    void ShowVisualObservations()
    {
        canvas.SetActive(true);
        Time.fixedDeltaTime = 1f / 30;
    }

    void SetupPlaybackMode()
    {
        SceneManager.LoadScene("Berlin_Walk_v2_aar");
        Screen.SetResolution(960, 540, false);
    }

    void SetupFastForward(int index)
    {
        try
        {
            fastForward = int.Parse(args[index]);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to set fast forward. Defaulting to 0. Error:" + e);
            fastForward = 0;
        }
    }

    void SetupAvailableArea(int agentAllowedAreaIndex)
    {
        try
        {
            if (agentAllowedAreaIndex != -1)
            {
                float[] selectedArea = (int.Parse(args[agentAllowedAreaIndex])) switch
                {
                    1 => trainingArea,
                    2 => testingArea,
                    _ => throw new Exception("An invalid area index was received"),
                };
                FindObjectOfType<TaskManager>().agentAllowedArea = selectedArea;
                FindObjectOfType<DynamicSceneLoader>().LimitSceneGridArray(selectedArea);
            }
                
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to set agent boundaries. Defaulting to full map. Error:" + e);
        }
    }
}
