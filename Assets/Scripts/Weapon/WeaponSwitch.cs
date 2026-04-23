using Photon.Pun;
using UnityEngine;

public class WeaponSwitch : MonoBehaviourPunCallbacks
{
    public GameObject[] weapons;

    public int selectedWeapon = 0;



    void Start()
    {
        SelectWeapon();
        
        // Si NO es el jugador local, cambiar el layer de las armas para que la CameraWeapon no las renderice
        if (!photonView.IsMine)
        {
            CambiarLayerArmasRecursivamente(transform, 0); // Layer 0 = Default
            Debug.Log($"[WeaponSwitch] Armas del jugador remoto ViewID {photonView.ViewID} cambiadas a Layer Default (no visibles en CameraWeapon)");
        }
    }
    
    // Método para cambiar recursivamente el layer de un objeto y todos sus hijos
    private void CambiarLayerRecursivamente(Transform obj, int nuevoLayer)
    {
        obj.gameObject.layer = nuevoLayer;
        foreach (Transform hijo in obj)
        {
            CambiarLayerRecursivamente(hijo, nuevoLayer);
        }
    }
    
    // Método específico para cambiar solo las armas
    private void CambiarLayerArmasRecursivamente(Transform contenedor, int nuevoLayer)
    {
        foreach (Transform arma in contenedor)
        {
            CambiarLayerRecursivamente(arma, nuevoLayer);
        }
    }


    void Update()
    {
        if (photonView.IsMine)
        {
            int previousWeapon = selectedWeapon;

            //para que se pueda con la rueda del raton seleccionar el arma 
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                if (selectedWeapon >= weapons.Length - 1)
                {
                    selectedWeapon = 0;
                }
                else
                {
                    selectedWeapon++;
                }
            }

            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                if (selectedWeapon <= 0)
                {
                    selectedWeapon = weapons.Length - 1;
                }
                else
                {
                    selectedWeapon--;
                }
            }

            //mover con los numeros del 1 al 9

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                selectedWeapon = 0;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && weapons.Length >= 2)
            {
                selectedWeapon = 1;
            }


            //SI TIENES MAS ARMAS PONES MAS DE ESTO







            //para evitar si tenemos el arma llame al mismo metodo
            if (previousWeapon != selectedWeapon)
            {
                SelectWeapon();
            }
        }

        
    }

    void SelectWeapon()
    {
        int i = 0;
        foreach (Transform weapon in transform)
        {
            if (weapon.gameObject.layer == LayerMask.NameToLayer("Weapon"))
            {
                if (i == selectedWeapon)
                {
                    weapon.gameObject.SetActive(true);
                }
                else
                {
                    weapon.gameObject.SetActive(false);
                }

                i++;

                
            }


            
        }
    }
}
