using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Boss : MonoBehaviourPunCallbacks, IDamageable
{
    [SerializeField] private GameManager gameManager;

    [Header("-----Health")]
    [SerializeField] private float m_inicialHealth;
    [SerializeField] private HealthBar healthBar;
    private float m_currentLife;

    [Header("-----Attack")]
    [SerializeField] private float m_spikesSpawnHeight;
    [SerializeField] private Vector2 m_spikesSpawnArea;
    [SerializeField] private Vector2Int m_spikesSpawnQty;
    [SerializeField] private Vector2 m_handAttackTime;
    [SerializeField] private Vector2 m_spikesAttackTime;
    private bool m_isSpikeAttacking;

    [Header("-----Animations")]
    [SerializeField] private Animator marcosAnim;
    [SerializeField] private Animator leftHandAnim;
    [SerializeField] private Animator rightHandAnim;
    [SerializeField] private SpriteRenderer marcosSr;

    private void Awake()
    {
        if (PhotonNetwork.PlayerList.Length == 1) m_inicialHealth = 500f;
        m_currentLife = m_inicialHealth;
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            healthBar.InitializeBar(m_inicialHealth);

            StartCoroutine(LeftHandAttack());
            StartCoroutine(RightHandAttack());
            StartCoroutine(SpikesAttack());
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (m_isSpikeAttacking)
        {
            marcosAnim.SetBool("Attacking2", true);
        }
        else if (leftHandAnim.GetCurrentAnimatorStateInfo(0).IsName("LeftHand_Idle") && rightHandAnim.GetCurrentAnimatorStateInfo(0).IsName("RightHand_Idle"))
        {
            marcosAnim.SetBool("Attacking1", false);
            marcosAnim.SetBool("Attacking2", false);
        }
        else
        {
            marcosAnim.SetBool("Attacking1", true);
        }
    }

    private IEnumerator LeftHandAttack()
    {
        yield return new WaitForSeconds(Random.Range(m_handAttackTime.x, m_handAttackTime.y));
        while (m_isSpikeAttacking)
        {
            yield return new WaitForSeconds(.5f);
        }
        if (m_currentLife > 0)
        {
            leftHandAnim.SetTrigger($"Move{Random.Range(0, 2)}");
            StartCoroutine(LeftHandAttack());
        }
    }

    private IEnumerator RightHandAttack()
    {
        yield return new WaitForSeconds(Random.Range(m_handAttackTime.x, m_handAttackTime.y));
        while (m_isSpikeAttacking)
        {
            yield return new WaitForSeconds(.5f);
        }
        if (m_currentLife > 0)
        {
            rightHandAnim.SetTrigger($"Move{Random.Range(0, 2)}");
            StartCoroutine(RightHandAttack());
        }
    }

    private IEnumerator SpikesAttack()
    {
        yield return new WaitForSeconds(Random.Range(m_spikesAttackTime.x, m_spikesAttackTime.y));
        m_isSpikeAttacking = true;
        SpawnSpikes();
        yield return new WaitForSeconds(2f);
        m_isSpikeAttacking = false;
        if (m_currentLife > 0)
        {
            StartCoroutine(SpikesAttack());
        }
    }

    public void Damage(float damage)
    {
        if (m_currentLife <= 0) return;

        m_currentLife -= damage;

        if (m_currentLife > 0)
        {
            healthBar.UpdateHelthBar(m_currentLife);
        }
        else
        {
            marcosAnim.SetTrigger("Dead");
            healthBar.UpdateHelthBar(0);
            gameManager.PlayVictoryDirector();
        }
    }

    private void SpawnSpikes()
    {
        int spikesQty = Random.Range(m_spikesSpawnQty.x, m_spikesSpawnQty.y + 1);
        for (int i = 0; i < spikesQty; i++)
        {
            GameObject spikeGO = PhotonNetwork.Instantiate("Spike", new Vector3(Random.Range(m_spikesSpawnArea.x, m_spikesSpawnArea.y), m_spikesSpawnHeight, 0), Quaternion.identity);
            StartCoroutine(DestroySpike(spikeGO));
        }
    }

    private IEnumerator DestroySpike(GameObject spikeGO)
    {
        yield return new WaitForSeconds(5f);
        PhotonNetwork.Destroy(spikeGO);
    }
}
