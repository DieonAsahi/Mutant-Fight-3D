using UnityEngine;

public class HealthCube : MonoBehaviour
{
    [Header("Movement Animation")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatHeight = 0.15f;

    private Vector3 startPosition;

    void Start()
    {
        // Menyimpan posisi awal agar rotasi dan efek melayang tidak melenceng
        startPosition = transform.position;
    }

    void Update()
    {
        // Animasi berputar di tempat ala item koin Subway Surfers
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // Animasi melayang naik-turun halus agar menarik perhatian player
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatusManager playerStatus = other.GetComponent<PlayerStatusManager>();

            if (playerStatus != null)
            {
                // Memanggil fungsi baru yang aman untuk mengisi darah dan mengupdate UI
                playerStatus.HealToFull();

                Debug.Log("Kotak Medkit berhasil memicu pemulihan penuh pada player.");
            }

            // Hancurkan kotak medkit dari map
            Destroy(gameObject);
        }
    }
}