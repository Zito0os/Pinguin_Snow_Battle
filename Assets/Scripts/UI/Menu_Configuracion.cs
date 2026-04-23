using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class Menu_Configuracion : MonoBehaviour
{
    public Slider musicVolumeSlider;
    public float sliderVolume;
    public Image imageMute;
    public TMP_Dropdown qualityDropdown;
    public Slider sensitivitySlider;
    public GameObject cameraLookObject;
    public float sensibilidadMin = 10f;
    public float sensibilidadMax = 1000f;

    [Header("ESP Navegacion Configuracion")]
    public bool usarMenuEsp32 = true;
    public Selectable[] elementosConfiguracion;
    public float deadZoneNavegacion = 0.45f;
    public float deadZoneHorizontalSlider = 0.35f;
    public float cooldownNavegacionSegundos = 0.2f;
    public float pasoSliderNormalizado = 0.1f;

    private int indiceSeleccionConfiguracion = 0;
    private bool joystickNavegacionBloqueado = false;
    private bool joystickHorizontalBloqueado = false;
    private bool espTriggerPresionadoFrameAnterior = false;
    private float proximoMovimientoMenuTime = 0f;

    private const string MusicVolumeKey = "MusicVolume";
    private const string QualityLevelKey = "QualityLevel";


    private void Awake()
    {
        AsegurarEventSystem();
    }


    void Start()
    {
        sliderVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f);
        musicVolumeSlider.value = sliderVolume;
        AudioListener.volume = sliderVolume;

        ConfigurarSensibilidad();

        ConfigurarCalidad();
        RevisarSiEstoyMute();

        AutoAsignarElementosConfiguracionSiFaltan();
        InicializarSeleccionConfiguracion();
    }

    private void Update()
    {
        ProcesarNavegacionConfiguracionConEsp();
    }

    private void ConfigurarSensibilidad()
    {
        Cameralook cameralook = ObtenerCameraLook();
        if (sensitivitySlider == null || cameralook == null)
            return;

        if (SliderEsNormalizado())
        {
            float valorNormalizado = Mathf.InverseLerp(sensibilidadMin, sensibilidadMax, cameralook.sensitivity);
            sensitivitySlider.SetValueWithoutNotify(valorNormalizado);
        }
        else
        {
            sensitivitySlider.SetValueWithoutNotify(cameralook.sensitivity);
        }

        sensitivitySlider.onValueChanged.RemoveListener(ChangeSensitivitySlider);
        sensitivitySlider.onValueChanged.AddListener(ChangeSensitivitySlider);
    }

    public void ChangeSensitivitySlider(float valor)
    {
        Cameralook cameralook = ObtenerCameraLook();
        if (cameralook != null)
        {
            if (SliderEsNormalizado())
            {
                cameralook.sensitivity = Mathf.Lerp(sensibilidadMin, sensibilidadMax, valor);
            }
            else
            {
                cameralook.sensitivity = valor;
            }
        }
    }

    private bool SliderEsNormalizado()
    {
        if (sensitivitySlider == null)
            return false;

        return sensitivitySlider.maxValue <= 1.01f && sensitivitySlider.minValue >= -0.01f;
    }

    private Cameralook ObtenerCameraLook()
    {
        if (cameraLookObject == null)
            return null;

        return cameraLookObject.GetComponent<Cameralook>();
    }

    //void Update()
    //{
    //    RevisarSiEstoyMute();
    //}

    public void ChangeSlider(float valor)
    {
        sliderVolume = valor;
        PlayerPrefs.SetFloat(MusicVolumeKey, sliderVolume);
        AudioListener.volume = sliderVolume;
        RevisarSiEstoyMute();
    }

    private void ConfigurarCalidad()
    {
        if (qualityDropdown == null)
            return;

        int nivelGuardado = PlayerPrefs.GetInt(QualityLevelKey, 2);
        int nivelClamped = Mathf.Clamp(nivelGuardado, 0, 2);

        qualityDropdown.SetValueWithoutNotify(nivelClamped);
        AplicarCalidadPorIndice(nivelClamped);
        Debug.Log($"[Calidad] Inicial aplicada: indice={nivelClamped}, nombre={QualitySettings.names[nivelClamped]}");

        qualityDropdown.onValueChanged.RemoveListener(OnQualityDropdownChanged);
        qualityDropdown.onValueChanged.AddListener(OnQualityDropdownChanged);
    }

    public void OnQualityDropdownChanged(int indice)
    {
        int nivelClamped = Mathf.Clamp(indice, 0, 2);
        AplicarCalidadPorIndice(nivelClamped);
        PlayerPrefs.SetInt(QualityLevelKey, nivelClamped);
        Debug.Log($"[Calidad] Seleccionada: indice={nivelClamped}, nombre={QualitySettings.names[nivelClamped]}");
    }

    private void AplicarCalidadPorIndice(int indice)
    {
        QualitySettings.SetQualityLevel(indice, true);
    }


    public void RevisarSiEstoyMute()
    {
        if (sliderVolume == 0)
        {
            imageMute.enabled = true;
        }
        else
        {
            imageMute.enabled = false;
        }
    }

    private void ProcesarNavegacionConfiguracionConEsp()
    {
        if (!usarMenuEsp32)
            return;

        if (!gameObject.activeInHierarchy)
            return;

        if (elementosConfiguracion == null || elementosConfiguracion.Length == 0)
            return;

        if (TryGetEspJoystick(out float joyX, out float joyY))
        {
            float absY = Mathf.Abs(joyY);

            if (absY < deadZoneNavegacion)
            {
                joystickNavegacionBloqueado = false;
            }
            else if (!joystickNavegacionBloqueado && Time.unscaledTime >= proximoMovimientoMenuTime)
            {
                int direccion = joyY > 0f ? -1 : 1;
                CambiarSeleccionConfiguracion(direccion);
                joystickNavegacionBloqueado = true;
                proximoMovimientoMenuTime = Time.unscaledTime + cooldownNavegacionSegundos;
            }

            AjustarSliderHorizontalSiCorresponde(joyX);
        }

        if (TryGetEspTriggerDown(ref espTriggerPresionadoFrameAnterior))
        {
            ActivarElementoConfiguracionSeleccionado();
        }
    }

    private bool TryGetEspJoystick(out float joyX, out float joyY)
    {
        joyX = 0f;
        joyY = 0f;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        return receiver.TryGetControlValues(out _, out _, out joyX, out joyY);
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

    private void CambiarSeleccionConfiguracion(int direccion)
    {
        int total = elementosConfiguracion.Length;
        if (total == 0)
            return;

        for (int i = 0; i < total; i++)
        {
            indiceSeleccionConfiguracion = (indiceSeleccionConfiguracion + direccion + total) % total;
            Selectable candidato = elementosConfiguracion[indiceSeleccionConfiguracion];
            if (candidato != null && candidato.gameObject.activeInHierarchy && candidato.interactable)
            {
                SeleccionarElementoActual();
                return;
            }
        }
    }

    private void AjustarSliderHorizontalSiCorresponde(float joyX)
    {
        if (elementosConfiguracion == null || elementosConfiguracion.Length == 0)
            return;

        Selectable actual = elementosConfiguracion[indiceSeleccionConfiguracion];
        if (!(actual is Slider slider))
            return;

        float absX = Mathf.Abs(joyX);
        if (absX < deadZoneHorizontalSlider)
        {
            joystickHorizontalBloqueado = false;
            return;
        }

        if (joystickHorizontalBloqueado || Time.unscaledTime < proximoMovimientoMenuTime)
            return;

        float paso = Mathf.Max(0.01f, (slider.maxValue - slider.minValue) * pasoSliderNormalizado);
        float direccion = joyX > 0f ? 1f : -1f;
        float nuevoValor = Mathf.Clamp(slider.value + (paso * direccion), slider.minValue, slider.maxValue);
        slider.value = nuevoValor;

        joystickHorizontalBloqueado = true;
        proximoMovimientoMenuTime = Time.unscaledTime + cooldownNavegacionSegundos;
    }

    private void ActivarElementoConfiguracionSeleccionado()
    {
        if (elementosConfiguracion == null || elementosConfiguracion.Length == 0)
            return;

        Selectable actual = elementosConfiguracion[indiceSeleccionConfiguracion];
        if (actual == null || !actual.gameObject.activeInHierarchy || !actual.interactable)
            return;

        if (actual is Button button)
        {
            button.onClick.Invoke();
            return;
        }

        if (actual is Toggle toggle)
        {
            toggle.isOn = !toggle.isOn;
            return;
        }

        if (actual is TMP_Dropdown dropdown)
        {
            if (dropdown.options != null && dropdown.options.Count > 0)
            {
                int siguiente = (dropdown.value + 1) % dropdown.options.Count;
                dropdown.value = siguiente;
            }
            return;
        }

        if (actual is Slider)
        {
            // En sliders: gatillo confirma el valor actual y avanza a la siguiente opción.
            CambiarSeleccionConfiguracion(1);
            return;
        }
    }

    private void InicializarSeleccionConfiguracion()
    {
        if (elementosConfiguracion == null || elementosConfiguracion.Length == 0)
            return;

        indiceSeleccionConfiguracion = 0;
        joystickNavegacionBloqueado = false;
        joystickHorizontalBloqueado = false;
        espTriggerPresionadoFrameAnterior = false;
        SeleccionarElementoActual();
    }

    private void SeleccionarElementoActual()
    {
        if (elementosConfiguracion == null || elementosConfiguracion.Length == 0)
            return;

        Selectable actual = elementosConfiguracion[indiceSeleccionConfiguracion];
        if (actual == null)
            return;

        EventSystem es = EventSystem.current;
        if (es == null)
            return;

        es.SetSelectedGameObject(actual.gameObject);
        actual.Select();
    }

    private void AutoAsignarElementosConfiguracionSiFaltan()
    {
        if (elementosConfiguracion != null && elementosConfiguracion.Length > 0)
            return;

        elementosConfiguracion = GetComponentsInChildren<Selectable>(true);
    }

    private void AsegurarEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.tag = "EventSystem";
        eventSystemGO.AddComponent<EventSystem>();

        StandaloneInputModule inputModule = eventSystemGO.AddComponent<StandaloneInputModule>();
        inputModule.forceModuleActive = true;
    }
}
