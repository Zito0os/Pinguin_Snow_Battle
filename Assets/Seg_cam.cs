using System.Collections;
using UnityEngine;

public class Seg_cam : MonoBehaviour
{

    //variables de la camara
    public GameObject tercera;
    public GameObject primera;
    public int modo = 0;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (modo == 1)
            {
                modo = 0;
            }
            else
            {
                modo = 1;
            }
        }
        StartCoroutine(cambio());
    }
    //corutina
    IEnumerator cambio()
    {
        yield return new WaitForSeconds(0.01f);
        if (modo == 0)
        {
            tercera.SetActive(true);
            primera.SetActive(false);
        }
        if (modo == 1)
        {
            tercera.SetActive(false);
            primera.SetActive(true);
        }
    }
}

