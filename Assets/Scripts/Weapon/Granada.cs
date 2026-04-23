using UnityEngine;

public class Granada : MonoBehaviour
{

    public float delay = 3f;

    float countdown;

    public float radius = 5f;

    public float explotionforce = 70f;

    bool exploted = false;

    public GameObject explotionEffect;



    private AudioSource audioSource;

    public AudioClip explotionSound;


    public float damage = 50f;


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        countdown = delay;
    }



    void Update()
    {
        countdown -= Time.deltaTime;




        if (countdown <= 0f && !exploted)
        {

            Explode();
            exploted = true;
        }

    }



    void Explode()
    {
        Instantiate(explotionEffect, transform.position, transform.rotation);

        //generar coliders para colisionar con todos los rigibody
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (var rangeObjects in colliders)
        {
            //Aplicar fuerza de explocion
            AI ai = rangeObjects.GetComponent<AI>();

            if (ai != null)
            {
                ai.GrenadeImpact(damage);
            }
            
            Rigidbody rb = rangeObjects.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explotionforce*10,transform.position,radius);
            }
        }

        audioSource.PlayOneShot(explotionSound);

        gameObject.GetComponent<CapsuleCollider>().enabled = false;
        //gameObject.GetComponent<MeshRenderer>().enabled = false;


        //destruir granada
        Destroy(gameObject,delay * 2);
        
    }
}
