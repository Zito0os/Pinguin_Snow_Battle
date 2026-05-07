using UnityEngine;

public class EmotePanel : MonoBehaviour
{
    public GameObject emotePanel;
    //public Animator playerAnimator;

    private int emoteActual = -1;
    private int ultimoEmote = -1;
    private int cantidadEmotes = 5;

    // Variable estática para que otros scripts sepan si el panel está abierto
    public static bool isEmotePanelActive = false;


    public Animator animator; // Asignar el Animator del jugador en el Inspector
    [Header("Highlights (5)")]
    public GameObject[] highlights; // 5 objetos

    private bool play_emote;



    void Update()
    {
        if (Input.GetKey(KeyCode.H))
        {
            emotePanel.SetActive(true);
            isEmotePanelActive = true;
            Cursor.lockState = CursorLockMode.None; // Desbloquear cursor para que detecte la posición del mouse
            DetectarEmote();
        }

        if (Input.GetKeyUp(KeyCode.H))
        {
            emotePanel.SetActive(false);
            isEmotePanelActive = false;
            Cursor.lockState = CursorLockMode.Locked; // Volver a bloquear el cursor
            LimpiarHighlights();
            ReproducirEmote();
        }


    }

    void DetectarEmote()
    {
        Vector2 centroPantalla = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 posicionMouse = (Vector2)Input.mousePosition;
        Vector2 direccion = posicionMouse - centroPantalla;

        // Si el mouse está muy cerca del centro, no seleccionar nada
        if (direccion.magnitude < 50f)
        {
            emoteActual = -1;
            return;
        }

        // Invertir Y porque las coordenadas de pantalla están invertidas
        float angulo = Mathf.Atan2(-direccion.y, direccion.x) * Mathf.Rad2Deg;

        if (angulo < 0)
            angulo += 360;

        // Invertir la dirección de los ángulos completamente
        angulo = (360 - angulo) % 360;

        // Mapear ángulo a sectores para 5 emotes:
        // Sector 0: Arriba - rango 18-90°
        // Sector 1: Derecha - rango 305-17° (cruza 0°)
        // Sector 2: Abajo-Derecha - rango 233-305°
        // Sector 3: Abajo-Izquierda - rango 161-233°
        // Sector 4: Izquierda - rango 89-161°
        
        if (angulo >= 18 && angulo < 90)
            emoteActual = 0;
        else if (angulo >= 305 || angulo < 17)
            emoteActual = 1;
        else if (angulo >= 233 && angulo < 305)
            emoteActual = 2;
        else if (angulo >= 161 && angulo < 233)
            emoteActual = 3;
        else if (angulo >= 89 && angulo < 161)
            emoteActual = 4;
        else
            emoteActual = -1;

        //  SOLO actualiza si cambia
        if (emoteActual != ultimoEmote)
        {
            Debug.Log("ANGULO: " + angulo.ToString("F2") + " | SECTOR: " + emoteActual);
            ActualizarHighlights();
            ultimoEmote = emoteActual;
        }
    }

    void ActualizarHighlights()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].SetActive(i == emoteActual);
        }
    }

    void LimpiarHighlights()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].SetActive(false);
        }

        ultimoEmote = -1;
    }


    void ReproducirEmote()
    {
        if (emoteActual == -1) return;

        animator.SetFloat("EmoteIndex 0", emoteActual);
        animator.SetTrigger("Play_Emote");
    }
}
