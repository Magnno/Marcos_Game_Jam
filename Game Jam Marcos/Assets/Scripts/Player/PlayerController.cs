using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;

public sealed class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
    private Rigidbody2D rb;
    private GameManager m_gameManager;

    private int m_playerID;

    [Header("Nickname")]
    [SerializeField] private TextMeshPro nicknameText;

    [Header("-----Health")]
    [SerializeField] private GameObject m_healthBarGO;
    [SerializeField] private HealthBar m_healthBar;
    [SerializeField] private float m_maxLife;
    private int m_currentLifeUps;
    private float m_currentLife;
    private bool m_canTakeDamage = true;

    [Header("-----Shot")]
    [SerializeField] private float m_bulletVelocity;
    [SerializeField] private float m_shotCooldown;
    private float m_currentShotCooldown;

    [Header("-----Horizontal Movement")]
    [SerializeField] private float m_horizontalMaxVelocity;
    [SerializeField] private float m_horizontalAcceleration;
    [SerializeField] private float m_horizontalDeceleration;
    [System.NonSerialized] public bool canMove = true;

    [Header("-----Vertical Movement")]
    [SerializeField] private float m_jumpingMaxVelocity;
    [SerializeField] private float m_jumpingAcceleration;
    [SerializeField] private float m_jumpingTime;
    [SerializeField] private float m_fallMaxVelocity;
    private float m_currentJumpingTime;
    private float m_gravityScale;
    private bool m_isJumping;
    private bool m_jumpReleased;
    private Vector2 m_directionInput;

    [Header("-----Dash")]
    [SerializeField] private float m_dashVelocity;
    [SerializeField] private float m_dashTime;
    private float m_currentDashTime;
    private bool m_isDashing;
    private bool m_hasDashed;

    [Header("-----Check Ground")]
    [SerializeField] private Vector2 m_checkGroundPoint;
    [SerializeField] private Vector2 m_checkGroundSize;
    [SerializeField] private LayerMask m_groundLayer;

    [Header("-----Camera")]
    private Camera mainCamera;

    [Header("-----Audio")]
    [SerializeField] private AudioSource shotAudio;
    [SerializeField] private AudioSource damageAudio;
    [SerializeField] private AudioSource itemAudio;

    [Header("-----Animations")]
    [SerializeField] private SpriteRenderer bodySr;
    [SerializeField] private SpriteRenderer gunSr;
    [SerializeField] private Animator anim;
    [SerializeField] private Animator gunAnim;
    [SerializeField] private Animator dashAnim;

    [Header("-----Weapon")]
    [SerializeField] private Transform aimTransform;
    [SerializeField] private SpriteRenderer weaponSr;
    private float weaponAngle;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        m_gameManager = FindObjectOfType<GameManager>();
        rb = GetComponent<Rigidbody2D>();

        m_currentLife = m_maxLife;
        m_gravityScale = rb.gravityScale;

        m_playerID = photonView.ControllerActorNr;

        if (!photonView.IsMine)
        {
            return;
        }

        m_healthBar.InitializeBar(m_maxLife);

        m_gameManager.thisMachinePlayerID = m_playerID;

        photonView.RPC("SetNickname", RpcTarget.AllBuffered, PhotonNetwork.NickName);

        //m_gameManager.thisMachinePlayerID = m_playerID;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        m_directionInput = MovementDirectionInput();
        ShotInput();
        DashInput();
        AnimatePlayer();
        RotateWeapon();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        rb.AddForce(new Vector2(m_directionInput.x * m_horizontalAcceleration, 0), ForceMode2D.Impulse);
        if (CanJump()) rb.AddForce(new Vector2(0, m_directionInput.y * m_jumpingAcceleration), ForceMode2D.Impulse);
        if (m_directionInput.x == 0 && IsGrounded() && !m_isDashing)
        {
            if (rb.velocity.x > .1f) rb.AddForce(new Vector2(-m_horizontalDeceleration, 0), ForceMode2D.Impulse);
            else if (rb.velocity.x < -.1f) rb.AddForce(new Vector2(m_horizontalDeceleration, 0), ForceMode2D.Impulse);
            else rb.velocity = new Vector2(0, rb.velocity.y);
        }

        if (!m_isDashing)
        {
            if (rb.velocity.x > m_horizontalMaxVelocity) rb.velocity = new Vector2(m_horizontalMaxVelocity, rb.velocity.y);
            else if (rb.velocity.x < -m_horizontalMaxVelocity) rb.velocity = new Vector2(-m_horizontalMaxVelocity, rb.velocity.y);

            if (rb.velocity.y > m_jumpingMaxVelocity) rb.velocity = new Vector2(rb.velocity.x, m_jumpingMaxVelocity);
            else if (rb.velocity.y < -m_fallMaxVelocity) rb.velocity = new Vector2(rb.velocity.x, -m_fallMaxVelocity);
        }
    }

    private Vector2 MovementDirectionInput()
    {
        Vector2 directionInput = Vector2.zero;
        if (!canMove) return directionInput;

        if (Input.GetKey(KeyCode.D)) directionInput = new Vector2(1, 0);
        else if (Input.GetKey(KeyCode.A)) directionInput = new Vector2(-1, 0);

        if (Input.GetKey(KeyCode.Space)) directionInput = new Vector2(directionInput.x, 1);
        else if (Input.GetKey(KeyCode.S)) directionInput = new Vector2(directionInput.x, -1);

        return directionInput;
    }

    private bool CanJump()
    {
        if (!canMove) return false;

        bool isGrounded = IsGrounded();
        if (isGrounded)
        {
            m_jumpReleased = false;
            m_currentJumpingTime = m_jumpingTime;
        }
        else if (!Input.GetKey(KeyCode.Space)) m_jumpReleased = true;

        m_currentJumpingTime = m_currentJumpingTime > 0 ? m_currentJumpingTime -= Time.deltaTime : 0;

        return isGrounded || (!isGrounded && !m_jumpReleased && m_currentJumpingTime > 0 && !m_isDashing);
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapBox(m_checkGroundPoint + (Vector2)transform.position, m_checkGroundSize, 0, m_groundLayer) != null;
    }

    private void DashInput()
    {
        m_currentDashTime = m_currentDashTime > 0 ? m_currentDashTime -= Time.deltaTime : 0f;

        if (m_currentDashTime == 0 && m_isDashing)
        {
            m_isDashing = false;
            rb.gravityScale = m_gravityScale;
            m_hasDashed = true;
        }

        if (!canMove) return;

        if (IsGrounded()) m_hasDashed = false;

        bool canDash = m_currentDashTime == 0 && !m_hasDashed && !m_isDashing;

        if (Input.GetKeyDown(KeyCode.Mouse1) && canDash)
        {
            int yDir = 0;
            if (Input.GetKey(KeyCode.W)) yDir = 1;
            else if (Input.GetKey(KeyCode.W)) yDir = -1;

            m_isDashing = true;
            m_currentDashTime = m_dashTime;
            dashAnim.SetTrigger("Dash");
            rb.AddForce(new Vector2(m_directionInput.x * m_dashVelocity, yDir * m_dashVelocity), ForceMode2D.Impulse);
            rb.gravityScale = 0;
        }
    }

    private void AnimatePlayer()
    {
        if (!canMove) return;

        //bodySr.flipX = GetMousePosition().x < transform.position.x;
        photonView.RPC("RotatePlayerForEveryone", RpcTarget.All, GetMousePosition().x < transform.position.x);
        anim.SetBool("IsRunning", Mathf.Abs(rb.velocity.x) > .1f);
        anim.SetBool("IsGrounded", IsGrounded());
    }

    [PunRPC]
    private void RotatePlayerForEveryone(bool flipBody)
    {
        bodySr.flipX = flipBody;
        gunSr.flipY = aimTransform.eulerAngles.z > 90f && aimTransform.eulerAngles.z < 270f;
    }

    private void RotateWeapon()
    {
        if (!canMove) return;

        weaponAngle = Mathf.Atan2((GetMousePosition() - transform.position).normalized.y, (GetMousePosition() - transform.position).normalized.x) * Mathf.Rad2Deg;
        aimTransform.eulerAngles = new Vector3(0, 0, weaponAngle);
    }

    private Vector3 GetMousePosition()
    {
        return mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    private void ShotInput()
    {
        m_currentShotCooldown = m_currentShotCooldown > 0 ? m_currentShotCooldown -= Time.deltaTime : 0f;

        if (!canMove) return;

        if (m_currentShotCooldown == 0f && Input.GetKeyDown(KeyCode.Mouse0))
        {
            GameObject bulletGO = PhotonNetwork.Instantiate("Bullet", aimTransform.GetChild(0).position, Quaternion.Euler(aimTransform.eulerAngles));
            bulletGO.transform.SetParent(null);
            bulletGO.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector2(m_bulletVelocity, 0), ForceMode2D.Impulse);

            m_currentShotCooldown = m_shotCooldown;
            gunAnim.SetTrigger("Shot");
            photonView.RPC("PlayShotAudioForEverybody", RpcTarget.All);
        }
    }

    [PunRPC]
    private void PlayShotAudioForEverybody()
    {
        shotAudio.Play();
    }

    public void Damage(float damage)
    {
        if (m_currentLife <= 0 || !m_canTakeDamage) return;

        m_currentLife -= damage;

        if (m_currentLife > 0)
        {
            m_canTakeDamage = false;
            m_healthBar.UpdateHelthBar(m_currentLife);
            photonView.RPC("ShowDamageEffectToEveryone", RpcTarget.All);
        }
        else
        {
            m_healthBar.UpdateHelthBar(0f);
            m_gameManager.UpdatePlayersInfo(m_playerID, m_gameManager.playersInfoDictionary[m_playerID].lifeUps - 1);
            m_gameManager.PlayLoseDirector();
            canMove = false;
            anim.SetTrigger("IsDead");

            StartCoroutine(RevivePlayer());
        }
    }

    [PunRPC]
    private void ShowDamageEffectToEveryone()
    {
        StartCoroutine(DamageEffect());
    }

    private IEnumerator DamageEffect()
    {
        damageAudio.Play();

        for (int i = 0; i < 5; i++)
        {
            bodySr.color = new Color(1f, 0f, 0f, 0f);
            yield return new WaitForSeconds(.1f);
            bodySr.color = new Color(1f, 0f, 0f, 1f);
            yield return new WaitForSeconds(.1f);
        }

        bodySr.color = new Color(1f, 1f, 1f, 1f);
        if (photonView.IsMine) m_canTakeDamage = true;
    }

    private IEnumerator RevivePlayer()
    {
        yield return new WaitForSeconds(5f);
        while (m_gameManager.playersInfoDictionary[m_playerID].lifeUps < 0)
        {
            yield return null;
        }
        m_gameManager.InstantiatePlayer();
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void SetNickname(string nickname)
    {
        nicknameText.text = nickname;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (photonView.IsMine && collision.CompareTag("FallColider")) Damage(1000f);

        if (photonView.IsMine && collision.CompareTag("Enemy") && collision.gameObject.name != "MarcosBoss") Damage(10f);

        if (photonView.IsMine && collision.CompareTag("LifeUp"))
        {
            m_dashVelocity = 30f;
            foreach(var player in PhotonNetwork.PlayerList)
            {
                m_gameManager.UpdatePlayersInfo(player.ActorNumber, m_gameManager.playersInfoDictionary[player.ActorNumber].lifeUps + 1);
            }
            photonView.RPC("PlayItemAudioForEveryone", RpcTarget.All);
        }
    }

    [PunRPC]
    private void PlayItemAudioForEveryone()
    {
        itemAudio.Play();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(m_checkGroundPoint + (Vector2)transform.position, m_checkGroundSize);
    }
}
