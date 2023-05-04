using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class mmscript : MonoBehaviour
{
    public GameObject loading;
    // Start is called before the first frame update
    void Start()
    {
        loading = GameObject.Find("loading");

        DontDestroyOnLoad(this.gameObject);
        DontDestroyOnLoad(GameObject.Find("Canvas"));
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Use a coroutine to load the Scene in the background
            IEnumerator c = LoadYourAsyncScene(1);
            StartCoroutine(c);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Use a coroutine to load the Scene in the background
            IEnumerator c = LoadYourAsyncScene(2);
            StartCoroutine(c);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Use a coroutine to load the Scene in the background
            IEnumerator c = LoadYourAsyncScene(3);
            StartCoroutine(c);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // Use a coroutine to load the Scene in the background
            IEnumerator c = LoadYourAsyncScene(4);
            StartCoroutine(c);
        }
    }

    IEnumerator LoadYourAsyncScene(int index)
    {
        yield return null;
        // The Application loads the Scene in the background as the current Scene runs.
        // This is particularly good for creating loading screens.
        // You could also load the Scene by using sceneBuildIndex. In this case Scene2 has
        // a sceneBuildIndex of 1 as shown in Build Settings.
        AsyncOperation asyncOperation=null;
        switch (index)
        {
            case 1:
                asyncOperation = SceneManager.LoadSceneAsync("100");
                break;
            case 2:
                asyncOperation = SceneManager.LoadSceneAsync("200");
                break;
            case 3:
                asyncOperation = SceneManager.LoadSceneAsync("300");
                break;
            case 4:
                asyncOperation = SceneManager.LoadSceneAsync("400");
                break;

        }
        //Don't let the Scene activate until you allow it to
        asyncOperation.allowSceneActivation = false;
        Debug.Log("Pro :" + asyncOperation.progress);
        //When the load is still in progress, output the Text and progress bar
        while (!asyncOperation.isDone)
        {
            //Output the current progress
            if(!asyncOperation.allowSceneActivation)
                loading.GetComponent<Text>().text = "Loading progress: " + (asyncOperation.progress * 100) + "%";
            else
                loading.GetComponent<Text>().text = "Finishing up: " + (asyncOperation.progress * 100) + "%";


            // Check if the load has finished
            if (asyncOperation.progress >= 0.9f)
            {
                //Change the Text to show the Scene is ready
                if(!asyncOperation.allowSceneActivation)
                    loading.GetComponent<Text>().text = "Press the space bar to continue";
                //Wait to you press the space key to activate the Scene
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    //Activate the Scene
                    asyncOperation.allowSceneActivation = true;

                }
                    
            }

            yield return null;
        }
        loading.GetComponent<Text>().text = "Scene " + index + ". Press 1,2,3 or 4 to begin loading scene";

        // Wait until the asynchronous scene fully loads

    }


    public void startButton()
    {

        SceneManager.LoadScene("buildScene");

    }
}
