using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WeaponSway : MonoBehaviour
{

    private Quaternion startRotation;//la rotacion inicial del arma 

    public float swayAmount = 8f;


    void Start()
    {
        startRotation = transform.localRotation;
    }


    void Update()
    {
        Sway();
    }


    private void Sway()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Quaternion xAngle = Quaternion.AngleAxis(mouseX * -1.25f, Vector3.up);
        Quaternion yAngle = Quaternion.AngleAxis(mouseY * -1.25f, Vector3.right); 


        Quaternion targetRotation = startRotation * xAngle * yAngle;

        //target rotation a dponde queremos llegar donde giramos el mouse 
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * swayAmount);

    }
}
