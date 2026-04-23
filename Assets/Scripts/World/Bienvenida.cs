using UnityEngine;
using System.Collections;

public class Bienvenida : MonoBehaviour
{
    [SerializeField] private GameObject objetoADesactivar;
    [SerializeField] private float tiempoParaDesactivar = 10f;

    private void Awake()
    {
        if (objetoADesactivar == null)
        {
            objetoADesactivar = gameObject;
        }

        if (!objetoADesactivar.activeSelf)
        {
            objetoADesactivar.SetActive(true);
        }
    }

    private void Start()
    {
        StartCoroutine(DesactivarDespuesDeTiempo());
    }

    private IEnumerator DesactivarDespuesDeTiempo()
    {
        yield return new WaitForSeconds(tiempoParaDesactivar);

        if (objetoADesactivar != null)
        {
            objetoADesactivar.SetActive(false);
        }
    }
}
