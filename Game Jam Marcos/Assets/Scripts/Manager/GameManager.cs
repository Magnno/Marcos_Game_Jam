using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;

public class GameManager : MonoBehaviourPunCallbacks
{
    public Dictionary<int, PlayersInfo> playersInfoDictionary = new();

    [System.NonSerialized] public int thisMachinePlayerID;

    [Header("-----Start Points")]
    [SerializeField] private Vector2[] m_startPoints;
    [SerializeField] private Vector2[] m_enemysPoints;
    [SerializeField] private Vector2[] m_dropLifePoints;

    [Header("-----Spawn")]
    [SerializeField] private float m_maxEnemys;
    [SerializeField] private Vector2 m_spawnTime;
    [SerializeField] private Vector2 m_dropLifeTime;

    [Header("-----Timelines")]
    [SerializeField] private PlayableDirector gameOverDirector;
    [SerializeField] private PlayableDirector bossDirector;
    [SerializeField] private PlayableDirector victoryDirector;
    [SerializeField] private PlayableDirector loseDirector;

    [Header("-----UI Elements")]
    [SerializeField] private TextMeshProUGUI lifeUpsText;

    [Header("-----Audio")]
    [SerializeField] private AudioSource mainThemeAudio;

    [Header("-----Boss")]
    [SerializeField] private GameObject bossGO;
    [SerializeField] private GameObject bossSrGO;
    [SerializeField] private float m_bossTime;

    private bool m_isBossTime;

    private void Start()
    {
        bossGO.SetActive(false);
    }

    public void StartGame()
    {
        mainThemeAudio.Play();

        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.PlayerList.Length > 1) m_spawnTime = new Vector2(m_spawnTime.x / 2f, m_spawnTime.y/2f);

            StartCoroutine(SpawnEnemys());
            StartCoroutine(SpawnLifeUps());
            StartCoroutine(BattleTime());
            photonView.RPC("InstantiatePlayer", RpcTarget.AllBuffered);
        }

        foreach (var player in PhotonNetwork.PlayerList)
        {
            playersInfoDictionary.Add(player.ActorNumber, new PlayersInfo(1));
        }
        AdjustSettingsAfterChangePlayersInfo();
    }

    private IEnumerator SpawnEnemys()
    {
        yield return new WaitForSeconds(Random.Range(m_spawnTime.x, m_spawnTime.y));
        var enemysList = GameObject.FindGameObjectsWithTag("Enemy");
        if ((enemysList.Length < m_maxEnemys && PhotonNetwork.PlayerList.Length > 1) || (PhotonNetwork.PlayerList.Length == 1 && enemysList.Length < 5)) PhotonNetwork.Instantiate($"Enemy{Random.Range(0, 4)}", m_enemysPoints[UnityEngine.Random.Range(0, m_enemysPoints.Length)], Quaternion.identity);
        if (!m_isBossTime) StartCoroutine(SpawnEnemys());
    }

    private IEnumerator SpawnLifeUps()
    {
        yield return new WaitForSeconds(Random.Range(m_dropLifeTime.x, m_dropLifeTime.y));
        PhotonNetwork.Instantiate("LifeUps", m_dropLifePoints[UnityEngine.Random.Range(0, m_dropLifePoints.Length)], Quaternion.identity);
        StartCoroutine(SpawnLifeUps());
    }

    private IEnumerator BattleTime()
    {
        yield return new WaitForSeconds(m_bossTime);
        m_isBossTime = true;
        photonView.RPC("PlayBossDirectorForEveryone", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void InstantiatePlayer()
    {
        GameObject playerGO = PhotonNetwork.Instantiate("PlayerController", m_startPoints[UnityEngine.Random.Range(0, m_startPoints.Length)], Quaternion.identity);
        thisMachinePlayerID = playerGO.GetComponent<PhotonView>().ControllerActorNr;
    }

    public void StartBattle()
    {
        bossSrGO.SetActive(false);
        bossGO.SetActive(true);
    }

    #region Update Players Info
    public void UpdatePlayersInfo(int id, int lifeUps) // Jogador envia uma socilicitação para alterar as informações dos jogadores
    {
        photonView.RPC("UpdatePlayersInfoMasterClient", RpcTarget.MasterClient, id, lifeUps);
    }

    [PunRPC]
    private void UpdatePlayersInfoMasterClient(int id, int lifeUps) // Master Client altera o dicionário de informações dos jogadores
    {
        playersInfoDictionary[id].lifeUps = lifeUps;
        photonView.RPC("SynchronizePlayersInfo", RpcTarget.OthersBuffered, id, lifeUps);
        AdjustSettingsAfterChangePlayersInfo();
    }

    [PunRPC] // Master Client envia a alteração para todos os jogadores
    private void SynchronizePlayersInfo(int id, int lifeUps)
    {
        playersInfoDictionary[id].lifeUps = lifeUps;
        AdjustSettingsAfterChangePlayersInfo();
    }

    private void AdjustSettingsAfterChangePlayersInfo()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            bool gameOver = true;
            foreach (var info in playersInfoDictionary)
            {
                if (info.Value.lifeUps > -1) gameOver = false;
            }
            if (gameOver) photonView.RPC("PlayGameOverDirectorForEveryone", RpcTarget.AllBuffered);
        }

        if (thisMachinePlayerID == 0) lifeUpsText.text = playersInfoDictionary[1].lifeUps.ToString();
        else if (playersInfoDictionary[thisMachinePlayerID].lifeUps > -1) lifeUpsText.text = playersInfoDictionary[thisMachinePlayerID].lifeUps.ToString();
    }
    #endregion

    #region Playables
    public void PlayVictoryDirector()
    {
        if (PhotonNetwork.IsMasterClient) photonView.RPC("PlayVictoryDirectorForEveryone", RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void PlayVictoryDirectorForEveryone()
    {
        victoryDirector.Play();
    }

    public void PlayLoseDirector()
    {
        loseDirector.Play();
    }

    [PunRPC]
    private void PlayGameOverDirectorForEveryone()
    {
        bossDirector.Pause();
        gameOverDirector.Play();
    }

    [PunRPC]
    private void PlayBossDirectorForEveryone()
    {
        bossDirector.Play();
    }
    #endregion

    #region Finish Game For Everyone
    public void FinishGame()
    {
        if (PhotonNetwork.IsMasterClient) photonView.RPC("FinishGameForEveryone", RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void FinishGameForEveryone()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(0);
    }
    #endregion
}

[System.Serializable]
public class PlayersInfo
{
    public int lifeUps;

    public PlayersInfo(int lifeUps)
    {
        this.lifeUps = lifeUps;
    }
}
