using UnityEngine;

public class Movimiento : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //variables para el movimiento y salto
    public float Velocidad = 5f;
    public float fuerzaSalto = 5f;
    private Rigidbody RB;
    private Vector3 Movi;

    void Start()
    {
        RB = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        ProcesarMovimiento();
        ProcesarSalto();
    }
    private void FixedUpdate()
    {
        RB.linearVelocity = new Vector3(Movi.x * Velocidad, RB.linearVelocity.y, Movi.z * Velocidad);
    }
    void ProcesarMovimiento()
    {
        float MoverX = Input.GetAxisRaw("Horizontal"); // A/D
        float MoverZ = Input.GetAxisRaw("Vertical");   // W/S
        Movi = new Vector3(MoverX, 0f, MoverZ).normalized;
    }
    void ProcesarSalto()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RB.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
        }
    }
}
