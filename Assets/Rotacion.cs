using UnityEngine;

public class Rotacion : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    [SerializeField] private float velocidad = 50f; // Velocidad en grados por segundo

    void Update()
    {
        // Rota sobre el eje Y (horizontal)
        transform.Rotate(0f, velocidad * Time.deltaTime, 0f, Space.World);
    }


}
