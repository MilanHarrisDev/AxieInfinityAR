using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AxieInfo{
    string owner;
    int id;
    string name;
    string genes;
    string birthDate;
    int sireId;
    int matronId;
    int stage;
}

public class GetAxieInformation : MonoBehaviour
{
    UnityWebRequest www;

    private IEnumerator TryGetAxieInfo(){
        www = UnityWebRequest.Get("https://api.axieinfinity.com/v1/axies/15143");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Show results as text
            Debug.Log(www.downloadHandler.text);


        }
    }
}
