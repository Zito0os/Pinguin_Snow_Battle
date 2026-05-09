using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
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

    private PhotonView ownerPhotonView;
    private Coroutine vibracionContinuaCoroutine;
    private bool vibracionContinuaActiva = false;
    private Coroutine resetBoolDisparoCoroutine;
    private Coroutine disparoConDelayCoroutine;
    private bool disparoPendienteSalida = false;
    private PlayerMovement playerMovement;
    
    // --- Charge (hold right click) ---------------------------------
    [Header("Carga de disparo")]
    public float tiempoPorFase = 0.5f; // 0.5s por fase
    private bool isCharging = false;
    private float tiempoCarga = 0f;
    private int faseCarga = 1; // 1..7
    public GameObject[] cargaImages = new GameObject[7]; // UI images Carga1..Carga7
    private Vector3 balaBaseScale = Vector3.one;
    [Tooltip("Escalas por fase (índice 0 = fase 1). 7 valores esperados.")]
    public float[] faseEscala = new float[] { 1f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f, 2.2f };







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

        // guardar escala base del prefab de la bala
        if (bullet != null)
            balaBaseScale = bullet.transform.localScale;

        // autovincular UI de carga - siempre intentar en MultiPlayer o si array está vacío
        bool necesitaBuscar = cargaImages == null || cargaImages.Length < 7;
        if (!necesitaBuscar && cargaImages.Length >= 7)
        {
            // Verificar si el array está completamente vacío
            int imagenesAsignadas = 0;
            for (int i = 0; i < 7; i++)
            {
                if (cargaImages[i] != null)
                    imagenesAsignadas++;
            }
            necesitaBuscar = (imagenesAsignadas == 0);
        }

        if (necesitaBuscar)
        {
            if (cargaImages == null || cargaImages.Length < 7)
            {
                cargaImages = new GameObject[7];
            }
            VincularImagenesDeCarga();
        }

        // asegurar que UI esté oculta al inicio
        ActualizarUICarga(0, false);

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

            // Carga de disparo con click derecho: mantener para aumentar fase
            bool rightDown = Input.GetMouseButtonDown(1);
            bool rightHeld = Input.GetMouseButton(1);
            bool rightUp = Input.GetMouseButtonUp(1);

            // Si presiona click derecho, o si suelta la carga pero mantiene presionado, reinicia
            if (rightDown || (!isCharging && rightHeld))
            {
                isCharging = true;
                tiempoCarga = 0f;
                faseCarga = 1;
                ActualizarUICarga(faseCarga, true);
            }

            if (rightHeld && isCharging)
            {
                tiempoCarga += Time.deltaTime;
                int nuevaFase = Mathf.Min(7, 1 + Mathf.FloorToInt(tiempoCarga / tiempoPorFase));
                if (nuevaFase != faseCarga)
                {
                    faseCarga = nuevaFase;
                    ActualizarUICarga(faseCarga, true);
                }
            }

            if (rightUp && isCharging)
            {
                isCharging = false;
                ActualizarUICarga(0, false);
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
                Debug.Log($"[WeaponLogic] ✓✓✓ Disparo presionado - Balas: {GameManager.Instance.gunammo}, Photon: {usarPhotonEnEscena}, IsMine: {(ownerPhotonView != null ? ownerPhotonView.IsMine.ToString() : "NO_PHOTON")}");
                
                //para la cadencia
                if (Time.time > shootRateTime && GameManager.Instance.gunammo > 0)
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
                    Debug.Log($"[WeaponLogic] ✗ No se puede disparar - ShootRateTime: {Time.time > shootRateTime}, Balas: {GameManager.Instance.gunammo}");
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

            // Reload system disabled: reloading by R is removed per design.

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

    private bool CoincideNombreFlexible(string nombreObjeto, int numeroFase)
    {
        if (string.IsNullOrEmpty(nombreObjeto))
            return false;

        string sinEspacios = nombreObjeto.Replace(" ", "").ToLower();
        string buscadoSinEspacios = $"carga{numeroFase}".ToLower();
        
        return sinEspacios == buscadoSinEspacios || sinEspacios.Contains(buscadoSinEspacios);
    }

    private void VincularImagenesDeCarga()
    {
        Debug.Log("[WeaponLogic] ✓ Iniciando búsqueda de imágenes de carga...");

        // Estrategia 1: Buscar globalmente por nombre exacto (tolerando espacios)
        for (int i = 0; i < 7; i++)
        {
            if (cargaImages[i] == null)
            {
                // Intentar con "Carga1", "Carga 1", etc.
                string[] nombresAlternativos = new string[]
                {
                    $"Carga{i + 1}",
                    $"Carga {i + 1}",
                    $"Carga{i}",
                    $"Carga_{i + 1}",
                    $"Carga_{i}"
                };

                foreach (string nombre in nombresAlternativos)
                {
                    GameObject cargo = GameObject.Find(nombre);
                    if (cargo != null)
                    {
                        cargaImages[i] = cargo;
                        Debug.Log($"[WeaponLogic] ✓ Encontrada imagen {i + 1}: '{nombre}'");
                        break;
                    }
                }
            }
        }

        // Estrategia 2: Buscar en TODOS los Canvas (incluyendo deshabilitados)
        Canvas[] todosLosCanvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"[WeaponLogic] Encontrados {todosLosCanvas.Length} Canvas en escena");

        foreach (Canvas canvas in todosLosCanvas)
        {
            Debug.Log($"[WeaponLogic] Inspeccionando Canvas: {canvas.gameObject.name}");
            
            // Buscar recursivamente en todos los hijos (incluyendo deshabilitados)
            Image[] todasLasImagenes = canvas.GetComponentsInChildren<Image>(true);
            Debug.Log($"[WeaponLogic]   → {todasLasImagenes.Length} imágenes encontradas (incluyendo deshabilitadas)");

            foreach (Image img in todasLasImagenes)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (cargaImages[i] == null && CoincideNombreFlexible(img.gameObject.name, i + 1))
                    {
                        cargaImages[i] = img.gameObject;
                        Debug.Log($"[WeaponLogic] ✓ Vinculada Carga{i + 1}: {img.gameObject.name} (Enabled: {img.gameObject.activeSelf})");
                    }
                }
            }
        }

        // Estrategia 3: Si aún faltan, buscar recursivamente en hierarchía general
        if (usarPhotonEnEscena)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (cargaImages[i] == null)
                    {
                        Transform encontrado = BuscarHijoRecursivoFlexible(canvas.transform, i + 1);
                        if (encontrado != null)
                        {
                            cargaImages[i] = encontrado.gameObject;
                            Debug.Log($"[WeaponLogic] ✓ Vinculada (búsqueda recursiva) Carga{i + 1}: {encontrado.gameObject.name}");
                        }
                    }
                }
            }
        }

        // Log final detallado
        int encontrados = 0;
        for (int i = 0; i < 7; i++)
        {
            if (cargaImages[i] != null)
            {
                encontrados++;
                Debug.Log($"[WeaponLogic]   ✓ Carga{i + 1}: '{cargaImages[i].name}' (Active: {cargaImages[i].activeSelf})");
            }
            else
            {
                Debug.LogWarning($"[WeaponLogic]   ✗ Carga{i + 1}: NO ENCONTRADA");
            }
        }
        Debug.Log($"[WeaponLogic] RESULTADO FINAL: {encontrados}/7 imágenes de carga vinculadas");
    }

    private Transform BuscarHijoRecursivoFlexible(Transform raiz, int numeroFase)
    {
        if (raiz == null)
            return null;

        // Verificar el nodo actual
        if (CoincideNombreFlexible(raiz.gameObject.name, numeroFase))
            return raiz;

        // Buscar recursivamente en hijos
        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform resultado = BuscarHijoRecursivoFlexible(raiz.GetChild(i), numeroFase);
            if (resultado != null)
                return resultado;
        }

        return null;
    }

    public void ShootRPC() 
    {
        Debug.Log($"[WeaponLogic] ✓✓✓ ShootRPC llamado - UsarPhoton: {usarPhotonEnEscena}, OwnerPhotonView: {ownerPhotonView != null}, Balas: {GameManager.Instance.gunammo}");
        
        if (!usarPhotonEnEscena || ownerPhotonView == null)
        {
            Debug.Log("[WeaponLogic] Fallback a Shoot() single player");
            Shoot();
            return;
        }

        if (GameManager.Instance.gunammo <= 0)
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

        GameManager.Instance.gunammo--;
        EjecutarVibracionPorDisparo();
        SincronizarHaciaGameManager();

        ActivarAnimacionDisparo();
        disparoConDelayCoroutine = StartCoroutine(DispararConDelay(true));
        shootRateTime = Time.time + shotRate;
    }


    [PunRPC]
    public void ShootMultiplayer(Vector3 position, Vector3 direccionDisparo, int fase)
    {
        Debug.Log($"[WeaponLogic] ✓✓✓ RPC ShootMultiplayer recibido - Posición: {position}, Dirección: {direccionDisparo}, IsMine: {(ownerPhotonView != null ? ownerPhotonView.IsMine.ToString() : "NO_PHOTON")}");
        
        GameObject newBullet;
        Quaternion rotacionDisparo = Quaternion.LookRotation(direccionDisparo, Vector3.up);

        // instanciamos una bala y aplicamos escala según fase
        newBullet = Instantiate(bullet, position, rotacionDisparo);
        // Si fase == 0 -> no aplicar escala, dejar la escala del prefab tal como está
        if (fase > 0)
        {
            int faseIndex = Mathf.Clamp(fase - 1, 0, 6);
            float multiplicador = faseEscala[faseIndex];
            if (newBullet != null)
                newBullet.transform.localScale = balaBaseScale * multiplicador;
        }

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
        // Solo sincronizar desde GameManager (gunammo es la fuente única)
    }

    private void SincronizarHaciaGameManager()
    {
        if (GameManager.Instance == null)
            return;
        // gunammo es sincronizado automáticamente desde el disparo
    }

    public void Shoot()
    {
        //checar si tenemos la municion
        if (GameManager.Instance.gunammo > 0)
        {
            if (disparoPendienteSalida)
            {
                return;
            }

            disparoPendienteSalida = true;

            GameManager.Instance.gunammo--;
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

        // determinar la fase al momento de disparar
        // Si el jugador NO está manteniendo el click derecho (no está cargando),
        // enviaremos fase = 0 para indicar que se debe usar la escala del prefab tal cual.
        bool aplicarEscala = isCharging;
        int faseAlDisparar = aplicarEscala ? Mathf.Clamp(faseCarga, 1, 7) : 0;

        if (enviarPorRPC && usarPhotonEnEscena && ownerPhotonView != null)
        {
            Debug.Log($"[WeaponLogic] Enviando RPC ShootMultiplayer - Posición: {spawnPoint.position}, Dirección: {direccionDisparo}, Fase: {faseAlDisparar}, ViewID: {ownerPhotonView.ViewID}");
            ownerPhotonView.RPC("ShootMultiplayer", RpcTarget.AllViaServer, spawnPoint.position, direccionDisparo, faseAlDisparar);
        }

        if (!enviarPorRPC || !usarPhotonEnEscena || ownerPhotonView == null)
        {
            InstanciarBalaLocal(spawnPoint.position, direccionDisparo, faseAlDisparar);
        }

        // Resetear carga después de disparar
        isCharging = false;
        tiempoCarga = 0f;
        faseCarga = 1;
        ActualizarUICarga(0, false);

        disparoPendienteSalida = false;
        disparoConDelayCoroutine = null;
    }

    private void InstanciarBalaLocal(Vector3 position, Vector3 direccionDisparo, int fase)
    {
        Quaternion rotacionDisparo = Quaternion.LookRotation(direccionDisparo, Vector3.up);
        GameObject newBullet = Instantiate(bullet, position, rotacionDisparo);

        // Si fase == 0 -> no aplicar escala, dejar la escala del prefab tal como está
        if (fase > 0)
        {
            int faseIndex = Mathf.Clamp(fase - 1, 0, 6);
            float multiplicador = faseEscala[faseIndex];
            if (newBullet != null)
                newBullet.transform.localScale = balaBaseScale * multiplicador;
        }

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
            SincronizarAnimacionDisparoMultiplayer();
            return;
        }

        if (parametroEncontrado.type == AnimatorControllerParameterType.Bool)
        {
            animatorDisparo.SetBool(parametroDisparo, true);

            SincronizarAnimacionDisparoMultiplayer();

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

    private void SincronizarAnimacionDisparoMultiplayer()
    {
        if (!usarPhotonEnEscena || playerMovement == null || playerMovement.photonView == null || !playerMovement.photonView.IsMine)
            return;

        playerMovement.photonView.RPC(nameof(PlayerMovement.RPC_Disparo), RpcTarget.Others);
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
        // Reload system disabled: function no longer used
        return;
    }

    private IEnumerator RecargarConTiempo()
    {
        // Reload system disabled: coroutine no longer used
        yield break;
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

    private void ActualizarUICarga(int fase, bool mostrar)
    {
        if (cargaImages == null || cargaImages.Length == 0)
        {
            Debug.LogWarning("[WeaponLogic] ActualizarUICarga: cargaImages es null o vacío");
            return;
        }

        int imagenesValidas = 0;
        for (int i = 0; i < cargaImages.Length; i++)
        {
            if (cargaImages[i] != null)
            {
                imagenesValidas++;
                
                if (!mostrar)
                {
                    cargaImages[i].SetActive(false);
                }
                else
                {
                    cargaImages[i].SetActive(i == (fase - 1));
                    if (i == (fase - 1))
                    {
                        Debug.Log($"[WeaponLogic] Mostrando imagen de carga {fase} en: {cargaImages[i].name}");
                    }
                }
            }
        }
        
        if (imagenesValidas == 0)
        {
            Debug.LogError("[WeaponLogic] ActualizarUICarga: Ninguna imagen de carga está vinculada (todos son null)");
        }
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