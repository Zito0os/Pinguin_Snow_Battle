using UnityEngine;

public class EmotePanel : MonoBehaviour
{
    public GameObject emotePanel;
    //public Animator playerAnimator;

    private int emoteActual = -1;
    private int ultimoEmote = -1;
    private int cantidadEmotes = 8;

    // Variable estática para que otros scripts sepan si el panel está abierto
    public static bool isEmotePanelActive = false;



    [Header("Highlights (8)")]
    public GameObject[] highlights; // 8 objetos



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

        // Mapear ángulo a sectores basado en los rangos especificados:
        // Sector 0: 45-90°   | Sector 1: 0-45°    | Sector 2: 315-360°
        // Sector 3: 270-315° | Sector 4: 225-270° | Sector 5: 180-225°
        // Sector 6: 135-180° | Sector 7: 90-135°
        
        if (angulo >= 45 && angulo < 90)
            emoteActual = 0;
        else if (angulo >= 0 && angulo < 45)
            emoteActual = 1;
        else if (angulo >= 315)
            emoteActual = 2;
        else if (angulo >= 270 && angulo < 315)
            emoteActual = 3;
        else if (angulo >= 225 && angulo < 270)
            emoteActual = 4;
        else if (angulo >= 180 && angulo < 225)
            emoteActual = 5;
        else if (angulo >= 135 && angulo < 180)
            emoteActual = 6;
        else  // 90-135
            emoteActual = 7;

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

        //playerAnimator.SetInteger("EmoteIndex", emoteActual);
    }
}
