using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;


public class Cameralook : MonoBehaviourPunCallbacks
{
    [Header("Multijugador")]
    public string nombreEscenaMultiplayer = "MultiPlayer";
    private bool usarPhotonEnEscena = false;

    public float sensitivity = 80f;
    [Header("ESP-32")]
    public bool usarInputEsp32 = true;
    //posicion del player
    public Transform playerBody;

    float xRotation = 0f;


    [Header("ESP Camera")]
    public float espLookMultiplier = 4f;
    public float espDeltaDeadZone = 0.03f;
    public float espSmooth = 12f;

    private bool espInitialized = false;
    private float prevEspPitch = 0f;
    private float prevEspRoll = 0f;
    private float smoothEspX = 0f;
    private float smoothEspY = 0f;


    [Header("ESP Gyro")]
    public float espSensitivityX = 2.0f;
    public float espSensitivityY = 1.2f;
    //public float espSmooth = 14f;

    private float smoothLookX = 0f;
    private float smoothLookY = 0f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();

        if (!EsControlLocal())
        {
            // Deshabilitar cámara del jugador remoto
            Camera cam = GetComponent<Camera>();
            if (cam != null)
                cam.enabled = false;

            // CRÍTICO: Deshabilitar AudioListener del jugador remoto
            AudioListener listener = GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = false;
                Debug.Log($"[Cameralook] AudioListener DESHABILITADO en jugador remoto");
            }
            
            // Deshabilitar TODOS los AudioListeners en este jugador remoto
            AudioListener[] allListeners = GetComponentsInChildren<AudioListener>(true);
            foreach (AudioListener al in allListeners)
            {
                al.enabled = false;
                Debug.Log($"[Cameralook] AudioListener DESHABILITADO en: {al.gameObject.name}");
            }

            return;
        }

        Debug.Log("[Cameralook] Jugador LOCAL - AudioListener permanece activo");
        
        // Asegurar que solo hay UN AudioListener activo en el jugador local
        AudioListener[] listeners = GetComponentsInChildren<AudioListener>(true);
        bool foundMain = false;
        foreach (AudioListener al in listeners)
        {
            if (!foundMain && al.gameObject == gameObject)
            {
                al.enabled = true;
                foundMain = true;
                Debug.Log($"[Cameralook] AudioListener PRINCIPAL activo en: {al.gameObject.name}");
            }
            else
            {
                al.enabled = false;
                Debug.Log($"[Cameralook] AudioListener SECUNDARIO deshabilitado en: {al.gameObject.name}");
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    // Update is called once per frame
    //void Update()
    //{
    //    if (EsControlLocal())
    //    {
    //        // Si el EmotePanel está abierto, no procesar input de cámara
    //        if (EmotePanel.isEmotePanelActive)
    //            return;

    //        // esto guarda la rotacion del mouse en el eje X y Y
    //        float inputX = Input.GetAxis("Mouse X");
    //        float inputY = Input.GetAxis("Mouse Y");

    //        if (usarInputEsp32 && TryGetEspCameraInput(out float espPitch, out float espRoll))
    //        {
    //            inputX = espPitch;
    //            inputY = espRoll;
    //        }

    //        float mouseX = inputX * sensitivity * Time.deltaTime;
    //        float mouseY = inputY * sensitivity * Time.deltaTime;

    //        xRotation -= mouseY;

    //        xRotation = Mathf.Clamp(xRotation, -90f, 90f);//minimo y maximo de la rotacion de la camara en el eje X

    //        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);// esto hace que la camara gire en el eje X, y se le asigna a la camara la rotacion en el eje X, Y y Z



    //        //el rotate hace que la camara gire en el eje X
    //        playerBody.Rotate(Vector3.up * mouseX);
    //    }




    //}
    //void Update()
    //{
    //    if (!EsControlLocal())
    //        return;

    //    if (EmotePanel.isEmotePanelActive)
    //        return;

    //    float lookX;
    //    float lookY;

    //    if (usarInputEsp32 && TryGetEspCameraInput(out float espPitch, out float espRoll))
    //    {
    //        if (!espInitialized)
    //        {
    //            prevEspPitch = espPitch;
    //            prevEspRoll = espRoll;
    //            espInitialized = true;
    //            return;
    //        }

    //        // roll = izquierda / derecha
    //        // pitch = arriba / abajo
    //        float deltaRoll = espRoll - prevEspRoll;
    //        float deltaPitch = espPitch - prevEspPitch;

    //        prevEspRoll = espRoll;
    //        prevEspPitch = espPitch;

    //        if (Mathf.Abs(deltaRoll) < espDeltaDeadZone)
    //            deltaRoll = 0f;

    //        if (Mathf.Abs(deltaPitch) < espDeltaDeadZone)
    //            deltaPitch = 0f;

    //        smoothEspX = Mathf.Lerp(smoothEspX, deltaRoll, Time.deltaTime * espSmooth);
    //        smoothEspY = Mathf.Lerp(smoothEspY, deltaPitch, Time.deltaTime * espSmooth);

    //        lookX = smoothEspX * espLookMultiplier;
    //        lookY = smoothEspY * espLookMultiplier;
    //    }
    //    else
    //    {
    //        espInitialized = false;

    //        lookX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
    //        lookY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
    //    }

    //    xRotation -= lookY;
    //    xRotation = Mathf.Clamp(xRotation, -90f, 90f);

    //    transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    //    playerBody.Rotate(Vector3.up * lookX);
    //}


    void Update()
    {
        if (!EsControlLocal())
            return;

        // Si el juego está pausado, no procesar input de cámara
        // Esto permite que el EventSystem procese eventos UI en Android
        if (Time.timeScale == 0)
            return;

        if (EmotePanel.isEmotePanelActive)
            return;

        float mouseX;
        float mouseY;

        if (usarInputEsp32 && TryGetEspCameraInput(out float espLookX, out float espLookY))
        {
            smoothLookX = Mathf.Lerp(smoothLookX, espLookX, Time.deltaTime * espSmooth);
            smoothLookY = Mathf.Lerp(smoothLookY, espLookY, Time.deltaTime * espSmooth);

            mouseX = smoothLookX * espSensitivityX * Time.deltaTime;
            mouseY = smoothLookY * espSensitivityY * Time.deltaTime;
        }
        else
        {
            mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private bool TryGetEspCameraInput(out float pitchInput, out float rollInput)
    {
        pitchInput = 0f;
        rollInput = 0f;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        if (!receiver.TryGetControlValues(out pitchInput, out rollInput, out _, out _))
            return false;

        return true;
    }

    private bool EsControlLocal()
    {
        if (!usarPhotonEnEscena)
            return true;

        return photonView != null && photonView.IsMine;
    }

    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == nombreEscenaMultiplayer || escena == "MultiPlayer" || escena == "Multi_Player";
    }
}
