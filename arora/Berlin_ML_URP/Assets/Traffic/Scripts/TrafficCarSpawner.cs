/*
 * Handles placement and removal of traffic cars
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class TrafficCarSpawner : MonoBehaviour
{
    private TrafficSystem ts;
    private GameObject carsContainer;
    public LinkedList<GameObject> cars;
    public int maxCars = 50;
    //[HideInInspector]
    public List<GameObject> spawnPoints;
    public GameObject[] carPrefabs;
    public GameObject[] carPrefabsKinematic;
    //public static System.Random carRNG = new System.Random(1);
    public static StreamWriter writer;
    //public float minSpawnDistance = 200, maxSpawnDistance = 400;
    public static SpawnPointLoadEvent loadEvent;
    public static SpawnPointUnloadEvent unloadEvent;
    float maxDistance;
    int autonomousvehicleLayer;

    public List<GameObject> usedPoints;

    public class SpawnPointLoadEvent : UnityEvent<SpawnPointComponent> { }
    public class SpawnPointUnloadEvent : UnityEvent<SpawnPointComponent> { }

    public enum TrafficType
    {
        None,
        Kinematic,
        Physics
    }
    [HideInInspector]
    public TrafficType trafficType = TrafficType.Kinematic;
    [HideInInspector]
    public bool obstacleDetection = false;
    SegmentationSetup segmentationSetup;

    void Awake()
    {
        ts = GetComponent<TrafficSystem>();
        ts.tcs = this;
        carsContainer = transform.Find("Cars").gameObject;
        segmentationSetup = FindObjectOfType<SegmentationSetup>();
        cars = new LinkedList<GameObject>();
        spawnPoints = new List<GameObject>();
        loadEvent = new SpawnPointLoadEvent();
        unloadEvent = new SpawnPointUnloadEvent();
        //loadEvent.AddListener(AddSpawnPoint);
        //unloadEvent.AddListener(RemoveSpawnPoint);

        usedPoints = new List<GameObject>();
        autonomousvehicleLayer = LayerMask.GetMask("AutonomousVehicle");
    }

    private void OnEnable()
    {
        DynamicSceneLoader l = DynamicSceneLoader.instance;

        l.tilePositionUpdateEvent.AddListener(ClearSpawnPoints);
        l.edgeTileLoadedEvent.AddListener(UpdateSpawnPoints);

        maxDistance = l.range * (2*l.radiusOfTile)*1.1f;
    }

    private void OnDisable()
    {
        DynamicSceneLoader l = DynamicSceneLoader.instance;
        l.tilePositionUpdateEvent.RemoveListener(ClearSpawnPoints);
        l.edgeTileLoadedEvent.RemoveListener(UpdateSpawnPoints);
    }

    void ClearSpawnPoints()
    {
        spawnPoints.Clear();
    }
    void UpdateSpawnPoints(Scene s)
    {
        foreach (SpawnPointComponent c in s.GetRootGameObjects()[0].transform.GetComponentsInChildren<SpawnPointComponent>())
            spawnPoints.Add(c.gameObject);
    }

    private void Start()
    {
        ts.agent = GameObject.FindObjectOfType<DynamicSceneLoader>().agent;
    }

    private void FixedUpdate()
    {
        if (trafficType == TrafficType.None) return;

        usedPoints.Clear();
            

        Profiler.BeginSample("Check Cars");
        CheckCars();
        Profiler.EndSample();
        Profiler.BeginSample("Place Cars");
        if(spawnPoints.Count > 0)
            PlaceCars();
        Profiler.EndSample();
    }

    void AddSpawnPoint(SpawnPointComponent spc)
    {
        Profiler.BeginSample("Get Points");
        spawnPoints.Add(spc.gameObject);
        Profiler.EndSample();
    }

    void RemoveSpawnPoint(SpawnPointComponent spc)
    {
        Profiler.BeginSample("Get Points");
        spawnPoints.Remove(spc.gameObject);
        Profiler.EndSample();
    }

    // Places cars based on min/maxSpawnDistance and if the point is active or not (controlled by DynamicSceneLoader)
    void PlaceCars()
    {
        if (!ts.agent) // for testing ONLY, if mlagent is disabled then place cars around the map
        {
            for(int i = ts.numCars; i < maxCars; i++)
            {
                PlaceCar(spawnPoints[Random.Range(0, spawnPoints.Count)]);
                ts.numCars++;
            }
        }
        else // this is the loop used to place traffic cars when using mlagents
        {
            int i = 0;
            Collider[] hits = new Collider[1];
            while(ts.numCars < maxCars && i<10 && spawnPoints.Count > 0)
            {
                GameObject newSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
                if (!usedPoints.Contains(newSpawnPoint))
                {
                    if (Physics.OverlapSphereNonAlloc(newSpawnPoint.transform.position, 2, hits, autonomousvehicleLayer) == 0)
                    {
                        PlaceCar(newSpawnPoint);
                        ts.numCars++;
                        usedPoints.Add(newSpawnPoint);
                    }
                }
                i++;
            }
        }

        //string s = "randoms";
        //while (System.IO.File.Exists(s))
        //{
        //    s += "-";
        //}
        //TrafficCarSpawner.writer = new StreamWriter(s, true);
    }

    // instantiate a car at a generated "spawn point" depending on traffic type
    public void PlaceCar(GameObject spawnPoint)
    {
        GameObject car;
        if (trafficType == TrafficType.Kinematic)
        {
            car = Instantiate(carPrefabsKinematic[0], spawnPoint.transform.position,
                spawnPoint.transform.rotation, carsContainer.transform);
            car.GetComponent<CarAIKinematic>().trafficSystem = ts;
        }
        else //if (trafficType == TrafficType.Physics)
        {
            car = Instantiate(carPrefabs[0], spawnPoint.transform.position,
                spawnPoint.transform.rotation, carsContainer.transform);
            car.GetComponent<CarAI>().trafficSystem = ts;
            car.GetComponent<CarAI>().castRays = obstacleDetection;
        }
        if (segmentationSetup != null)
            segmentationSetup.SetupGameObjectSegmentation(car);
        cars.AddLast(car);
    }

    // Removes cars that are out of range
    public void CheckCars()
    {
        float distance;
        List<GameObject> carsToDestroy = new List<GameObject>();

        // collect cars that go out of range
        foreach (GameObject car in cars)
        {
            if (ts.agent != null)
                distance = Vector3.Distance(ts.agent.transform.position, car.transform.position);
            else
                distance = 0;

            if (distance > maxDistance)
                carsToDestroy.Add(car);
        }

        // destroy them (updates list in OnDestroy)
        foreach(GameObject car in carsToDestroy)
            Destroy(car);
    }
}
