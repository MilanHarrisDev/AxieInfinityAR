using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class AxieObject : MonoBehaviour
{
    public static AxieObject Instance;

    public GameObject axieUI;
    public InputField axieInput;
    public Text errorText;

    private void Start()
    {
        Instance = this;
    }

    public void StartCreateAxie()
    {
        AxieManager.Manager.CreateAxie(axieInput.text);
    }

    public void Update()
    {
        transform.LookAt(Camera.main.transform.position, Vector3.up);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    public void CreationErrorReturn(bool isError)
    {
        if (isError)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = "invalid Axie ID";
        }
        else
        {
            axieUI.SetActive(false);
        }
    }
}
