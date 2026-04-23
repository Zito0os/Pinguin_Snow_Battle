using System.Globalization;
using System.Text;
using UnityEngine;

public class UsbSerialReceiver : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject plugin;
#endif

    public static UsbSerialReceiver Instance { get; private set; }

    [Header("Conexion")]
    public bool autoConnectOnStart = false;
    public int baudRate = 115200;

    [Header("Auto inicializacion")]
    public bool autoCenterAfterOpen = true;
    public float autoCenterDelay = 0.35f;
    public bool showOnScreenDebug = false;
    public bool reintentarTrasPermiso = true;
    public float primerReintentoTrasPermisoDelay = 0.2f;
    public float segundoReintentoTrasPermisoDelay = 1.0f;
    public float intervaloReintentoConexion = 1.0f;

    [Header("Estado")]
    public string statusText = "Esperando...";
    public string lastRawChunk = "";
    public string lastLine = "";
    public int parsedMessages = 0;

    [Header("Datos del ESP32 - Control analógico")]
    public float pitch = 0f;
    public float roll = 0f;
    public float joyX = 0f;
    public float joyY = 0f;

    [Header("Datos del ESP32 - Botones")]
    public int button = 0;           // índice 5: disparo/trigger
    public int btnCorrer = 0;        // índice 6: correr (joystick SW)
    public int btnSaltar = 0;        // índice 7: saltar (Space)
    public int btnTomar = 0;         // índice 8: tomar (E)
    public int btnPausa = 0;         // índice 9: pausa (ESC)
    public int btnGranada = 0;       // índice 10: granada (G)
    public int btnCurar = 0;         // índice 11: curar (X)
    public int btnTDisparo = 0;      // índice 12: cambiar tipo disparo (C)
    public int btnTab = 0;           // índice 13: tab
    public int btnApuntar = 0;       // índice 14: apuntar (click derecho)
    public int btnRecargar = 0;      // índice 15: recargar (R)

    [Header("Lectura de control")]
    public float inputFreshTime = 0.5f;

    [Header("Debug visual")]
    [TextArea(8, 20)]
    public string consoleText = "";

    private readonly StringBuilder serialBuffer = new StringBuilder();
    private float lastCtrlMessageTime = -999f;
    private bool isUsbOpen = false;
    private bool centerConfirmed = false;
    private bool autoSetupRequested = false;
    private bool setupSequenceRunning = false;
    private Coroutine forcedConnectionCoroutine;
    private bool vibracionHabilitadaEnEsp = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !UNITY_EDITOR
        EnsurePluginReady();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool EnsurePluginReady()
    {
        try
        {
            if (plugin == null)
            {
                AndroidJavaClass cls = new AndroidJavaClass("com.example.usbserialplugin.UsbSerialBridge");
                plugin = cls.CallStatic<AndroidJavaObject>("getInstance");
            }

            if (plugin == null)
                return false;

            plugin.Call("setUnityObject", gameObject.name);
            return true;
        }
        catch (System.Exception e)
        {
            statusText = "Error al iniciar plugin: " + e.Message;
            Debug.LogError(statusText);
            return false;
        }
    }
#endif

    public bool HasFreshInput()
    {
        return Time.unscaledTime - lastCtrlMessageTime <= inputFreshTime;
    }

    public bool TryGetControlValues(out float outPitch, out float outRoll, out float outJoyX, out float outJoyY)
    {
        outPitch = pitch;
        outRoll = roll;
        outJoyX = joyX;
        outJoyY = joyY;

        return parsedMessages > 0 && HasFreshInput();
    }

    public bool TryGetTriggerPressed(out bool triggerPressed)
    {
        triggerPressed = button == 1;
        return parsedMessages > 0 && HasFreshInput();
    }

    public bool TryGetButtonPressed(int buttonIndex, out bool isPressed)
    {
        isPressed = false;

        if (buttonIndex == 6)
            isPressed = btnCorrer == 1;
        else if (buttonIndex == 7)
            isPressed = btnSaltar == 1;
        else if (buttonIndex == 8)
            isPressed = btnTomar == 1;
        else if (buttonIndex == 9)
            isPressed = btnPausa == 1;
        else if (buttonIndex == 10)
            isPressed = btnGranada == 1;
        else if (buttonIndex == 11)
            isPressed = btnCurar == 1;
        else if (buttonIndex == 12)
            isPressed = btnTDisparo == 1;
        else if (buttonIndex == 13)
            isPressed = btnTab == 1;
        else if (buttonIndex == 14)
            isPressed = btnApuntar == 1;
        else if (buttonIndex == 15)
            isPressed = btnRecargar == 1;
        else
            return false;

        return parsedMessages > 0 && HasFreshInput();
    }

    public bool TryGetAllButtons(out int outCorrer, out int outSaltar, out int outTomar, out int outPausa,
                                 out int outGranada, out int outCurar, out int outTDisparo, out int outTab, out int outApuntar,
                                 out int outRecargar)
    {
        outCorrer = btnCorrer;
        outSaltar = btnSaltar;
        outTomar = btnTomar;
        outPausa = btnPausa;
        outGranada = btnGranada;
        outCurar = btnCurar;
        outTDisparo = btnTDisparo;
        outTab = btnTab;
        outApuntar = btnApuntar;
        outRecargar = btnRecargar;

        return parsedMessages > 0 && HasFreshInput();
    }

    public bool IsAutoSetupReady()
    {
        return isUsbOpen && centerConfirmed;
    }

    public bool IsAutoSetupInProgress()
    {
        return autoSetupRequested && !IsAutoSetupReady();
    }

    public void StartAutoSetup()
    {
        autoSetupRequested = true;
        isUsbOpen = false;
        centerConfirmed = false;
        setupSequenceRunning = false;
        if (forcedConnectionCoroutine != null)
        {
            StopCoroutine(forcedConnectionCoroutine);
            forcedConnectionCoroutine = null;
        }

        forcedConnectionCoroutine = StartCoroutine(ForzarConexionHastaConectar());
        ConnectUsb();
    }

    private System.Collections.IEnumerator ForzarConexionHastaConectar()
    {
        while (autoSetupRequested && !IsAutoSetupReady())
        {
            ConnectUsb();
            yield return new WaitForSecondsRealtime(intervaloReintentoConexion);
        }

        forcedConnectionCoroutine = null;
    }

    private System.Collections.IEnumerator EjecutarDobleIntentoDespuesPermiso()
    {
        if (setupSequenceRunning)
            yield break;

        setupSequenceRunning = true;

        yield return new WaitForSecondsRealtime(primerReintentoTrasPermisoDelay);
        ConnectUsb();

        yield return new WaitForSecondsRealtime(segundoReintentoTrasPermisoDelay);
        ConnectUsb();

        setupSequenceRunning = false;
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (EnsurePluginReady())
        {
            statusText = "Plugin Android listo";

            if (autoConnectOnStart)
            {
                StartAutoSetup();
            }
        }
#else
        statusText = "Solo funciona en Android real";
#endif
    }

    public void ConnectUsb()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!EnsurePluginReady())
            {
                statusText = "Plugin Android no disponible";
                Debug.LogError(statusText);
                return;
            }

            bool ok = plugin.Call<bool>("openFirst", baudRate);
            statusText = ok ? "Intentando abrir USB..." : "No se pudo abrir o se pidio permiso";
            Debug.Log(statusText);
        }
        catch (System.Exception e)
        {
            statusText = "Error ConnectUsb: " + e.Message;
            Debug.LogError(statusText);
        }
#else
        statusText = "ConnectUsb solo funciona en Android";
        Debug.Log(statusText);
#endif
    }

    public void CloseUsb()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!EnsurePluginReady())
                return;

            plugin.Call("close");
            statusText = "USB cerrado";
            Debug.Log(statusText);
        }
        catch (System.Exception e)
        {
            statusText = "Error CloseUsb: " + e.Message;
            Debug.LogError(statusText);
        }
#endif
    }

    public void SendText(string text)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!EnsurePluginReady())
                return;

            bool ok = plugin.Call<bool>("write", text);
            statusText = ok ? "Enviado: " + text : "No se pudo enviar";
            Debug.Log(statusText);
        }
        catch (System.Exception e)
        {
            statusText = "Error SendText: " + e.Message;
            Debug.LogError(statusText);
        }
#endif
    }

    public void SendCenter()
    {
        SendText("CENTER");

        if (autoSetupRequested)
        {
            centerConfirmed = true;
        }
    }

    public void SendPing()
    {
        SendText("PING");
    }

    public void EnsureVibrationEnabled()
    {
        if (vibracionHabilitadaEnEsp)
            return;

        SendText("VIBRAR_ON");
        vibracionHabilitadaEnEsp = true;
    }

    public void VibrarMs(int duracionMs)
    {
        if (duracionMs <= 0)
            return;

        EnsureVibrationEnabled();
        SendText("VIBRAR," + duracionMs.ToString(CultureInfo.InvariantCulture));
    }

    // ===== Callbacks desde Java =====

    public void OnUsbOpen(string msg)
    {
        isUsbOpen = true;
        vibracionHabilitadaEnEsp = false;
        statusText = "USB abierto: " + msg;
        AddConsoleLine(statusText);
        Debug.Log(statusText);

        if (autoCenterAfterOpen)
        {
            CancelInvoke(nameof(SendCenter));
            Invoke(nameof(SendCenter), autoCenterDelay);
        }
    }

    public void OnUsbClosed(string msg)
    {
        isUsbOpen = false;
        vibracionHabilitadaEnEsp = false;
        statusText = "USB cerrado: " + msg;
        AddConsoleLine(statusText);
        Debug.Log(statusText);
    }

    public void OnUsbPermission(string msg)
    {
        statusText = "Permiso USB: " + msg;
        AddConsoleLine(statusText);
        Debug.Log(statusText);

        if (autoSetupRequested && msg == "granted")
        {
            // Reintentos solo DESPUES de aceptar el popup de permiso USB.
            if (reintentarTrasPermiso)
            {
                StopCoroutine(nameof(EjecutarDobleIntentoDespuesPermiso));
                StartCoroutine(EjecutarDobleIntentoDespuesPermiso());
            }
        }
    }

    public void OnUsbError(string msg)
    {
        statusText = "USB error: " + msg;
        AddConsoleLine(statusText);
        Debug.LogError(statusText);
    }

    public void OnUsbData(string data)
    {
        lastRawChunk = data;
        serialBuffer.Append(data);

        string current = serialBuffer.ToString();
        int newlineIndex;

        while ((newlineIndex = current.IndexOf('\n')) >= 0)
        {
            string line = current.Substring(0, newlineIndex).Trim();
            current = current.Substring(newlineIndex + 1);

            if (!string.IsNullOrEmpty(line))
            {
                ParseLine(line);
            }
        }

        serialBuffer.Clear();
        serialBuffer.Append(current);
    }

    private void ParseLine(string line)
    {
        lastLine = line;
        AddConsoleLine("RX: " + line);
        Debug.Log("RX line: " + line);

        string[] parts = line.Split(',');

        if (parts.Length == 0)
            return;

        // Mensajes informativos tipo: INFO,CENTERED
        if (parts[0] == "INFO")
        {
            statusText = line;

            if (line.Contains("GYRO_CENTERED"))
            {
                centerConfirmed = true;
            }

            return;
        }

        // Formato esperado:
        // CTRL,pitch,roll,joyX,joyY,btn,correr,saltar,tomar,pausa,granada,curar,t_disparo,tab,apuntar,recargar
        if (parts[0] != "CTRL")
            return;

        if (parts.Length < 16)
        {
            statusText = "Linea CTRL incompleta (necesita 16 valores)";
            return;
        }

        if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedPitch))
            return;

        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedRoll))
            return;

        if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedJoyX))
            return;

        if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedJoyY))
            return;

        if (!int.TryParse(parts[5], out int parsedButton))
            return;

        if (!int.TryParse(parts[6], out int parsedCorrer))
            return;

        if (!int.TryParse(parts[7], out int parsedSaltar))
            return;

        if (!int.TryParse(parts[8], out int parsedTomar))
            return;

        if (!int.TryParse(parts[9], out int parsedPausa))
            return;

        if (!int.TryParse(parts[10], out int parsedGranada))
            return;

        if (!int.TryParse(parts[11], out int parsedCurar))
            return;

        if (!int.TryParse(parts[12], out int parsedTDisparo))
            return;

        if (!int.TryParse(parts[13], out int parsedTab))
            return;

        if (!int.TryParse(parts[14], out int parsedApuntar))
            return;

        if (!int.TryParse(parts[15], out int parsedRecargar))
            return;

        pitch = parsedPitch;
        roll = parsedRoll;
        joyX = parsedJoyX;
        joyY = parsedJoyY;
        button = parsedButton;
        btnCorrer = parsedCorrer;
        btnSaltar = parsedSaltar;
        btnTomar = parsedTomar;
        btnPausa = parsedPausa;
        btnGranada = parsedGranada;
        btnCurar = parsedCurar;
        btnTDisparo = parsedTDisparo;
        btnTab = parsedTab;
        btnApuntar = parsedApuntar;
        btnRecargar = parsedRecargar;

        parsedMessages++;
        lastCtrlMessageTime = Time.unscaledTime;
        statusText = "Datos actualizados";
    }

    private void AddConsoleLine(string line)
    {
        consoleText += line + "\n";

        if (consoleText.Length > 2500)
        {
            consoleText = consoleText.Substring(consoleText.Length - 2500);
        }
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug)
            return;

        //ESTO ES LO DE PRUEBA
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 28;
        labelStyle.wordWrap = true;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 26;

        float w = Screen.width;
        float h = Screen.height;

        float x = 20f;
        float buttonW = w - 40f;
        float buttonH = 85f;

        if (GUI.Button(new Rect(x, 20, buttonW, buttonH), "Connect USB", buttonStyle))
        {
            ConnectUsb();
        }

        if (GUI.Button(new Rect(x, 120, buttonW, buttonH), "CENTER", buttonStyle))
        {
            SendCenter();
        }

        if (GUI.Button(new Rect(x, 220, buttonW, buttonH), "PING", buttonStyle))
        {
            SendPing();
        }

        if (GUI.Button(new Rect(x, 320, buttonW, buttonH), "Close USB", buttonStyle))
        {
            CloseUsb();
        }

        string info =
            "STATUS: " + statusText + "\n\n" +
            "pitch: " + pitch.ToString("F2", CultureInfo.InvariantCulture) + "\n" +
            "roll: " + roll.ToString("F2", CultureInfo.InvariantCulture) + "\n" +
            "joyX: " + joyX.ToString("F2", CultureInfo.InvariantCulture) + "\n" +
            "joyY: " + joyY.ToString("F2", CultureInfo.InvariantCulture) + "\n" +
            "button: " + button + "\n" +
            "parsedMessages: " + parsedMessages + "\n\n" +
            "lastLine: " + lastLine + "\n\n" +
            "console:\n" + consoleText;

        GUI.Label(new Rect(x, 430, w - 40, h - 450), info, labelStyle);
    }
}