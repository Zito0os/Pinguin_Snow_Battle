using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class StatsPanel : MonoBehaviourPunCallbacks
{
    [Header("Referencias UI")]
    public TextMeshProUGUI primerLugarText;
    public TextMeshProUGUI segundoLugarText;
    public TextMeshProUGUI tercerLugarText;
    
    [Header("Multijugador")]
    public string nombreEscenaMultiplayer = "MultiPlayer";
    private bool usarPhotonEnEscena = false;
    
    private void Start()
    {
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();
        Debug.Log($"[StatsPanel] Start - EsMultiplayer: {usarPhotonEnEscena}");
        
        // Asegurarse de que el panel esté cerrado al iniciar
        gameObject.SetActive(false);
    }
    
    private void OnEnable()
    {
        Debug.Log("[StatsPanel] OnEnable - Panel abierto");
        if (usarPhotonEnEscena)
        {
            ActualizarStats();
            // Iniciar actualización periódica cuando el panel se abre
            InvokeRepeating("ActualizarStatsAutomatico", 0.3f, 0.3f);
        }
    }
    
    private void OnDisable()
    {
        Debug.Log("[StatsPanel] OnDisable - Panel cerrado");
        // Detener actualización periódica cuando el panel se cierra
        CancelInvoke("ActualizarStatsAutomatico");
    }
    
    // Callback de Photon cuando las propiedades de un jugador cambian
    // IMPORTANTE: Este callback funciona SIEMPRE, incluso si el panel está cerrado
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!usarPhotonEnEscena)
            return;
            
        // Si cambiaron kills o muertes, actualizar el panel SOLO si está visible
        if (changedProps.ContainsKey("Kills") || changedProps.ContainsKey("Muertes"))
        {
            Debug.Log($"[StatsPanel] ✓✓✓ Propiedades actualizadas para {targetPlayer.NickName}");
            if (changedProps.ContainsKey("Kills"))
            {
                Debug.Log($"[StatsPanel] Kills: {changedProps["Kills"]}");
            }
            if (changedProps.ContainsKey("Muertes"))
            {
                Debug.Log($"[StatsPanel] Muertes: {changedProps["Muertes"]}");
            }
            
            // Actualizar solo si el panel está activo
            if (gameObject.activeInHierarchy)
            {
                ActualizarStats();
            }
        }
    }
    
    private void ActualizarStatsAutomatico()
    {
        if (usarPhotonEnEscena)
        {
            ActualizarStats();
        }
    }
    
    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == nombreEscenaMultiplayer || escena == "MultiPlayer" || escena == "Multi_Player";
    }
    
    public void ActualizarStats()
    {
        if (!usarPhotonEnEscena)
        {
            Debug.Log("[StatsPanel] No es escena multiplayer");
            return;
        }
            
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("[StatsPanel] CurrentRoom es NULL");
            return;
        }
        
        Debug.Log($"[StatsPanel] Actualizando stats... Jugadores en sala: {PhotonNetwork.PlayerList.Length}");
        
        // Obtener todos los jugadores y sus stats
        List<PlayerStats> playersStats = new List<PlayerStats>();
        
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            PlayerStats stats = new PlayerStats();
            stats.playerName = player.NickName;
            
            // Obtener kills de las custom properties
            if (player.CustomProperties.ContainsKey("Kills"))
            {
                stats.kills = (int)player.CustomProperties["Kills"];
                Debug.Log($"[StatsPanel] {player.NickName} - Kills: {stats.kills}");
            }
            else
            {
                stats.kills = 0;
                Debug.LogWarning($"[StatsPanel] {player.NickName} NO tiene 'Kills' en CustomProperties");
            }
            
            // Obtener muertes de las custom properties
            if (player.CustomProperties.ContainsKey("Muertes"))
            {
                stats.muertes = (int)player.CustomProperties["Muertes"];
            }
            else
            {
                stats.muertes = 0;
            }
            
            playersStats.Add(stats);
        }
        
        // Ordenar por kills (de mayor a menor)
        playersStats = playersStats.OrderByDescending(p => p.kills).ToList();
        
        // Actualizar UI
        if (primerLugarText != null)
        {
            if (playersStats.Count > 0)
            {
                primerLugarText.text = $"{playersStats[0].playerName} - {playersStats[0].kills} Kills";
            }
            else
            {
                primerLugarText.text = "Vacio";
            }
        }
        
        if (segundoLugarText != null)
        {
            if (playersStats.Count > 1)
            {
                segundoLugarText.text = $"{playersStats[1].playerName} - {playersStats[1].kills} Kills";
            }
            else
            {
                segundoLugarText.text = "Vacio";
            }
        }
        
        if (tercerLugarText != null)
        {
            if (playersStats.Count > 2)
            {
                tercerLugarText.text = $"{playersStats[2].playerName} - {playersStats[2].kills} Kills";
            }
            else
            {
                tercerLugarText.text = "Vacio";
            }
        }
    }
    
    private class PlayerStats
    {
        public string playerName;
        public int kills;
        public int muertes;
    }
}
