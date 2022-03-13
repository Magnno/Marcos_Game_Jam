using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;

public class MenuManager : MonoBehaviourPunCallbacks
{
    [Header("-----Pages")]
    [SerializeField] private GameObject m_page0;
    [SerializeField] private GameObject m_page1;

    [Header("-----Texts")]
    [SerializeField] private TextMeshProUGUI m_connectedText;
    [SerializeField] private TMP_InputField m_playerNameText;
    [SerializeField] private TMP_InputField m_roomNameText;
    [SerializeField] private TextMeshProUGUI m_playerListText;

    [Header("-----Buttons")]
    [SerializeField] private Button m_StartBTN;

    private void Awake()
    {
        StartCoroutine(ConnectToServer());
    }

    private IEnumerator ConnectToServer()
    {
        while (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            yield return new WaitForSeconds(1f);
        }
    }

    public void HostRoom()
    {
        PhotonNetwork.NickName = m_playerNameText.text;

        if (string.IsNullOrEmpty(m_playerNameText.text) || string.IsNullOrEmpty(m_roomNameText.text)) return;

        PhotonNetwork.CreateRoom(m_roomNameText.text);
    }

    public void ConnectToRoom()
    {
        PhotonNetwork.NickName = m_playerNameText.text;

        if (string.IsNullOrEmpty(m_playerNameText.text) || string.IsNullOrEmpty(m_roomNameText.text)) return;

        PhotonNetwork.JoinRoom(m_roomNameText.text);
    }

    public void StartGame()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;

        PhotonNetwork.LoadLevel(1);

        print("COMEÇAR O JOGO!");
    }

    public void DisconnectRoom()
    {
        PhotonNetwork.LeaveRoom();
        SwitchPage(m_page0);
    }

    public void SwitchPage(GameObject page)
    {
        m_page0.SetActive(false);
        m_page1.SetActive(false);

        page.SetActive(true);
    }

    private void UpdatePlayersList()
    {
        m_playerListText.text = string.Empty;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (string.IsNullOrEmpty(m_playerListText.text)) m_playerListText.text = player.NickName;
            else m_playerListText.text = $"{m_playerListText.text}; {player.NickName}";
        }

        print("ATUALIZOU A LISTA DE JOGADORES");
    }

    public override void OnConnected()
    {
        base.OnConnected();

        m_connectedText.color = Color.green;
        m_connectedText.text = "CONECTADO AO SERVIDOR";
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        base.OnDisconnected(cause);

        m_connectedText.color = new Color(1f, 255f/189f, 0f);
        m_connectedText.text = "DESCONECTADO DO SERVIDOR";
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();

        print("SALA CRIADA!");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);

        print($"SALA NÃO CRIADA! --> {message}");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        SwitchPage(m_page1);
        UpdatePlayersList();
        m_StartBTN.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        PhotonNetwork.AutomaticallySyncScene = true;

        print("ENTROU NA SALA!");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);

        print($"NÃO ENTROU NA SALA! --> {message}");
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        print("SAIU DA SALA!");
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        UpdatePlayersList();
        print("JOGADOR ENTROU NA SALA!");
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        m_StartBTN.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        UpdatePlayersList();
        print("JOGADOR SAIU DA SALA!");
    }
}
