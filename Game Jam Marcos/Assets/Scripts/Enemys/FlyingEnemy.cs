using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FlyingEnemy : MonoBehaviourPunCallbacks, IDamageable
{
    [Header("-----Values")]
    [SerializeField] private float m_maxVelocity;
    [SerializeField] private float m_acceleration;
    [SerializeField] private float m_inicialLife;

    [Header("-----References")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private ParticleSystem systemParticle;
    [SerializeField] private AudioSource audioS;
    [SerializeField] private BoxCollider2D box;

    [SerializeField] private SpriteRenderer sr;

    private float m_currentLife;

    private Rigidbody2D rb;
    private Transform targetPlayer;

    private void Awake()
    {
        m_currentLife = m_inicialLife;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        healthBar.InitializeBar(m_inicialLife);
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (targetPlayer == null)
        {
            var playersList = FindObjectsOfType<PlayerController>();
            targetPlayer = playersList[Random.Range(0, playersList.Length)].transform;
        }

        photonView.RPC("RotateEnemyForEveryone", RpcTarget.All, rb.velocity.x < 0f);
    }

    [PunRPC]
    private void RotateEnemyForEveryone(bool flip)
    {
        sr.flipX = flip;
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
            photonView.RPC("EnemeyDeadForEveryone", RpcTarget.AllBuffered);
            healthBar.UpdateHelthBar(0f);
            StartCoroutine(DestroyAfterTime());
        }
    }

    [PunRPC]
    private void EnemeyDeadForEveryone()
    {
        sr.color = Vector4.zero;
        audioS.Play();
        systemParticle.Play();
        box.enabled = false;
        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(2f);
        PhotonNetwork.Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!PhotonNetwork.IsMasterClient || m_currentLife <= 0) return;

        if (targetPlayer != null) rb.AddForce(new Vector2((targetPlayer.position - transform.position).normalized.x * m_acceleration, (targetPlayer.position - transform.position).normalized.y * m_acceleration), ForceMode2D.Impulse);

        if (rb.velocity.x > m_maxVelocity) rb.velocity = new Vector2(m_maxVelocity, rb.velocity.y);
        else if (rb.velocity.x < -m_maxVelocity) rb.velocity = new Vector2(-m_maxVelocity, rb.velocity.y);
        if (rb.velocity.y > m_maxVelocity) rb.velocity = new Vector2(rb.velocity.x, m_maxVelocity);
        else if (rb.velocity.y < -m_maxVelocity) rb.velocity = new Vector2(rb.velocity.x, -m_maxVelocity);
    }
}
