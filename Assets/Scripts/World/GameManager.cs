using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviour
{
    //entender mejo el game manager
    //SINGELTON


    public static GameManager Instance { get; private set; }


    //public Text ammoText; // UI Text to display ammo count
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI granadasText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI vendasText;
    public TextMeshProUGUI muertesText;
    
    [Header("Info del Jugador")]
    public TextMeshProUGUI playerInfoText; // Muestra "Player 1 - 1°" o "Player 2 - 3°"

    public int gunammo = 500;
    [SerializeField] private int maxGunAmmo = 210;
    public int cantidad_granadas = 20;
    public int Kills = 0;
    public int Muertes = 0;

    public int cantidad_vendas = 2;
    public int curacion_venda = 20;
    public int max_cantidad_vendas = 4;

    public int health = 100;
    public int maxHealth = 100;

    public int kills_para_ganar = 20;

    [Header("Multijugador")]
    public string nombreEscenaMultiplayer = "MultiPlayer";
    private bool usarPhotonEnEscena = false;
    private PlayerStats playerStatsLocal;

    private void Awake()
    {
        Instance = this;
        gunammo = Mathf.Clamp(gunammo, 0, maxGunAmmo);
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();
    }

    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == nombreEscenaMultiplayer || escena == "MultiPlayer" || escena == "Multi_Player";
    }
    
    private PlayerStats ObtenerPlayerStatsLocal()
    {
        if (playerStatsLocal != null)
            return playerStatsLocal;
            
        // Buscar el jugador local en la escena
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                playerStatsLocal = player.GetComponent<PlayerStats>();
                if (playerStatsLocal == null)
                {
                    playerStatsLocal = player.GetComponentInChildren<PlayerStats>();
                }
                return playerStatsLocal;
            }
        }
        
        return null;
    }


    private void Update()
    {
        // En multijugador, sincronizar stats desde PlayerStats del jugador local
        if (usarPhotonEnEscena)
        {
            PlayerStats localStats = ObtenerPlayerStatsLocal();
            if (localStats != null)
            {
                Kills = localStats.kills;
                Muertes = localStats.muertes;
                health = localStats.health;
            }
        }
        else
        {
            // En single player, checar victoria por kills
            checar_kills();
        }
        
        gunammo = Mathf.Clamp(gunammo, 0, maxGunAmmo);
        // Mostrar solo el número de balas (gunammo)
        if (ammoText != null)
            ammoText.text = gunammo.ToString();
        //healthText.text = health.ToString();
        killsText.text = Kills.ToString();
        granadasText.text = cantidad_granadas.ToString();
        vendasText.text = cantidad_vendas.ToString();
        
        if (muertesText != null)
            muertesText.text = Muertes.ToString();
            
        // Actualizar info del jugador en multiplayer
        if (usarPhotonEnEscena && playerInfoText != null)
        {
            ActualizarInfoJugador();
        }
    }


    public void LoseHealth(int healthToReduce)
    {
        if (usarPhotonEnEscena)
        {
            // En multijugador, delegar al PlayerStats
            PlayerStats localStats = ObtenerPlayerStatsLocal();
            if (localStats != null)
            {
                localStats.LoseHealth(healthToReduce);
            }
        }
        else
        {
            // Single player
            health -= healthToReduce;
            CheckHealth();
        }
    }


    public void CheckHealth()
    {
        if (health <= 0)
        {
            Debug.Log("Haz muerto");
            
            if (usarPhotonEnEscena)
            {
                // En multijugador, delegar al PlayerStats
                PlayerStats localStats = ObtenerPlayerStatsLocal();
                if (localStats != null)
                {
                    // PlayerStats maneja muerte y respawn
                }
            }
            else
            {
                // En modo single player, cargar escena de game over
                SceneManager.LoadScene(4);
            }
        }
    }
    
    public void AddKill()
    {
        if (usarPhotonEnEscena)
        {
            // En multijugador, delegar al PlayerStats
            PlayerStats localStats = ObtenerPlayerStatsLocal();
            if (localStats != null)
            {
                localStats.AddKill();
            }
        }
        else
        {
            // Single player
            Kills++;
            Debug.Log("Kill registrado! Total: " + Kills);
        }
    }


    public void AddHealth(int healthToAdd)
    {
        if (usarPhotonEnEscena)
        {
            // En multijugador, delegar al PlayerStats
            PlayerStats localStats = ObtenerPlayerStatsLocal();
            if (localStats != null)
            {
                localStats.AddHealth(healthToAdd);
            }
        }
        else
        {
            // Single player
            // si mi vida mas la vida que viene es mayor o igual a mi vida maxima se queda en 100
            if (this.health + healthToAdd >= maxHealth)
            {
                this.health = maxHealth;
            }
            else
            {
                this.health += healthToAdd;
            }
        }
    }



    public void agregar_vendas(int vendas_agregar)
    {
        // si mi vida mas la vida que viene es mayor o igual a mi vida maxima se queda en 100
        if (this.cantidad_vendas + vendas_agregar >= max_cantidad_vendas)
        {
            this.cantidad_vendas = max_cantidad_vendas;
        }
        else
        {
            this.cantidad_vendas += vendas_agregar;
        }

    }
    public void usarvenda()
    {
        if (cantidad_vendas <= 0)
            return;

        cantidad_vendas -= 1;
        AddHealth(curacion_venda);
    }

    public void AddGunAmmo(int ammoToAdd)
    {
        if (ammoToAdd <= 0)
            return;

        gunammo = Mathf.Clamp(gunammo + ammoToAdd, 0, maxGunAmmo);
    }


    public void AddGranade(int Granadatoadd)
    {

        if (Granadatoadd <= 0)
            return;

        if (Random.value <= 0.2f)
        {
            cantidad_granadas += Granadatoadd;

        }
    }


    public void checar_kills()
    {
        if (Kills >= kills_para_ganar)
        {
            Debug.Log("Has ganado");
            SceneManager.LoadScene(3);
        }


    }
    
    private void ActualizarInfoJugador()
    {
        if (!usarPhotonEnEscena || PhotonNetwork.CurrentRoom == null || PhotonNetwork.LocalPlayer == null)
            return;
            
        // Obtener nombre del jugador local
        string nombreJugador = PhotonNetwork.LocalPlayer.NickName;
        
        // Obtener todos los jugadores y ordenarlos por kills
        var jugadoresOrdenados = new System.Collections.Generic.List<Player>(PhotonNetwork.PlayerList);
        jugadoresOrdenados.Sort((a, b) =>
        {
            int killsA = a.CustomProperties.ContainsKey("Kills") ? (int)a.CustomProperties["Kills"] : 0;
            int killsB = b.CustomProperties.ContainsKey("Kills") ? (int)b.CustomProperties["Kills"] : 0;
            return killsB.CompareTo(killsA); // Orden descendente
        });
        
        // Encontrar la posición del jugador local
        int posicion = 1;
        for (int i = 0; i < jugadoresOrdenados.Count; i++)
        {
            if (jugadoresOrdenados[i].ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                posicion = i + 1;
                break;
            }
        }
        
        // Determinar el sufijo (1°, 2°, 3°, etc.)
        string sufijo = "°";
        if (posicion == 1)
            sufijo = "° 🏆"; // Emoji de trofeo para el primero
        
        // Actualizar el texto
        playerInfoText.text = $"{nombreJugador} - {posicion}{sufijo}";
    }



}
