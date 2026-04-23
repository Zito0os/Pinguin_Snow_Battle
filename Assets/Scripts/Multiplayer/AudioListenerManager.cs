using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

/// <summary>
/// Asegura que solo haya UN AudioListener activo en la escena en todo momento.
/// Este script debe ejecutarse PRIMERO (configurar en Script Execution Order).
/// </summary>
public class AudioListenerManager : MonoBehaviourPunCallbacks
{
    [Header("Configuración")]
    public string nombreEscenaMultiplayer = "MultiPlayer";
    private bool usarPhotonEnEscena = false;
    
    [Header("Debug")]
    public bool mostrarLogs = true;
    
    private void Awake()
    {
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();
        
        if (!usarPhotonEnEscena)
        {
            // No hacer nada en single player
            return;
        }
        
        // Esperar un frame para que los demás scripts se inicialicen
        Invoke(nameof(DeshabilitarAudioListenersInnecesarios), 0.1f);
        
        // Deshabilitar AudioListeners globales que no pertenezcan a jugadores
        Invoke(nameof(DeshabilitarAudioListenersGlobales), 0.15f);
    }
    
    private void Start()
    {
        if (usarPhotonEnEscena)
        {
            // Verificar nuevamente después de Start
            Invoke(nameof(DeshabilitarAudioListenersInnecesarios), 0.2f);
        }
    }
    
    private void DeshabilitarAudioListenersInnecesarios()
    {
        if (!usarPhotonEnEscena)
            return;
            
        PhotonView myPhotonView = GetComponent<PhotonView>();
        if (myPhotonView == null)
        {
            myPhotonView = GetComponentInParent<PhotonView>();
        }
        
        bool esLocal = myPhotonView != null && myPhotonView.IsMine;
        
        if (mostrarLogs)
        {
            Debug.Log($"[AudioListenerManager] Jugador: {gameObject.name}, IsMine: {esLocal}");
        }
        
        // Obtener TODOS los AudioListeners en este jugador
        AudioListener[] listeners = GetComponentsInChildren<AudioListener>(true);
        
        if (mostrarLogs)
        {
            Debug.Log($"[AudioListenerManager] Encontrados {listeners.Length} AudioListeners en {gameObject.name}");
        }
        
        if (esLocal)
        {
            // Jugador LOCAL: Solo mantener ACTIVO el primer AudioListener encontrado
            bool primerEncontrado = false;
            
            foreach (AudioListener listener in listeners)
            {
                if (!primerEncontrado)
                {
                    listener.enabled = true;
                    primerEncontrado = true;
                    
                    if (mostrarLogs)
                    {
                        Debug.Log($"[AudioListenerManager] ✓ AudioListener ACTIVO en: {listener.gameObject.name} (JUGADOR LOCAL)");
                    }
                }
                else
                {
                    listener.enabled = false;
                    
                    if (mostrarLogs)
                    {
                        Debug.Log($"[AudioListenerManager] ✗ AudioListener DESHABILITADO en: {listener.gameObject.name} (duplicado)");
                    }
                }
            }
            
            // Solo el jugador local hace la verificación global
            Invoke(nameof(VerificarEstadoFinal), 0.5f);
        }
        else
        {
            // Jugador REMOTO: DESHABILITAR TODOS los AudioListeners
            foreach (AudioListener listener in listeners)
            {
                listener.enabled = false;
                
                if (mostrarLogs)
                {
                    Debug.Log($"[AudioListenerManager] ✗ AudioListener DESHABILITADO en: {listener.gameObject.name} (JUGADOR REMOTO)");
                }
            }
        }
    }
    
    private void DeshabilitarAudioListenersGlobales()
    {
        if (!usarPhotonEnEscena)
            return;
            
        PhotonView myPhotonView = GetComponent<PhotonView>();
        if (myPhotonView == null)
        {
            myPhotonView = GetComponentInParent<PhotonView>();
        }
        
        // Solo el jugador local ejecuta esta limpieza
        if (myPhotonView == null || !myPhotonView.IsMine)
            return;
        
        if (mostrarLogs)
        {
            Debug.Log("[AudioListenerManager] Limpiando AudioListeners globales de la escena...");
        }
        
        // Buscar TODOS los AudioListeners en la escena
        AudioListener[] todosListeners = FindObjectsOfType<AudioListener>(true);
        Transform miRaiz = transform.root;
        
        foreach (AudioListener listener in todosListeners)
        {
            Transform listenerRaiz = listener.transform.root;
            
            // Si el listener NO pertenece a este jugador
            if (listenerRaiz != miRaiz)
            {
                // Verificar si pertenece a otro jugador
                PhotonView listenerPhotonView = listenerRaiz.GetComponent<PhotonView>();
                
                if (listenerPhotonView != null)
                {
                    // Es de otro jugador, ya se maneja en su propio AudioListenerManager
                    continue;
                }
                else
                {
                    // No pertenece a ningún jugador (ej: AudioListener suelto en la escena)
                    listener.enabled = false;
                    
                    if (mostrarLogs)
                    {
                        Debug.Log($"[AudioListenerManager] ✗ AudioListener GLOBAL deshabilitado: {listener.gameObject.name} (no pertenece a jugador)");
                    }
                }
            }
        }
    }
    
    private void VerificarEstadoFinal()
    {
        // Buscar todos los AudioListeners en la escena (solo los activos)
        AudioListener[] todosListeners = FindObjectsOfType<AudioListener>();
        int activos = 0;
        
        foreach (AudioListener listener in todosListeners)
        {
            if (listener.enabled && listener.gameObject.activeInHierarchy)
            {
                activos++;
                if (mostrarLogs)
                {
                    Debug.Log($"[AudioListenerManager] AudioListener ACTIVO encontrado: {listener.gameObject.name} en {listener.transform.root.name}");
                }
            }
        }
        
        if (activos == 0)
        {
            Debug.LogWarning("[AudioListenerManager] ⚠️ NO HAY AudioListeners ACTIVOS en la escena!");
        }
        else if (activos == 1)
        {
            Debug.Log($"[AudioListenerManager] ✓ CORRECTO: Exactamente {activos} AudioListener activo en la escena");
        }
        else
        {
            Debug.LogError($"[AudioListenerManager] ✗ ERROR: {activos} AudioListeners ACTIVOS en la escena (debería ser 1)");
            
            // Mostrar detalles de cada uno
            if (mostrarLogs)
            {
                foreach (AudioListener listener in todosListeners)
                {
                    if (listener.enabled && listener.gameObject.activeInHierarchy)
                    {
                        Debug.LogError($"[AudioListenerManager] → {listener.gameObject.name} (Padre: {listener.transform.parent?.name ?? "ROOT"}, Root: {listener.transform.root.name})");
                    }
                }
            }
        }
    }
    
    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == nombreEscenaMultiplayer || escena == "MultiPlayer" || escena == "Multi_Player";
    }
    
    // Método público para forzar verificación
    public void ForzarVerificacion()
    {
        DeshabilitarAudioListenersInnecesarios();
    }
}
