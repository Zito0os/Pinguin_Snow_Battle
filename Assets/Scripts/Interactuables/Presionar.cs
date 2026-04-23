using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Linq;

public class Presionar : MonoBehaviour
{
    //public GameObject Pivot;
    //private Transform playerPosition;

    public Transform cameraReferencia;
    public string nombreEscenaMultiplayer = "MultiPlayer";
    private bool usarPhotonEnEscena = false;
    private float proximaBusquedaCamara = 0f;





    private void Start()
    {
        usarPhotonEnEscena = EsEscenaMultiplayerActiva();

        if (cameraReferencia == null)
        {
            ResolverCamaraReferencia();
        }

        if (cameraReferencia == null)
        {
           Debug.LogWarning("No se ha asignado una referencia de c�mara. El objeto no podr� rotar hacia la c�mara.");
        }
    }

    void LateUpdate()
    {
        if (cameraReferencia == null || (usarPhotonEnEscena && Time.unscaledTime >= proximaBusquedaCamara))
        {
            proximaBusquedaCamara = Time.unscaledTime + 0.5f;
            ResolverCamaraReferencia();
        }

        if (cameraReferencia != null)
        {
            transform.LookAt(cameraReferencia);
            transform.Rotate(90, 0, 0);

        }

    }

    private bool EsEscenaMultiplayerActiva()
    {
        string escena = SceneManager.GetActiveScene().name;
        return escena == nombreEscenaMultiplayer || escena == "MultiPlayer" || escena == "Multi_Player";
    }

    private void ResolverCamaraReferencia()
    {
        if (usarPhotonEnEscena)
        {
            Cameralook[] camarasLook = FindObjectsOfType<Cameralook>(true);
            Cameralook camLocal = camarasLook.FirstOrDefault(c => c != null && c.photonView != null && c.photonView.IsMine);
            if (camLocal != null)
            {
                cameraReferencia = camLocal.transform;
                return;
            }
        }

        if (Camera.main != null)
        {
            cameraReferencia = Camera.main.transform;
        }
    }
}



