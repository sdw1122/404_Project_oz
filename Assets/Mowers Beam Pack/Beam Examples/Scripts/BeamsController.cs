using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class BeamsController : MonoBehaviour
{
    public VisualEffect waterBeam;
    public VisualEffect fantasyBeam;
    public VisualEffect fireBeam;
    public VisualEffect electricityBeam;
    public ParticleSystem shiled;

    Quaternion defaultRot;
    private bool isBeam = false;
    void Update()
    {
        //Water Beam Controller
        if (Input.GetKeyDown(KeyCode.E))
        {
            waterBeam.Play();
            shiled.Play();
        }
        //Fantasy Beam Controller
        if (Input.GetKeyDown(KeyCode.F))
        {
            defaultRot = fantasyBeam.transform.rotation;
            fantasyBeam.Play();
            isBeam = true;
        }
        //Fire Beam Controller
        if (Input.GetKeyDown(KeyCode.G))
        {
            fireBeam.Play();
            
        }
        //Electricity Beam Controller
        if (Input.GetKeyDown(KeyCode.C))
        {
            isBeam = false;
            fantasyBeam.transform.rotation = defaultRot;
        }
        if (isBeam)
        {
            fantasyBeam.transform.rotation = Quaternion.RotateTowards(fantasyBeam.transform.rotation, defaultRot * Quaternion.Euler(0f, -40f, 0f), 14 * Time.deltaTime);
            if(fantasyBeam.transform.rotation == defaultRot * Quaternion.Euler(0f, -40f, 0f))
            {
                isBeam = false;
                fantasyBeam.transform.rotation = defaultRot;
            }
        }
    }
}
