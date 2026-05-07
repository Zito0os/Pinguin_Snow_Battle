using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;


public class PlayerMovement : MonoBehaviourPunCallbacks
{
    public static bool bloqueoMovimientoExterno = false;

    [Header("Multijugador")]
    public string nombreEscenaMultiplayer = "MultiPlayer";
    private bool usarPhotonEnEscena = false;
    public bool renombrarParaIdentificar = true;
    [Header("ESP-32")]
    public bool usarInputEsp32 = true;

    public CharacterController characterController;
    public float speed = 15f;

    public float gravity = -3f;

    //gravedad velocidad
    Vector3 velocity;




    //comprobacion de suelo
    public Transform groundCheck;

    public float sphereRadius = 0.3f;

    //etiqueta para el suelo, para que el player sepa si esta en el suelo o no
    public LayerMask groundMask;

    public bool isGrounded;



    //salto
    public float jumpheigth = 3f;


    public bool isSprinting;

    public float sprintSpeedMultiplier = 2f;

    private float sprintSpeed = 1;


    public float staminaUseAmount = 5f;

    private StaminaBar staminaSlider;

    [Header("Slide")]
    public float slideDistance = 5f;
    public float slideSpeedMultiplier = 1.5f;
    public float slideStaminaCost = 20f;
    private bool isSliding = false;

    public Animator animator;

    [Header("Audio pasos")]
    public AudioSource audioPasosCaminar;
    public AudioSource audioPasosCorrer;
    public AudioClip sonidoCaminar;
    public AudioClip sonidoCorrer;
    [Range(0f, 1f)] public float volumenCaminar = 0.7f;
    [Range(0f, 1f)] public float volumenCorrer = 0.85f;



    void Start()
    {
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();

        if (usarPhotonEnEscena && renombrarParaIdentificar && photonView != null)
        {
            string tipo = photonView.IsMine ? "LOCAL" : "REMOTE";
            gameObject.name = $"Player_{photonView.OwnerActorNr}_{tipo}";
        }

        //encontrar la slider de stamina en la escena
        //como tiene el script stamina bar , lo busca y lo asigna a la variable
        staminaSlider = FindObjectOfType<StaminaBar>();

        ConfigurarAudioPasos();
    }


    void Update()
    {
        if (EsControlLocal())
        {
            if (bloqueoMovimientoExterno)
            {
                velocity.y += gravity * Time.deltaTime;
                characterController.Move(velocity * Time.deltaTime);
                return;
            }

            // Si el juego está pausado (Time.timeScale == 0), no procesar input de movimiento
            // Esto permite que el EventSystem procese eventos UI en Android
            if (Time.timeScale == 0)
            {
                // Mantener la gravedad aunque no se pueda mover
                velocity.y += gravity * Time.deltaTime;
                characterController.Move(velocity * Time.deltaTime);
                return;
            }

            // Si el EmotePanel está abierto, no procesar input de movimiento
            if (EmotePanel.isEmotePanelActive)
            {
                // Mantener la gravedad aunque no se pueda mover
                velocity.y += gravity * Time.deltaTime;
                characterController.Move(velocity * Time.deltaTime);
                return;
            }

            //esto es para saber si el player esta en el suelo o no mediante una funcion de unity
            //CheckSphere crea una esfera en el punto que le digamos, en este caso groundCheck.position
            isGrounded = Physics.CheckSphere(groundCheck.position, sphereRadius, groundMask);

            if (isGrounded && velocity.y < 0)
            {
                //si el player esta en el suelo, la velocidad en y se pone a 0
                velocity.y = -2f;
            }



            //esto es para el movimiento del player asignacion de teclas de movimiento
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            if (usarInputEsp32 && TryGetEspMovementInput(out float espJoyX, out float espJoyY))
            {
                x = espJoyX;
                z = espJoyY;
            }

            bool estaMoviendose = Mathf.Abs(x) > 0.01f || Mathf.Abs(z) > 0.01f;

            animator.SetFloat("VelX", x);
            animator.SetFloat("VelZ", z);
            animator.SetBool("isSprinting", isSprinting);

            // Input para iniciar slide: solo si se está corriendo, en suelo y no ya deslizando
            if (!isSliding && isSprinting && isGrounded && Input.GetKeyDown(KeyCode.LeftControl))
            {
                StartCoroutine(Slide());
            }

            //esto es para mover al jugador adelante o hacia atras 
            Vector3 move = transform.right * x + transform.forward * z;


            JunpCheck();
            RunCheck();
            ActualizarAudioPasos(estaMoviendose);



            //esto le asigna el movimiento al caracter controler del player 
            //y le asigna la velocidad que se le dio en el inspector
            if (!isSliding)
            {
                characterController.Move(move * speed * Time.deltaTime * sprintSpeed);
            }

            // si alguien juega a 30 y alguien a 60 fps, el que juega a 30 fps se movera mas lento
            //por eso se multiplica por Time.deltaTime



            //para activarle la gravedad 
            velocity.y += gravity * Time.deltaTime;
            //esto le asigna la gravedad al player
            characterController.Move(velocity * Time.deltaTime);

        }




    }

    public static void SetBloqueoMovimientoExterno(bool bloquear)
    {
        bloqueoMovimientoExterno = bloquear;
    }

    private bool TryGetEspMovementInput(out float joyXInput, out float joyYInput)
    {
        joyXInput = 0f;
        joyYInput = 0f;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        if (!receiver.TryGetControlValues(out _, out _, out joyXInput, out joyYInput))
            return false;

        return true;
    }

    private bool TryGetEspRunInput(out bool runPressed)
    {
        runPressed = false;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        return receiver.TryGetButtonPressed(6, out runPressed);
    }

    private bool TryGetEspJumpInput(out bool jumpPressed)
    {
        jumpPressed = false;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        return receiver.TryGetButtonPressed(7, out jumpPressed);
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
    public void JunpCheck()
    {
        bool jumpInput = Input.GetKeyDown(KeyCode.Space);

        if (usarInputEsp32 && TryGetEspJumpInput(out bool espJump))
        {
            jumpInput = jumpInput || espJump;
        }

        if (jumpInput && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpheigth * -2f * gravity);
            animator.SetBool("isJumping", true);
        }

        if (isGrounded && velocity.y <= 0f)
        {
            animator.SetBool("isJumping", false);
        }
    }

    public void RunCheck()
    {
        bool toggleSprintInput = Input.GetKeyDown(KeyCode.LeftShift);

        if (usarInputEsp32 && TryGetEspRunInput(out bool espRun))
        {
            toggleSprintInput = toggleSprintInput || espRun;
        }

        if (toggleSprintInput)
        {
            isSprinting = !isSprinting;

            if (isSprinting)
            {
                staminaSlider.UseStamina(staminaUseAmount);
            }
            else
            {
                staminaSlider.StopSprinting();
            }
        }

        if (isSprinting)
        {
            sprintSpeed = sprintSpeedMultiplier;
        }
        else
        {
            sprintSpeed = 1;
        }
    }




    private void ConfigurarAudioPasos()
    {
        if (audioPasosCaminar == null)
        {
            audioPasosCaminar = gameObject.AddComponent<AudioSource>();
        }

        if (audioPasosCorrer == null)
        {
            audioPasosCorrer = gameObject.AddComponent<AudioSource>();
        }

        audioPasosCaminar.playOnAwake = false;
        audioPasosCaminar.loop = true;
        audioPasosCaminar.spatialBlend = 0f;
        audioPasosCaminar.clip = sonidoCaminar;
        audioPasosCaminar.volume = volumenCaminar;

        audioPasosCorrer.playOnAwake = false;
        audioPasosCorrer.loop = true;
        audioPasosCorrer.spatialBlend = 0f;
        audioPasosCorrer.clip = sonidoCorrer;
        audioPasosCorrer.volume = volumenCorrer;
    }

    private void ActualizarAudioPasos(bool estaMoviendose)
    {
        if (!estaMoviendose)
        {
            if (audioPasosCaminar != null && audioPasosCaminar.isPlaying)
                audioPasosCaminar.Stop();

            if (audioPasosCorrer != null && audioPasosCorrer.isPlaying)
                audioPasosCorrer.Stop();

            return;
        }

        if (isSprinting)
        {
            if (audioPasosCaminar != null && audioPasosCaminar.isPlaying)
                audioPasosCaminar.Stop();

            if (audioPasosCorrer != null)
            {
                audioPasosCorrer.clip = sonidoCorrer;
                audioPasosCorrer.volume = volumenCorrer;

                if (sonidoCorrer != null && !audioPasosCorrer.isPlaying)
                    audioPasosCorrer.Play();
            }
        }
        else
        {
            if (audioPasosCorrer != null && audioPasosCorrer.isPlaying)
                audioPasosCorrer.Stop();

            if (audioPasosCaminar != null)
            {
                audioPasosCaminar.clip = sonidoCaminar;
                audioPasosCaminar.volume = volumenCaminar;

                if (sonidoCaminar != null && !audioPasosCaminar.isPlaying)
                    audioPasosCaminar.Play();
            }
        }
    }

    private IEnumerator Slide()
    {
        if (animator != null)
        {
            animator.SetBool("barrida", true);
        }

        // Consumir stamina de una sola vez al inicio del slide
        if (staminaSlider != null)
        {
            bool puedeSlide = staminaSlider.UseStaminaInstant(slideStaminaCost);
            if (!puedeSlide)
            {
                if (animator != null)
                {
                    animator.SetBool("barrida", false);
                }

                yield break; // No hay suficiente stamina, cancelar slide
            }
        }

        isSliding = true;

        float slideSpeed = speed * sprintSpeedMultiplier * slideSpeedMultiplier;
        float duration = slideDistance / (slideSpeed > 0f ? slideSpeed : 1f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // aplicar movimiento hacia adelante durante el slide y gravedad
            Vector3 slideMove = transform.forward * slideSpeed;
            velocity.y += gravity * Time.deltaTime;
            characterController.Move((slideMove + new Vector3(0f, velocity.y, 0f)) * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isSliding = false;
        if (animator != null)
        {
            animator.SetBool("barrida", false);
        }
        // El jugador mantiene su estado de sprint actual, permitiendo hacer otro slide inmediatamente
    }

}

