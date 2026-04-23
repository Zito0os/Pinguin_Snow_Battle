using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class Bullet : MonoBehaviour
{
    public int damageToPlayer = 20;
    private bool usarPhotonEnEscena = false;
    private PhotonView shooterPhotonView;
    private bool shooterAsignado = false;
    private bool inicializado = false;
    
    private void Awake()
    {
        // Detectar escena en Awake para que esté listo ANTES de cualquier colisión
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();
        inicializado = true;
        Debug.Log($"[Bullet] Awake - Escena Multiplayer: {usarPhotonEnEscena}");
    }
    
    private void Start()
    {
        if (!inicializado)
        {
            usarPhotonEnEscena = EsEscenaMultiplayerActiva();
            inicializado = true;
        }
        Debug.Log($"[Bullet] Start - Escena Multiplayer: {usarPhotonEnEscena}");
    }
    
    public void SetShooter(PhotonView shooter)
    {
        shooterPhotonView = shooter;
        shooterAsignado = shooter != null;
        Debug.Log($"[Bullet] SetShooter llamado - Shooter ViewID: {shooter?.ViewID}, Asignado: {shooterAsignado}");
    }
    
    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        bool esMulti = escena == "MultiPlayer" || escena == "Multi_Player";
        return esMulti;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // CRÍTICO: Verificar escena multiplayer en cada colisión por si acaso
        if (!inicializado)
        {
            usarPhotonEnEscena = EsEscenaMultiplayerActiva();
            inicializado = true;
            Debug.LogWarning("[Bullet] ⚠️ Inicialización tardía en OnCollisionEnter");
        }
        
        Debug.Log($"[Bullet] ===== COLISION DETECTADA =====");
        Debug.Log($"[Bullet] Objeto: {collision.gameObject.name}");
        Debug.Log($"[Bullet] Tag: {collision.gameObject.tag}");
        Debug.Log($"[Bullet] Escena Multiplayer: {usarPhotonEnEscena}");
        Debug.Log($"[Bullet] Shooter Asignado: {shooterAsignado}");
        
        // Daño a jugadores (solo en multijugador)
        if (collision.gameObject.CompareTag("Player") && usarPhotonEnEscena)
        {
            Debug.Log("[Bullet] ✓ Es un Player en escena multiplayer");
            
            PhotonView targetPhotonView = collision.gameObject.GetComponent<PhotonView>();
            
            if (targetPhotonView == null)
            {
                Debug.LogError("[Bullet] ✗ Target NO tiene PhotonView!");
                Destroy(gameObject);
                return;
            }
            
            Debug.Log($"[Bullet] Target ViewID: {targetPhotonView.ViewID}, IsMine: {targetPhotonView.IsMine}");
            
            if (shooterPhotonView == null)
            {
                Debug.LogWarning("[Bullet] ✗ Shooter PhotonView es NULL - aplicando daño sin verificar ViewID");
                // Aplicar daño de todos modos si no hay shooter asignado
                PlayerStats targetStats = collision.gameObject.GetComponent<PlayerStats>();
                if (targetStats == null)
                {
                    targetStats = collision.gameObject.GetComponentInChildren<PlayerStats>();
                }
                
                if (targetStats != null)
                {
                    Debug.Log($"[Bullet] Aplicando daño sin shooter check - Salud antes: {targetStats.health}");
                    targetStats.LoseHealth(damageToPlayer);
                }
                
                Destroy(gameObject);
                return;
            }
            
            Debug.Log($"[Bullet] Shooter ViewID: {shooterPhotonView.ViewID}");
            
            // Verificar que no nos disparemos a nosotros mismos
            if (targetPhotonView.ViewID != shooterPhotonView.ViewID)
            {
                Debug.Log("[Bullet] ✓ ViewIDs DIFERENTES - Aplicando daño");
                
                // Hacer daño al jugador objetivo usando PlayerStats
                PlayerStats targetStats = collision.gameObject.GetComponent<PlayerStats>();
                if (targetStats == null)
                {
                    targetStats = collision.gameObject.GetComponentInChildren<PlayerStats>();
                }
                
                if (targetStats != null)
                {
                    Debug.Log($"[Bullet] ✓ PlayerStats encontrado - Salud REMOTA (puede estar desactualizada): {targetStats.health}");
                    
                    // Enviar daño CON el ViewID del shooter para que la víctima pueda notificarle si muere
                    targetStats.LoseHealth(damageToPlayer, shooterPhotonView.ViewID);
                    
                    Debug.Log($"[Bullet] Daño enviado con ShooterViewID: {shooterPhotonView.ViewID}");
                }
                else
                {
                    Debug.LogWarning("[Bullet] ✗ PlayerStats NO encontrado - Usando fallback GameManager");
                    
                    // Fallback a GameManager para compatibilidad con single player
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.LoseHealth(damageToPlayer);
                    }
                }
                
                Destroy(gameObject);
                return;
            }
            else
            {
                Debug.Log("[Bullet] ✗ MISMO ViewID (disparo propio) - Ignorando");
            }
        }

        //checa si la bala colision� con un enemigo
        if (collision.gameObject.CompareTag("Enemy"))
        {
            collision.gameObject.GetComponent<AI>().LooseLife(20);

            // Destroy the enemy
            //Destroy(collision.gameObject);
            // Destroy the bullet
            //Destroy(gameObject);
        }


        if (collision.gameObject.CompareTag("Destruible"))
        {

           

            // Destroy the enemy
            Destroy(collision.gameObject);
            // Destroy the bullet
            //Destroy(gameObject);
        }



    }
}
