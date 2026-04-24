using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Cofre_Receptor : MonoBehaviour
{
    [Header("Entrada")]
    public KeyCode teclaVaciar = KeyCode.I;
    public KeyCode teclaInventario = KeyCode.M;

    [Header("Vaciar Balas")]
    [Tooltip("Cantidad de balas por vaciado")]
    public int balasPorVaciado = 10;
    [Tooltip("Segundos entre cada bala vaciada para visualizar progreso")]
    public float intervaloPorBala = 0.08f;

    [Header("UI")]
    public Slider barraProgresoVaciado;
    public GameObject panelInventario;
    public TMP_Text textoInventario;

    [Header("Estado (solo lectura)")]
    [SerializeField] private int balasDepositadas = 0;

    private bool jugadorEnRango = false;
    private bool vaciando = false;
    private bool inventarioAbierto = false;
    private Coroutine rutinaVaciado;

    private void Start()
    {
        ConfigurarUIInicial();
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaInventario) && !vaciando)
        {
            ToggleInventario();
        }

        if (!jugadorEnRango)
            return;

        if (inventarioAbierto)
            return;

        if (Input.GetKeyDown(teclaVaciar) && !vaciando)
        {
            IntentarVaciar();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorEnRango = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        jugadorEnRango = false;

        if (!vaciando && !inventarioAbierto)
        {
            PlayerMovement.SetBloqueoMovimientoExterno(false);
        }
    }

    private void ConfigurarUIInicial()
    {
        if (barraProgresoVaciado != null)
        {
            barraProgresoVaciado.minValue = 0f;
            barraProgresoVaciado.maxValue = Mathf.Max(balasPorVaciado, balasDepositadas);
            barraProgresoVaciado.value = balasDepositadas;
            barraProgresoVaciado.gameObject.SetActive(true);
        }

        if (panelInventario != null)
        {
            panelInventario.SetActive(false);
        }

        ActualizarTextoInventario();
    }

    private void ToggleInventario()
    {
        inventarioAbierto = !inventarioAbierto;

        if (panelInventario != null)
        {
            panelInventario.SetActive(inventarioAbierto);
        }

        if (inventarioAbierto)
        {
            ActualizarTextoInventario();
        }

        PlayerMovement.SetBloqueoMovimientoExterno(inventarioAbierto || vaciando);
    }

    private void IntentarVaciar()
    {
        if (GameManager.Instance == null)
            return;

        int balasDisponibles = Mathf.Max(0, GameManager.Instance.gunammo);
        int balasATransferir = Mathf.Min(balasDisponibles, balasPorVaciado);

        if (balasATransferir <= 0)
        {
            Debug.Log("[Cofre_Receptor] No hay balas para vaciar.");
            return;
        }

        if (rutinaVaciado != null)
        {
            StopCoroutine(rutinaVaciado);
        }

        rutinaVaciado = StartCoroutine(VaciarBalasCoroutine(balasATransferir));
    }

    private IEnumerator VaciarBalasCoroutine(int balasATransferir)
    {
        vaciando = true;
        PlayerMovement.SetBloqueoMovimientoExterno(true);

        int valorInicialBarra = balasDepositadas;

        if (barraProgresoVaciado != null)
        {
            barraProgresoVaciado.maxValue = Mathf.Max(balasPorVaciado, valorInicialBarra + balasATransferir);
            barraProgresoVaciado.value = valorInicialBarra;
        }

        int transferidas = 0;
        while (transferidas < balasATransferir)
        {
            transferidas++;

            if (barraProgresoVaciado != null)
            {
                barraProgresoVaciado.value = valorInicialBarra + transferidas;
            }

            yield return new WaitForSeconds(intervaloPorBala);
        }

        GameManager.Instance.gunammo = Mathf.Max(0, GameManager.Instance.gunammo - balasATransferir);
        balasDepositadas += balasATransferir;
        ActualizarTextoInventario();

        if (barraProgresoVaciado != null)
        {
            barraProgresoVaciado.maxValue = Mathf.Max(balasPorVaciado, balasDepositadas);
            barraProgresoVaciado.value = balasDepositadas;
        }

        vaciando = false;
        rutinaVaciado = null;

        PlayerMovement.SetBloqueoMovimientoExterno(inventarioAbierto);
    }

    private void ActualizarTextoInventario()
    {
        if (textoInventario == null)
            return;

        textoInventario.text = "Inventario\nBalas vaciadas en caja: " + balasDepositadas;
    }
}
