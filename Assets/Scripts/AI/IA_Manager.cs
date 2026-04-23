using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class IA_Manager : MonoBehaviour
{
    [Header("Spawn Config")]
    public Transform[] puntosSpawn;
    public GameObject enemigoPrefab;

    [Header("Control de clones")]
    public int cantidad_enemigos = 5;
    public float tiempo_spawn = 1f;
    public int clones_maximos = 20;
    public int clones_minimos = 2;

    [Header("Oleadas")]
    public int cantidad_spawn_bajo_minimo = 3;
    public int cantidad_spawn_por_minuto = 3;
    public float intervalo_spawn_minuto = 60f;

    private readonly List<GameObject> enemigosActivos = new List<GameObject>();
    private Transform player;

    void Start()
    {
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }

        SpawnInicial();
        StartCoroutine(ControlarMinimos());
        StartCoroutine(SpawnPorMinuto());
    }


    void Update()
    {
        LimpiarListaEnemigos();
    }

    private void SpawnInicial()
    {
        SpawnEnPuntosFijos(cantidad_enemigos);
    }

    private IEnumerator ControlarMinimos()
    {
        while (true)
        {
            LimpiarListaEnemigos();

            if (enemigosActivos.Count < clones_minimos)
            {
                SpawnEnPuntosMasLejanos(cantidad_spawn_bajo_minimo);
            }

            yield return new WaitForSeconds(tiempo_spawn);
        }
    }

    private IEnumerator SpawnPorMinuto()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervalo_spawn_minuto);
            SpawnEnPuntosRandom(cantidad_spawn_por_minuto);
        }
    }

    private void SpawnEnPuntosFijos(int cantidad)
    {
        if (!PuedeSpawnear())
            return;

        int spawned = 0;
        for (int i = 0; i < puntosSpawn.Length && spawned < cantidad; i++)
        {
            if (!PuedeSpawnearMas())
                break;

            if (puntosSpawn[i] == null)
                continue;

            InstanciarEnemigo(puntosSpawn[i]);
            spawned++;
        }
    }

    private void SpawnEnPuntosMasLejanos(int cantidad)
    {
        if (!PuedeSpawnear())
            return;

        if (player == null)
        {
            SpawnEnPuntosRandom(cantidad);
            return;
        }

        List<Transform> ordenados = puntosSpawn
            .Where(p => p != null)
            .OrderByDescending(p => Vector3.Distance(player.position, p.position))
            .ToList();

        int spawned = 0;
        for (int i = 0; i < ordenados.Count && spawned < cantidad; i++)
        {
            if (!PuedeSpawnearMas())
                break;

            InstanciarEnemigo(ordenados[i]);
            spawned++;
        }
    }

    private void SpawnEnPuntosRandom(int cantidad)
    {
        if (!PuedeSpawnear())
            return;

        List<Transform> puntosValidos = puntosSpawn.Where(p => p != null).ToList();
        if (puntosValidos.Count == 0)
            return;

        for (int i = 0; i < cantidad; i++)
        {
            if (!PuedeSpawnearMas())
                break;

            Transform punto = puntosValidos[Random.Range(0, puntosValidos.Count)];
            InstanciarEnemigo(punto);
        }
    }

    private void InstanciarEnemigo(Transform punto)
    {
        GameObject nuevoEnemigo = Instantiate(enemigoPrefab, punto.position, punto.rotation);

        AI[] enemigosIA = nuevoEnemigo.GetComponentsInChildren<AI>(true);
        for (int i = 0; i < enemigosIA.Length; i++)
        {
            if (enemigosIA[i] != null)
            {
                enemigosIA[i].ValidarSpawnEnemigo();
            }
        }

        enemigosActivos.Add(nuevoEnemigo);
    }

    private void LimpiarListaEnemigos()
    {
        enemigosActivos.RemoveAll(e => e == null);
    }

    private bool PuedeSpawnear()
    {
        return enemigoPrefab != null && puntosSpawn != null && puntosSpawn.Length > 0;
    }

    private bool PuedeSpawnearMas()
    {
        LimpiarListaEnemigos();
        return enemigosActivos.Count < clones_maximos;
    }
}
