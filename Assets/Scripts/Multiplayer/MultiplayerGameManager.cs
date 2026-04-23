using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class MultiplayerGameManager : MonoBehaviourPunCallbacks
{
    [Header("Configuración del Juego")]
    public float tiempoPartidaSegundos = 300f; // 5 minutos por defecto (300 segundos)
    
    [Header("UI Referencias")]
    public TextMeshProUGUI tiempoRestanteText;
    public GameObject panelEsperandoJugadores;
    public TextMeshProUGUI textoEsperandoJugadores;
    public GameObject panelFinPartida;
    public TextMeshProUGUI ganadorNombreText;
    public TextMeshProUGUI ganadorKillsText;
    public Button botonContinuar;
    
    private float tiempoRestante;
    private bool juegoIniciado = false;
    private bool juegoTerminado = false;
    
    public static MultiplayerGameManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        tiempoRestante = tiempoPartidaSegundos;
        
        // Configurar UI inicial
        if (panelEsperandoJugadores != null)
            panelEsperandoJugadores.SetActive(true);
            
        if (panelFinPartida != null)
            panelFinPartida.SetActive(false);
            
        if (tiempoRestanteText != null)
            tiempoRestanteText.text = "Esperando jugadores...";
            
        if (botonContinuar != null)
            botonContinuar.onClick.AddListener(OnBotonContinuar);
            
        Debug.Log($"[MultiplayerGameManager] Iniciado - Conectado: {PhotonNetwork.IsConnected}, En Sala: {PhotonNetwork.InRoom}, Jugadores: {(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0)}");
    }
    
    private void Update()
    {
        if (juegoTerminado)
            return;
        
        // Si el juego NO ha iniciado, actualizar UI de espera y verificar inicio
        if (!juegoIniciado)
        {
            ActualizarUIEspera();
            VerificarInicioJuego();
        }
        else
        {
            // Actualizar contador si el juego ha iniciado
            if (PhotonNetwork.IsMasterClient)
            {
                tiempoRestante -= Time.deltaTime;
                
                // Sincronizar tiempo con todos los clientes
                if (Time.frameCount % 10 == 0) // Cada ~10 frames para no saturar
                {
                    photonView.RPC("RPC_ActualizarTiempo", RpcTarget.All, tiempoRestante);
                }
                
                // Verificar si el tiempo se acabó
                if (tiempoRestante <= 0f)
                {
                    tiempoRestante = 0f;
                    FinalizarPartida();
                }
            }
            
            // Actualizar UI del tiempo
            ActualizarUITiempo();
        }
    }
    
    private void ActualizarUIEspera()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            if (textoEsperandoJugadores != null)
                textoEsperandoJugadores.text = "Conectando...";
            return;
        }
        
        int numJugadores = PhotonNetwork.CurrentRoom.PlayerCount;
        
        if (textoEsperandoJugadores != null)
        {
            textoEsperandoJugadores.text = $"Esperando jugadores... ({numJugadores}/2)";
        }
    }
    
    private void VerificarInicioJuego()
    {
        if (juegoIniciado || juegoTerminado)
            return;
        
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;
            
        int numJugadores = PhotonNetwork.CurrentRoom.PlayerCount;
        
        Debug.Log($"[MultiplayerGameManager] Verificando inicio - Jugadores: {numJugadores}");
        
        if (numJugadores >= 2)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[MultiplayerGameManager] ✓ 2+ jugadores detectados - INICIANDO PARTIDA");
                photonView.RPC("RPC_IniciarPartida", RpcTarget.All);
            }
        }
    }
    
    [PunRPC]
    private void RPC_IniciarPartida()
    {
        if (juegoIniciado)
            return;
            
        juegoIniciado = true;
        tiempoRestante = tiempoPartidaSegundos;
        
        Debug.Log("[MultiplayerGameManager] ✓✓✓ PARTIDA INICIADA - Timer: " + tiempoPartidaSegundos + " segundos");
        
        if (panelEsperandoJugadores != null)
            panelEsperandoJugadores.SetActive(false);
            
        if (tiempoRestanteText != null)
            tiempoRestanteText.gameObject.SetActive(true);
    }
    
    [PunRPC]
    private void RPC_ActualizarTiempo(float nuevoTiempo)
    {
        // Solo actualizar si no somos MasterClient (el MasterClient ya tiene el valor correcto)
        if (!PhotonNetwork.IsMasterClient)
        {
            tiempoRestante = nuevoTiempo;
        }
    }
    
    private void ActualizarUITiempo()
    {
        if (tiempoRestanteText == null)
            return;
            
        if (!juegoIniciado)
        {
            tiempoRestanteText.text = "Esperando jugadores...";
            return;
        }
        
        // Formatear tiempo como MM:SS
        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
        tiempoRestanteText.text = $"{minutos:00}:{segundos:00}";
        
        // Cambiar color si queda poco tiempo
        if (tiempoRestante <= 30f)
        {
            tiempoRestanteText.color = Color.red;
        }
        else if (tiempoRestante <= 60f)
        {
            tiempoRestanteText.color = Color.yellow;
        }
        else
        {
            tiempoRestanteText.color = Color.white;
        }
    }
    
    private void FinalizarPartida()
    {
        if (juegoTerminado)
            return;
            
        juegoTerminado = true;
        
        Debug.Log("[MultiplayerGameManager] ✓✓✓ TIEMPO TERMINADO - Finalizando partida...");
        
        // Obtener el ganador (jugador con más kills)
        Player ganador = null;
        int maxKills = -1;
        
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int kills = 0;
            if (player.CustomProperties.ContainsKey("Kills"))
            {
                kills = (int)player.CustomProperties["Kills"];
            }
            
            Debug.Log($"[MultiplayerGameManager] {player.NickName} - Kills: {kills}");
            
            if (kills > maxKills)
            {
                maxKills = kills;
                ganador = player;
            }
        }
        
        string nombreGanador = ganador != null ? ganador.NickName : "Nadie";
        int killsGanador = maxKills;
        
        Debug.Log($"[MultiplayerGameManager] GANADOR: {nombreGanador} con {killsGanador} kills");
        
        // Mostrar panel de victoria para todos
        photonView.RPC("RPC_MostrarPanelVictoria", RpcTarget.All, nombreGanador, killsGanador);
    }
    
    [PunRPC]
    private void RPC_MostrarPanelVictoria(string nombreGanador, int killsGanador)
    {
        Debug.Log($"[MultiplayerGameManager] RPC_MostrarPanelVictoria - Ganador: {nombreGanador}, Kills: {killsGanador}");
        
        // Pausar el juego
        Time.timeScale = 0f;
        
        // Ocultar UI del tiempo
        if (tiempoRestanteText != null)
            tiempoRestanteText.gameObject.SetActive(false);
        
        // Mostrar panel de fin de partida
        if (panelFinPartida != null)
            panelFinPartida.SetActive(true);
            
        if (ganadorNombreText != null)
            ganadorNombreText.text = $"Ganador: {nombreGanador}";
            
        if (ganadorKillsText != null)
        {
            if (killsGanador == 1)
                ganadorKillsText.text = $"{killsGanador} Kill";
            else
                ganadorKillsText.text = $"{killsGanador} Kills";
        }
        
        // Habilitar cursor para hacer clic en el botón
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    private void OnBotonContinuar()
    {
        Debug.Log("[MultiplayerGameManager] Botón Continuar presionado");
        
        // Restaurar timeScale
        Time.timeScale = 1f;
        
        // Desconectar de Photon
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[MultiplayerGameManager] Desconectando de Photon...");
            PhotonNetwork.Disconnect();
        }
        
        // Cargar escena del menú principal (escena 0)
        SceneManager.LoadScene(0);
    }
    
    // Callbacks de Photon
    public override void OnJoinedRoom()
    {
        int totalJugadores = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
        Debug.Log($"[MultiplayerGameManager] OnJoinedRoom - Jugadores en sala: {totalJugadores}");
        
        // Actualizar UI inmediatamente
        ActualizarUIEspera();
        
        // Verificar si ya podemos iniciar
        if (!juegoIniciado)
        {
            VerificarInicioJuego();
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        int totalJugadores = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
        Debug.Log($"[MultiplayerGameManager] Jugador entró: {newPlayer.NickName}, Total: {totalJugadores}");
        
        // Si el juego no ha iniciado, verificar si ya podemos empezar
        if (!juegoIniciado)
        {
            VerificarInicioJuego();
        }
        else
        {
            // Si el juego ya inició, informar al nuevo jugador del tiempo restante
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_SincronizarNuevoJugador", newPlayer, tiempoRestante);
            }
        }
    }
    
    [PunRPC]
    private void RPC_SincronizarNuevoJugador(float tiempoActual)
    {
        if (!juegoIniciado)
        {
            juegoIniciado = true;
            if (panelEsperandoJugadores != null)
                panelEsperandoJugadores.SetActive(false);
        }
        
        tiempoRestante = tiempoActual;
        Debug.Log($"[MultiplayerGameManager] Nuevo jugador sincronizado - Tiempo restante: {tiempoActual}s");
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        int totalJugadores = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
        Debug.Log($"[MultiplayerGameManager] Jugador salió: {otherPlayer.NickName}, Total: {totalJugadores}");
        
        // Si quedan menos de 2 jugadores y el juego está en curso, podrías pausar o terminar
        // Por ahora solo lo notificamos
        if (juegoIniciado && totalJugadores < 2)
        {
            Debug.LogWarning("[MultiplayerGameManager] ⚠️ Quedan menos de 2 jugadores en la partida");
        }
    }
    
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[MultiplayerGameManager] Nuevo MasterClient: {newMasterClient.NickName}");
        
        // El nuevo MasterClient toma control del timer
        if (PhotonNetwork.IsMasterClient && juegoIniciado && !juegoTerminado)
        {
            Debug.Log("[MultiplayerGameManager] Tomando control del timer como nuevo MasterClient");
        }
    }
}
