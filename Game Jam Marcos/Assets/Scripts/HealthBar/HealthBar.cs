using UnityEngine;
using Photon.Pun;

public class HealthBar : MonoBehaviourPunCallbacks
{
    [SerializeField] private Gradient m_barGradient;
    private float m_maxLife;

    private Transform barTransform;
    private SpriteRenderer sr;

    private void Awake()
    {
        barTransform = transform.GetChild(1);
        sr = barTransform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    public void InitializeBar(float maxLife)
    {
        photonView.RPC("InitializeBarForEveryone", RpcTarget.AllBuffered, maxLife);
    }

    [PunRPC]
    public void InitializeBarForEveryone(float maxLife)
    {
        m_maxLife = maxLife;
        barTransform.localScale = new Vector3(1, 1, 1);
        sr.color = m_barGradient.Evaluate(1);
    }

    public void UpdateHelthBar(float life)
    {
        photonView.RPC("UpdateHealthBarForEverybody", RpcTarget.AllBuffered, life);
        //_photonView.RPC("UpdateHealthBarForEverybody", RpcTarget.AllBuffered, life);
    }

    [PunRPC]
    private void UpdateHealthBarForEverybody(float life)
    {
        barTransform.localScale = new Vector3(life / m_maxLife, 1, 1);
        sr.color = m_barGradient.Evaluate(life / m_maxLife);
    }
}
