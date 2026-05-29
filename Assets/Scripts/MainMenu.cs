using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private TextMeshProUGUI  statusText;

    private void Start()
    {
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopHost();
            NetworkManager.singleton.StopClient();
        }
    }

    public void OnPlayButton()
    {
        statusText.text = "Starting...";
        NetworkManager.singleton.StartHost();
    }

    public void OnJoinButton()
    {
        string address = string.IsNullOrEmpty(addressInput.text.Trim())
            ? "localhost"
            : addressInput.text.Trim();

        statusText.text = "Connecting...";
        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.StartClient();
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}