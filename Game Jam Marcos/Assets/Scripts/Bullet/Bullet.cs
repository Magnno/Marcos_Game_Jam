using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviourPunCallbacks
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (photonView.IsMine && (collision.gameObject.layer == 7 || collision.gameObject.layer == 6)) PhotonNetwork.Destroy(gameObject);

        if (PhotonNetwork.IsMasterClient && collision.CompareTag("Enemy") && collision.gameObject.layer != 10)
        {
            if (PhotonNetwork.IsMasterClient) collision.GetComponentInParent<IDamageable>().Damage(5);
            if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
        }
    }
}
