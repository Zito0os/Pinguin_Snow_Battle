using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerMinimapOwner : MonoBehaviourPunCallbacks
{
    [Header("Escena Multiplayer")]
    public string nombreEscenaMultiplayer = "MultiPlayer";

    [Header("Referencias minimapa (opcionales)")]
    public Camera minimapCamera;
    public GameObject minimapUI;

    [Header("Busqueda automatica")]
    public string nombreCamaraMinimapa = "Camara_Minimapa";
    public string nombreUIMinimapa = "MiniMapa";
    public bool autoconfigurarEnInicio = true;

    [Header("Validacion")]
    public bool validarPeriodicamente = true;
    public float intervaloValidacion = 0.5f;

    private bool usarPhotonEnEscena = false;
    private float proximaValidacion = 0f;

    private void Start()
    {
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();

        if (autoconfigurarEnInicio)
        {
            AutoDetectarReferenciasEnEstaInstancia();
        }

        AplicarEstadoMinimapa();
    }

    private void Update()
    {
        if (!usarPhotonEnEscena || !validarPeriodicamente)
            return;

        if (Time.unscaledTime < proximaValidacion)
            return;

        proximaValidacion = Time.unscaledTime + Mathf.Max(0.1f, intervaloValidacion);
        AplicarEstadoMinimapa();
    }

    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == nombreEscenaMultiplayer || escena == "MultiPlayer" || escena == "Multi_Player";
    }

    private bool EsLocalOwner()
    {
        if (!usarPhotonEnEscena)
            return true;

        return photonView != null && photonView.IsMine;
    }

    private void AutoDetectarReferenciasEnEstaInstancia()
    {
        if (minimapCamera == null)
        {
            Camera[] camaras = GetComponentsInChildren<Camera>(true);
            minimapCamera = camaras.FirstOrDefault(c => c != null && c.name == nombreCamaraMinimapa)
                            ?? camaras.FirstOrDefault(c => c != null && c.name.ToLower().Contains("minimapa"))
                            ?? camaras.FirstOrDefault(c => c != null && c.name.ToLower().Contains("mini"));
        }

        if (minimapUI == null)
        {
            Transform ui = BuscarHijoPorNombre(transform, nombreUIMinimapa);
            if (ui != null)
            {
                minimapUI = ui.gameObject;
            }
        }
    }

    private void AplicarEstadoMinimapa()
    {
        if (!usarPhotonEnEscena)
            return;

        bool esLocal = EsLocalOwner();

        if (minimapCamera != null)
        {
            minimapCamera.enabled = esLocal;
            AudioListener listener = minimapCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = esLocal;
            }
        }

        if (minimapUI != null)
        {
            minimapUI.SetActive(esLocal);
        }
    }

    private Transform BuscarHijoPorNombre(Transform raiz, string nombre)
    {
        if (raiz == null)
            return null;

        if (raiz.name == nombre)
            return raiz;

        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform encontrado = BuscarHijoPorNombre(raiz.GetChild(i), nombre);
            if (encontrado != null)
                return encontrado;
        }

        return null;
    }
}
