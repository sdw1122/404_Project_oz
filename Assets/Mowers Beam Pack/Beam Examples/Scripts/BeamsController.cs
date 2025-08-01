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
    void Update()
    {
        //Water Beam Controller
        if (Input.GetKeyDown(KeyCode.E))
        {
            waterBeam.Play();
        }
        //Fantasy Beam Controller
        if (Input.GetKeyDown(KeyCode.F))
        {
            fantasyBeam.Play();
        }
        //Fire Beam Controller
        if (Input.GetKeyDown(KeyCode.G))
        {
            fireBeam.Play();
        }
        //Electricity Beam Controller
        if (Input.GetKeyDown(KeyCode.C))
        {
            electricityBeam.Play();
        }
    }
}
