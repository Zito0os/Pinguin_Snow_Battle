using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Menu_principal : MonoBehaviour
{
    [Header("Escenas")]
    public int sceneSingleplayerIndex = 1;
    public int sceneMultiplayerIndex = 2;

    [Header("USB auto setup")]
    public bool prepararUsbAntesDeSingleplayer = true;
    private bool cargandoSingleplayer = false;

    [Header("ESP Navegacion Menu Principal")]
    public bool usarMenuEsp32 = true;
    public bool prepararUsbParaNavegacionMenu = true;
    public Button[] botonesMenuPrincipal;
    public float deadZoneNavegacion = 0.45f;
    public float cooldownNavegacionSegundos = 0.2f;

    private int indiceSeleccionMenu = 0;
    private bool joystickNavegacionBloqueado = false;
    private bool espTriggerPresionadoFrameAnterior = false;
    private float proximoMovimientoMenuTime = 0f;

    private void OnEnable()
    {
        AutoAsignarBotonesSiFaltan();
        InicializarSeleccionMenuPrincipal();
    }

    private void Start()
    {
        AsegurarEventSystem();
        AutoAsignarBotonesSiFaltan();
        InicializarSeleccionMenuPrincipal();
        InicializarUsbParaNavegacionMenu();
    }

    private void Update()
    {
        if (botonesMenuPrincipal != null && botonesMenuPrincipal.Length > 0)
        {
            Button seleccionado = ObtenerBotonSeleccionadoActual();
            if (!EsBotonValido(seleccionado))
            {
                SeleccionarPrimerBotonValido();
            }
        }

        ProcesarNavegacionMenuPrincipalConEsp();
    }

    public void Cargar_Singleplayer()
    {
        if (cargandoSingleplayer)
            return;

        StartCoroutine(CargarSingleplayerConPreparacion());
    }

    public void Cargar_Multiplayer()
    {
        SceneManager.LoadScene(sceneMultiplayerIndex);
    }

    private IEnumerator CargarSingleplayerConPreparacion()
    {
        cargandoSingleplayer = true;

        if (!prepararUsbAntesDeSingleplayer)
        {
            SceneManager.LoadScene(sceneSingleplayerIndex);
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;

        if (receiver == null)
        {
            GameObject receiverObj = new GameObject("UsbSerialReceiver_Auto");
            receiver = receiverObj.AddComponent<UsbSerialReceiver>();
        }

        receiver.showOnScreenDebug = false;
        receiver.autoConnectOnStart = false;
        receiver.StartAutoSetup();

        while (receiver != null && !receiver.IsAutoSetupReady())
        {
            yield return null;
        }
#endif

        SceneManager.LoadScene(sceneSingleplayerIndex);
    }


    public void Salir()
    {
#if UNITY_EDITOR 
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void InicializarUsbParaNavegacionMenu()
    {
        if (!usarMenuEsp32 || !prepararUsbParaNavegacionMenu)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
        {
            GameObject receiverObj = new GameObject("UsbSerialReceiver_MenuPrincipal");
            receiver = receiverObj.AddComponent<UsbSerialReceiver>();
        }

        receiver.showOnScreenDebug = false;
        receiver.autoConnectOnStart = false;
        receiver.StartAutoSetup();
#endif
    }

    private void ProcesarNavegacionMenuPrincipalConEsp()
    {
        if (!usarMenuEsp32)
            return;

        if (botonesMenuPrincipal == null || botonesMenuPrincipal.Length == 0)
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
                CambiarSeleccionMenuPrincipal(direccion);
                joystickNavegacionBloqueado = true;
                proximoMovimientoMenuTime = Time.unscaledTime + cooldownNavegacionSegundos;
            }
        }

        if (TryGetEspTriggerDown(ref espTriggerPresionadoFrameAnterior))
        {
            ActivarBotonSeleccionadoMenuPrincipal();
        }
    }

    private bool TryGetEspJoyY(out float joyY)
    {
        joyY = 0f;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        return receiver.TryGetControlValues(out _, out _, out _, out joyY);
    }

    private bool TryGetEspTriggerDown(ref bool previousPressed)
    {
        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        if (!receiver.TryGetTriggerPressed(out bool currentPressed))
            return false;

        bool triggerDown = currentPressed && !previousPressed;
        previousPressed = currentPressed;
        return triggerDown;
    }

    private void CambiarSeleccionMenuPrincipal(int direccion)
    {
        if (botonesMenuPrincipal == null || botonesMenuPrincipal.Length == 0)
            return;

        int total = botonesMenuPrincipal.Length;

        for (int i = 0; i < total; i++)
        {
            indiceSeleccionMenu = (indiceSeleccionMenu + direccion + total) % total;
            Button candidato = botonesMenuPrincipal[indiceSeleccionMenu];
            if (EsBotonValido(candidato))
            {
                SeleccionarBotonActual();
                return;
            }
        }
    }

    private void ActivarBotonSeleccionadoMenuPrincipal()
    {
        Button actual = ObtenerBotonSeleccionadoActual();
        if (!EsBotonValido(actual))
        {
            SeleccionarPrimerBotonValido();
            actual = ObtenerBotonSeleccionadoActual();
        }

        if (actual == null || !actual.gameObject.activeInHierarchy || !actual.interactable)
            return;

        actual.onClick.Invoke();
    }

    private void InicializarSeleccionMenuPrincipal()
    {
        if (botonesMenuPrincipal == null || botonesMenuPrincipal.Length == 0)
            return;

        indiceSeleccionMenu = 0;
        joystickNavegacionBloqueado = false;
        espTriggerPresionadoFrameAnterior = false;
        SeleccionarPrimerBotonValido();
    }

    private void SeleccionarPrimerBotonValido()
    {
        if (botonesMenuPrincipal == null || botonesMenuPrincipal.Length == 0)
            return;

        for (int i = 0; i < botonesMenuPrincipal.Length; i++)
        {
            if (EsBotonValido(botonesMenuPrincipal[i]))
            {
                indiceSeleccionMenu = i;
                SeleccionarBotonActual();
                return;
            }
        }
    }

    private Button ObtenerBotonSeleccionadoActual()
    {
        if (botonesMenuPrincipal == null || botonesMenuPrincipal.Length == 0)
            return null;

        if (indiceSeleccionMenu < 0 || indiceSeleccionMenu >= botonesMenuPrincipal.Length)
            return null;

        return botonesMenuPrincipal[indiceSeleccionMenu];
    }

    private bool EsBotonValido(Button boton)
    {
        return boton != null && boton.gameObject.activeInHierarchy && boton.interactable;
    }

    private void SeleccionarBotonActual()
    {
        if (botonesMenuPrincipal == null || botonesMenuPrincipal.Length == 0)
            return;

        Button actual = botonesMenuPrincipal[indiceSeleccionMenu];
        if (actual == null)
            return;

        EventSystem es = EventSystem.current;
        if (es == null)
            return;

        es.SetSelectedGameObject(actual.gameObject);
        actual.Select();
    }

    private void AutoAsignarBotonesSiFaltan()
    {
        if (botonesMenuPrincipal != null && botonesMenuPrincipal.Length > 0)
            return;

        botonesMenuPrincipal = GetComponentsInChildren<Button>(true);
    }

    private void AsegurarEventSystem()
    {
        EventSystem existente = FindObjectOfType<EventSystem>();
        if (existente != null)
            return;

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.tag = "EventSystem";
        eventSystemGO.AddComponent<EventSystem>();

        StandaloneInputModule inputModule = eventSystemGO.AddComponent<StandaloneInputModule>();
        inputModule.forceModuleActive = true;
    }


}
