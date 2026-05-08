using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;

public class AI : MonoBehaviour
{

    public NavMeshAgent naveMeshAgent;

    public Transform[] destinations;
    public Transform contenedorDestinos;

    public float distanceToFollowPath;

    private int i = 0;

    public bool followPlayer;

    //[Header("---------Follow Player---------")]

    private float distanceToPlayer;

    public float distanceToFollowPlayer = 15;
    public float distanceToAttackPlayer = 8f;
    public float rotationSpeed = 8f;
    public float deathDelay = 2f;
    public bool perseguirSiempreSiRecibeDanio = true;

    [Header("Sonido muerte enemigo")]
    public AudioClip sonidoMuerte;
    [Range(0f, 1f)] public float volumenSonidoMuerte = 1f;
    public float distanciaMinSonidoMuerte = 2f;
    public float distanciaMaxSonidoMuerte = 35f;

    [Header("Animator")]
    public Animator animator;
    public string paramIsAlive = "isAlive";
    public string paramIsFire = "isFire";
    public string paramIsFollow = "isFollow";
    public string paramIsChill = "isChill";

    [Header("Weapon IK / Grips")]
    public TwoBoneIKConstraint rightHandIK;
    public TwoBoneIKConstraint leftHandIK;
    public Transform rightHandBone;
    public Transform leftHandBone;
    public Transform rightGrip;
    public Transform leftGrip;
    public bool actualizarGripsConHuesos = true;
    public bool soloGripSigueHueso = true;
    public bool desactivarIKAlMorir = true;
    public bool desactivarGripsAlMorir = true;

    [Header("Minimapa")]
    public bool habilitarIconoMinimapa = true;
    public bool mostrarSoloEnSinglePlayer = true;
    public string nombreEscenaSinglePlayer = "Single_Player";
    public string nombreCamaraMinimapa = "Camara_Minimapa";
    public string nombreCapaMinimapa = "MinimapIcon";
    public float alturaIconoMinimapa = 3f;
    public float tamanoIconoMinimapa = 0.4f;
    public Color colorIconoMinimapa = Color.red;
    public Sprite spriteIconoMinimapa;

    private GameObject player;
    private EnemyShoot enemyShoot;
    private bool isDead = false;
    private Anclajes_de_arma anclajesArma;
    private Transform iconoMinimapa;
    private static bool cullingMinimapaConfigurado = false;
    private static int escenaCullingConfigurada = -1;
    private static Sprite spriteDefaultMinimapa;
    private float siguienteRevisionCulling = 0f;
    private bool iconoMinimapaListo = false;
    private bool agresivoPorDanio = false;

    public float liveEnemy = 100;

    //public GameObject destination1;
    //public GameObject destination2;

    private void Awake()
    {
        ValidarSpawnEnemigo();
    }

    private void OnEnable()
    {
        ValidarSpawnEnemigo();
    }

    void Start()
    {
        ValidarSpawnEnemigo();

        AutoAsignarDestinosSiFaltan();

        // Elegir un destino inicial aleatorio para que cada IA arranque distinto.
        if (TieneDestinosValidos())
        {
            i = ObtenerIndiceDestinoAleatorio(-1);
            naveMeshAgent.destination = destinations[i].transform.position;
        }

        //busca el objeto del jugador en la escena que tenga el script PlayerMovement
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.gameObject;
        }
        enemyShoot = GetComponent<EnemyShoot>();
        anclajesArma = GetComponentInChildren<Anclajes_de_arma>(true);

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        AutoAsignarReferenciasIKYGrips();
        ValidarAsignacionesCriticas();
        ConfigurarIconoMinimapa();

        SetAnimationState(chill: true, follow: false, fire: false);
        SetAnimatorBool(paramIsAlive, true);
    }

    private void AutoAsignarDestinosSiFaltan()
    {
        bool destinosVacios = destinations == null || destinations.Length == 0 || destinations.All(d => d == null);
        if (!destinosVacios)
            return;

        if (contenedorDestinos == null)
        {
            GameObject contenedor = GameObject.Find("Destinos_IA");
            if (contenedor != null)
            {
                contenedorDestinos = contenedor.transform;
            }
        }

        if (contenedorDestinos == null)
            return;

        List<Transform> hijos = new List<Transform>();
        for (int i = 0; i < contenedorDestinos.childCount; i++)
        {
            Transform hijo = contenedorDestinos.GetChild(i);
            if (hijo != null)
            {
                hijos.Add(hijo);
            }
        }

        destinations = hijos.ToArray();
    }


    void Update()
    {
        if (isDead)
            return;

        if (player == null)
            return;

        //float distance = Vector3.Distance(transform.position, destination1.transform.position);

        ////usando las distancias podemos hacer de todos que vaya en un loop de un lado al otro o asi o que si esta cerca de nosotros pues que nos empiece a seguir
        //if (distance < 2) {

        //    //aqui mandamos al agente a el destino que es el destination2
        //    naveMeshAgent.destination = destination2.transform.position;
        //}





        //si pones solo transform entra al que tiene asignado el script en este caso es el enemy
        //calcula la distancia de punto a a punto b 
        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= distanceToAttackPlayer)
        {
            AttackPlayer();
            return;
        }

        bool debeSeguirPlayer = (distanceToPlayer <= distanceToFollowPlayer && followPlayer) || agresivoPorDanio;

        if (debeSeguirPlayer)
        {
            naveMeshAgent.isStopped = false;
            if (enemyShoot != null)
            {
                enemyShoot.SetShooting(false);
            }
            FollowPlayer();
        }
        else
        {
            naveMeshAgent.isStopped = false;
            if (enemyShoot != null)
            {
                enemyShoot.SetShooting(false);
            }
            //si no esta cerca del jugador entonces sigue el camino
            if (TieneDestinosValidos())
            {
                EnemyPath();
            }
            else
            {
                SetAnimationState(chill: true, follow: false, fire: false);
                naveMeshAgent.ResetPath();
            }
        }




    }

    private void LateUpdate()
    {
        if (isDead)
            return;

        RevisarCullingMinimapaPeriodicamente();

        if (soloGripSigueHueso)
        {
            SetIKWeight(rightHandIK, 0f);
            SetIKWeight(leftHandIK, 0f);
        }

        if (anclajesArma != null && anclajesArma.isActiveAndEnabled)
            return;

        if (!actualizarGripsConHuesos)
            return;

        AnclarGripAHueso(rightGrip, rightHandBone);
        AnclarGripAHueso(leftGrip, leftHandBone);
    }

    public void EnemyPath()
    {
        if (!TieneDestinosValidos())
            return;

        SetAnimationState(chill: true, follow: false, fire: false);
        naveMeshAgent.destination = destinations[i].transform.position;


        //si la distancia entre el agente y el destino es menor o igual a la distancia que queremos para seguir el camino, entonces cambiamos al siguiente destino
        if (Vector3.Distance(transform.position, destinations[i].position) <= distanceToFollowPath)
        {
            i = ObtenerIndiceDestinoAleatorio(i);
            naveMeshAgent.destination = destinations[i].position;
        }


    }

    private int ObtenerIndiceDestinoAleatorio(int indiceActual)
    {
        if (!TieneDestinosValidos())
            return 0;

        List<int> indicesValidos = new List<int>();
        for (int j = 0; j < destinations.Length; j++)
        {
            if (destinations[j] != null && j != indiceActual)
            {
                indicesValidos.Add(j);
            }
        }

        if (indicesValidos.Count == 0)
            return Mathf.Max(0, indiceActual);

        if (indicesValidos.Count == 1)
            return indicesValidos[0];

        return indicesValidos[Random.Range(0, indicesValidos.Count)];
    }




    public void FollowPlayer()
    {
        SetAnimationState(chill: false, follow: true, fire: false);
        RotateTowardsPlayer();
        // Aqui podemos hacer que el agente siga al jugador
        naveMeshAgent.destination = player.transform.position;
    }

    private void AttackPlayer()
    {
        SetAnimationState(chill: false, follow: false, fire: true);
        naveMeshAgent.isStopped = true;
        naveMeshAgent.ResetPath();
        RotateTowardsPlayer();

        if (enemyShoot != null)
        {
            enemyShoot.SetShooting(true);
        }
    }

    private void RotateTowardsPlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }


    public void GrenadeImpact(float damage)
    {
        LooseLife(damage);
    }


    public void LooseLife(float LiveToLose)
    {
        if (isDead)
            return;

        if (LiveToLose > 0f && perseguirSiempreSiRecibeDanio)
        {
            agresivoPorDanio = true;
            followPlayer = true;
        }

        liveEnemy = liveEnemy - LiveToLose;

        if(liveEnemy <= 0)
        {
            StartCoroutine(HandleDeath());
        }
    }

    private IEnumerator HandleDeath()
    {
        isDead = true;

        ReproducirSonidoMuerte();

        if (iconoMinimapa != null)
        {
            iconoMinimapa.gameObject.SetActive(false);
        }

        if (enemyShoot != null)
        {
            enemyShoot.SetShooting(false);
        }

        if (naveMeshAgent != null)
        {
            naveMeshAgent.isStopped = true;
            naveMeshAgent.ResetPath();
        }

        SetAnimationState(chill: false, follow: false, fire: false);
        SetAnimatorBool(paramIsAlive, false);

        if (desactivarIKAlMorir)
        {
            SetIKWeight(rightHandIK, 0f);
            SetIKWeight(leftHandIK, 0f);
        }

        if (desactivarGripsAlMorir)
        {
            if (rightGrip != null)
                rightGrip.gameObject.SetActive(false);

            if (leftGrip != null)
                leftGrip.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(deathDelay);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Kills += 1;
        }

        Destroy(gameObject);
    }

    private void ReproducirSonidoMuerte()
    {
        if (sonidoMuerte == null)
            return;

        GameObject audioTemp = new GameObject("SFX_EnemyDeath");
        audioTemp.transform.position = transform.position;

        AudioSource source = audioTemp.AddComponent<AudioSource>();
        source.clip = sonidoMuerte;
        source.volume = Mathf.Clamp01(volumenSonidoMuerte);
        source.spatialBlend = 1f;
        source.minDistance = Mathf.Max(0.1f, distanciaMinSonidoMuerte);
        source.maxDistance = Mathf.Max(source.minDistance + 0.1f, distanciaMaxSonidoMuerte);
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.Play();

        Destroy(audioTemp, sonidoMuerte.length + 0.1f);
    }

    private void SetAnimationState(bool chill, bool follow, bool fire)
    {
        SetAnimatorBool(paramIsChill, chill);
        SetAnimatorBool(paramIsFollow, follow);
        SetAnimatorBool(paramIsFire, fire);
    }

    private void SetAnimatorBool(string parameter, bool value)
    {
        if (animator == null || string.IsNullOrEmpty(parameter))
            return;

        animator.SetBool(parameter, value);
    }

    private void AnclarGripAHueso(Transform grip, Transform handBone)
    {
        if (grip == null || handBone == null)
            return;

        grip.position = handBone.position;
        grip.rotation = handBone.rotation;
    }

    private void SetIKWeight(TwoBoneIKConstraint ik, float weight)
    {
        if (ik == null)
            return;

        ik.weight = Mathf.Clamp01(weight);
    }

    private void AutoAsignarReferenciasIKYGrips()
    {
        if (animator != null)
        {
            if (rightHandBone == null)
                rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);

            if (leftHandBone == null)
                leftHandBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        if (rightGrip == null)
            rightGrip = FindChildRecursive(transform, "Grip_rigth");

        if (leftGrip == null)
            leftGrip = FindChildRecursive(transform, "Grip_lefth");

        if (rightHandIK == null)
            rightHandIK = FindIKByName("RigthHandIK", "RightHandIK");

        if (leftHandIK == null)
            leftHandIK = FindIKByName("LeftHandIK");
    }

    private TwoBoneIKConstraint FindIKByName(params string[] possibleNames)
    {
        TwoBoneIKConstraint[] constraints = GetComponentsInChildren<TwoBoneIKConstraint>(true);
        for (int i = 0; i < constraints.Length; i++)
        {
            TwoBoneIKConstraint constraint = constraints[i];
            if (constraint == null)
                continue;

            string currentName = constraint.gameObject.name;
            for (int j = 0; j < possibleNames.Length; j++)
            {
                if (currentName == possibleNames[j])
                    return constraint;
            }
        }

        return null;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void ValidarAsignacionesCriticas()
    {
        if (animator == null)
            Debug.LogWarning($"[AI] {name}: animator no asignado.");

        if (enemyShoot == null)
            Debug.LogWarning($"[AI] {name}: EnemyShoot no asignado/encontrado.");

        if (actualizarGripsConHuesos)
        {
            if (rightGrip == null || leftGrip == null)
                Debug.LogWarning($"[AI] {name}: falta asignar grips (rightGrip/leftGrip).");

            if (rightHandBone == null || leftHandBone == null)
                Debug.LogWarning($"[AI] {name}: falta asignar huesos de manos (rightHandBone/leftHandBone).");
        }
    }

    public void ValidarSpawnEnemigo()
    {
        isDead = false;
        agresivoPorDanio = false;

        if (liveEnemy <= 0f)
            liveEnemy = 100f;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        SetAnimationState(chill: true, follow: false, fire: false);
        SetAnimatorBool(paramIsAlive, true);
    }

    private void ConfigurarIconoMinimapa()
    {
        if (!habilitarIconoMinimapa)
            return;

        if (mostrarSoloEnSinglePlayer && SceneManager.GetActiveScene().name != nombreEscenaSinglePlayer)
            return;

        int layerMinimapa = LayerMask.NameToLayer(nombreCapaMinimapa);
        if (layerMinimapa < 0)
        {
            Debug.LogWarning($"[AI] {name}: no existe la capa '{nombreCapaMinimapa}'.");
            return;
        }

        if (iconoMinimapa == null)
        {
            Transform existente = transform.Find("MinimapEnemyIcon");
            if (existente != null)
            {
                iconoMinimapa = existente;
            }
        }

        if (iconoMinimapa != null && iconoMinimapa.GetComponent<SpriteRenderer>() == null)
        {
            Destroy(iconoMinimapa.gameObject);
            iconoMinimapa = null;
        }

        if (iconoMinimapa == null)
        {
            GameObject icono = new GameObject("MinimapEnemyIcon");
            icono.transform.SetParent(transform, false);
            iconoMinimapa = icono.transform;
        }

        if (iconoMinimapa != null)
        {
            iconoMinimapa.localPosition = new Vector3(0f, alturaIconoMinimapa, 0f);
            iconoMinimapa.localRotation = Quaternion.Euler(90f, 0f, 0f);
            iconoMinimapa.localScale = Vector3.one * tamanoIconoMinimapa;
            iconoMinimapa.gameObject.layer = layerMinimapa;

            SpriteRenderer spriteRenderer = iconoMinimapa.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = iconoMinimapa.gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = spriteIconoMinimapa != null ? spriteIconoMinimapa : GetSpriteDefaultMinimapa();
            spriteRenderer.color = colorIconoMinimapa;
            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingOrder = 500;
        }

        int escenaActual = SceneManager.GetActiveScene().buildIndex;
        if (!cullingMinimapaConfigurado || escenaCullingConfigurada != escenaActual)
        {
            bool ok = ConfigurarCullingDeCamaras(layerMinimapa);
            cullingMinimapaConfigurado = ok;
            escenaCullingConfigurada = ok ? escenaActual : -1;
            iconoMinimapaListo = ok;
        }
        else
        {
            iconoMinimapaListo = true;
        }

        if (iconoMinimapa != null)
        {
            iconoMinimapa.gameObject.SetActive(iconoMinimapaListo);
        }
    }

    private bool TieneDestinosValidos()
    {
        return destinations != null && destinations.Length > 0 && destinations.Any(d => d != null);
    }

    private Sprite GetSpriteDefaultMinimapa()
    {
        if (spriteDefaultMinimapa != null)
            return spriteDefaultMinimapa;

        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float centro = 15.5f;
        float radio = 14f;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro));
                Color color = distancia <= radio ? Color.white : new Color(1f, 1f, 1f, 0f);
                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        spriteDefaultMinimapa = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 64f);
        return spriteDefaultMinimapa;
    }

    private bool ConfigurarCullingDeCamaras(int layerMinimapa)
    {
        Camera[] camaras = FindObjectsOfType<Camera>(true);
        if (camaras == null || camaras.Length == 0)
            return false;

        Camera camaraMinimapa = camaras.FirstOrDefault(c => c != null && c.name == nombreCamaraMinimapa);
        if (camaraMinimapa == null)
        {
            camaraMinimapa = camaras.FirstOrDefault(c => c != null && c.name.ToLower().Contains("minimapa"));
        }

        if (camaraMinimapa == null)
        {
            Debug.LogWarning($"[AI] {name}: no se encontró la cámara de minimapa '{nombreCamaraMinimapa}'.");
            return false;
        }

        int maskLayer = 1 << layerMinimapa;
        camaraMinimapa.cullingMask |= maskLayer;

        for (int i = 0; i < camaras.Length; i++)
        {
            Camera cam = camaras[i];
            if (cam == null || cam == camaraMinimapa)
                continue;

            cam.cullingMask &= ~maskLayer;
        }

        return true;
    }

    private void RevisarCullingMinimapaPeriodicamente()
    {
        if (!habilitarIconoMinimapa)
            return;

        if (mostrarSoloEnSinglePlayer && SceneManager.GetActiveScene().name != nombreEscenaSinglePlayer)
            return;

        if (Time.unscaledTime < siguienteRevisionCulling)
            return;

        siguienteRevisionCulling = Time.unscaledTime + 1f;

        int layerMinimapa = LayerMask.NameToLayer(nombreCapaMinimapa);
        if (layerMinimapa < 0)
            return;

        bool ok = ConfigurarCullingDeCamaras(layerMinimapa);
        iconoMinimapaListo = ok;

        if (iconoMinimapa != null)
        {
            iconoMinimapa.gameObject.SetActive(ok);
        }
    }


}
