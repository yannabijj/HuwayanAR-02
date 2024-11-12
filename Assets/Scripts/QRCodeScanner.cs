using UnityEngine;
using UnityEngine.UI;
using ZXing;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.AI;
using ZXing.Common;

public class QRCodeScanner : MonoBehaviour
{
    public RawImage scanZone;
    public Button qrCodeButton;
    public GameObject navTargetObject;
    public Camera TopDownCamera;
    public LineRenderer line;
    public TMP_Dropdown destinationDropdown;
    public TMP_InputField searchInputField;
    public GameObject qrCodeOverlay;

    private WebCamTexture camTexture;
    private bool scanning = false;
    private NavMeshPath path;
    private bool lineToggle = false;
    private Vector3 userStartPosition;
    private string apiUrl = "http://localhost:8080/unityAR/getTargetCube.php";

    [System.Serializable]
    public class DestinationResponse
    {
        public List<string> destinations;
    }

    [System.Serializable]
    public class PositionData
    {
        public float x;
        public float y;
        public float z;
    }

    void Start()
    {
        qrCodeButton.onClick.AddListener(StartScanning);
        scanZone.gameObject.SetActive(false);
        destinationDropdown.gameObject.SetActive(false);
        searchInputField.gameObject.SetActive(false);
        path = new NavMeshPath();
        line.enabled = false;
        qrCodeOverlay.SetActive(false);

        userStartPosition = Camera.main.transform.position;

        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchInputChanged);
        }
        else
        {
            Debug.LogError("Search Input Field is not assigned.");
        }
    }

    void StartScanning()
    {
        scanZone.gameObject.SetActive(true);
        camTexture = new WebCamTexture();
        scanZone.texture = camTexture;
        camTexture.Play();
        scanning = true;
        StartCoroutine(ScanForQRCode());
        qrCodeOverlay.SetActive(true);
    }

    IEnumerator ScanForQRCode()
    {
        var barcodeReader = new BarcodeReader { AutoRotate = true, Options = new DecodingOptions { TryHarder = true } };

        while (scanning)
        {
            var snap = new Texture2D(camTexture.width, camTexture.height);
            snap.SetPixels32(camTexture.GetPixels32());
            var result = barcodeReader.Decode(snap.GetPixels32(), camTexture.width, camTexture.height);

            if (result != null && result.Text == "DEST_MENU")
            {
                searchInputField.gameObject.SetActive(true);
                destinationDropdown.gameObject.SetActive(true);
                scanning = false;
                camTexture.Stop();
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    public void OnSearchInputChanged(string inputText)
    {
        if (!string.IsNullOrEmpty(inputText))
        {
            StartCoroutine(FetchFilteredDestinationsFromDatabase(inputText));
        }
        else
        {
            destinationDropdown.ClearOptions();
        }
    }

    IEnumerator FetchFilteredDestinationsFromDatabase(string query)
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "?search=" + UnityWebRequest.EscapeURL(query));
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            DestinationResponse response = JsonUtility.FromJson<DestinationResponse>("{\"destinations\":" + request.downloadHandler.text + "}");
            destinationDropdown.ClearOptions();
            destinationDropdown.AddOptions(response.destinations);
            destinationDropdown.onValueChanged.AddListener(OnDestinationSelected);
        }
        else
        {
            Debug.LogError("Error fetching destinations: " + request.error);
        }
    }

    void OnDestinationSelected(int index)
    {
        string selectedDestination = destinationDropdown.options[index].text;
        StartCoroutine(GetTargetPosition(selectedDestination));
    }

    IEnumerator GetTargetPosition(string destination)
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "?destination=" + UnityWebRequest.EscapeURL(destination));
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            PositionData targetPosition = JsonUtility.FromJson<PositionData>(request.downloadHandler.text);
            ShowNavigationLine(new Vector3(targetPosition.x, targetPosition.y, targetPosition.z));
        }
        else
        {
            Debug.LogError("Error fetching target position: " + request.error);
        }
    }

    void ShowNavigationLine(Vector3 targetPosition)
    {
        navTargetObject.transform.position = targetPosition;

        lineToggle = !lineToggle;

        if (lineToggle)
        {
            NavMesh.CalculatePath(userStartPosition, targetPosition, NavMesh.AllAreas, path);
            line.positionCount = path.corners.Length;
            line.SetPositions(path.corners);
            line.enabled = true;

            if (TopDownCamera != null)
            {
                TopDownCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y + 10, targetPosition.z - 10);
                TopDownCamera.transform.LookAt(targetPosition);
            }
        }
        else
        {
            line.enabled = false;
        }
    }
}
