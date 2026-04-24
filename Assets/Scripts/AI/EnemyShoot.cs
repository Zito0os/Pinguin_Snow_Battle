using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    public GameObject enemyBullet;
    public Transform sapwnBulletPoint;
    [Header("Sonido disparo enemigo")]
    public AudioClip sonidoDisparoEnemigo;
    [Range(0f, 1f)] public float volumenDisparoEnemigo = 1f;
    public AudioSource audioSourceDisparo;

    private Transform playerPosition;
    public float bulletVelocity = 100f;
    public float shootInterval = 3f;
    [Header("Delay de ataque")]
    public float delayAntesPrimerDisparo = 2f;
    public float alturaObjetivoJugador = 1.2f;

    private bool canShoot = false;
    private float nextShootTime = 0f;



    void Start()
    {
        if (audioSourceDisparo == null)
        {
            audioSourceDisparo = GetComponent<AudioSource>();
        }

        PlayerInteractions player = FindObjectOfType<PlayerInteractions>();
        if (player != null)
        {
            playerPosition = player.transform;
        }
    }

    
    void Update()
    {
        if (!canShoot)
            return;

        if (Time.time < nextShootTime)
            return;

        ShootPlayer();
        nextShootTime = Time.time + shootInterval;
    }

    void ShootPlayer()
    {
        if (enemyBullet == null || sapwnBulletPoint == null || playerPosition == null)
            return;

        Vector3 puntoObjetivo = playerPosition.position + Vector3.up * alturaObjetivoJugador;
        Vector3 playerDirection = (puntoObjetivo - sapwnBulletPoint.position).normalized;

        if (playerDirection.sqrMagnitude <= 0.0001f)
            return;

        GameObject newbullet;

        Quaternion bulletRotation = Quaternion.LookRotation(playerDirection, Vector3.up);
        newbullet = Instantiate(enemyBullet, sapwnBulletPoint.position, bulletRotation);

        Rigidbody bulletRb = newbullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = playerDirection * bulletVelocity;
        }

        ReproducirSonidoDisparo();
    }

    public void SetShooting(bool shooting)
    {
        if (canShoot == shooting)
            return;

        canShoot = shooting;
        if (canShoot)
        {
            nextShootTime = Time.time + Mathf.Max(0f, delayAntesPrimerDisparo);
        }
    }

    private void ReproducirSonidoDisparo()
    {
        if (sonidoDisparoEnemigo == null)
            return;

        if (audioSourceDisparo != null)
        {
            audioSourceDisparo.PlayOneShot(sonidoDisparoEnemigo, volumenDisparoEnemigo);
            return;
        }

        AudioSource.PlayClipAtPoint(sonidoDisparoEnemigo, transform.position, volumenDisparoEnemigo);
    }
}
