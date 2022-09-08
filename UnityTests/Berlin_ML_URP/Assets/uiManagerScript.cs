using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class uiManagerScript : MonoBehaviour
{
    public bool isReturned, moving, clickedDown;
    public float lerpSpeed, lerpVal, counter, mouseOffsetY, ellipsesCounter;
    public RectTransform box, sidePanel, importMenu, geotiffMenu;
    private Vector3 cornerPos = new Vector3(340, -256, 0), hiddenPos = new Vector3(340, -300, 0), targetPos, startPos;
    [TextArea]
    public List<string> dialogueText = new List<string>();
    private int ellipsesCount, currentTextIndex;
    public TextMeshProUGUI staticTextField, dialogueTextField;
    public GameObject mapPreviewModel;
    public bool menuOpen;
    public Text fpscountertext;
    private bool initialLoad=true;
    // Start is called before the first frame update
    void Start()
    {
        mouseOffsetY = Screen.width * 0.052f;
        
    }

    // Update is called once per frame
    void Update()
    {

        fpscountertext.text = (1f / Time.unscaledDeltaTime).ToString();
        //FOR DEBUGGING ONLY
        if (Input.GetKeyDown(KeyCode.C))
        {
            revealDialogueBox();//FOR DEBUGGING SIMULATED REST SERVER INCOMING MESSAGE

        }
        //END FOR DEBUGGING ONLY

        //adding ellipses to dialogue box every second
        ellipsesCounter += Time.deltaTime;
        if (ellipsesCounter >= 1.0f)
        {
            ellipsesCounter = 0;
            ellipsesCount++;
            if (ellipsesCount == 3)
                ellipsesCount = 0;
            dialogueTextField.text = dialogueText[currentTextIndex];
            for (int i = 0; i <= ellipsesCount; i++)
                dialogueTextField.text += ".";
        }
        //done adding ellipses

        if (moving)//this is to move the dialogue box to target position from start position
        {
            lerpVal += lerpSpeed * Time.deltaTime;
            box.anchoredPosition = Vector3.Lerp(startPos, targetPos, lerpVal);
            if (lerpVal >= 1)
            {
                lerpVal = 0;
                moving = false;

            }

        }

        if (clickedDown)
        {
            counter += Time.deltaTime;
            box.transform.position = Input.mousePosition - new Vector3(0, mouseOffsetY, 0);
            if (Input.GetMouseButtonUp(0))
            {
                clickedDown = false;
                if (counter < 0.25f)
                {
                    startPos = box.anchoredPosition;
                    targetPos = Vector3.zero;
                    moving = true;
                }
                counter = 0;

            }
        }
    }


    public void clickOnBanner() {
        mouseOffsetY = Screen.width * 0.052f;
        clickedDown = true;

    }

    public void clickOnSideBanner()//this is the banner on the left that has debug options
    {
        if (sidePanel.anchoredPosition.x == -300)
        {
            sidePanel.anchoredPosition = new Vector3(-475, 100, 0);
            menuOpen = false;

        }
        else
        {
            sidePanel.anchoredPosition = new Vector3(-300, 100, 0);
            menuOpen = true;


        }


    }

    public void showImportMenu()
    {
        if(!initialLoad)
            importMenu.gameObject.SetActive(true);

        mapPreviewModel.gameObject.SetActive(true);
        clickOnSideBanner();//MODIFIED 9/9
        //menuOpen = true;
        initialLoad = false;
    }


    public void showGeotiffMenu()
    {
        
        geotiffMenu.gameObject.SetActive(true);

        clickOnSideBanner();//MODIFIED 9/9

        //menuOpen = true;

    }
    public void hideImportMenu()
    {
        importMenu.gameObject.SetActive(false);
        mapPreviewModel.gameObject.SetActive(false);
        menuOpen = false;

    }

    public void hideGeotiffMenu()
    {
        geotiffMenu.gameObject.SetActive(false);
        menuOpen = false;

    }

    public void returnDialogueBoxCorner()
    {
        startPos = box.anchoredPosition;
        targetPos = cornerPos;
        moving = true;

    }

    public void revealDialogueBox()
    {
        startPos = box.anchoredPosition;
        targetPos = cornerPos;
        moving = true;
    }

    public void hideDialogueBox()
    {

    }
}
