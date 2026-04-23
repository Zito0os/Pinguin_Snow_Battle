using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [Header("ESP-32")]
    public bool usarMenuEsp32 = true;
    public float toggleCooldownSegundos = 0.1f;

    [Header("ESP Navegacion Menu Pausa")]
    public Button[] botonesPausa;
    public float deadZoneNavegacion = 0.45f;
    public float cooldownNavegacionSegundos = 0.2f;

    public GameObject pausePanel;
    public GameObject configurationPanel;
    public GameObject statsPanel;
    private StatsPanel statsPanelScript;
    private Canvas pausePanelCanvas;
    private bool isGamePaused = false;
    private bool opcionesAbiertasDesdePausa = false;
    private bool statsAbierto = false;
    private bool espPausaPresionadoFrameAnterior = false;
    private bool espTabPresionadoFrameAnterior = false;
    private bool espTriggerPresionadoFrameAnterior = false;
    private bool joystickNavegacionBloqueado = false;
    private int indiceSeleccionPausa = 0;
    private float proximoTogglePausaTime = 0f;
    private float proximoToggleTabTime = 0f;
    private float proximoMovimientoMenuTime = 0f;

    private void Awake()
    {
        AsegurarEventSystem();
        AsegurarGraphicRaycaster();
    }

    private void Start()
    {
        SetPauseState(false);

        AutoAsignarBotonesPausaSiFaltan();
        
        if (statsPanel != null)
        {
            statsPanelScript = statsPanel.GetComponent<StatsPanel>();
            statsPanel.SetActive(false);
        }
    }

    void Update()
    {
        bool pauseInput = Input.GetKeyDown(KeyCode.Escape);

        bool espPauseDown = TryGetEspButtonDown(9, ref espPausaPresionadoFrameAnterior);
        if (espPauseDown && Time.unscaledTime >= proximoTogglePausaTime)
        {
            pauseInput = true;
            proximoTogglePausaTime = Time.unscaledTime + toggleCooldownSegundos;
        }

        if (pauseInput)
        {
            TogglePause();
        }
        
        bool statsInput = Input.GetKeyDown(KeyCode.Tab);

        bool espTabDown = TryGetEspButtonDown(13, ref espTabPresionadoFrameAnterior);
        if (espTabDown && Time.unscaledTime >= proximoToggleTabTime)
        {
            statsInput = true;
            proximoToggleTabTime = Time.unscaledTime + toggleCooldownSegundos;
        }

        if (statsInput)
        {
            if (!isGamePaused)
            {
                ToggleStats();
            }
        }

        ProcesarNavegacionMenuPausaConEsp();
    }

    public void TogglePause()
    {
        SetPauseState(!isGamePaused);
    }
    
    public void ToggleStats()
    {
        statsAbierto = !statsAbierto;
        
        if (statsPanel != null)
        {
            statsPanel.SetActive(statsAbierto);
            
            if (statsAbierto && statsPanelScript != null)
            {
                statsPanelScript.ActualizarStats();
            }
        }
        
        // NO pausar el juego - el panel es solo visual
        // El juego continúa corriendo normalmente
    }
    
    public void ToggleOptions()
    {
        bool abrir = configurationPanel == null || !configurationPanel.activeSelf;

        if (abrir)
        {
            AbrirConfiguracionDirecta();
        }
        else
        {
            Cerrar_opciones();
        }
    }

    public void ResumeGame()
    {
        SetPauseState(false);
    }

    public void PauseGame()
    {
        SetPauseState(true);
    }

    public void MenuPrincipal()
    {
        Time.timeScale = 1;
        
        // Si estamos en multiplayer, desconectar de Photon antes de cambiar de escena
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[Menu] Desconectando de Photon antes de ir al menú principal...");
            PhotonNetwork.Disconnect();
        }
        
        SceneManager.LoadScene(0);
    }

    public void Opciones()
    {
        Abrir_opciones(true);
    }

    public void AbrirConfiguracionDirecta()
    {
        Abrir_opciones(false);
    }

    private void SetPauseState(bool pause)
    {
        isGamePaused = pause;

        if (isGamePaused)
        {
            Time.timeScale = 0f;
            EventSystem es = EventSystem.current;
            if (es != null)
            {
                es.sendNavigationEvents = true;
            }
        }
        else
        {
            Time.timeScale = 1f;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(isGamePaused);
        }

        if (isGamePaused)
        {
            InicializarSeleccionMenuPausa();
        }
        else
        {
            LimpiarSeleccionMenuPausa();
        }

        if (configurationPanel != null)
        {
            configurationPanel.SetActive(false);
        }
        
        // Cerrar stats si se abre el men\u00fa de pausa
        if (isGamePaused && statsPanel != null)
        {
            statsPanel.SetActive(false);
            statsAbierto = false;
        }

        if (!Application.isMobilePlatform)
        {
            Cursor.visible = isGamePaused;
            Cursor.lockState = isGamePaused ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }


    private void Abrir_opciones(bool pause)
    {
        opcionesAbiertasDesdePausa = pause;
        isGamePaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        LimpiarSeleccionMenuPausa();

        if (configurationPanel != null)
        {
            configurationPanel.SetActive(true);
        }

        if (!Application.isMobilePlatform)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }


    public void Cerrar_opciones()
    {
        if (opcionesAbiertasDesdePausa)
        {
            isGamePaused = true;
            Time.timeScale = 0f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            InicializarSeleccionMenuPausa();

            if (configurationPanel != null)
            {
                configurationPanel.SetActive(false);
            }

            if (!Application.isMobilePlatform)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
        else
        {
            SetPauseState(false);
        }
    }

    private bool TryGetEspPauseInput(out bool pausePressed)
    {
        pausePressed = false;

        if (!usarMenuEsp32)
            return false;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        return receiver.TryGetButtonPressed(9, out pausePressed);
    }

    private bool TryGetEspStatsInput(out bool statsPressed)
    {
        statsPressed = false;

        if (!usarMenuEsp32)
            return false;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        return receiver.TryGetButtonPressed(13, out statsPressed);
    }

    private bool TryGetEspButtonDown(int buttonIndex, ref bool previousPressed)
    {
        bool currentPressed = false;

        bool hasInput;
        if (buttonIndex == 9)
            hasInput = TryGetEspPauseInput(out currentPressed);
        else if (buttonIndex == 13)
            hasInput = TryGetEspStatsInput(out currentPressed);
        else
            return false;

        if (!hasInput)
            return false;

        bool buttonDown = currentPressed && !previousPressed;
        previousPressed = currentPressed;
        return buttonDown;
    }

    private bool TryGetEspJoyY(out float joyY)
    {
        joyY = 0f;

        if (!usarMenuEsp32)
            return false;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        return receiver.TryGetControlValues(out _, out _, out _, out joyY);
    }

    private bool TryGetEspTriggerDown(ref bool previousPressed)
    {
        if (!usarMenuEsp32)
            return false;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        if (!receiver.TryGetTriggerPressed(out bool currentPressed))
            return false;

        bool triggerDown = currentPressed && !previousPressed;
        previousPressed = currentPressed;
        return triggerDown;
    }

    private void ProcesarNavegacionMenuPausaConEsp()
    {
        if (!isGamePaused)
            return;

        if (pausePanel == null || !pausePanel.activeInHierarchy)
            return;

        if (configurationPanel != null && configurationPanel.activeInHierarchy)
            return;

        if (botonesPausa == null || botonesPausa.Length == 0)
            return;

        if (TryGetEspJoyY(out float joyY))
        {
            float absY = Mathf.Abs(joyY);

            if (absY < deadZoneNavegacion)
            {
                joystickNavegacionBloqueado = false;
            }
            else if (!joystickNavegacionBloqueado && Time.unscaledTime >= proximoMovimientoMenuTime)
            {
                int direccion = joyY > 0f ? -1 : 1;
                CambiarSeleccionPausa(direccion);
                joystickNavegacionBloqueado = true;
                proximoMovimientoMenuTime = Time.unscaledTime + cooldownNavegacionSegundos;
            }
        }

        if (TryGetEspTriggerDown(ref espTriggerPresionadoFrameAnterior))
        {
            ActivarBotonSeleccionado();
        }
    }

    private void CambiarSeleccionPausa(int direccion)
    {
        if (botonesPausa == null || botonesPausa.Length == 0)
            return;

        int total = botonesPausa.Length;
        indiceSeleccionPausa = (indiceSeleccionPausa + direccion + total) % total;
        SeleccionarBotonActual();
    }

    private void ActivarBotonSeleccionado()
    {
        if (botonesPausa == null || botonesPausa.Length == 0)
            return;

        Button actual = botonesPausa[indiceSeleccionPausa];
        if (actual == null || !actual.gameObject.activeInHierarchy || !actual.interactable)
            return;

        actual.onClick.Invoke();
    }

    private void InicializarSeleccionMenuPausa()
    {
        AutoAsignarBotonesPausaSiFaltan();

        joystickNavegacionBloqueado = false;
        espTriggerPresionadoFrameAnterior = false;

        if (botonesPausa == null || botonesPausa.Length == 0)
            return;

        indiceSeleccionPausa = 0;
        SeleccionarBotonActual();
    }

    private void LimpiarSeleccionMenuPausa()
    {
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            es.SetSelectedGameObject(null);
        }
    }

    private void SeleccionarBotonActual()
    {
        if (botonesPausa == null || botonesPausa.Length == 0)
            return;

        Button actual = botonesPausa[indiceSeleccionPausa];
        if (actual == null)
            return;

        EventSystem es = EventSystem.current;
        if (es == null)
            return;

        es.SetSelectedGameObject(actual.gameObject);
        actual.Select();
    }

    private void AutoAsignarBotonesPausaSiFaltan()
    {
        if (botonesPausa != null && botonesPausa.Length > 0)
            return;

        if (pausePanel == null)
            return;

        botonesPausa = pausePanel.GetComponentsInChildren<Button>(true);
    }

    private void AsegurarGraphicRaycaster()
    {
        if (pausePanel == null)
            return;

        Canvas canvas = pausePanel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = pausePanel.GetComponentInParent<Canvas>();
        }

        if (canvas != null)
        {
            pausePanelCanvas = canvas;
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[Menu] GraphicRaycaster agregado al pausePanel Canvas - toques deberían funcionar");
            }
        }
    }

    private void AsegurarEventSystem()
    {
        EventSystem existente = FindObjectOfType<EventSystem>();
        if (existente != null)
        {
            existente.gameObject.tag = "EventSystem";
            StandaloneInputModule module = existente.GetComponent<StandaloneInputModule>();
            if (module != null)
            {
                module.forceModuleActive = true;
            }
            return;
        }

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.tag = "EventSystem";
        EventSystem es = eventSystemGO.AddComponent<EventSystem>();
        
        StandaloneInputModule inputModule = eventSystemGO.AddComponent<StandaloneInputModule>();
        inputModule.forceModuleActive = true;
        
        Debug.Log("[Menu] EventSystem creado en Awake con StandaloneInputModule.forceModuleActive = true");
    }

}