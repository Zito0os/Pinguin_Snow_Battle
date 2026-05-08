using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class EmotePanel : MonoBehaviour
{
    public GameObject emotePanel;
    //public Animator playerAnimator;

    private int emoteActual = -1;
    private int ultimoEmote = -1;
    private int cantidadEmotes = 5;
    public float duracionEmote = 10f;

    // Variable estática para que otros scripts sepan si el panel está abierto
    public static bool isEmotePanelActive = false;
    public static bool isEmotePlaying = false;


    public Animator animator; // Asignar el Animator del jugador en el Inspector
    [Header("Highlights (5)")]
    public GameObject[] highlights; // 5 objetos

    private bool play_emote;
    private float emoteFinTiempo = -1f;
    private PlayerMovement playerMovement;
    private bool intentoVinculoPendiente = true;
    private ThirdPersonCamera thirdPersonCamera;
    private CameraSwitch cameraSwitch;

    public static EmotePanel instancia;

    void Awake()
    {
        instancia = this;
        playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (!EsEscenaMultiplayerActiva())
        {
            intentoVinculoPendiente = false;
        }
    }

    void Start()
    {
        IntentarVincularAnimatorLocal();
        
        // Buscar ThirdPersonCamera y CameraSwitch
        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
        }
        
        if (cameraSwitch == null)
        {
            cameraSwitch = FindFirstObjectByType<CameraSwitch>();
        }
        
        if (thirdPersonCamera != null)
        {
            Debug.Log("[EmotePanel] ThirdPersonCamera encontrada");
        }
        if (cameraSwitch != null)
        {
            Debug.Log("[EmotePanel] CameraSwitch encontrada");
        }
    }



    void Update()
    {
        if (EsEscenaMultiplayerActiva() && intentoVinculoPendiente && !AnimatorValido(animator))
        {
            IntentarVincularAnimatorLocal();
        }

        if (isEmotePlaying && Time.time >= emoteFinTiempo)
        {
            FinalizarEmote();
        }

        if (Input.GetKey(KeyCode.H))
        {
            emotePanel.SetActive(true);
            isEmotePanelActive = true;
            Cursor.lockState = CursorLockMode.None; // Desbloquear cursor para que detecte la posición del mouse
            DetectarEmote();
        }

        if (Input.GetKeyUp(KeyCode.H))
        {
            emotePanel.SetActive(false);
            isEmotePanelActive = false;
            Cursor.lockState = CursorLockMode.Locked; // Volver a bloquear el cursor
            LimpiarHighlights();
            ReproducirEmote();
        }


    }

    void DetectarEmote()
    {
        Vector2 centroPantalla = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 posicionMouse = (Vector2)Input.mousePosition;
        Vector2 direccion = posicionMouse - centroPantalla;

        // Si el mouse está muy cerca del centro, no seleccionar nada
        if (direccion.magnitude < 50f)
        {
            emoteActual = -1;
            return;
        }

        // Invertir Y porque las coordenadas de pantalla están invertidas
        float angulo = Mathf.Atan2(-direccion.y, direccion.x) * Mathf.Rad2Deg;

        if (angulo < 0)
            angulo += 360;

        // Invertir la dirección de los ángulos completamente
        angulo = (360 - angulo) % 360;

        // Mapear ángulo a sectores para 5 emotes:
        // Sector 0: Arriba - rango 18-90°
        // Sector 1: Derecha - rango 305-17° (cruza 0°)
        // Sector 2: Abajo-Derecha - rango 233-305°
        // Sector 3: Abajo-Izquierda - rango 161-233°
        // Sector 4: Izquierda - rango 89-161°
        
        if (angulo >= 18 && angulo < 90)
            emoteActual = 0;
        else if (angulo >= 305 || angulo < 17)
            emoteActual = 1;
        else if (angulo >= 233 && angulo < 305)
            emoteActual = 2;
        else if (angulo >= 161 && angulo < 233)
            emoteActual = 3;
        else if (angulo >= 89 && angulo < 161)
            emoteActual = 4;
        else
            emoteActual = -1;

        //  SOLO actualiza si cambia
        if (emoteActual != ultimoEmote)
        {
            Debug.Log("ANGULO: " + angulo.ToString("F2") + " | SECTOR: " + emoteActual);
            ActualizarHighlights();
            ultimoEmote = emoteActual;
        }
    }

    void ActualizarHighlights()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].SetActive(i == emoteActual);
        }
    }

    void LimpiarHighlights()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].SetActive(false);
        }

        ultimoEmote = -1;
    }


    void ReproducirEmote()
    {
        if (emoteActual == -1) return;

        if (!AnimatorValido(animator))
        {
            Debug.LogWarning("[EmotePanel] No se encontró un Animator válido del player local. Emote cancelado.");
            IntentarVincularAnimatorLocal();
            return;
        }

        animator.SetFloat("EmoteIndex 0", emoteActual);
        animator.SetTrigger("Play_Emote");
        isEmotePlaying = true;
        emoteFinTiempo = Time.time + duracionEmote;

        // Cambiar a cámara de tercera persona
        CambiarATercerPersona();

        SincronizarInicioEmoteMultiplayer(emoteActual);
    }

    public static void CancelarEmotePorMovimiento()
    {
        if (instancia != null)
        {
            instancia.FinalizarEmote();
        }
    }

    private void FinalizarEmote()
    {
        FinalizarEmote(playerMovement != null && playerMovement.isSprinting);
    }

    private void FinalizarEmote(bool isSprintingLocal)
    {
        if (!isEmotePlaying)
        {
            return;
        }

        isEmotePlaying = false;
        emoteFinTiempo = -1f;

        if (animator != null)
        {
            animator.ResetTrigger("Play_Emote");
            animator.CrossFade(isSprintingLocal ? "Run Blend Tree" : "Blend Tree", 0.1f);
        }

        // Cambiar de vuelta a cámara de primera persona
        CambiarAPrimeraPersona();

        SincronizarFinEmoteMultiplayer(isSprintingLocal);
    }

    private void SincronizarInicioEmoteMultiplayer(int emoteIndex)
    {
        if (playerMovement == null || playerMovement.photonView == null || !playerMovement.photonView.IsMine)
        {
            return;
        }

        playerMovement.photonView.RPC("RPC_ReproducirEmote", RpcTarget.Others, emoteIndex);
    }

    private void SincronizarFinEmoteMultiplayer(bool isSprintingLocal)
    {
        if (playerMovement == null || playerMovement.photonView == null || !playerMovement.photonView.IsMine)
        {
            return;
        }

        playerMovement.photonView.RPC("RPC_FinalizarEmote", RpcTarget.Others, isSprintingLocal);
    }

    private void CambiarATercerPersona()
    {
        if (cameraSwitch != null)
        {
            // Usar reflexión para acceder al campo privado firtPersonEnable
            System.Reflection.FieldInfo field = cameraSwitch.GetType().GetField("firtPersonEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(cameraSwitch, false); // false = tercera persona
                cameraSwitch.ChangedCamera();
                Debug.Log("[EmotePanel] ✓ Cambiando a tercera persona");
            }
            return;
        }

        Debug.LogWarning("[EmotePanel] No se encontró CameraSwitch para cambiar a tercera persona");
    }

    private void CambiarAPrimeraPersona()
    {
        if (cameraSwitch != null)
        {
            // Usar reflexión para acceder al campo privado firtPersonEnable
            System.Reflection.FieldInfo field = cameraSwitch.GetType().GetField("firtPersonEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(cameraSwitch, true); // true = primera persona
                cameraSwitch.ChangedCamera();
                Debug.Log("[EmotePanel] ✓ Cambiando a primera persona");
            }
            return;
        }

        Debug.LogWarning("[EmotePanel] No se encontró CameraSwitch para cambiar a primera persona");
    }

    private void IntentarVincularAnimatorLocal()
    {
        if (!EsEscenaMultiplayerActiva())
        {
            return;
        }

        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement player in players)
        {
            if (player == null)
            {
                continue;
            }

            PhotonView view = player.GetComponent<PhotonView>();
            bool esLocal = view == null || view.IsMine;
            if (!esLocal)
            {
                continue;
            }

            Animator animatorJugador = player.animator != null ? player.animator : player.GetComponentInChildren<Animator>(true);
            if (AnimatorValido(animatorJugador))
            {
                animator = animatorJugador;
                playerMovement = player;
                intentoVinculoPendiente = false;
                Debug.Log($"[EmotePanel] Animator local vinculado al player '{player.gameObject.name}'");
                return;
            }
        }
    }

    private bool AnimatorValido(Animator animatorAValidar)
    {
        return animatorAValidar != null && animatorAValidar.isActiveAndEnabled && animatorAValidar.runtimeAnimatorController != null;
    }

    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == "MultiPlayer" || escena == "Multi_Player";
    }
}
