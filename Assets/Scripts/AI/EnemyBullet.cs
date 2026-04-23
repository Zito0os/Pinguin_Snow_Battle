using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject,3);  
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // destruir bala de enemigo al colisionar con el jugador
            Destroy(gameObject);
        }
    }
}
