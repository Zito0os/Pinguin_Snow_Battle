using UnityEngine;

public class cambiar_color : MonoBehaviour
{

    //-----TAREA DE COLOR -----
    public Material[] materials;
    Renderer render;

    void Start()
    {
        render = GetComponent<Renderer>();
        render.enabled = true;
        render.sharedMaterial = materials[0];
    }
    public void cambiar()
    {
        render.sharedMaterial = materials[1];
    }
}
