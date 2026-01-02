using UnityEngine;

public class Collectible : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(Vector3.up * 90f * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}
