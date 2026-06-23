using UnityEngine;

public class Coin : MonoBehaviour
{
    // int coinvalue = 1
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Coin Berhasil Diambil");

            Destroy(gameObject);
        }
    }
}
