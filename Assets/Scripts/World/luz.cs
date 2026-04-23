using UnityEngine;

public class luz : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Light prender_luz;

    //void Start()
    //{
    //    prender_luz = GetComponent<Light>();
    //}
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (prender_luz != null)
            {
                prender_luz.enabled = true;
            }

        }

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (prender_luz != null)
            {
                prender_luz.enabled = false;
            }

        }

    }
}
