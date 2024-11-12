using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class TargetDataFetcher : MonoBehaviour
{
    public string apiUrl = "http://localhost:8080/unityAR/getTargetCube.php"; // Replace with your API URL

    // Define a structure to hold the target data
    [System.Serializable]
    public class TargetData
    {
        public int id;
        public string name;
        public float x_position;
        public float y_position;
        public float z_position;
    }

    [System.Serializable]
    public class TargetDataList
    {
        public List<TargetData> targets;
    }

    // Start fetching data
    public void StartFetching()
    {
        StartCoroutine(FetchTargetData());
    }

    IEnumerator FetchTargetData()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            TargetDataList targetDataList = JsonUtility.FromJson<TargetDataList>("{\"targets\":" + json + "}");

            // Use target data here, e.g., create target objects in Unity
            foreach (var target in targetDataList.targets)
            {
                Debug.Log($"Target: {target.name} at ({target.x_position}, {target.y_position}, {target.z_position})");
                // Here, instantiate target cubes or assign data to existing cubes
            }
        }
        else
        {
            Debug.LogError("Error fetching target data: " + request.error);
        }
    }
}
