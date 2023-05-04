using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

#region TempMarker
public class TempMarker : MonoBehaviour
{
    [HideInInspector]
    public int id;
    [HideInInspector]
    public Segment segment;
    public SplitRoad parent;

    public void Init(int newId, Segment newSegment, SplitRoad parent)
    {
        id = newId;
        segment = newSegment;
        tag = "Waypoint";
        this.parent = parent;

        //Set the layer to Default
        gameObject.layer = 0;

        AddCollider();
    }

    public void AddCollider()
    {
        gameObject.AddComponent<SphereCollider>();
    }

    public void RemoveCollider()
    {
        if (GetComponent<SphereCollider>())
        {
            DestroyImmediate(gameObject.GetComponent<SphereCollider>());
        }
    }
}
#endregion

#region SplitRoad
public struct SplitRoad
{
    public int a, b; // segment ID's for resulting split roads
    public Vector3 start, end;
    public Vector3[] vecs;
    public TempMarker[] markers;
    public bool[] done;
    public SplitRoad(int a, int b, Vector3 start, Vector3 end, Vector3 startvec, Vector3 endvec, GameObject tempParent)
    {
        this.a = a; this.b = b; this.start = start; this.end = end;
        vecs = new Vector3[2];
        vecs[0] = startvec; vecs[1] = endvec;
        done = new bool[2];
        done[0] = done[1] = false;
        markers = new TempMarker[2];

        GameObject temp = new GameObject();
        markers[0] = temp.AddComponent<TempMarker>();
        markers[0].transform.position = start;
        markers[0].Init(0, null, this);
        temp.transform.SetParent(tempParent.transform);

        temp = new GameObject();
        markers[1] = temp.AddComponent<TempMarker>();
        markers[1].transform.position = end;
        markers[1].Init(1, null, this);
        temp.transform.SetParent(tempParent.transform);
    }
}
#endregion

// GameObject pathways needs to be set to the GameObject containing all the road lines
public class MakeRoads : Editor
{
    private static TrafficSystem ts;
    private static TrafficCarSpawner tcs;
    //private static TrafficCarSpawnerECS tcsecs;
    private static GameObject pathways;
    private static float thresholdNext = 0.5f;
    private static float thresholdNextAfterSplit = 1.0f; // CSVImporter-manipulated roads have larger gaps between end points
    private static List<SplitRoad> splitRoads;
    private static GameObject tempParent;

    [MenuItem("Component/Traffic/2. Make Roads", false, 1001)]
    private static void GetRoads()
    {
        pathways = GameObject.Find("Road");
        if(!pathways) { Debug.LogError("No Road gameobject found"); return; }

        GameObject trafficGO = GameObject.Find("Traffic System");
        ts = trafficGO.GetComponent<TrafficSystem>();
        splitRoads = new List<SplitRoad>();
        tempParent = new GameObject("Delete");

        Debug.Log("Creating roads...");
        // operating on each line in the pathways prefab
        foreach (Transform child in pathways.transform)
        {
            if (child.GetComponent<AttributeClass>()) // do single road
            {
                CreateRoad(child);
            }
            else if (IsAcceptableRoadType(child.GetChild(0))) // do split roads
            {
                int a = CreateRoad(child.GetChild(1), true);
                int b = CreateRoad(child.GetChild(2));

                LineRenderer original = child.GetChild(0).GetComponent<LineRenderer>();
                Vector3 start = original.GetPosition(0);
                Vector3 end = original.GetPosition(original.positionCount - 1);
                Vector3 startvec = start - original.GetPosition(1);
                Vector3 endvec = end - original.GetPosition(original.positionCount - 2);

                splitRoads.Add(new SplitRoad(a, b, start, end, startvec, endvec, tempParent));
            }
        }

        Debug.Log("Adjusting roads...");
        foreach (SplitRoad sr in splitRoads)
            AdjustSegments(sr);

        splitRoads.Clear();
        DestroyImmediate(tempParent);

        Debug.Log("Connecting roads...");
        foreach (Segment sg in ts.segments)
            AddNextSegments(sg);

        ClearColliders();
        Debug.Log("Finished building roads");
    }

    private static void AdjustSegments(SplitRoad sr)
    {
        // check for waypoints at the start of the segment
        Collider[] hits = Physics.OverlapSphere(sr.start, thresholdNext);
        List<Waypoint> wps = new List<Waypoint>();
        List<TempMarker> markers = new List<TempMarker>();
        foreach (Collider c in hits)
        {
            if (c.gameObject.GetComponent<Waypoint>())
                wps.Add(c.gameObject.GetComponent<Waypoint>());
            else if(c.gameObject.GetComponent<TempMarker>())
                markers.Add(c.gameObject.GetComponent<TempMarker>());
        }

        // fix intersections that have been split
        if (wps.Count == 0 && (markers.Count == 4 || markers.Count == 3 || markers.Count == 2)) // four-way & three-way intersections
        {
            AdjustSegmentsOnlySplit(markers);
        }
        else // all other cases
        {
            AdjustSegment(sr.a, sr.b, sr.start);
        }

        // check for waypoints at the end of the segment
        hits = Physics.OverlapSphere(sr.end, thresholdNext);
        wps.Clear();
        markers.Clear();
        foreach (Collider c in hits)
        {
            if (c.gameObject.GetComponent<Waypoint>())
                wps.Add(c.gameObject.GetComponent<Waypoint>());
            else if (c.gameObject.GetComponent<TempMarker>())
                markers.Add(c.gameObject.GetComponent<TempMarker>());
        }

        if (wps.Count == 0 && (markers.Count == 4 || markers.Count == 3 || markers.Count == 2)) // four-way & three-way intersections
        {
            AdjustSegmentsOnlySplit(markers);
        }
        else // all other cases
        {
            AdjustSegment(sr.b, sr.a, sr.end);
        }
    }

    private static void AdjustSegmentsOnlySplit(List<TempMarker> markers)
    {
        if (markers[0].parent.done[markers[0].id]) return;

        List<SRAngle> srangles = new List<SRAngle>();
        int[] markerID = new int[markers.Count];
        SplitRoad[] srs = new SplitRoad[markers.Count];
        for (int i = 0; i < markers.Count; i++) // organize data for convenience
        {
            markerID[i] = markers[i].id;
            srs[i] = markers[i].parent;
        }

        for (int i = 0; i < markers.Count - 1; i++)
        {
            float angle = Vector3.SignedAngle(-srs[0].vecs[markerID[0]], -srs[i + 1].vecs[markerID[i + 1]], Vector3.up);
            if (angle < 0) angle += 360;
            srangles.Add(new SRAngle(i, angle));
        }

        // sort angles and prepare array of results for operation
        srangles.Sort((p1, p2) => p2.angle.CompareTo(p1.angle)); // descending

        int[] roadIndices = new int[markers.Count];
        roadIndices[0] = 0;
        for (int i = 0; i < markers.Count - 1; i++)
            roadIndices[i + 1] = srangles[i].index + 1;

        // align the road segments ends to make four corners
        for (int i = 0; i < markers.Count; i++)
        {
            SplitRoad first = srs[roadIndices[i]];
            SplitRoad second = srs[roadIndices[(i + 1) % markers.Count]];

            Segment firstSeg, secondSeg;
            if (markerID[roadIndices[i]] == 0) // operating on start of segment
                firstSeg = ts.segments[first.a];
            else // operating on end of segment
                firstSeg = ts.segments[first.b];

            if (markerID[roadIndices[(i + 1) % markers.Count]] == 0)
                secondSeg = ts.segments[second.b];
            else
                secondSeg = ts.segments[second.a];

            if (markers.Count == 2)
            {
                Vector3 avgPos = (firstSeg.waypoints[firstSeg.waypoints.Count - 1].transform.position - secondSeg.waypoints[0].transform.position) / 2;
                firstSeg.waypoints[firstSeg.waypoints.Count - 1].transform.position -= avgPos;
                secondSeg.waypoints[0].transform.position += avgPos;
            }
            else
            {
                // choose closest point on firstSeg to be intersection and adjust each segment appropriately
                Vector3 intersection = ClosestPointsOnTwoLines(firstSeg.waypoints[firstSeg.waypoints.Count - 1].transform.position,
                                    firstSeg.waypoints[firstSeg.waypoints.Count - 1].transform.position - firstSeg.waypoints[firstSeg.waypoints.Count - 2].transform.position,
                                    secondSeg.waypoints[0].transform.position,
                                    secondSeg.waypoints[1].transform.position - secondSeg.waypoints[0].transform.position);
                firstSeg.waypoints[firstSeg.waypoints.Count - 1].transform.position = intersection;
                secondSeg.waypoints[0].transform.position = intersection;
            }

            // mark these ends as completed
            for (int j = 0; j < markers.Count; j++)
                srs[j].done[markerID[j]] = true;
        }
    }

    struct SRAngle
    {
        public float angle;
        public int index;
        public SRAngle(int index, float angle) { this.index = index; this.angle = angle; }
    }

    public static Vector3 ClosestPointsOnTwoLines(Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        Vector3 closestPoint = Vector3.zero;

        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        float d = a * e - b * b;

        //lines are not parallel
        if (d != 0.0f)
        {

            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPoint = linePoint1 + lineVec1 * s;
        }

        return closestPoint;
    }

    private static void AdjustSegment(int id1, int id2, Vector3 pos)
    {
        Segment road1 = ts.segments[id1];
        Segment road2 = ts.segments[id2];

        // align paths at one end of the split road
        Collider[] hits = Physics.OverlapSphere(pos, thresholdNext);
        List<Waypoint> wps = new List<Waypoint>();

        foreach (Collider c in hits)
        {
            if (c.CompareTag("Waypoint") && c.GetComponent<Waypoint>())
                wps.Add(c.gameObject.GetComponent<Waypoint>());
        }
        foreach(Waypoint wp in wps)
        {
            Vector3 poswp1 = road1.waypoints[road1.waypoints.Count - 1].transform.position;
            Vector3 poswp2 = road2.waypoints[0].transform.position;
            if (wp.id == 0 && wp.segment.id != id2) // the connecting road is outbound
            {
                Vector3 poswpnext = wp.segment.waypoints[1].transform.position;
                if (Vector3.Distance(poswpnext, poswp2) < Vector3.Distance(poswpnext, poswp1))
                {
                    wp.transform.position = poswp2;
                }
                else
                {
                    wp.transform.position = poswp1;
                }
            }
            else if (wp.id == wp.segment.waypoints.Count - 1 && wp.segment.id != id1) // the connecting road is inbound
            {
                Vector3 poswpback = wp.segment.waypoints[wp.id - 1].transform.position;
                if (Vector3.Distance(poswpback, poswp1) < Vector3.Distance(poswpback, poswp2))
                {
                    wp.segment.waypoints[wp.segment.waypoints.Count - 1].transform.position = poswp1;
                    Vector3 line1 = poswp1 - road1.waypoints[road1.waypoints.Count-2].transform.position;
                    Vector3 line2 = poswp1 - wp.segment.waypoints[wp.segment.waypoints.Count - 2].transform.position;
                    if (Vector3.Angle(line1, line2) > 70)// || wps.Count < 3)
                        wp.segment.waypoints[wp.segment.waypoints.Count - 1].transform.position = poswp2;
                }
                else
                {
                    wp.segment.waypoints[wp.segment.waypoints.Count - 1].transform.position = poswp2;
                    Vector3 line1 = road2.waypoints[1].transform.position - poswp2;
                    Vector3 line2 = poswp2 - wp.segment.waypoints[wp.segment.waypoints.Count - 2].transform.position;
                    if(Vector3.Angle(line1, line2) > 135)
                        wp.segment.waypoints[wp.segment.waypoints.Count - 1].transform.position = poswp1;
                }
            }
        }
    }

    private static int CreateRoad(Transform road)
    { return CreateRoad(road, false); }

    private static int CreateRoad(Transform road, bool reverse)
    {
        int segId = -1;
        // use only certain road designations
        if (IsAcceptableRoadType(road))
        {
            LineRenderer line = road.gameObject.GetComponent<LineRenderer>();
            Vector3[] lines = new Vector3[line.positionCount];
            line.GetPositions(lines);

            if(reverse) Array.Reverse(lines);

            //string name = attr.GetValueForKey("name");
            //if (name.ToLower().Contains("tunnel")) continue;

            Vector3 offset = pathways.transform.position;
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    segId = AddSegment(lines[i] + offset);
                    AddWaypoint(lines[i] + offset);
                }
                else
                    AddWaypoint(lines[i] + offset);
            }
        }
        return segId;
    }

    private static void AddNextSegments(Segment sg)
    {
        List<Segment> nextSegs = new List<Segment>();
        Waypoint wp = sg.waypoints[sg.waypoints.Count - 1];
        Waypoint wp2;
        Vector3 poswp = wp.transform.position;

        Collider[] hits = Physics.OverlapSphere(poswp, thresholdNextAfterSplit);
        foreach (Collider c in hits)
        {
            if (c.CompareTag("Waypoint") && c.gameObject != wp.gameObject)
            {
                wp2 = c.gameObject.GetComponent<Waypoint>();
                if(wp2 && wp2.id == 0)
                    nextSegs.Add(c.gameObject.GetComponent<Waypoint>().segment);
            }
        }

        SortAndAddNextSegments(sg, nextSegs);
    }

    private static void SortAndAddNextSegments(Segment sg, List<Segment> nextSegs)
    {
        if (nextSegs.Count == 0) return;
        if (nextSegs.Count == 1)
        {
            sg.nextSegments.Add(nextSegs[0]);
            return;
        }

        // sort possible next segments so that a right-turn is the first option
        Vector3 line0 = sg.waypoints[sg.waypoints.Count - 2].transform.position
                        - sg.waypoints[sg.waypoints.Count - 1].transform.position;
        List<PathAngle> paths = new List<PathAngle>();
        foreach (Segment sg3 in nextSegs)
        {
            Vector3 line1 = sg3.waypoints[1].transform.position - sg3.waypoints[0].transform.position;
            float angle = Vector3.SignedAngle(line0, line1, Vector3.up);
            if (angle < 0) angle += 360;
            paths.Add(new PathAngle(sg3, angle));
        }

        paths.Sort((p1, p2) => p2.angle.CompareTo(p1.angle)); // descending

        foreach (PathAngle p in paths)
            sg.nextSegments.Add(p.seg);
    }

    struct PathAngle
    {
        public float angle;
        public Segment seg;
        public PathAngle(Segment seg, float angle) { this.seg = seg; this.angle = angle; }
    }

    #region Make Spawn Points
    [MenuItem("Component/Traffic/3. Make Points", false, 1002)]
    private static void GetSpawnLocations()
    {
        GameObject trafficGO = GameObject.Find("Traffic System");
        ts = trafficGO.GetComponent<TrafficSystem>();
        tcs = trafficGO.GetComponent<TrafficCarSpawner>();
        //tcsecs = trafficGO.GetComponent<TrafficCarSpawnerECS>();

        GameObject spawnPointsGO = trafficGO.transform.Find("SpawnPoints").gameObject;

        List<GameObject> tempSpawnPoints = new List<GameObject>();
        foreach (Segment sg in ts.segments)
        {
            Vector3 wp0, wp1, direction;
            wp0 = sg.waypoints[0].transform.position;
            if (sg.waypoints.Count < 3
                || Vector3.Distance(wp0, sg.waypoints[sg.waypoints.Count - 1].transform.position) < 40)
                continue; // don't place cars on segments that are short or have few waypoints
            for (int i = 1; i < sg.waypoints.Count - 1; i++)
            {
                wp0 = sg.waypoints[i-1].transform.position;
                wp1 = sg.waypoints[i].transform.position;
                direction = wp1 - wp0;
                if (direction.magnitude < 10) continue; // if this point is too close to previous, skip

                if (Physics.Raycast(wp1 + Vector3.up, Vector3.down, out RaycastHit hit)
                    && hit.collider.gameObject.CompareTag("ground"))
                {
                    GameObject spawnPoint = new GameObject("SpawnPoint");
                    spawnPoint.transform.position = hit.point;
                    spawnPoint.transform.rotation = Quaternion.LookRotation(direction);
                    spawnPoint.transform.parent = spawnPointsGO.transform;
                    spawnPoint.tag = "SpawnPoint";
                    spawnPoint.AddComponent(typeof(SpawnPointComponent));
                    SpawnPointComponent spc = spawnPoint.GetComponent<SpawnPointComponent>();
                    spc.seg = sg.id;
                    spc.wp = i;

                    tempSpawnPoints.Add(spawnPoint);
                }
            }
        }

        tcs.spawnPoints = tempSpawnPoints;
        //tcsecs.spawnPoints = tempSpawnPoints.ToArray();
    }
    #endregion

    private static void AddWaypoint(Vector3 position)
    {
        GameObject go = EditorHelper.CreateGameObject("Waypoint-" + ts.curSegment.waypoints.Count, ts.curSegment.transform);
        go.transform.position = position;

        Waypoint wp = EditorHelper.AddComponent<Waypoint>(go);
        wp.Init(ts.curSegment.waypoints.Count, ts.curSegment);

        //Record changes to the TrafficSystem (string not relevant here)
        Undo.RecordObject(ts.curSegment, "");
        ts.curSegment.waypoints.Add(wp);
    }

    private static int AddSegment(Vector3 position)
    {
        int segId = ts.segments.Count;
        GameObject segGo = EditorHelper.CreateGameObject("Segment-" + segId, ts.transform.GetChild(0).transform);
        segGo.transform.position = position;

        ts.curSegment = EditorHelper.AddComponent<Segment>(segGo);
        ts.curSegment.id = segId;
        ts.curSegment.waypoints = new List<Waypoint>();
        ts.curSegment.nextSegments = new List<Segment>();

        //Record changes to the TrafficSystem (string not relevant here)
        Undo.RecordObject(ts, "");
        ts.segments.Add(ts.curSegment);

        return segId;
    }

    private static void ClearColliders()
    {
        foreach(Segment sg in ts.segments)
            foreach (Waypoint wp in sg.waypoints)
                wp.RemoveCollider();
    }

    private static bool IsAcceptableRoadType(Transform trans)
    {
        AttributeClass attr = trans.GetComponent<AttributeClass>();

        if (!attr) return false;

        String roadType = attr.GetValueForKey("highway");

        return roadType.Equals("primary")
            || roadType.Equals("secondary")
            || roadType.Equals("tertiary")
            || roadType.Equals("residential");
    }
}