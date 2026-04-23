using UnityEngine;
using System.Collections;
using UnityEngine.Animations.Rigging;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class ThrowGranade : MonoBehaviour
{
    [Header("Multijugador")]
    public string nombreEscenaMultiplayer = "MultiPlayer";

    [Header("ESP-32")]
    public bool usarThrowGranadeEsp32 = true;

    public float throwForce = 400f;

    public GameObject granadePrefab;

    public Animator animator;
    public string parametroLanzarGranada = "lanzoGranada";

    [Header("IK de manos (opcional)")]
    public TwoBoneIKConstraint rightHandIK;
    public TwoBoneIKConstraint leftHandIK;
    public bool desactivarIkDuranteGranada = true;
    [Range(0f, 1f)] public float pesoIkDuranteGranada = 0f;

    [Header("Arma siguiendo mano izquierda")]
    public bool seguirArmaConManoIzquierda = false;
    public Transform armaASeguir;
    public Transform manoIzquierdaReferencia;
    public Vector3 offsetPosicionArma;
    public Vector3 offsetRotacionArma;

    [Header("Direccion de lanzamiento")]
    public Transform referenciaDireccionLanzamiento;

    [Header("Punto de spawn granada")]
    public Transform puntoSpawnGranada;

    public float tiempoAntesDeLanzar = 3f;
    public float duracionAnimacionLanzar = 3.0f;

    private bool lanzandoGranada = false;
    private Vector3 posicionLocalOriginalArma;
    private Quaternion rotacionLocalOriginalArma;
    private bool poseOriginalArmaGuardada = false;
    private bool usarPhotonEnEscena = false;
    private PhotonView photonViewRef;

    void Update()
    {
        if (!EsControlLocal())
            return;

        bool throwGrenadeInput = Input.GetKeyDown(KeyCode.G);

        if (usarThrowGranadeEsp32 && TryGetEspThrowGranadeInput(out bool espThrowGrenade))
        {
            throwGrenadeInput = throwGrenadeInput || espThrowGrenade;
        }

        if (throwGrenadeInput && Time.timeScale != 0)
        {
            IntentarLanzar();
        }
    }

    private void LateUpdate()
    {
        if (!EsControlLocal())
            return;

        if (!seguirArmaConManoIzquierda)
            return;

        if (!lanzandoGranada)
            return;

        if (armaASeguir == null || manoIzquierdaReferencia == null)
            return;

        armaASeguir.position = manoIzquierdaReferencia.TransformPoint(offsetPosicionArma);
        armaASeguir.rotation = manoIzquierdaReferencia.rotation * Quaternion.Euler(offsetRotacionArma);
    }

    private void Start()
    {
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();
        photonViewRef = GetComponent<PhotonView>();
        if (photonViewRef == null)
        {
            photonViewRef = GetComponentInParent<PhotonView>();
        }

        if (animator == null)
        {
            animator = GetComponentInParent<Animator>();
        }

        if (animator == null)
        {
            PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                animator = playerMovement.animator;
            }
        }

        if (animator == null)
        {
            Debug.LogWarning("[Granada] No se encontró Animator para reproducir la animación de lanzamiento.");
        }
        else if (!AnimatorTieneParametro(parametroLanzarGranada))
        {
            Debug.LogWarning($"[Granada] El Animator no tiene el parámetro '{parametroLanzarGranada}'.");
        }

        AutoAsignarIKSiHaceFalta();
        AutoAsignarReferenciasArma();
        AutoAsignarDireccionLanzamiento();
    }

    private void IntentarLanzar()
    {
        if (!EsControlLocal())
            return;

        if (lanzandoGranada)
            return;

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.cantidad_granadas <= 0)
            return;

        StartCoroutine(LanzarConAnimacion());
    }

    private IEnumerator LanzarConAnimacion()
    {
        lanzandoGranada = true;
        GuardarPoseOriginalArma();

        float pesoOriginalIKDerecha = rightHandIK != null ? rightHandIK.weight : 1f;
        float pesoOriginalIKIzquierda = leftHandIK != null ? leftHandIK.weight : 1f;

        if (desactivarIkDuranteGranada)
        {
            AplicarPesoIK(pesoIkDuranteGranada);
        }

        if (animator != null)
        {
            if (AnimatorTieneParametro(parametroLanzarGranada))
            {
                animator.SetBool(parametroLanzarGranada, true);
                Debug.Log($"[Granada] Animación activada con parámetro '{parametroLanzarGranada}'.");
            }
        }

        if (tiempoAntesDeLanzar > 0)
        {
            yield return new WaitForSeconds(tiempoAntesDeLanzar);
        }

        Throw();

        float tiempoRestanteAnimacion = Mathf.Max(0f, duracionAnimacionLanzar - tiempoAntesDeLanzar);
        if (tiempoRestanteAnimacion > 0)
        {
            yield return new WaitForSeconds(tiempoRestanteAnimacion);
        }

        if (animator != null)
        {
            if (AnimatorTieneParametro(parametroLanzarGranada))
            {
                animator.SetBool(parametroLanzarGranada, false);
            }
        }

        if (desactivarIkDuranteGranada)
        {
            RestaurarPesoIK(pesoOriginalIKDerecha, pesoOriginalIKIzquierda);
        }

        lanzandoGranada = false;
        RestaurarPoseOriginalArma();
    }

    public void Throw()
    {
        if (!EsControlLocal())
            return;

        if (GameManager.Instance == null || GameManager.Instance.cantidad_granadas <= 0)
            return;

        Vector3 direccionLanzamiento = ObtenerDireccionLanzamiento();
        Quaternion rotacionLanzamiento = Quaternion.LookRotation(direccionLanzamiento, Vector3.up);
        Vector3 posicionSpawn = puntoSpawnGranada != null ? puntoSpawnGranada.position : transform.position;

        GameObject newgranade = Instantiate(granadePrefab, posicionSpawn, rotacionLanzamiento);

        Rigidbody rigidbodyGranada = newgranade.GetComponent<Rigidbody>();
        if (rigidbodyGranada != null)
        {
            rigidbodyGranada.AddForce(direccionLanzamiento * throwForce);
        }

        GameManager.Instance.cantidad_granadas--;

        //Destroy(gameObject,10);
    }

    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == nombreEscenaMultiplayer || escena == "MultiPlayer" || escena == "Multi_Player";
    }

    private bool EsControlLocal()
    {
        if (!usarPhotonEnEscena)
            return true;

        return photonViewRef != null && photonViewRef.IsMine;
    }

    private bool TryGetEspThrowGranadeInput(out bool throwGranadePressed)
    {
        throwGranadePressed = false;

        UsbSerialReceiver receiver = UsbSerialReceiver.Instance;
        if (receiver == null)
            return false;

        return receiver.TryGetButtonPressed(10, out throwGranadePressed);
    }

    private bool AnimatorTieneParametro(string nombreParametro)
    {
        if (animator == null)
            return false;

        AnimatorControllerParameter[] parametros = animator.parameters;
        for (int i = 0; i < parametros.Length; i++)
        {
            if (parametros[i].name == nombreParametro)
                return true;
        }

        return false;
    }

    private void AutoAsignarIKSiHaceFalta()
    {
        if (rightHandIK != null && leftHandIK != null)
            return;

        Transform raizBusqueda = animator != null ? animator.transform : transform.root;
        if (raizBusqueda == null)
            return;

        TwoBoneIKConstraint[] constraints = raizBusqueda.GetComponentsInChildren<TwoBoneIKConstraint>(true);
        for (int i = 0; i < constraints.Length; i++)
        {
            TwoBoneIKConstraint constraint = constraints[i];
            if (constraint == null)
                continue;

            string nombreConstraint = constraint.gameObject.name.ToLowerInvariant();
            string nombreTarget = constraint.data.target != null ? constraint.data.target.name.ToLowerInvariant() : string.Empty;

            bool esDerecha = nombreConstraint.Contains("rigthhandik") ||
                             nombreConstraint.Contains("righthandik") ||
                             nombreTarget.Contains("gun_right_hand_grip") ||
                             nombreTarget.Contains("gun_rigth_hand_grip");

            bool esIzquierda = nombreConstraint.Contains("lefthandik") ||
                               nombreTarget.Contains("gun_left_hand_grip");

            if (esDerecha && rightHandIK == null)
            {
                rightHandIK = constraint;
                continue;
            }

            if (esIzquierda && leftHandIK == null)
            {
                leftHandIK = constraint;
            }
        }
    }

    private void AutoAsignarReferenciasArma()
    {
        if (armaASeguir == null)
        {
            armaASeguir = transform.parent;
        }

        if (manoIzquierdaReferencia == null && leftHandIK != null)
        {
            if (leftHandIK.data.tip != null)
            {
                manoIzquierdaReferencia = leftHandIK.data.tip;
            }
            else if (leftHandIK.data.target != null)
            {
                manoIzquierdaReferencia = leftHandIK.data.target;
            }
        }
    }

    private void AutoAsignarDireccionLanzamiento()
    {
        if (referenciaDireccionLanzamiento != null)
            return;

        if (animator != null)
        {
            referenciaDireccionLanzamiento = animator.transform;
        }
    }

    private Vector3 ObtenerDireccionLanzamiento()
    {
        Transform referencia = referenciaDireccionLanzamiento != null ? referenciaDireccionLanzamiento : transform;
        Vector3 direccion = referencia.forward;

        if (direccion.sqrMagnitude <= 0.0001f)
        {
            direccion = transform.forward;
        }

        return direccion.normalized;
    }

    private void AplicarPesoIK(float peso)
    {
        if (rightHandIK != null)
            rightHandIK.weight = Mathf.Clamp01(peso);

        if (leftHandIK != null)
            leftHandIK.weight = Mathf.Clamp01(peso);
    }

    private void RestaurarPesoIK(float pesoDerecha, float pesoIzquierda)
    {
        if (rightHandIK != null)
            rightHandIK.weight = Mathf.Clamp01(pesoDerecha);

        if (leftHandIK != null)
            leftHandIK.weight = Mathf.Clamp01(pesoIzquierda);
    }

    private void GuardarPoseOriginalArma()
    {
        if (!seguirArmaConManoIzquierda)
            return;

        if (armaASeguir == null)
            return;

        posicionLocalOriginalArma = armaASeguir.localPosition;
        rotacionLocalOriginalArma = armaASeguir.localRotation;
        poseOriginalArmaGuardada = true;
    }

    private void RestaurarPoseOriginalArma()
    {
        if (!seguirArmaConManoIzquierda)
            return;

        if (!poseOriginalArmaGuardada || armaASeguir == null)
            return;

        armaASeguir.localPosition = posicionLocalOriginalArma;
        armaASeguir.localRotation = rotacionLocalOriginalArma;
        poseOriginalArmaGuardada = false;
    }
}
