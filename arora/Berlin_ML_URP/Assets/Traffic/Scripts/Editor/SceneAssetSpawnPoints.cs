/*
 * Context actions when selecting scenes, performs setup and cleanup to traffic SpawnPoint objects in each scene
 * This was created to aid testing and development, not necessary for project cohesion
 */

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneAssetSpawnPoints
{
    // looks for "SpawnPoints" parent object and modifies all children to have "SpawnPoint" tag, saves scene
    [MenuItem("Assets/TagSpawnPoints")]
    private static void SetTags()
    {
        foreach (SceneAsset sceneAsset in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            var scene = EditorSceneManager.OpenScene(path);
            var rootGOs = scene.GetRootGameObjects();

            foreach(GameObject go in rootGOs)
            {
                Transform trans = go.transform.Find("SpawnPoints");
                if(trans)
                {
                    foreach (Transform child in trans)
                    {
                        child.tag = "SpawnPoint";
                        child.gameObject.AddComponent<SpawnPointComponent>();
                    }
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("Modified " + path);
                }
            }
        }
    }

    // deletes "SpawnPoints" GameObject in each selected scene
    [MenuItem("Assets/DeleteSpawnPoints")]
    private static void DeleteSpawnPoints()
    {
        foreach (SceneAsset sceneAsset in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            var scene = EditorSceneManager.OpenScene(path);
            var rootGOs = scene.GetRootGameObjects();

            foreach (GameObject go in rootGOs)
            {
                Transform trans = go.transform.Find("SpawnPoints");
                if (trans)
                {
                    GameObject.DestroyImmediate(trans.gameObject);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("Modified " + path);
                }
            }
        }
    }

    // make sure selection is a SceneAsset for both methods
    [MenuItem("Assets/TagSpawnPoints", true)]
    [MenuItem("Assets/DeleteSpawnPoints", true)]
    private static bool OptionValidation()
    {
        if (!Selection.activeObject) return false;
        return Selection.activeObject.GetType() == typeof(SceneAsset);
    }
}
