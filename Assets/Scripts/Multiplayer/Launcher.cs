using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public PhotonView player_prefab;

    public Transform[] spawnPoints;
    
    void Start()
    {
        // Solo conectar si no estamos conectados ya
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[Launcher] Conectando a Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.Log("[Launcher] Ya conectado a Photon, intentando unirse a sala...");
            PhotonNetwork.JoinRandomOrCreateRoom();
        }
        
        // Buscar spawn points si no están asignados
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("Respawn");
            if (spawns.Length > 0)
            {
                spawnPoints = new Transform[spawns.Length];
                for (int i = 0; i < spawns.Length; i++)
                {
                    spawnPoints[i] = spawns[i].transform;
                }
                Debug.Log($"[Launcher] Se encontraron {spawnPoints.Length} spawn points");
            }
            else
            {
                // Fallback: buscar por nombre
                GameObject spawn = GameObject.Find("SpawnPoint");
                if (spawn != null)
                {
                    spawnPoints = new Transform[] { spawn.transform };
                    Debug.Log("[Launcher] Se encontró 1 spawn point por nombre");
                }
            }
        }
    }
    
    void OnDestroy()
    {
        // Desconectar cuando se destruye el Launcher (al cambiar de escena)
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[Launcher] OnDestroy - Desconectando de Photon...");
            PhotonNetwork.Disconnect();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado al Servidor");
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Conectado a la Sala");
        
        // Asignar nombre al jugador basado en su Actor Number
        string playerName = "Player " + PhotonNetwork.LocalPlayer.ActorNumber;
        PhotonNetwork.NickName = playerName;
        PhotonNetwork.LocalPlayer.NickName = playerName;
        
        Debug.Log("Tu nombre es: " + playerName);
        
        // Elegir un spawn point aleatorio
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform selectedSpawn = spawnPoints[randomIndex];
            
            Debug.Log($"[Launcher] Spawneando en punto {randomIndex + 1}/{spawnPoints.Length}");
            PhotonNetwork.Instantiate(player_prefab.name, selectedSpawn.position, selectedSpawn.rotation);
        }
        else
        {
            Debug.LogWarning("[Launcher] No hay spawn points disponibles, usando posición por defecto");
            PhotonNetwork.Instantiate(player_prefab.name, Vector3.zero, Quaternion.identity);
        }
    }
}
