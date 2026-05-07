using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class WeaponLogic : MonoBehaviourPunCallbacks
{
    [Header("Multijugador")]
    public string nombreEscenaMultiplayer = "MultiPlayer";
    private bool usarPhotonEnEscena = false;

    [Header("ESP-32")]
    public bool usarDisparoEsp32 = true;

    [Header("Vibracion ESP-32")]
    public bool usarVibracionEsp32 = true;
    public int vibracionDisparoNoContinuoMs = 100;
    public int vibracionPulsoContinuoMs = 140;
    public float intervaloPulsoContinuoSegundos = 0.08f;


    public Transform spawnPoint;

    public GameObject bullet;


    public float shotforce = 15f;
    public float shotRate = 0.3f;

    [Header("Apuntado por Raycast")]
    public bool usarRaycastParaApuntar = true;
    public Camera camaraApuntado;
    public float distanciaMaximaApuntado = 300f;
    public LayerMask mascaraApuntado = ~0;

    private float shootRateTime = 0f;

    private AudioSource audioSource;

    public AudioClip shotSound;
    [Header("Sonidos de acciones")]
    public AudioClip recargaSound;
    public AudioClip cambiarModoDisparoSound;
    public AudioClip curarSound;

    [Header("Animacion de disparo (personaje)")]
    public Animator animatorDisparo;
    public string parametroDisparo = "disparo";
    public float delayDisparo = 0.12f;
    public float duracionPulsoBoolDisparo = 0.05f;

    public bool continueShooting = false;
    public float tiempoRecarga = 3f;
    public float tiempoCuracion = 3f;
    public GameObject recargandoUI;
    public GameObject curandoUI;
    [Header("Autovincular UI Multiplayer")]
    public bool autovincularUIEnMultiplayer = true;
    public string nombreObjRecargando = "Recargando";
    public string nombreObjCurando = "Curando";
    public string nombreObjContinue = "Modo_arma";
    [Header("UI Continue Shooting")]
    public TextMeshProUGUI continueShootingText;
    public string textoAutomatico = "Automatico";
    public string textoManual = "Manual";

    private bool recargando = false;
    private Coroutine recargaCoroutine;
    private bool curando = false;
    private Coroutine curarCoroutine;
    private bool espTriggerPresionadoFrameAnterior = false;
    private bool espAimPresionadoFrameAnterior = false;
    private bool espModoDisparoPresionadoFrameAnterior = false;
    private bool espCurarPresionadoFrameAnterior = false;
    private bool espRecargarPresionadoFrameAnterior = false;
    private float proximoToggleApuntarTime = 0f;
    private float proximoToggleModoDisparoTime = 0f;

    [Header("Anti rebote ESP-32")]
    public float toggleCooldownSegundos = 0.1f;


    public float miraZoom = 40f;
    public float miraNormal = 60f;
    public bool esta_apuntando = false;



    //----CARGADOR----
    int capacidad_cargador = 30;
    int cantidad_balas_total = 0;
    int cargador_actual = 0;
    //----CARGADOR----

    private PhotonView ownerPhotonView;
    private Coroutine vibracionContinuaCoroutine;
    private bool vibracionContinuaActiva = false;
    private Coroutine resetBoolDisparoCoroutine;
    private Coroutine disparoConDelayCoroutine;
    private bool disparoPendienteSalida = false;
    private PlayerMovement playerMovement;







    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        ownerPhotonView = GetComponent<PhotonView>();
        if (ownerPhotonView == null)
        {
            ownerPhotonView = GetComponentInParent<PhotonView>();
        }
        
        // Obtener referencia a PlayerMovement para verificar isGrounded
        playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
        }
        
        Debug.Log($"[WeaponLogic] Start - OwnerPhotonView: {ownerPhotonView != null}, ViewID: {ownerPhotonView?.ViewID}, IsMine: {ownerPhotonView?.IsMine}, PlayerMovement: {playerMovement != null}");

        usarPhotonEnEscena = EsEscenaMultiplayerActiva();

        if (usarPhotonEnEscena && EsControlLocal() && autovincularUIEnMultiplayer)
        {
            AutovincularUIMultiplayerSiFalta();
        }

        if (camaraApuntado == null)
        {
            camaraApuntado = Camera.main;
        }

        ResolverAnimatorDisparo();

        if (!usarPhotonEnEscena || EsControlLocal())
        {
            if (recargandoUI != null)
            {
                recargandoUI.SetActive(false);
            }

            if (curandoUI != null)
            {
                curandoUI.SetActive(false);
            }
        }

        ActualizarUIContinueShooting();

        SincronizarDesdeGameManager();
    }


    void Update()
    {
        if (EsControlLocal())
        {
            SincronizarDesdeGameManager();

            // Si el EmotePanel está abierto, no procesar disparo
            if (EmotePanel.isEmotePanelActive)
                return;

            // Si el juego está pausado, cancelar disparos activos y salir
            // Esto permite que el EventSystem procese eventos UI en Android
            if (Time.timeScale == 0)
            {
                if (continueShooting)
                {
                    CancelInvoke("Shoot");
                    CancelInvoke("ShootRPC");
                    DetenerVibracionContinua();
                }
                return;
            }

            // Permitir apuntar/desapuntar siempre
            bool aimToggleInput = Input.GetMouseButtonDown(1);
            bool espAimDown = TryGetEspButtonDown(14, ref espAimPresionadoFrameAnterior);
            if (espAimDown && Time.unscaledTime >= proximoToggleApuntarTime)
            {
                aimToggleInput = true;
                proximoToggleApuntarTime = Time.unscaledTime + toggleCooldownSegundos;
            }

            if (aimToggleInput)
            {
                esta_apuntando = !esta_apuntando;
                Mira();
            }

            if (recargando || curando)
            {
                SincronizarHaciaGameManager();
                return;
            }

            bool triggerPresionado = false;
            bool triggerSoltado = false;
            ActualizarTriggerEsp32(out triggerPresionado, out triggerSoltado);

            bool disparoPresionado = Input.GetMouseButtonDown(0) || triggerPresionado;
            bool disparoSoltado = Input.GetMouseButtonUp(0) || triggerSoltado;

            //fire1 - Disparo con MOUSE (PC) - Móvil usará ESP32 háptico
            if (disparoPresionado && Time.timeScale != 0)
            {
                Debug.Log($"[WeaponLogic] ✓✓✓ Disparo presionado - Cargador: {cargador_actual}, Photon: {usarPhotonEnEscena}, IsMine: {(ownerPhotonView != null ? ownerPhotonView.IsMine.ToString() : "NO_PHOTON")}");
                
                //para la cadencia
                if (Time.time > shootRateTime && cargador_actual > 0)
                {
                    if (continueShooting)
                    {
                        IniciarVibracionContinua();
                        //llamar continuamente un metodo establecido cada cierto tiempo
                        InvokeRepeating(usarPhotonEnEscena ? "ShootRPC" : "Shoot", .001f, shotRate);
                        Debug.Log($"[WeaponLogic] InvokeRepeating iniciado - Método: {(usarPhotonEnEscena ? "ShootRPC" : "Shoot")}");
                    }
                    else
                    {
                        if (usarPhotonEnEscena)
                        {
                            Debug.Log("[WeaponLogic] Llamando ShootRPC");
                            ShootRPC();
                        }
                        else
                        {
                            Debug.Log("[WeaponLogic] Llamando Shoot");
                            Shoot();
                        }
                    }
                }
                else
                {
                    Debug.Log($"[WeaponLogic] ✗ No se puede disparar - ShootRateTime: {Time.time > shootRateTime}, Balas: {cargador_actual}");
                }
            }

            // Soltar disparo (solo mouse)
            else if (disparoSoltado && continueShooting && Time.timeScale != 0)
            {
                Debug.Log("[WeaponLogic] ✓ Disparo soltado - Cancelando InvokeRepeating");
                //dejar de llamar al metodo shoot
                CancelInvoke("Shoot");
                CancelInvoke("ShootRPC");
                DetenerVibracionContinua();
            }

            bool reloadInput = Input.GetKeyDown(KeyCode.R) || TryGetEspButtonDown(15, ref espRecargarPresionadoFrameAnterior);

            //recargar el cargador
            if (reloadInput && cargador_actual < capacidad_cargador && cantidad_balas_total > 0)
            {
                recargar_cargador();
            }

            bool fireModToggleInput = Input.GetKeyDown(KeyCode.C);

            bool espModoDisparoDown = TryGetEspButtonDown(12, ref espModoDisparoPresionadoFrameAnterior);
            if (espModoDisparoDown && Time.unscaledTime >= proximoToggleModoDisparoTime)
            {
                fireModToggleInput = true;
                proximoToggleModoDisparoTime = Time.unscaledTime + toggleCooldownSegundos;
            }

            if (fireModToggleInput)
            {
                continueShooting = !continueShooting;
                ReproducirSonido(cambiarModoDisparoSound);
                ActualizarUIContinueShooting();
            }

            bool healInput = Input.GetKeyDown(KeyCode.X) || TryGetEspButtonDown(11, ref espCurarPresionadoFrameAnterior);

            if (healInput)
            {
                CurarConVenda();
            }

            SincronizarHaciaGameManager();
        }


    }

    private bool TryGetEspButtonRaw(int buttonIndex, out bool pressed)
    {
        pressed = false;

        if (!usarDisparoEsp32)
            return false;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        return receiver.TryGetButtonPressed(buttonIndex, out pressed);
    }

    private bool TryGetEspButtonDown(int buttonIndex, ref bool previousPressed)
    {
        bool currentPressed = false;
        if (!TryGetEspButtonRaw(buttonIndex, out currentPressed))
        {
            currentPressed = false;
        }

        bool buttonDown = currentPressed && !previousPressed;
        previousPressed = currentPressed;
        return buttonDown;
    }

    private void ActualizarTriggerEsp32(out bool triggerPresionado, out bool triggerSoltado)
    {
        triggerPresionado = false;
        triggerSoltado = false;

        if (!usarDisparoEsp32)
            return;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
        {
            if (espTriggerPresionadoFrameAnterior)
                triggerSoltado = true;

            espTriggerPresionadoFrameAnterior = false;
            return;
        }

        if (!receiver.TryGetTriggerPressed(out bool triggerActual))
        {
            if (espTriggerPresionadoFrameAnterior)
                triggerSoltado = true;

            espTriggerPresionadoFrameAnterior = false;
            return;
        }

        triggerPresionado = triggerActual && !espTriggerPresionadoFrameAnterior;
        triggerSoltado = !triggerActual && espTriggerPresionadoFrameAnterior;
        espTriggerPresionadoFrameAnterior = triggerActual;
    }

    private bool EsControlLocal()
    {
        if (!usarPhotonEnEscena)
            return true;

        return ownerPhotonView != null && ownerPhotonView.IsMine;
    }

    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == nombreEscenaMultiplayer || escena == "MultiPlayer" || escena == "Multi_Player";
    }

    private void AutovincularUIMultiplayerSiFalta()
    {
        if (recargandoUI == null)
        {
            Transform recargandoTransform = BuscarHijoRecursivo(transform.root, nombreObjRecargando);
            if (recargandoTransform != null)
            {
                recargandoUI = recargandoTransform.gameObject;
            }
            else
            {
                GameObject objRecargando = GameObject.Find(nombreObjRecargando);
                if (objRecargando != null)
                    recargandoUI = objRecargando;
            }
        }

        if (curandoUI == null)
        {
            Transform curandoTransform = BuscarHijoRecursivo(transform.root, nombreObjCurando);
            if (curandoTransform != null)
            {
                curandoUI = curandoTransform.gameObject;
            }
            else
            {
                GameObject objCurando = GameObject.Find(nombreObjCurando);
                if (objCurando != null)
                    curandoUI = objCurando;
            }
        }

        if (continueShootingText == null)
        {
            Transform continueTransform = BuscarHijoRecursivo(transform.root, nombreObjContinue);
            if (continueTransform != null)
            {
                continueShootingText = continueTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject objContinue = GameObject.Find(nombreObjContinue);
                if (objContinue != null)
                    continueShootingText = objContinue.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private Transform BuscarHijoRecursivo(Transform raiz, string nombreBuscado)
    {
        if (raiz == null || string.IsNullOrEmpty(nombreBuscado))
            return null;

        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform hijo = raiz.GetChild(i);
            if (hijo.name == nombreBuscado)
                return hijo;

            Transform encontrado = BuscarHijoRecursivo(hijo, nombreBuscado);
            if (encontrado != null)
                return encontrado;
        }

        return null;
    }

    public void ShootRPC() 
    {
        Debug.Log($"[WeaponLogic] ✓✓✓ ShootRPC llamado - UsarPhoton: {usarPhotonEnEscena}, OwnerPhotonView: {ownerPhotonView != null}, Balas: {cargador_actual}");
        
        if (!usarPhotonEnEscena || ownerPhotonView == null)
        {
            Debug.Log("[WeaponLogic] Fallback a Shoot() single player");
            Shoot();
            return;
        }

        if (cargador_actual <= 0)
        {
            Debug.Log("[WeaponLogic] Sin balas, cancelando ShootRPC");
            CancelInvoke("ShootRPC");
            DetenerVibracionContinua();
            return;
        }

        if (disparoPendienteSalida)
        {
            return;
        }

        disparoPendienteSalida = true;

        cargador_actual--;
        EjecutarVibracionPorDisparo();
        SincronizarHaciaGameManager();

        ActivarAnimacionDisparo();
        disparoConDelayCoroutine = StartCoroutine(DispararConDelay(true));
        shootRateTime = Time.time + shotRate;
    }


    [PunRPC]
    public void ShootMultiplayer(Vector3 position, Vector3 direccionDisparo)
    {
        Debug.Log($"[WeaponLogic] ✓✓✓ RPC ShootMultiplayer recibido - Posición: {position}, Dirección: {direccionDisparo}, IsMine: {(ownerPhotonView != null ? ownerPhotonView.IsMine.ToString() : "NO_PHOTON")}");
        
        GameObject newBullet;
        Quaternion rotacionDisparo = Quaternion.LookRotation(direccionDisparo, Vector3.up);

        //instanciamos una bala 
        newBullet = Instantiate(bullet, position, rotacionDisparo);

        // Asignar el shooter para detección de daño entre jugadores
        Bullet bulletScript = newBullet.GetComponent<Bullet>();
        if (bulletScript != null && ownerPhotonView != null)
        {
            Debug.Log($"[WeaponLogic] ShootMultiplayer - Asignando shooter ViewID: {ownerPhotonView.ViewID}");
            bulletScript.SetShooter(ownerPhotonView);
        }
        else
        {
            Debug.LogWarning($"[WeaponLogic] ShootMultiplayer - BulletScript: {bulletScript != null}, OwnerPhotonView: {ownerPhotonView != null}");
        }

        Rigidbody balaRigidbody = newBullet.GetComponent<Rigidbody>();
        if (balaRigidbody != null)
        {
            balaRigidbody.AddForce(direccionDisparo * shotforce);
        }
        
        Destroy(newBullet, 1);
    }


    private void SincronizarDesdeGameManager()
    {
        if (GameManager.Instance == null)
            return;

        cantidad_balas_total = Mathf.Max(0, GameManager.Instance.gunammo);
        //clamp hace que este entre 0 y 30 el cargador
        cargador_actual = Mathf.Clamp(GameManager.Instance.cargador_actual, 0, capacidad_cargador);
    }

    private void SincronizarHaciaGameManager()
    {
        if (GameManager.Instance == null)
            return;
        //.max devuelve el mayor de los dos valores
        GameManager.Instance.gunammo = Mathf.Max(0, cantidad_balas_total);                                                                                                                                                  
        GameManager.Instance.cargador_actual = Mathf.Clamp(cargador_actual, 0, capacidad_cargador);
    }

    public void Shoot()
    {
        //checar si tenemos la municion
        if (cargador_actual > 0)
        {
            if (disparoPendienteSalida)
            {
                return;
            }

            disparoPendienteSalida = true;

            cargador_actual--;
            EjecutarVibracionPorDisparo();
            SincronizarHaciaGameManager();

            ActivarAnimacionDisparo();
            disparoConDelayCoroutine = StartCoroutine(DispararConDelay(false));

            shootRateTime = Time.time + shotRate;
        }
        else
        {
            CancelInvoke("Shoot");
            DetenerVibracionContinua();
        }
    }

    private IEnumerator DispararConDelay(bool enviarPorRPC)
    {
        // Contador de delay que se pausa si el jugador está en el aire
        float tiempoRestante = delayDisparo;
        
        while (tiempoRestante > 0f)
        {
            // Solo contar si el jugador está en el suelo (grounded)
            if (playerMovement != null && playerMovement.isGrounded)
            {
                tiempoRestante -= Time.deltaTime;
            }
            
            yield return null;
        }

        if (audioSource != null && shotSound != null)
        {
            audioSource.PlayOneShot(shotSound);
        }

        Vector3 direccionDisparo = ObtenerDireccionDisparo();

        if (enviarPorRPC && usarPhotonEnEscena && ownerPhotonView != null)
        {
            Debug.Log($"[WeaponLogic] Enviando RPC ShootMultiplayer - Posición: {spawnPoint.position}, Dirección: {direccionDisparo}, ViewID: {ownerPhotonView.ViewID}");
            ownerPhotonView.RPC("ShootMultiplayer", RpcTarget.AllViaServer, spawnPoint.position, direccionDisparo);
        }

        if (!enviarPorRPC || !usarPhotonEnEscena || ownerPhotonView == null)
        {
            InstanciarBalaLocal(spawnPoint.position, direccionDisparo);
        }

        disparoPendienteSalida = false;
        disparoConDelayCoroutine = null;
    }

    private void InstanciarBalaLocal(Vector3 position, Vector3 direccionDisparo)
    {
        Quaternion rotacionDisparo = Quaternion.LookRotation(direccionDisparo, Vector3.up);
        GameObject newBullet = Instantiate(bullet, position, rotacionDisparo);

        Bullet bulletScript = newBullet.GetComponent<Bullet>();
        if (bulletScript != null && ownerPhotonView != null)
        {
            Debug.Log($"[WeaponLogic] Shoot - Asignando shooter ViewID: {ownerPhotonView.ViewID}");
            bulletScript.SetShooter(ownerPhotonView);
        }
        else
        {
            Debug.LogWarning($"[WeaponLogic] Shoot - BulletScript: {bulletScript != null}, OwnerPhotonView: {ownerPhotonView != null}");
        }

        Rigidbody balaRigidbody = newBullet.GetComponent<Rigidbody>();
        if (balaRigidbody != null)
        {
            balaRigidbody.AddForce(direccionDisparo * shotforce);
        }

        Destroy(newBullet, 1);
    }

    private void ActivarAnimacionDisparo()
    {
        if (animatorDisparo == null || string.IsNullOrEmpty(parametroDisparo))
            return;

        AnimatorControllerParameter parametroEncontrado = null;
        foreach (AnimatorControllerParameter parametro in animatorDisparo.parameters)
        {
            if (parametro.name == parametroDisparo)
            {
                parametroEncontrado = parametro;
                break;
            }
        }

        if (parametroEncontrado == null)
            return;

        if (parametroEncontrado.type == AnimatorControllerParameterType.Trigger)
        {
            animatorDisparo.SetTrigger(parametroDisparo);
            return;
        }

        if (parametroEncontrado.type == AnimatorControllerParameterType.Bool)
        {
            animatorDisparo.SetBool(parametroDisparo, true);

            if (resetBoolDisparoCoroutine != null)
            {
                StopCoroutine(resetBoolDisparoCoroutine);
            }

            resetBoolDisparoCoroutine = StartCoroutine(ResetBoolDisparo());
        }
    }

    private void ResolverAnimatorDisparo()
    {
        if (animatorDisparo != null)
            return;

        // Prioridad: Animator del personaje (PlayerMovement)
        PlayerMovement playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement != null && playerMovement.animator != null)
        {
            animatorDisparo = playerMovement.animator;
            return;
        }

        // Fallback por compatibilidad
        animatorDisparo = GetComponentInParent<Animator>();
    }

    private IEnumerator ResetBoolDisparo()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, duracionPulsoBoolDisparo));

        if (animatorDisparo != null && !string.IsNullOrEmpty(parametroDisparo))
        {
            animatorDisparo.SetBool(parametroDisparo, false);
        }

        resetBoolDisparoCoroutine = null;
    }

    private void EjecutarVibracionPorDisparo()
    {
        if (!usarVibracionEsp32)
            return;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return;

        if (!continueShooting)
        {
            receiver.VibrarMs(vibracionDisparoNoContinuoMs);
        }
    }

    private void IniciarVibracionContinua()
    {
        if (!usarVibracionEsp32)
            return;

        if (vibracionContinuaActiva)
            return;

        vibracionContinuaActiva = true;

        if (vibracionContinuaCoroutine != null)
        {
            StopCoroutine(vibracionContinuaCoroutine);
        }

        vibracionContinuaCoroutine = StartCoroutine(MantenerVibracionContinua());
    }

    private void DetenerVibracionContinua()
    {
        vibracionContinuaActiva = false;

        if (vibracionContinuaCoroutine != null)
        {
            StopCoroutine(vibracionContinuaCoroutine);
            vibracionContinuaCoroutine = null;
        }
    }

    private IEnumerator MantenerVibracionContinua()
    {
        while (vibracionContinuaActiva)
        {
            UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
            if (receiver != null)
            {
                receiver.VibrarMs(vibracionPulsoContinuoMs);
            }

            float intervalo = Mathf.Max(0.02f, intervaloPulsoContinuoSegundos);
            yield return new WaitForSecondsRealtime(intervalo);
        }

        vibracionContinuaCoroutine = null;
    }

    private Vector3 ObtenerDireccionDisparo()
    {
        if (!usarRaycastParaApuntar || camaraApuntado == null)
        {
            return spawnPoint.forward;
        }

        Ray rayoApuntado = camaraApuntado.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 puntoObjetivo = rayoApuntado.origin + rayoApuntado.direction * distanciaMaximaApuntado;

        if (Physics.Raycast(rayoApuntado, out RaycastHit hit, distanciaMaximaApuntado, mascaraApuntado, QueryTriggerInteraction.Ignore))
        {
            puntoObjetivo = hit.point;
        }

        Vector3 direccionDisparo = (puntoObjetivo - spawnPoint.position).normalized;

        if (direccionDisparo.sqrMagnitude <= 0.0001f)
        {
            return spawnPoint.forward;
        }

        return direccionDisparo;
    }



    public void recargar_cargador()
    {
        if (usarPhotonEnEscena && !EsControlLocal())
            return;

        if (recargando)
            return;

        if (cargador_actual >= capacidad_cargador)
            return;

        if (cantidad_balas_total <= 0)
            return;

        ReproducirSonido(recargaSound);

        recargaCoroutine = StartCoroutine(RecargarConTiempo());
    }

    private IEnumerator RecargarConTiempo()
    {
        if (usarPhotonEnEscena && !EsControlLocal())
            yield break;

        recargando = true;

        if (continueShooting)
        {
            CancelInvoke("Shoot");
            CancelInvoke("ShootRPC");
            DetenerVibracionContinua();
        }

        if (recargandoUI != null)
        {
            recargandoUI.SetActive(true);
        }

        yield return new WaitForSeconds(tiempoRecarga);

        int espacioEnCargador = capacidad_cargador - cargador_actual;
        int balasACargar = Mathf.Min(espacioEnCargador, cantidad_balas_total);

        if (balasACargar > 0)
        {
            cargador_actual += balasACargar;
            cantidad_balas_total -= balasACargar;
            SincronizarHaciaGameManager();
        }

        if (recargandoUI != null)
        {
            recargandoUI.SetActive(false);
        }

        recargando = false;
        recargaCoroutine = null;
    }

    private void CurarConVenda()
    {
        if (usarPhotonEnEscena && !EsControlLocal())
            return;

        if (curando)
            return;

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.cantidad_vendas <= 0)
            return;

        ReproducirSonido(curarSound);

        curarCoroutine = StartCoroutine(CurarConTiempo());
    }

    private IEnumerator CurarConTiempo()
    {
        if (usarPhotonEnEscena && !EsControlLocal())
            yield break;

        curando = true;

        if (continueShooting)
        {
            CancelInvoke("Shoot");
            CancelInvoke("ShootRPC");
            DetenerVibracionContinua();
        }

        if (curandoUI != null)
        {
            curandoUI.SetActive(true);
        }

        yield return new WaitForSeconds(tiempoCuracion);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.usarvenda();
        }

        if (curandoUI != null)
        {
            curandoUI.SetActive(false);
        }

        curando = false;
        curarCoroutine = null;
    }




    public void Mira()
    {
        if (esta_apuntando)
        {
            camaraApuntado.fieldOfView = miraZoom; // Cambia el valor para ajustar la cantidad de zoom
        }
        else
        {
            camaraApuntado.fieldOfView = miraNormal;
        }
        
    }

    private void ActualizarUIContinueShooting()
    {
        if (usarPhotonEnEscena && !EsControlLocal())
            return;

        if (continueShootingText == null)
            return;

        continueShootingText.gameObject.SetActive(true);
        continueShootingText.text = continueShooting ? textoAutomatico : textoManual;
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    private void OnDisable()
    {
        CancelInvoke("Shoot");
        CancelInvoke("ShootRPC");
        DetenerVibracionContinua();

        if (disparoConDelayCoroutine != null)
        {
            StopCoroutine(disparoConDelayCoroutine);
            disparoConDelayCoroutine = null;
        }

        disparoPendienteSalida = false;

        if (resetBoolDisparoCoroutine != null)
        {
            StopCoroutine(resetBoolDisparoCoroutine);
            resetBoolDisparoCoroutine = null;
        }
    }
}