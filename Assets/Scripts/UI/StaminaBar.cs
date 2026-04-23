using UnityEngine;
using UnityEngine.UI;
using System.Collections;




public class StaminaBar : MonoBehaviour
{

    public Slider staminaSlider;

    public float maxStamina = 100;

    private float currentStamina;

    private float regenerateStaminaTime = 0.1f;
    private float regeneratesAmount = 2;

    private float losingStaminaTime = 0.1f;

    private Coroutine myCoroutineLosing;
    private Coroutine myCoroutineRegenerate;


    void Start()
    {
        currentStamina = maxStamina;
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = maxStamina;
    }


    public void UseStamina(float amount)
    {
        if (currentStamina - amount > 0)
        {
            //por si ya hasy una corutina activa para perder stamina, la paras
            if (myCoroutineLosing != null)
            {
                StopCoroutine(myCoroutineLosing);
            }
            //detener regeneración si estaba activa
            if (myCoroutineRegenerate != null)
            {
                StopCoroutine(myCoroutineRegenerate);
                myCoroutineRegenerate = null;
            }
            //co-rutinas
            //iniciar corutina para perder stamina
            myCoroutineLosing = StartCoroutine(LosingStaminaCoroutine(amount));
        }
        else
        {
            Debug.Log("No hay stamina");
            FindObjectOfType<PlayerMovement>().isSprinting = false;
        }
    }

    public void StopSprinting()
    {
        if (myCoroutineLosing != null)
        {
            StopCoroutine(myCoroutineLosing);
            myCoroutineLosing = null;
        }
        //Iniciar regeneración cuando se detiene el sprint
        if (myCoroutineRegenerate != null)
        {
            StopCoroutine(myCoroutineRegenerate);
        }
        myCoroutineRegenerate = StartCoroutine(RegenerateStaminaCoroutineStart());
    }

    private IEnumerator RegenerateStaminaCoroutineStart()
    {
        //esperar 1 segundo antes de empezar a regenerar stamina
        yield return new WaitForSeconds(1);

        while (currentStamina < maxStamina)
        {
            //darle la stamina poco a poco
            currentStamina += regeneratesAmount;
            currentStamina = Mathf.Min(currentStamina, maxStamina); //No superar el máximo
            //ponerle el valor a la barra de stamina 
            staminaSlider.value = currentStamina;

            yield return new WaitForSeconds(regenerateStaminaTime);

        }
        myCoroutineRegenerate = null;

    }


    private IEnumerator LosingStaminaCoroutine(float amount)
    {
        while (currentStamina > 0)
        {
            //ir perdiendo stamina poco a poco
            currentStamina -= amount;
            currentStamina = Mathf.Max(currentStamina, 0); //Asegura que no baje de 0

            //actualizar barra de stamina
            staminaSlider.value = currentStamina;

            //esperar un tiempo para la proxima perdida de stamina
            yield return new WaitForSeconds(losingStaminaTime);
            //de esta manera perdemos stamina cada 0.1 segundos dependiendo 
        }

        myCoroutineLosing = null;
        //este script es el que maneja lo de correr accedemos el que tiene ese archivo para que deje de correr
        FindObjectOfType<PlayerMovement>().isSprinting = false;
        //Iniciar regeneración automática cuando se agota la stamina
        if (myCoroutineRegenerate != null)
        {
            StopCoroutine(myCoroutineRegenerate);
        }
        myCoroutineRegenerate = StartCoroutine(RegenerateStaminaCoroutineStart());
    }


}
