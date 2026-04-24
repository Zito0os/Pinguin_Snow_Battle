using UnityEngine;
using System.Collections;

public class Abrir_puerta : MonoBehaviour
{
    [Header("Puerta")]
    [Tooltip("Empty con la posición y rotación de la puerta abierta")]
    public Transform posicionAbierta;
    [Tooltip("Tiempo en segundos para abrir o cerrar la puerta")]
    public float tiempoInterpolacion = 0.4f;

    private bool estaAbierta;
    private Vector3 posicionCerrada;
    private Quaternion rotacionCerrada;
    private Coroutine rutinaMovimiento;

    private void Awake()
    {
        posicionCerrada = transform.position;
        rotacionCerrada = transform.rotation;
        estaAbierta = false;
    }

    public void Interactuar()
    {
        Debug.Log("[Abrir_puerta] Interactuar() llamado");
        estaAbierta = !estaAbierta;
        Debug.Log("[Abrir_puerta] Estado: " + (estaAbierta ? "ABIERTA" : "CERRADA"));

        if (rutinaMovimiento != null)
        {
            Debug.Log("[Abrir_puerta] Deteniendo corrutina anterior");
            StopCoroutine(rutinaMovimiento);
        }

        if (estaAbierta)
        {
            Debug.Log("[Abrir_puerta] Iniciando AbrirPuerta()");
            rutinaMovimiento = StartCoroutine(AbrirPuerta());
        }
        else
        {
            Debug.Log("[Abrir_puerta] Iniciando CerrarPuerta()");
            rutinaMovimiento = StartCoroutine(CerrarPuerta());
        }
    }

    private IEnumerator AbrirPuerta()
    {
        Debug.Log("[Abrir_puerta] AbrirPuerta() iniciado");
        if (posicionAbierta == null)
        {
            Debug.LogError("[Abrir_puerta] ❌ ERROR: No se ha asignado 'posicionAbierta' en el Inspector");
            estaAbierta = false;
            yield break;
        }
        Debug.Log("[Abrir_puerta] posicionAbierta asignada correctamente");

        float tiempoTranscurrido = 0f;
        Vector3 posInicial = transform.position;
        Quaternion rotInicial = transform.rotation;

        Vector3 posDestino = posicionAbierta.position;
        Quaternion rotDestino = posicionAbierta.rotation;

        while (tiempoTranscurrido < tiempoInterpolacion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / tiempoInterpolacion;

            transform.position = Vector3.Lerp(posInicial, posDestino, progreso);
            transform.rotation = Quaternion.Lerp(rotInicial, rotDestino, progreso);

            yield return null;
        }

        transform.position = posDestino;
        transform.rotation = rotDestino;
        Debug.Log("[Abrir_puerta] Puerta abierta completamente");
        rutinaMovimiento = null;
    }

    private IEnumerator CerrarPuerta()
    {
        Debug.Log("[Abrir_puerta] CerrarPuerta() iniciado");
        float tiempoTranscurrido = 0f;
        Vector3 posInicial = transform.position;
        Quaternion rotInicial = transform.rotation;

        while (tiempoTranscurrido < tiempoInterpolacion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / tiempoInterpolacion;

            transform.position = Vector3.Lerp(posInicial, posicionCerrada, progreso);
            transform.rotation = Quaternion.Lerp(rotInicial, rotacionCerrada, progreso);

            yield return null;
        }

        transform.position = posicionCerrada;
        transform.rotation = rotacionCerrada;
        Debug.Log("[Abrir_puerta] Puerta cerrada completamente");
        rutinaMovimiento = null;
    }
}
