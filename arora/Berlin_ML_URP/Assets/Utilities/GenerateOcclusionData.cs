#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using static DynamicSceneLoader;

public class GenerateOcclusionData : MonoBehaviour
{
#if UNITY_EDITOR
    public SceneInfo[,] sceneGrid;
    public int range;

    int gridWidth, gridHeight;

    public List<string> loadingScenes = new List<string>();
    public List<SceneInfo> tilesWaitingToUnload = new List<SceneInfo>();

    DynamicSceneLoader dynamicSceneLoader;

    [ContextMenu("Generate Occlusion Data")]
    public void GenerateData()
    {
        int x, y;
        dynamicSceneLoader = GetComponent<DynamicSceneLoader>();
        if(dynamicSceneLoader.sceneGrid == null)
            dynamicSceneLoader.PopulateSceneArray();
        sceneGrid = dynamicSceneLoader.sceneGrid;
        gridWidth = dynamicSceneLoader.gridWidth;
        gridHeight = dynamicSceneLoader.gridHeight;

        EditorSceneManager.sceneLoaded += SceneManager_sceneLoaded;

        for (x = 0; x < gridWidth; x++)
            for (y = 0; y < gridHeight; y++)
            {
                UpdateActiveScenes(new int[] { x, y });
                int scenesToLoad = loadingScenes.Count;
                while (dynamicSceneLoader.loadingScenes.Count > 0)
                    if (EditorUtility.DisplayCancelableProgressBar("Loading scene files for center tile " + x + "-" + y, "Scenes remaining " + dynamicSceneLoader.loadingScenes.Count + " of " + scenesToLoad, dynamicSceneLoader.loadingScenes.Count / scenesToLoad))
                    {
                    
                        return;
                    }
                EditorSceneManager.SetActiveScene(dynamicSceneLoader.sceneGrid[x, y].scene);
                //StaticOcclusionCulling.Compute();
                //while (StaticOcclusionCulling.isRunning)
                //;
            }
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        loadingScenes.Remove(arg0.name);
        int[] pos = DynamicSceneLoader.ParseSceneName(arg0.name);
        sceneGrid[pos[0], pos[1]].loadState = SceneInfo.LoadState.Loaded;
    }

    public void UpdateActiveScenes(int[] position)
    {
        int x, y;

        Debug.Log("New center tile:" + position[0] + ", " + position[1]);
        List<Vector2> newActiveTiles = new List<Vector2>();

        for (x = position[0] - range; x <= position[0] + range; x++)
            for (y = position[1] - range; y <= position[1] + range; y++)
                newActiveTiles.Add(new Vector2(x, y));

        for (x = 0; x < gridWidth; x++)
            for (y = 0; y < gridHeight; y++)
            {
                bool active = newActiveTiles.Contains(new Vector2(x, y));

                //If the tile should be active and isn't
                if (active)
                {
                    if (sceneGrid[x, y].loadState == SceneInfo.LoadState.Unloaded)
                    {
                        sceneGrid[x, y].loadState = SceneInfo.LoadState.Loaded;
                        sceneGrid[x, y].distanceFromCenter = Mathf.Abs(position[0] - x) + Mathf.Abs(position[1] - y);
                        loadingScenes.Add(sceneGrid[x, y].sceneName);
                        sceneGrid[x,y].scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneGrid[x, y].sceneName + ".unity", OpenSceneMode.Additive);
                    }
                }
                //If the tile is active and shouldn't be
                else if (!active)
                {
                    if (sceneGrid[x, y].loadState == SceneInfo.LoadState.Loaded) //Tile is ready to be unloaded;
                    {
                        sceneGrid[x, y].loadState = SceneInfo.LoadState.Unloaded;
                        EditorSceneManager.CloseScene(sceneGrid[x, y].scene, true);
                    }
                }
            }
    }
#endif
}
