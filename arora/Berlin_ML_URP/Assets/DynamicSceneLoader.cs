using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.Events;
using Unity.MLAgents;

public class DynamicSceneLoader : MonoBehaviour
{
    public static DynamicSceneLoader instance;
    [System.Serializable]
    public class SceneInfo
    {
        public string sceneName;
        public AsyncOperationHandle<SceneInstance> sceneHandle;
        public Scene scene;
        public LoadState loadState;
        public int distanceFromCenter;
        public Vector2 xzCenterPosition;
        public Terrain terrain;
        public bool allowed;
        
        public enum LoadState
        {
            Unloaded,
            Loading,
            Loaded
        }
    }
    public SceneInfo[,] sceneGrid;
    public int gridWidth, gridHeight;
    public Vector2 zero_zeroTileCenterPosition, one_oneTileCenterPosition;
    public GameObject agent;
    
    public int range; 
    public float pixelErrorModificationMultiplier;

    public List<string> loadingScenes = new List<string>();
    public List<SceneInfo> tilesWaitingToUnload = new List<SceneInfo>();

    int[] lastTilePosition;
    public float radiusOfTile;

    public UnityEvent tilePositionUpdateEvent = new UnityEvent();
    public UnityEvent<Scene> edgeTileLoadedEvent = new UnityEvent<Scene>();
    public List<Vector2> edgeTiles = new List<Vector2>();

    SceneInfo lastTile;
    Vector2 vectorFromCenterOfTile;
    List<Vector2> newActiveTiles = new List<Vector2>();
    int fullTileLoads = 0;
    WaitForSecondsRealtime oneRealSecond = new WaitForSecondsRealtime(1);

    [HideInInspector]public bool isPlayback = false;
    [HideInInspector]public bool roadCoversEnabled = true;

    private void Awake()
    {
        instance = this;
        //Get the smallest distance from the center of the tile to it's edge
        radiusOfTile = Mathf.Min(Mathf.Abs(zero_zeroTileCenterPosition[0] - one_oneTileCenterPosition[0]), Mathf.Abs(zero_zeroTileCenterPosition[1] - one_oneTileCenterPosition[1])) / 2;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (sceneGrid == null)
            PopulateSceneArray();

        Debug.Log("Scene grid size" + sceneGrid.GetLength(0) + ", " + sceneGrid.GetLength(1) + ". Scene count: " + sceneGrid.Length);
        StartCoroutine(UnloadUnusedTiles());
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void FixedUpdate()
    {
        if (agent != null && pausingOperations == 0)
        {
            if (lastTilePosition != null)
            {
                /*
                int[] currentTilePosition = getCurrentlyOccupiedTile();
                SceneInfo currentlyOccupiedTile = sceneGrid[currentTilePosition[0], currentTilePosition[1]];
                if (currentlyOccupiedTile != sceneGrid[lastTilePosition[0], lastTilePosition[1]])
                    UpdateActiveScenes(currentTilePosition);
                */
                lastTile = sceneGrid[lastTilePosition[0], lastTilePosition[1]];
                vectorFromCenterOfTile= new Vector2(agent.transform.position.x, agent.transform.position.z) - lastTile.xzCenterPosition;
                if (lastTile.terrain == null || Mathf.Abs(vectorFromCenterOfTile[0]) > radiusOfTile || Mathf.Abs(vectorFromCenterOfTile[1]) > radiusOfTile)
                    UpdateActiveScenes(getCurrentlyOccupiedTile());
                
            }  
            else if(loadingScenes.Count == 0)
            {
                //StartCoroutine(LoadInitialTiles());
            }
        }
    }

    [ContextMenu("Trigger initial tile load")]
    public static void CallInitialTileLoad()
    {
        instance.StartCoroutine(instance.LoadInitialTiles());
    }
    
    public IEnumerator LoadInitialTiles()
    {
        Pause(true);
        if(!isPlayback) // avoid lazy init of academy during playback
            Academy.Instance.AutomaticSteppingEnabled=false;
        CleanAndUpdateActiveScenes(getCurrentlyOccupiedTile());
        yield return new WaitUntil(() => loadingScenes.Count == 0);
        
        if (fullTileLoads++ > 10)
        {
            Debug.Log("Clearing unused assets");
            Resources.UnloadUnusedAssets();
            fullTileLoads = 0;
        }
        if(!isPlayback)
            Academy.Instance.AutomaticSteppingEnabled = true;
        Pause(false);
    }

    public IEnumerator UnloadUnusedTiles()
    {
        while(true)
        {
            for (int i = tilesWaitingToUnload.Count; i > 0; i--)
            {
                SceneInfo s = tilesWaitingToUnload[i - 1];

                if (s.loadState == SceneInfo.LoadState.Loaded)
                {
                    int[] xy = ParseSceneName(s.sceneName);
                    sceneGrid[xy[0], xy[1]].loadState = SceneInfo.LoadState.Unloaded;
                    if (sceneGrid[xy[0], xy[1]].sceneHandle.IsValid())
                        Addressables.UnloadSceneAsync(sceneGrid[xy[0], xy[1]].sceneHandle);
                        
                    tilesWaitingToUnload.Remove(s);
                }
                else if (s.loadState == SceneInfo.LoadState.Unloaded)
                    tilesWaitingToUnload.Remove(s);
            }

            yield return oneRealSecond;
        }
    }

    int[] getCurrentlyOccupiedTile()
    {
        Vector2 xzAgentPosition = new Vector2(agent.transform.position.x, agent.transform.position.z);
        return ParseSceneName(getTileForPosition(xzAgentPosition));
    }

    public string getTileForPosition(Vector2 position)
    {
        if (sceneGrid == null)
            PopulateSceneArray();

        float closestDistance = float.MaxValue;
        string closestTileName = sceneGrid[0, 0].sceneName;
        
        foreach (SceneInfo info in sceneGrid)
        {
            if(info.allowed)
            {
                float distance = Vector2.Distance(position, info.xzCenterPosition);
                //Debug.Log(info.sceneName + " Center:" + info.xzCenterPosition + " Distance:" + distance);
                if (distance < closestDistance)
                {
                    closestTileName = info.sceneName;
                    closestDistance = distance;
                }
            }
        }
        return closestTileName;
    }

    public void UpdateRadius(int r)
    {
        range = r;
        UpdateActiveScenes(getCurrentlyOccupiedTile());
    }

    void UpdateActiveScenes(Terrain currentTerrainTile)
    {
        UpdateActiveScenes(ParseSceneName(currentTerrainTile.gameObject.scene.name));
    }
    public void UpdateActiveScenes(int[] position)
    {
        int x, y;
        bool active;
        tilePositionUpdateEvent.Invoke();
        Debug.Log("New center tile:" + position[0] + ", " + position[1]);
        newActiveTiles.Clear();
        edgeTiles.Clear();

        lastTilePosition = position;

        for (x = position[0] - range; x <= position[0] + range; x++)
            for (y = position[1] - range; y <= position[1] + range; y++)
            {
                newActiveTiles.Add(new Vector2(x, y));
                if (x == position[0] - range || x == position[0] + range || y == position[1] - range || y == position[1] + range)
                    edgeTiles.Add(new Vector2(x, y));
            }
                    

        for (x = 0; x < gridWidth; x++)
            for (y = 0; y < gridHeight; y++)
            {
                if(sceneGrid[x,y].allowed)
                {
                    active = newActiveTiles.Contains(new Vector2(x, y));

                    //Update pixel error of loaded tiles
                    if (active && sceneGrid[x, y].loadState == SceneInfo.LoadState.Loaded && pixelErrorModificationMultiplier != 1 && sceneGrid[x, y].terrain != null)
                    {
                        int newDistanceFromCenter = Mathf.Abs(position[0] - x) + Mathf.Abs(position[1] - y);
                        if (newDistanceFromCenter != sceneGrid[x, y].distanceFromCenter)
                        {
                            sceneGrid[x, y].distanceFromCenter = newDistanceFromCenter;
                            sceneGrid[x, y].terrain.heightmapPixelError = Mathf.Max(1, Mathf.Pow(sceneGrid[x, y].distanceFromCenter - 1, pixelErrorModificationMultiplier));
                        }
                        edgeTileLoadedEvent.Invoke(sceneGrid[x, y].scene);
                    }

                    //If the tile should be active and isn't
                    if (active)
                    {
                        tilesWaitingToUnload.Remove(sceneGrid[x, y]);
                        if (sceneGrid[x, y].loadState == SceneInfo.LoadState.Unloaded)
                        {
                            sceneGrid[x, y].loadState = SceneInfo.LoadState.Loading;
                            sceneGrid[x, y].distanceFromCenter = Mathf.Abs(position[0] - x) + Mathf.Abs(position[1] - y);
                            sceneGrid[x, y].sceneHandle = Addressables.LoadSceneAsync("Assets/Scenes/" + sceneGrid[x, y].sceneName + ".unity", LoadSceneMode.Additive);
                            sceneGrid[x, y].sceneHandle.Completed += DynamicSceneLoader_Completed;
                            loadingScenes.Add(sceneGrid[x, y].sceneName);
                        }
                    }
                    //If the tile is active and shouldn't be
                    else
                    {
                        if (sceneGrid[x, y].loadState == SceneInfo.LoadState.Loaded) //Tile is ready to be unloaded;
                        {
                            sceneGrid[x, y].loadState = SceneInfo.LoadState.Unloaded;
                            if (sceneGrid[x, y].sceneHandle.IsValid())
                                Addressables.UnloadSceneAsync(sceneGrid[x, y].sceneHandle);

                        }
                        else if (sceneGrid[x, y].loadState == SceneInfo.LoadState.Loading && !tilesWaitingToUnload.Contains(sceneGrid[x, y])) //Tile has to wait to fully load before it can unload
                        {
                            tilesWaitingToUnload.Add(sceneGrid[x, y]);
                        }
                    }
                }         
            }
    }

    //Takes longer than UpdateActiveScenes but removes unused scenes first to limit memory usage
    public void CleanAndUpdateActiveScenes(int[] position)
    {
        int x, y;
        bool active;
        tilePositionUpdateEvent.Invoke();
        Debug.Log("New center tile:" + position[0] + ", " + position[1]);
        newActiveTiles.Clear();
        edgeTiles.Clear();

        lastTilePosition = position;

        for (x = position[0] - range; x <= position[0] + range; x++)
            for (y = position[1] - range; y <= position[1] + range; y++)
            {
                newActiveTiles.Add(new Vector2(x, y));
                if (x == position[0] - range || x == position[0] + range || y == position[1] - range || y == position[1] + range)
                    edgeTiles.Add(new Vector2(x, y));
            }

        for (x = 0; x < gridWidth; x++)
            for (y = 0; y < gridHeight; y++)
            {
                if(sceneGrid[x,y].allowed)
                {
                    active = newActiveTiles.Contains(new Vector2(x, y));

                    if (!active)
                    {
                        if (sceneGrid[x, y].loadState == SceneInfo.LoadState.Loaded) //Tile is ready to be unloaded;
                        {
                            sceneGrid[x, y].loadState = SceneInfo.LoadState.Unloaded;
                            if (sceneGrid[x, y].sceneHandle.IsValid())
                                Addressables.UnloadSceneAsync(sceneGrid[x, y].sceneHandle);
                        }
                        else if (sceneGrid[x, y].loadState == SceneInfo.LoadState.Loading && !tilesWaitingToUnload.Contains(sceneGrid[x, y])) //Tile has to wait to fully load before it can unload
                        {
                            tilesWaitingToUnload.Add(sceneGrid[x, y]);
                        }
                    }
                }
            }

        for (x = 0; x < gridWidth; x++)
            for (y = 0; y < gridHeight; y++)
            {
                if (sceneGrid[x, y].allowed)
                {
                    active = newActiveTiles.Contains(new Vector2(x, y));

                    //Update pixel error of loaded tiles
                    if (active && sceneGrid[x, y].loadState == SceneInfo.LoadState.Loaded && pixelErrorModificationMultiplier != 1 && sceneGrid[x, y].terrain != null)
                    {
                        int newDistanceFromCenter = Mathf.Abs(position[0] - x) + Mathf.Abs(position[1] - y);
                        if (newDistanceFromCenter != sceneGrid[x, y].distanceFromCenter)
                        {
                            sceneGrid[x, y].distanceFromCenter = newDistanceFromCenter;
                            sceneGrid[x, y].terrain.heightmapPixelError = Mathf.Max(1, Mathf.Pow(sceneGrid[x, y].distanceFromCenter - 1, pixelErrorModificationMultiplier));
                        }
                        edgeTileLoadedEvent.Invoke(sceneGrid[x, y].scene);
                    }

                    //If the tile should be active and isn't
                    if (active)
                    {
                        tilesWaitingToUnload.Remove(sceneGrid[x, y]);
                        if (sceneGrid[x, y].loadState == SceneInfo.LoadState.Unloaded)
                        {
                            loadingScenes.Add(sceneGrid[x, y].sceneName);
                            sceneGrid[x, y].loadState = SceneInfo.LoadState.Loading;
                            sceneGrid[x, y].distanceFromCenter = Mathf.Abs(position[0] - x) + Mathf.Abs(position[1] - y);
                            sceneGrid[x, y].sceneHandle = Addressables.LoadSceneAsync("Assets/Scenes/" + sceneGrid[x, y].sceneName + ".unity", LoadSceneMode.Additive);
                            sceneGrid[x, y].sceneHandle.Completed += DynamicSceneLoader_Completed;
                        }
                    }
                }
            }
    }

    //In this instance changes to sceneInfo from sceneInfo = sceneGrid[x,y] don't update the original sceneGrid[x,y] entry
    private void DynamicSceneLoader_Completed(AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            Scene s = obj.Result.Scene;
            int[] pos = ParseSceneName(s.name);
            int x = pos[0], y = pos[1];
            try
            {
                sceneGrid[x, y].terrain = s.GetRootGameObjects()[0].GetComponent<Terrain>();
                if (pixelErrorModificationMultiplier != 1)
                    sceneGrid[x, y].terrain.heightmapPixelError = Mathf.Max(1, Mathf.Pow(sceneGrid[x, y].distanceFromCenter - 1, pixelErrorModificationMultiplier));
            }
            catch(System.Exception e)
            {
                Debug.LogError("Failed to locate and update terrain object in newly loaded scene " + s.name + "\n" + e);
            }

            sceneGrid[x, y].scene = s;
            if (edgeTiles.Contains(new Vector2(x, y)))
                edgeTileLoadedEvent.Invoke(s);
            
            //sceneGrid[x, y].xzCenterPosition = new Vector2(terrainCenterPos.x, terrainCenterPos.z);
            if (obj.IsValid())
            {
                sceneGrid[x, y].loadState = SceneInfo.LoadState.Loaded;
                loadingScenes.Remove(s.name);
                if(isPlayback) SetupRoadCovers(s); // precaution to avoid potential cost to normal operations
            }
            else
                sceneGrid[x, y].loadState = SceneInfo.LoadState.Unloaded;

            //Debug.Log("Loaded scene: " + s.name + ". Grid state " + sceneGrid[pos[0], pos[1]].loadState);
        }
        else
        {
            Debug.LogError("Failed to load scene: " + obj.Result.Scene.name);
        }    
    }

    //Choose whether road covers are shown or not based on state
    void SetupRoadCovers(Scene s)
    {
        GameObject[] rootObjs = s.GetRootGameObjects();
        if (rootObjs != null && rootObjs.Length > 0)
        {
            Transform roadCovers = rootObjs[0].transform.Find("Road");
            if (roadCovers)
                roadCovers.gameObject.SetActive(roadCoversEnabled);
        }
    }

    //This function is used to limit the tiles that are able to load to support separate training and testing areas. For this function the bounds are setup as x,z,y to allow the bounds.contains function to work correctly on our vector2 scenegrid positions
    public void LimitSceneGridArray(float[] bounds)
    {
        Bounds b = new Bounds
        {
            min = new Vector3(bounds[0], bounds[1], -1),
            max = new Vector3(bounds[2], bounds[3], 1)
        };

        if (sceneGrid == null)
            PopulateSceneArray();
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                if(!b.Contains(sceneGrid[x, y].xzCenterPosition))
                {
                    sceneGrid[x, y].allowed = false;
                }
            }
    }

    [ContextMenu("Populate scene array")]
    public void PopulateSceneArray()
    {
        Debug.Log("Populating scene array");
        sceneGrid = new SceneInfo[gridWidth, gridHeight];
        Vector3 tileOffset = one_oneTileCenterPosition - zero_zeroTileCenterPosition;

        for (int x =0;x<gridWidth;x++)
            for(int y=0; y<gridHeight;y++)
            {
                sceneGrid[x, y] = new SceneInfo
                {
                    sceneName = "" + x + "-" + y,
                    loadState = SceneInfo.LoadState.Unloaded,
                    xzCenterPosition = new Vector2(zero_zeroTileCenterPosition.x + (tileOffset.x * x), zero_zeroTileCenterPosition.y + (tileOffset.y * y)),
                    allowed = true
                };
            }
        //sceneGrid[0, 0].loadState = SceneInfo.LoadState.Loading;
        //sceneGrid[0, 0].sceneHandle = Addressables.LoadSceneAsync("Assets/Scenes/" + sceneGrid[0, 0].sceneName + ".unity", LoadSceneMode.Additive);
        //sceneGrid[0, 0].sceneHandle.Completed += DynamicSceneLoader_Completed;
    }

    public static int[] ParseSceneName(string name)
    {
        string[] positionStrings = name.Split('-');
        return new int[] { int.Parse(positionStrings[0]), int.Parse(positionStrings[1]) };
    }

    int pausingOperations = 0;
    float normalTimescale = 1;
    Rigidbody agentRigidbody;
    public void Pause(bool pause)
    {
        if(pause)
        {
            Debug.Log("Loading initial scenes. Pausing. Timescale was " + Time.timeScale + ". " + pausingOperations + " pausing operations occuring");
            if (pausingOperations == 0)
            {
                normalTimescale = Time.timeScale;
                Time.timeScale = 0;
                if(agentRigidbody == null)
                    agentRigidbody = agent.GetComponent<Rigidbody>();
                if (agentRigidbody != null) agentRigidbody.isKinematic = true;
            }
            pausingOperations++;
        }
        else
        {
            pausingOperations--;
            if (pausingOperations == 0)
            {
                Debug.Log("Initial scenes loaded. Resuming");
                if (agentRigidbody == null)
                    agentRigidbody = agent.GetComponent<Rigidbody>();
                if (agentRigidbody != null && !isPlayback) agentRigidbody.isKinematic = false;
                Time.timeScale = normalTimescale;
            }
        }
    }
}
