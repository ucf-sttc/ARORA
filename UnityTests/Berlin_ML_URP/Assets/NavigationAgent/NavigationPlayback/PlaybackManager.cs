using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using UnityEngine.SceneManagement;

public class PlaybackManager : MonoBehaviour
{
    readonly int i_ep = 0, i_step = 1, i_spl = 2, i_time = 3, i_pos = 4, i_vel = 7, i_rot = 10, i_goal = 14;
    readonly int max_rate = 10;
    public GameObject agentPrefab, goalPrefab;
    GameObject m_agent, m_goal;
    List<List<Step>> steps;
    FollowAgent fa;
    DynamicSceneLoader dsl;
    AttributeDisplayController adc;
    int m_currentEpisode, m_currentStep, m_rate, m_epOffset = 0;
    float delaySeconds;
    bool isPaused = true, isLoaded = false; // isLoaded gets set true for the first time a scene is loaded and never changed after
    bool m_attributeEnabled, m_roadsCoversEnabled = false;
    public Camera mainCamera;
    public GameObject canvasGO, placeholderGO, sensorCanvasGO;
    [HideInInspector]
    public GameObject controlsGO, debugGO;
    VCRControl vcrControl;

    struct Step
    {
        public int episode, num;
        public Vector3 pos, vel, goal;
        public Quaternion rot;
        public double spl, time;

        public Step(int episode, int num, double spl, double time, Vector3 pos, Vector3 vel, Quaternion rot, Vector3 goal)
        {
            this.episode = episode;
            this.num = num;
            this.spl = spl;
            this.time = time;
            this.pos = pos;
            this.vel = vel;
            this.rot = rot;
            this.goal = goal;
        }
    }

    void Awake()
    {
        m_agent = Instantiate(agentPrefab, this.transform);
        m_agent.GetComponent<Rigidbody>().isKinematic = true;
        m_goal = Instantiate(goalPrefab, this.transform);

        dsl = FindObjectOfType<DynamicSceneLoader>();
        if (dsl)
        {
            dsl.agent = m_agent;
            dsl.isPlayback = true;
            dsl.roadCoversEnabled = m_roadsCoversEnabled;
        }

        adc = FindObjectOfType<AttributeDisplayController>();
        if (adc)
            m_attributeEnabled = adc.attributeEnabled;

        fa = FindObjectOfType<FollowAgent>();
        if (fa)
        {
            fa.target = m_agent.transform.Find("LookAt");
            fa.keysEnabled = false;
        }

        if (canvasGO)
        {
            vcrControl = canvasGO.GetComponent<VCRControl>();
            controlsGO = canvasGO.transform.Find("Controls").gameObject;
            debugGO = canvasGO.transform.Find("Debug").gameObject;
            ToggleDebug();
        }

        AttributeDisplayController ads = FindObjectOfType<AttributeDisplayController>();
        if (ads != null)
            ads.target = m_agent;
    }

    private void Start()
    {
        Time.timeScale = 1;
        Debug.Log(delaySeconds = Time.fixedDeltaTime);
        dsl.range = 3;
        //vcrControl.setRadius(dsl.range); // set tile radius input box
    }

    private void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (isLoaded)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                    vcrControl.StartBnClick();
                else if (Input.GetKey(KeyCode.RightArrow) && isPaused)
                    LoadNextStep();
                else if (Input.GetKey(KeyCode.LeftArrow) && isPaused)
                    LoadPrevStep();
                else if (Input.GetKeyDown(KeyCode.T) && fa)
                    fa.Toggle();
                else if (Input.GetKeyDown(KeyCode.R) && fa)
                    fa.Reset();
                else if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.H))
                    ToggleCanvas();
                else if (Input.GetKeyDown(KeyCode.O))
                    ToggleSensorCanvas();
                else if (Input.GetKeyDown(KeyCode.S))
                    ToggleDebug();
                else if (Input.GetKeyDown(KeyCode.V))
                    ToggleRoadCovers();
                /*
                else if (Input.GetKeyDown("e"))
                    IncreaseRate();
                else if (Input.GetKeyDown("q"))
                    DecreaseRate();
                */
            }
        }
    }

    public void ToggleRoadCovers()
    {
        m_roadsCoversEnabled = !m_roadsCoversEnabled;
        dsl.roadCoversEnabled = m_roadsCoversEnabled; // update dynamic scene loader state

        for(int i = 0; i < SceneManager.sceneCount; i++)
        {
            GameObject[] rootObjs = SceneManager.GetSceneAt(i).GetRootGameObjects();
            if(rootObjs != null && rootObjs.Length > 0)
            {
                Transform roadCovers = rootObjs[0].transform.Find("Road");
                if(roadCovers)
                    roadCovers.gameObject.SetActive(m_roadsCoversEnabled);
            }
        }
    }

    public void LoadFile(string filename)
    {
        m_currentEpisode = 0;
        m_currentStep = 0;
        m_rate = 1;
        ReadCSV(filename);
    }

    [ContextMenu("ReadCSV")]
    bool ReadCSV(string filename)
    {
        try
        {
            StreamReader reader = new StreamReader(filename);
            StartCoroutine(LoadCSV(reader));
        }
        catch (FileNotFoundException)
        {
            vcrControl.OnLoadError("Could not find file", isLoaded);
            return false;
        }
        return true;
    }

    IEnumerator LoadCSV(StreamReader reader)
    {
        vcrControl.setStatusNote("Reading CSV...");
        yield return new WaitForFixedUpdate(); // let the UI update before starting load
        steps = new List<List<Step>>(); // steps stored in 2D lists, outer list contains episodes, inner lists contain steps
        List<Step> episode = new List<Step>();

        int ep = -1;
        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();
            if (line == null) continue;

            string[] vals = line.Split(',');
            if (vals == null || vals.Length < 12 || !int.TryParse(vals[0], out int result)) continue; // skip non-numerical or short lines

            Step a = new Step(int.Parse(vals[i_ep]), int.Parse(vals[i_step]), double.Parse(vals[i_spl]), double.Parse(vals[i_time]),
                new Vector3(float.Parse(vals[i_pos]), float.Parse(vals[i_pos + 1]), float.Parse(vals[i_pos + 2])),
                new Vector3(float.Parse(vals[i_vel]), float.Parse(vals[i_vel + 1]), float.Parse(vals[i_vel + 2])),
                new Quaternion(float.Parse(vals[i_rot]), float.Parse(vals[i_rot + 1]), float.Parse(vals[i_rot + 2]), float.Parse(vals[i_rot + 3])),
                new Vector3(float.Parse(vals[i_goal]), float.Parse(vals[i_goal + 1]), float.Parse(vals[i_goal + 2])));
            //DebugStep(a);

            // start new list of steps for each episode
            if (a.episode != ep)
            {
                ep = a.episode;
                episode = new List<Step>();
                steps.Add(episode);
            }

            episode.Add(a);
        }
        m_epOffset = steps[0][0].episode;
        Debug.Log("Num of eps read: " + steps.Count);

        yield return StartCoroutine(LoadEpisode(m_currentEpisode));
        reader.Close();
    }

    public IEnumerator Play()
    {
        if (m_rate == 0)
        {
            vcrControl.ResetPlayBn();
            yield break;
        }

        isPaused = false;
        if(m_rate > 0)
            while(!isPaused && LoadNextStep(m_rate)) { yield return new WaitForSecondsRealtime(delaySeconds); }
        else if(m_rate < 0)
            while(!isPaused && LoadPrevStep(-m_rate)) { yield return new WaitForSecondsRealtime(delaySeconds); }
    }

    public void Pause()
    {
        isPaused = true;
        vcrControl.ResetPlayBn();
    }

    // textbox input tile radius
    public void SetLoadRadius(int r)
    {
        if (r < 0 || r > 20)
        {
            vcrControl.setRadius(dsl.range);
            vcrControl.setErrorNote("0 <= radius <= 20");
            return;
        }

        dsl.range = r;
    }

    // slider control tile radius
    public void ChangeLoadRadius(int r)
    {
        if (!isLoaded)
            dsl.range = r;
        else
            dsl.UpdateRadius(r);
    }

    public bool LoadNextStep(int delta = 1)
    {
        if(m_currentStep == steps[m_currentEpisode].Count-1)
        {
            Debug.Log("Already reached final step");
            Pause();
            return false;
        }
        else if(m_currentStep + delta >= steps[m_currentEpisode].Count) // if delta > remaining steps
            m_currentStep = steps[m_currentEpisode].Count-1;
        else
            m_currentStep += delta;

        LoadStep(m_currentEpisode, m_currentStep);
        return true;
    }

    public bool LoadPrevStep(int delta = 1)
    {
        if(m_currentStep == 0)
        {
            Debug.Log("Already at first step");
            Pause();
            return false;
        }
        else if(m_currentStep - delta < 0) // if delta > remaining steps
            m_currentStep = 0;
        else
            m_currentStep -= delta;

        LoadStep(m_currentEpisode, m_currentStep);
        return true;
    }

    public void IncreaseRate()
    {
        if (m_rate < max_rate) m_rate++;
        if (m_rate == 0) Pause();
        vcrControl.setCurrentPlayRate(m_rate);
    }
    
    public void DecreaseRate()
    {
        if (m_rate > -max_rate) m_rate--;
        if (m_rate == 0) Pause();
        vcrControl.setCurrentPlayRate(m_rate);
    }

    IEnumerator LoadEpisode(int ep)
    {
        Pause();
        if (ep < 0 || ep >= steps.Count)
        {
            vcrControl.OnLoadError($"Invalid episode number [1,{steps.Count}]");
            vcrControl.setEpisodeNum(m_currentEpisode + m_epOffset);
            yield break;
        }
        m_currentEpisode = ep;
        m_currentStep = 0;
        LoadStep(m_currentEpisode, m_currentStep);

        vcrControl.setStatusNote("Loading scene...");
        vcrControl.setEpisodeNum(m_currentEpisode + m_epOffset);
        placeholderGO.SetActive(false);

        if (dsl)
            yield return StartCoroutine(dsl.LoadInitialTiles());

        isLoaded = true;
        vcrControl.OnLoad();
        vcrControl.setCurrentPlayRate(m_rate);
    }

    bool LoadStep(int ep, int step)
    {
        if(step < 0 || step >= steps[ep].Count)
        {
            vcrControl.OnLoadError("Invalid step number");
            vcrControl.setStepNum(m_currentStep);
            return false;
        }
        m_currentEpisode = ep;
        m_currentStep = step;

        Step cur = steps[ep][step];
        m_agent.transform.SetPositionAndRotation(cur.pos, cur.rot);
        m_goal.transform.SetPositionAndRotation(cur.goal, Quaternion.identity);

        vcrControl.setStepNum(cur.num);
        vcrControl.setTimeMS(cur.time);
        vcrControl.setSPL(cur.spl);
        vcrControl.setDebugInfo(cur.pos, cur.rot.eulerAngles, cur.vel, cur.goal);

        //DebugStep(cur);
        return true;
    }

    public void LoadStep(int step) {
        bool success = LoadStep(m_currentEpisode, step);
        if (success) vcrControl.ClearNote();
    }

    void ToggleCanvas() {
        controlsGO.SetActive(!controlsGO.activeSelf);
    }
    void ToggleSensorCanvas() {
        bool toggleBool = !sensorCanvasGO.activeSelf;
        sensorCanvasGO.SetActive(toggleBool);
        mainCamera.gameObject.SetActive(!toggleBool);
        controlsGO.SetActive(!toggleBool);

        if (toggleBool) // if enabling sensor view, save attribute state
        {
            m_attributeEnabled = adc.attributeEnabled;
            adc.SetAttributeEnabled(false);
        }
        else
            adc.SetAttributeEnabled(m_attributeEnabled);
    }

    void ToggleDebug() {
        debugGO.SetActive(!debugGO.activeSelf);
    }

    public void OnPlay() {
        if (isPaused && isLoaded) StartCoroutine(Play());
    }

    public void OnStepF() {
        if (isPaused && isLoaded) LoadNextStep();
    }

    public void OnStepR() {
        if (isPaused && isLoaded) LoadPrevStep();
    }

    public void OnLoadEp(int ep)
    {
        StartCoroutine(LoadEpisodeCRStarter(ep - m_epOffset));
    }

    IEnumerator LoadEpisodeCRStarter(int ep)
    {
        vcrControl.setStatusNote("Loading episode...");
        yield return new WaitForFixedUpdate(); // let the UI update before starting load
        yield return new WaitForSecondsRealtime(0.1f); // hack to let UI update immediately; a better method probably exists
        yield return StartCoroutine(LoadEpisode(ep));
    }

    void DebugStep(Step a) {
        Debug.LogFormat("{0}: {1}.{2} - {3} - {4} - {5} - {6}", a.time.ToString("F5"), a.episode, a.num, a.spl.ToString("F14"), a.pos, a.rot.ToString("F6"), a.goal);
    }
}
