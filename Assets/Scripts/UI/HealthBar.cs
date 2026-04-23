using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
    public Image fillImage;

    public Color colorRojo = Color.red;
    public Color colorAmarillo = Color.yellow;
    public Color colorVerde = Color.green;

    private int ultimoMaxHealth = -1;

    void Start()
    {
        InicializarBarra();
    }

    void Update()
    {
        if (GameManager.Instance == null || healthSlider == null)
            return;

        if (ultimoMaxHealth != GameManager.Instance.maxHealth)
        {
            ultimoMaxHealth = Mathf.Max(1, GameManager.Instance.maxHealth);
            healthSlider.maxValue = ultimoMaxHealth;
        }

        healthSlider.value = Mathf.Clamp(GameManager.Instance.health, 0, healthSlider.maxValue);
        ActualizarColorRelleno();
    }

    private void InicializarBarra()
    {
        if (healthSlider == null || GameManager.Instance == null)
            return;

        ultimoMaxHealth = Mathf.Max(1, GameManager.Instance.maxHealth);
        healthSlider.maxValue = ultimoMaxHealth;
        healthSlider.value = Mathf.Clamp(GameManager.Instance.health, 0, healthSlider.maxValue);
        ActualizarColorRelleno();
    }

    private void ActualizarColorRelleno()
    {
        if (fillImage == null || healthSlider.maxValue <= 0)
            return;

        float porcentajeVida = (healthSlider.value / healthSlider.maxValue) * 100f;

        if (porcentajeVida <= 33f)
        {
            fillImage.color = colorRojo;
        }
        else if (porcentajeVida <= 66f)
        {
            fillImage.color = colorAmarillo;
        }
        else
        {
            fillImage.color = colorVerde;
        }
    }
}
