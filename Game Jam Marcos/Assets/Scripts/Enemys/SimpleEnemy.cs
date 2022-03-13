using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SimpleEnemy : MonoBehaviourPunCallbacks, IDamageable
{
    [Header("-----Values")]
    [SerializeField] private float m_maxVelocity;
    [SerializeField] private float m_acceleration;
    [SerializeField] private float m_inicialLife;
    [SerializeField] private float m_jumpImpulse;

    [Header("-----References")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private ParticleSystem systemParticle;
    [SerializeField] private AudioSource audioS;
    [SerializeField] private BoxCollider2D box;

    [SerializeField] private SpriteRenderer sr;

    private int m_direction;
    private float m_currentLife;

    private Rigidbody2D rb;

    private void Awake()
    {
        m_currentLife = m_inicialLife;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        healthBar.InitializeBar(m_inicialLife);
        m_direction = FindObjectOfType<PlayerController>().transform.position.x > transform.position.x ? 1 : -1;
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (transform.position.x > 25) m_direction = -1;
        else if (transform.position.x < -25) m_direction = 1;

        photonView.RPC("RotateEnemyForEveryone", RpcTarget.All, rb.velocity.x < 0f);
    }

    [PunRPC]
    private void RotateEnemyForEveryone(bool flip)
    {
        sr.flipX = !flip;
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
        m_direction = 0;
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(2f);
        PhotonNetwork.Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!PhotonNetwork.IsMasterClient || m_currentLife <= 0) return;

        rb.AddForce(new Vector2(m_direction * m_acceleration, 0f), ForceMode2D.Impulse);

        if (rb.velocity.x > m_maxVelocity) rb.velocity = new Vector2(m_maxVelocity, rb.velocity.y);
        else if (rb.velocity.x < -m_maxVelocity) rb.velocity = new Vector2(-m_maxVelocity, rb.velocity.y);

        if (Mathf.Abs(rb.velocity.x) < 0.4f && rb.velocity.y < .2f) rb.AddForce(new Vector2(0, m_jumpImpulse/3), ForceMode2D.Impulse);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (PhotonNetwork.IsMasterClient && collision.gameObject.layer == 6) rb.AddForce(new Vector2(0, m_jumpImpulse), ForceMode2D.Impulse);
    }
}
