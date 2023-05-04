using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
//using SFB; // testing SimpleFileBrowser instead
using SimpleFileBrowser;

public class VCRControl : MonoBehaviour
{
    [System.Serializable] public class _UnityEventString : UnityEvent<string> { }
    [System.Serializable] public class _UnityEventInt : UnityEvent<int> { }
    [System.Serializable] public class _UnityEventBool : UnityEvent<bool> { }
    [Header("Event based VCR style control. Add listeners here")]
    public UnityEvent Stop;
    public UnityEvent Play;
    [Header("File Load listener, passes string")]
    public _UnityEventString LoadReq;
    [Header("Episode Load listener, passes int")]
    public _UnityEventInt EpisodeJump;
    [Header("Step Load listener, passes int")]
    public _UnityEventInt StepJump;
    public _UnityEventInt TileRadius, AttributeRadius;
    public _UnityEventBool AttributeDisplayEvent, AttributeZoom;
    public UnityEvent FF, Rew;
    public UnityEvent StepPlus, StepMinus;

    public Button m_LoadBn, m_OpenBn, m_StartButton, m_FFButton, m_RewButton, m_StepFwdButton, m_StepBWButton;
    public Sprite PlaySprite, PauseSprite;

    public InputField m_FileInputField, m_EpisodeInputField, m_StepInputField, m_RadiusInputField; 
    public Toggle m_attributeToggle, m_attributeZoomToggle;
    public Slider m_radiusSlider, m_attributeSlider;
    public Text m_PlayRateText, m_TimeMSText, m_SPLText;
    public Text m_FPSText, m_PositionText, m_RotationText, m_VelocityText, m_GoalText;
    public Text m_ErrorText, m_StatusText;

    //ExtensionFilter[] m_vecObsFilters = new[] {
    //    new ExtensionFilter("Vector Observations", "csv"), new ExtensionFilter("All Files", "*" ),
    //};
    float deltaTime = 0;

    void Start()
    {
        //Calls the TaskOnClick/TaskWithParameters/ButtonClicked method when you click the Button
        m_LoadBn.onClick.AddListener(LoadBnClick);
        m_OpenBn.onClick.AddListener(OpenBnClick);
        m_StartButton.onClick.AddListener(StartBnClick);
        //m_EpisodeInputField.onEndEdit.AddListener(delegate { EpisodeInput(m_EpisodeInputField); });
        m_RadiusInputField.onEndEdit.AddListener(delegate { RadiusInput(m_RadiusInputField); });
        m_FFButton.onClick.AddListener(FFBnClick);
        m_RewButton.onClick.AddListener(RewBnClick);
        m_StepFwdButton.onClick.AddListener(StepFWBnClick);
        m_StepBWButton.onClick.AddListener(StepBWBnClick);
        m_radiusSlider.onValueChanged.AddListener(RadiusSliderChange);
        m_attributeSlider.onValueChanged.AddListener(AttributeSliderChange);
        m_attributeToggle.onValueChanged.AddListener(AttributeToggleChange);
        m_attributeZoomToggle.onValueChanged.AddListener(AttributeZoomToggleChange);

        DisableControls();

        var fileFilter = new FileBrowser.Filter("Vector Observations", ".csv");
        FileBrowser.SetFilters(true, fileFilter);
        FileBrowser.SetDefaultFilter(".csv");
    }

    private void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1f / deltaTime;
        m_FPSText.text = Mathf.Ceil(fps).ToString();
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if(e.isKey && e.type == EventType.KeyUp && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            if (EventSystem.current.currentSelectedGameObject == m_FileInputField.gameObject)
                LoadBnClick();
            else if (EventSystem.current.currentSelectedGameObject == m_EpisodeInputField.gameObject)
                LoadEpBnClick();
            else if (EventSystem.current.currentSelectedGameObject == m_StepInputField.gameObject)
                LoadStepBnClick();
            else if (EventSystem.current.currentSelectedGameObject == m_RadiusInputField.gameObject)
                RadiusBnClick();
        }
    }

    #region Button click actions
    void OpenBnClick()
    {
        /*
        var paths = StandaloneFileBrowser.OpenFilePanel("Open Vector Observations File", Application.dataPath, m_vecObsFilters, false);
        if (paths.Length > 0 && paths[0].Length > 0)
            m_FileInputField.text = paths[0];
            //Debug.Log(paths[0]);
        */
        FileBrowser.ShowLoadDialog((paths) => {
                if (paths.Length > 0 && paths[0].Length > 0) m_FileInputField.text = paths[0];
            }, () => { }, FileBrowser.PickMode.Files);
    }

    void LoadBnClick()
    {
        if (!m_LoadBn.interactable) return; // use button state to determine if we can load
        if (m_FileInputField.text.Length == 0) return;

        DisableLoad();
        DisableControls();
        LoadReq.Invoke(m_FileInputField.text);
    }

    void LoadEpBnClick()
    {
        if (!m_LoadBn.interactable) return;
        if (m_EpisodeInputField.text.Length == 0) return;

        DisableLoad();
        DisableControls();
        EpisodeJump.Invoke(int.Parse(m_EpisodeInputField.text));
    }

    void LoadStepBnClick()
    {
        if (!m_LoadBn.interactable) return;

        if (m_StepInputField.text.Length > 0)
            StepJump.Invoke(int.Parse(m_StepInputField.text));
    }

    void RadiusBnClick()
    {
        if (!m_LoadBn.interactable) return;

        if (m_RadiusInputField.text.Length > 0)
            TileRadius.Invoke(int.Parse(m_RadiusInputField.text));
    }

    void RadiusSliderChange(float val)
    {
        if (!m_LoadBn.interactable) return;
        TileRadius.Invoke((int)val);
    }

    void AttributeSliderChange(float val)
    {
        AttributeRadius.Invoke((int)val);
    }

    void AttributeToggleChange(bool val)
    {
        AttributeDisplayEvent.Invoke(val);
    }

    void AttributeZoomToggleChange(bool val)
    {
        AttributeZoom.Invoke(val);
    }

    public void StartBnClick()
    {
        if ((m_StartButton.GetComponentInChildren<Text>().text == "Play"))
        {
            m_StartButton.GetComponent<Image>().sprite = PauseSprite;
            m_StartButton.GetComponentInChildren<Text>().text = "Pause";
            Play.Invoke();
        }
        else
        {
            m_StartButton.GetComponent<Image>().sprite = PlaySprite;
            m_StartButton.GetComponentInChildren<Text>().text = "Play";
            Stop.Invoke();
        }
    }

    void FFBnClick()
    {
        FF.Invoke();
    }

    void RewBnClick()
    {
        Rew.Invoke();
    }

    void StepFWBnClick()
    {
        StepPlus.Invoke();
    }

    void StepBWBnClick()
    {
        StepMinus.Invoke();
    }

    void RadiusInput(InputField input)
    {
        RadiusBnClick();
    }

    // Checks if there is anything entered into the input field.
    /*
    void EpisodeInput(InputField input)
    {
        if (input.text.Length > 0)
        {
            Debug.Log("Text has been entered");
            EpisodeJump.Invoke(int.Parse(input.text));
        }
        else if (input.text.Length == 0)
        {
            Debug.Log("Main Input Empty");
        }
    }
    */
    #endregion

    #region Text setters
    public void setCurrentPlayRate(int speed)
    {
        m_PlayRateText.text = speed.ToString();
    }

    public void setEpisodeNum(int currentEpisode)
    {
        m_EpisodeInputField.text = currentEpisode.ToString();
    }

    public void setStepNum(int stepNum)
    {
        m_StepInputField.text = stepNum.ToString();
    }

    public void setRadius(int r)
    {
        m_RadiusInputField.text = r.ToString();
    }

    public void setTimeMS(double ms)
    {
        m_TimeMSText.text = ms.ToString();
    }

    public void setSPL(double spl)
    {
        m_SPLText.text = spl.ToString();
    }

    public void setDebugInfo(Vector3 pos, Vector3 rot, Vector3 vel, Vector3 goal)
    {
        m_PositionText.text = pos.ToString("F4");
        m_RotationText.text = rot.ToString("F4");
        m_VelocityText.text = vel.ToString("F4");
        m_GoalText.text = goal.ToString("F4");
    }

    public void setStatusNote(string statusNote)
    {
        m_StatusText.text = statusNote;
        m_ErrorText.text = "";
    }

    public void setErrorNote(string errorNote)
    {
        m_ErrorText.text = errorNote;
        m_StatusText.text = "";
    }
    #endregion

    #region UI state
    void DisableControls()
    {
        m_StartButton.interactable = false;
        m_RewButton.interactable = false;
        m_FFButton.interactable = false;
        m_StepBWButton.interactable = false;
        m_StepFwdButton.interactable = false;
        m_EpisodeInputField.interactable = false;
        m_StepInputField.interactable = false;
        m_attributeToggle.interactable = false;
    }

    void DisableLoad()
    {
        m_LoadBn.interactable = false;
        m_RadiusInputField.interactable = false;
        m_radiusSlider.interactable = false;
    }

    void EnableControls()
    {
        m_StartButton.interactable = true;
        m_RewButton.interactable = true;
        m_FFButton.interactable = true;
        m_StepBWButton.interactable = true;
        m_StepFwdButton.interactable = true;
        m_EpisodeInputField.interactable = true;
        m_StepInputField.interactable = true;
        m_attributeToggle.interactable = true;
    }

    void EnableLoad()
    {
        m_LoadBn.interactable = true;
        m_RadiusInputField.interactable = true;
        m_radiusSlider.interactable = true;
    }

    public void ResetPlayBn()
    {
        m_StartButton.GetComponent<Image>().sprite = PlaySprite;
        m_StartButton.GetComponentInChildren<Text>().text = "Play";
    }

    public void OnLoad()
    {
        EnableControls();
        EnableLoad();
        setStatusNote("Loading complete");
    }

    public void OnLoadError(string error, bool controls = true)
    {
        if (controls) EnableControls();
        EnableLoad();
        setErrorNote(error);
    }

    public void ClearNote() { setStatusNote(""); }
    #endregion
}
