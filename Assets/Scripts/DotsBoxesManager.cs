using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/**
DOTS BOXES MANAGER:
- used for starting the server/client/host
- UI for host button, client button, and server button based on tutorial
- host must start before client joins
**/

public class DotsBoxesManager : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button clientButton;
    public Button serverButton;

    private bool buttonsDisabled = false;

    void OnEnable()
    {
        if (hostButton != null) hostButton.onClick.AddListener(OnHostButtonClicked);
        if (clientButton != null) clientButton.onClick.AddListener(OnClientButtonClicked);
        if (serverButton != null) serverButton.onClick.AddListener(OnServerButtonClicked);
    }

    void OnDisable()
    {
        if (hostButton != null) hostButton.onClick.RemoveListener(OnHostButtonClicked);
        if (clientButton != null) clientButton.onClick.RemoveListener(OnClientButtonClicked);
        if (serverButton != null) serverButton.onClick.RemoveListener(OnServerButtonClicked);
    }

    void OnHostButtonClicked()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.StartHost();
            GameManager.Instance.CreateBoard(); // start host and create board
    }

    void OnClientButtonClicked()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    void OnServerButtonClicked()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.StartServer();
    }

    void Update()
    {
        // disable start buttons once connected
        if (!buttonsDisabled && NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            if (hostButton != null) hostButton.gameObject.SetActive(false);
            if (clientButton != null) clientButton.gameObject.SetActive(false);
            if (serverButton != null) serverButton.gameObject.SetActive(false);
            buttonsDisabled = true;
        }
    }
}