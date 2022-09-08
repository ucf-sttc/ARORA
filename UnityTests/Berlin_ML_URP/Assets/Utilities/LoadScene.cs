using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    public string[] sceneNames;
    // Start is called before the first frame update
    void Start()
    {
        foreach (string sceneName in sceneNames)
        {
            bool sceneAlreadyLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).name == sceneName)
                {
                    sceneAlreadyLoaded = true;
                    break;
                }
            if(!sceneAlreadyLoaded)    
                SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
            
    }
}
