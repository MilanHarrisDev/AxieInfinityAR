﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class AxieManager : MonoBehaviour
{
    public static AxieManager Manager;
    public string axieId;

    private void Awake()
    {
        if(Manager == null)
            Manager = this;
        else
        {
            Destroy(Manager.gameObject);
            Manager = this;
        }
    }

    private void Start()
    {
    }


    public void ToggleTracking()
    {
        FindObjectOfType<ARSession>().enabled = FindObjectOfType<ARSession>().enabled;
    }

    [SerializeField]
    private List<Axie> axies = new List<Axie>();

    public void CreateAxie(string id)
    {
        Axie newAxie = new Axie(id);
        axies.Add(newAxie);
        GetAxieInformation.Instance.StartAxieCreate(id);
    }

    public Axie GetAxie(string id)
    {
        if (!AxieExists(id))
            return null;
        else
        {
            foreach (Axie axie in axies)
            {
                if (axie.id == id)
                    return axie;
            }
        }

        return null;
    }

    public bool AxieExists(string id)
    {
        foreach (Axie axie in axies)
        {
            if (axie.id == id)
                return true;
        }
        return false;
    }
   
}
