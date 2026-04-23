using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Recoleccion_cambiar : MonoBehaviour
{
    [SerializeField] private float intervaloSegundos = 15f;
    [SerializeField] private string healthTag = "HealthObject";
    [SerializeField] private string gunAmmoTag = "GunAmmo";

    private readonly List<GameObject> healthObjects = new List<GameObject>();
    private readonly List<GameObject> gunAmmoObjects = new List<GameObject>();

    // true = GunAmmo activos, false = Health activos
    private bool estadoGunAmmoActivo = true;

    private void Start()
    {
        CachearObjetosPorTag();

        // Estado inicial: GunAmmo ON, Health OFF
        estadoGunAmmoActivo = true;
        AplicarEstado();

        StartCoroutine(BucleCambio());
    }

    private void CachearObjetosPorTag()
    {
        healthObjects.Clear();
        gunAmmoObjects.Clear();

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform t in allTransforms)
        {
            GameObject go = t.gameObject;

            if (go.CompareTag(healthTag))
            {
                healthObjects.Add(go);
            }
            else if (go.CompareTag(gunAmmoTag))
            {
                gunAmmoObjects.Add(go);
            }
        }
    }

    private IEnumerator BucleCambio()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervaloSegundos);
            estadoGunAmmoActivo = !estadoGunAmmoActivo;
            AplicarEstado();
        }
    }

    private void AplicarEstado()
    {
        SetListaActiva(gunAmmoObjects, estadoGunAmmoActivo);
        SetListaActiva(healthObjects, !estadoGunAmmoActivo);
    }

    private void SetListaActiva(List<GameObject> lista, bool activo)
    {
        foreach (GameObject go in lista)
        {
            if (go != null)
            {
                go.SetActive(activo);
            }
        }
    }
}
