using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviourPunCallbacks
{
    [Header("Stats del Jugador")]
    public int kills = 0;
    public int muertes = 0;
    public int health = 100;
    public int maxHealth = 100;
    
    [Header("Multijugador")]
    public string nombreEscenaMultiplayer = "MultiPlayer";
    private bool usarPhotonEnEscena = false;
    private PhotonView photonViewRef;
    
    [Header("Respawn")]
    public Transform[] spawnPoints;
    private int lastSpawnIndex = -1; // Para evitar respawnear en el mismo lugar
    
    [Header("Respawn Fallback (si no hay spawn points en escena)")]
    public bool usarPosicionesPredefinidas = false;
    public Vector3[] posicionesPredefinidas = new Vector3[]
    {
        new Vector3(0, 1, 0),
        new Vector3(10, 1, 10),
        new Vector3(-10, 1, 10),
        new Vector3(10, 1, -10)
    };
    
    private void Start()
    {
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();
        photonViewRef = GetComponent<PhotonView>();
        
        if (photonViewRef == null)
        {
            photonViewRef = GetComponentInParent<PhotonView>();
        }
        
        Debug.Log($"[PlayerStats] Start - UsarPhoton: {usarPhotonEnEscena}, PhotonView: {photonViewRef != null}, ViewID: {photonViewRef?.ViewID}, IsMine: {photonViewRef?.IsMine}");
        
        // Solo funciona en multijugador
        if (!usarPhotonEnEscena)
        {
            Debug.Log("[PlayerStats] No es escena multiplayer, deshabilitando componente");
            enabled = false;
            return;
        }
        
        // Buscar spawn points SIEMPRE (porque el prefab de Photon no puede tener referencias de escena)
        bool necesitaBuscar = (spawnPoints == null || spawnPoints.Length == 0);
        
        // También buscar si el array tiene elementos null
        if (!necesitaBuscar && spawnPoints != null)
        {
            foreach (Transform sp in spawnPoints)
            {
                if (sp == null)
                {
                    necesitaBuscar = true;
                    break;
                }
            }
        }
        
        if (necesitaBuscar)
        {
            Debug.Log("[PlayerStats] Buscando spawn points en la escena...");
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("Respawn");
            if (spawns.Length > 0)
            {
                spawnPoints = new Transform[spawns.Length];
                for (int i = 0; i < spawns.Length; i++)
                {
                    spawnPoints[i] = spawns[i].transform;
                }
                Debug.Log($"[PlayerStats] ✓ Se encontraron {spawnPoints.Length} spawn points por tag 'Respawn'");
            }
            else
            {
                // Fallback: buscar por nombre
                GameObject spawn = GameObject.Find("SpawnPoint");
                if (spawn != null)
                {
                    spawnPoints = new Transform[] { spawn.transform };
                    Debug.Log("[PlayerStats] ✓ Se encontró 1 spawn point por nombre 'SpawnPoint'");
                }
                else
                {
                    Debug.LogError("[PlayerStats] ✗✗✗ NO SE ENCONTRARON SPAWN POINTS - Crea objetos con tag 'Respawn' en la escena");
                }
            }
        }
        else
        {
            Debug.Log($"[PlayerStats] Spawn points ya asignados: {spawnPoints.Length}");
        }
        
        // Detectar el spawn inicial más cercano para evitar respawnear en el mismo
        if (spawnPoints != null && spawnPoints.Length > 1)
        {
            float minDistance = float.MaxValue;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    float distance = Vector3.Distance(transform.position, spawnPoints[i].position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        lastSpawnIndex = i;
                    }
                }
            }
            Debug.Log($"[PlayerStats] Spawn inicial detectado: {lastSpawnIndex + 1}/{spawnPoints.Length}");
        }
        
        // Inicializar custom properties
        if (EsControlLocal())
        {
            Debug.Log("[PlayerStats] Jugador local, inicializando custom properties");
            SincronizarStatsConPhoton();
        }
        else
        {
            Debug.Log("[PlayerStats] Jugador remoto");
        }
    }
    
    private void Update()
    {
        if (!usarPhotonEnEscena || !EsControlLocal())
            return;
            
        // Sincronizar stats periódicamente (no cada frame para mejorar rendimiento)
        if (Time.frameCount % 30 == 0) // Cada 30 frames (~0.5 segundos a 60fps)
        {
            SincronizarStatsConPhoton();
        }
    }
    
    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == nombreEscenaMultiplayer || escena == "MultiPlayer" || escena == "Multi_Player";
    }
    
    private bool EsControlLocal()
    {
        if (!usarPhotonEnEscena)
            return true;
            
        return photonViewRef != null && photonViewRef.IsMine;
    }
    
    private void SincronizarStatsConPhoton()
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable
            {
                { "Kills", kills },
                { "Muertes", muertes }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
            Debug.Log($"[PlayerStats] ✓✓✓ CustomProperties SINCRONIZADAS - Kills: {kills}, Muertes: {muertes}");
        }
        else
        {
            Debug.LogError("[PlayerStats] ✗ PhotonNetwork.LocalPlayer es NULL, no se pueden sincronizar!");
        }
    }
    
    // Método público para agregar salud (usado por vendas, medkits, etc.)
    public void AddHealth(int healthToAdd)
    {
        if (healthToAdd <= 0)
            return;
            
        health += healthToAdd;
        
        // Clampear a maxHealth
        if (health > maxHealth)
        {
            health = maxHealth;
        }
        
        Debug.Log($"[PlayerStats] ✓ Salud agregada: +{healthToAdd}. Salud actual: {health}/{maxHealth}, ViewID: {photonViewRef?.ViewID}");
        
        // Sincronizar con GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.health = health;
        }
    }
    
    // Método público que se llama desde Bullet.cs
    public void LoseHealth(int damage, int shooterViewID = -1)
    {
        Debug.Log($"[PlayerStats] LoseHealth llamado - Damage: {damage}, ShooterViewID: {shooterViewID}, UsarPhoton: {usarPhotonEnEscena}, PhotonView: {photonViewRef != null}");
        
        // En multijugador, enviar RPC al jugador objetivo
        if (usarPhotonEnEscena && photonViewRef != null)
        {
            Debug.Log($"[PlayerStats] Enviando RPC_LoseHealth a ViewID: {photonViewRef.ViewID}");
            photonViewRef.RPC("RPC_LoseHealth", RpcTarget.All, damage, shooterViewID);
        }
        else
        {
            Debug.Log("[PlayerStats] Aplicando daño directo (no multiplayer)");
            // Single player o fallback
            AplicarDanio(damage, shooterViewID);
        }
    }
    
    [PunRPC]
    private void RPC_LoseHealth(int damage, int shooterViewID)
    {
        Debug.Log($"[PlayerStats] RPC_LoseHealth recibido - Damage: {damage}, ShooterViewID: {shooterViewID}, IsMine: {EsControlLocal()}");
        
        // Solo el jugador local procesa su propio daño
        if (!EsControlLocal())
        {
            Debug.Log("[PlayerStats] No es local, ignorando RPC");
            return;
        }
            
        AplicarDanio(damage, shooterViewID);
    }
    
    private void AplicarDanio(int damage, int shooterViewID = -1)
    {
        health -= damage;
        
        Debug.Log($"[PlayerStats] ✓ Daño aplicado: {damage}. Salud actual: {health}/{maxHealth}, ViewID: {photonViewRef?.ViewID}, IsMine: {EsControlLocal()}");
        
        if (health <= 0)
        {
            health = 0;
            Debug.Log($"[PlayerStats] ⚠️ HEALTH <= 0, llamando OnPlayerDeath. ViewID: {photonViewRef?.ViewID}");
            OnPlayerDeath(shooterViewID);
        }
        
        // Sincronizar salud con GameManager si existe
        if (GameManager.Instance != null)
        {
            GameManager.Instance.health = health;
        }
    }
    
    private void OnPlayerDeath(int shooterViewID)
    {
        Debug.Log($"[PlayerStats] ✓✓✓ HAS MUERTO - ShooterViewID: {shooterViewID}");
        muertes++;
        
        // Sincronizar muertes inmediatamente
        SincronizarStatsConPhoton();
        
        // Notificar al shooter que consiguió una kill
        if (shooterViewID > 0 && usarPhotonEnEscena)
        {
            Debug.Log($"[PlayerStats] Notificando kill al shooter ViewID: {shooterViewID}");
            
            // Buscar el GameObject con el PhotonView del shooter
            PhotonView shooterPhotonView = PhotonView.Find(shooterViewID);
            if (shooterPhotonView != null)
            {
                Debug.Log($"[PlayerStats] ✓ PhotonView encontrado: {shooterPhotonView.gameObject.name}");
                
                // Buscar PlayerStats en el objeto o en su padre
                PlayerStats shooterStats = shooterPhotonView.GetComponent<PlayerStats>();
                if (shooterStats == null)
                {
                    shooterStats = shooterPhotonView.GetComponentInParent<PlayerStats>();
                }
                
                if (shooterStats != null && shooterStats.photonViewRef != null)
                {
                    Debug.Log($"[PlayerStats] ✓ PlayerStats encontrado, enviando RPC al ViewID: {shooterStats.photonViewRef.ViewID}");
                    shooterStats.photonViewRef.RPC("RPC_NotificarKill", RpcTarget.All);
                }
                else
                {
                    Debug.LogError($"[PlayerStats] ✗ No se encontró PlayerStats en el shooter o su padre");
                }
            }
            else
            {
                Debug.LogError($"[PlayerStats] ✗ No se encontró PhotonView con ViewID: {shooterViewID}");
            }
        }
        
        RespawnPlayer();
    }
    
    private void RespawnPlayer()
    {
        Debug.Log($"[PlayerStats] ►►► RespawnPlayer LLAMADO - ViewID: {photonViewRef?.ViewID}, IsMine: {EsControlLocal()}, SpawnPoints: {spawnPoints?.Length ?? 0}");
        
        // Restaurar salud
        health = maxHealth;
        
        // Sincronizar con GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.health = maxHealth;
        }
        
        // Seleccionar spawn point aleatorio DIFERENTE al anterior
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex;
            int previousSpawnIndex = lastSpawnIndex;
            
            // Si hay más de 1 spawn, asegurar que sea diferente al anterior
            if (spawnPoints.Length > 1)
            {
                do
                {
                    randomIndex = Random.Range(0, spawnPoints.Length);
                }
                while (randomIndex == lastSpawnIndex);
                
                lastSpawnIndex = randomIndex;
            }
            else
            {
                // Si solo hay 1 spawn, usar ese
                randomIndex = 0;
            }
            
            Transform selectedSpawn = spawnPoints[randomIndex];
            
            if (selectedSpawn != null)
            {
                // IMPORTANTE: En multiplayer, usar RPC para sincronizar posición con todos los clientes
                if (usarPhotonEnEscena && photonViewRef != null)
                {
                    Debug.Log($"[PlayerStats] ✓✓✓ ENVIANDO RPC_SincronizarRespawn - Spawn {randomIndex + 1}/{spawnPoints.Length}, ViewID: {photonViewRef.ViewID}");
                    Vector3 spawnPos = selectedSpawn.position;
                    Quaternion spawnRot = selectedSpawn.rotation;
                    photonViewRef.RPC("RPC_SincronizarRespawn", RpcTarget.AllBuffered, spawnPos, spawnRot, randomIndex, previousSpawnIndex);
                }
                else
                {
                    Debug.LogError($"[PlayerStats] ✗✗✗ NO SE PUEDE ENVIAR RPC - UsarPhoton: {usarPhotonEnEscena}, PhotonView: {photonViewRef != null}");
                    // Singleplayer: teletransportar directo
                    TeleportarASpawn(selectedSpawn.position, selectedSpawn.rotation);
                    Debug.Log($"[PlayerStats] Reaparecido en spawn point {randomIndex + 1}/{spawnPoints.Length} (anterior: {previousSpawnIndex + 1})");
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlayerStats] No hay spawn points asignados!");
            
            // FALLBACK: Usar posiciones predefinidas
            if (usarPosicionesPredefinidas && posicionesPredefinidas != null && posicionesPredefinidas.Length > 0)
            {
                int randomIndex;
                int previousSpawnIndex = lastSpawnIndex;
                
                if (posicionesPredefinidas.Length > 1)
                {
                    do
                    {
                        randomIndex = Random.Range(0, posicionesPredefinidas.Length);
                    }
                    while (randomIndex == lastSpawnIndex);
                    
                    lastSpawnIndex = randomIndex;
                }
                else
                {
                    randomIndex = 0;
                }
                
                Vector3 spawnPos = posicionesPredefinidas[randomIndex];
                Quaternion spawnRot = Quaternion.identity;
                
                Debug.Log($"[PlayerStats] ✓ Usando posición predefinida {randomIndex + 1}/{posicionesPredefinidas.Length}: {spawnPos}");
                
                if (usarPhotonEnEscena && photonViewRef != null)
                {
                    photonViewRef.RPC("RPC_SincronizarRespawn", RpcTarget.AllBuffered, spawnPos, spawnRot, randomIndex, previousSpawnIndex);
                }
                else
                {
                    TeleportarASpawn(spawnPos, spawnRot);
                }
            }
            else
            {
                Debug.LogError("[PlayerStats] ✗✗✗ NO HAY FORMA DE HACER RESPAWN - Crea spawn points con tag 'Respawn' o activa 'usarPosicionesPredefinidas'");
            }
        }
    }
    
    [PunRPC]
    private void RPC_SincronizarRespawn(Vector3 posicion, Quaternion rotacion, int spawnIndex, int prevSpawnIndex)
    {
        Debug.Log($"[PlayerStats] ✓✓✓ RPC_SincronizarRespawn recibido - IsMine: {EsControlLocal()}, Spawn: {spawnIndex + 1}, Posición: {posicion}");
        
        // TODOS los clientes ejecutan el teletransporte (para que todos vean la nueva posición)
        TeleportarASpawn(posicion, rotacion);
        
        Debug.Log($"[PlayerStats] ✓ Jugador teletransportado a spawn {spawnIndex + 1}/{spawnPoints.Length} (anterior: {prevSpawnIndex + 1})");
    }
    
    private void TeleportarASpawn(Vector3 posicion, Quaternion rotacion)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            // Desactivar CharacterController para cambiar posición
            cc.enabled = false;
            transform.position = posicion;
            transform.rotation = rotacion;
            cc.enabled = true;
        }
        else
        {
            // Sin CharacterController, cambiar directo
            transform.position = posicion;
            transform.rotation = rotacion;
        }
    }
    
    public void AddKill()
    {
        Debug.Log($"[PlayerStats] AddKill llamado - UsarPhoton: {usarPhotonEnEscena}, PhotonView: {photonViewRef != null}");
        
        // En multijugador, enviar RPC
        if (usarPhotonEnEscena && photonViewRef != null)
        {
            Debug.Log($"[PlayerStats] Enviando RPC_AddKill a ViewID: {photonViewRef.ViewID}");
            photonViewRef.RPC("RPC_AddKill", RpcTarget.All);
        }
        else
        {
            Debug.Log("[PlayerStats] Incrementando kill directo (no multiplayer)");
            // Single player o fallback
            IncrementarKill();
        }
    }
    
    [PunRPC]
    private void RPC_AddKill()
    {
        Debug.Log($"[PlayerStats] RPC_AddKill recibido - IsMine: {EsControlLocal()}");
        
        // Solo el jugador local incrementa sus propios kills
        if (!EsControlLocal())
        {
            Debug.Log("[PlayerStats] No es local, ignorando RPC");
            return;
        }
            
        IncrementarKill();
    }
    
    // Nuevo RPC para notificar kills desde la víctima al shooter
    [PunRPC]
    private void RPC_NotificarKill()
    {
        Debug.Log($"[PlayerStats] ✓✓✓ RPC_NotificarKill recibido - IsMine: {EsControlLocal()}");
        
        // Solo el jugador local incrementa sus propios kills
        if (!EsControlLocal())
        {
            Debug.Log("[PlayerStats] No es local, ignorando notificación de kill");
            return;
        }
            
        IncrementarKill();
    }
    
    private void IncrementarKill()
    {
        kills++;
        Debug.Log($"[PlayerStats] ✓✓✓ KILL REGISTRADO! Total: {kills}");
        
        // Sincronizar con GameManager para UI
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Kills = kills;
            Debug.Log($"[PlayerStats] GameManager.Kills actualizado: {GameManager.Instance.Kills}");
        }
        
        // Sincronizar INMEDIATAMENTE con Photon cuando se consigue una kill
        SincronizarStatsConPhoton();
        
        // Verificar que se sincronizó
        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Kills"))
        {
            Debug.Log($"[PlayerStats] Verificación - CustomProperties[Kills]: {PhotonNetwork.LocalPlayer.CustomProperties["Kills"]}");
        }
    }
}
