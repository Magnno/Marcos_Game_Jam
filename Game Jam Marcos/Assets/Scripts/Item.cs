using UnityEngine;
using Photon.Pun;

public class Item : MonoBehaviourPunCallbacks
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (photonView.IsMine && collision.CompareTag("Player")) PhotonNetwork.Destroy(gameObject);
    }
}
