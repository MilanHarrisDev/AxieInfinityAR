using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Data;

[System.Serializable]
public class Axie
{
    public string id;
    public Texture2D axieTexture;
    public string[] atlas;
    public List<AxiePart> parts = new List<AxiePart>();
    public List<AxieBone> bones;

    public Axie(string id)
    {
        this.id = id;
    }

    public Sprite GetPartSprite(string partName)
    {
        foreach(AxiePart part in parts)
        {
            if (part.name == partName)
                return part.sprite;
        }

        return null;
    }
}

[System.Serializable]
public class AxiePart{
    public string name = "";
    public Sprite sprite;

    public AxiePart(string name, Sprite sprite)
    {
        this.name = name;
        this.sprite = sprite;
    }
}

[System.Serializable]
public class AxieBone
{
    public string name;
    public string parent;
    public float rotation;
    public float x;
    public float y;
    public float length;
}

public class AxieSkin{
    public float rotation;
    public float x;
    public float y;
    public float width;
    public float height;
}

public class GetAxieInformation : MonoBehaviour
{
    public static GetAxieInformation Instance;

    private string currentId;

    private float scaleFactor = 100f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    public void StartAxieCreate(string id)
    {
        currentId = id;
        StartCoroutine(GetAxieImageUrl());

        Debug.LogFormat("Started Creating Axie \"{0}\"", currentId);

    }

    private IEnumerator GetAxieImageUrl(string type = "axie"){
        UnityWebRequest wwwImage = UnityWebRequest.Get("https://us-central1-axieinfinityar.cloudfunctions.net/getAxieImage?axieId=" + currentId + "&type=" + type);
        yield return wwwImage.SendWebRequest();

        if (wwwImage.isNetworkError || wwwImage.isHttpError)
            Debug.Log(wwwImage.error);
        else
        {
            Debug.LogFormat("downloading axie image from {0}", wwwImage.downloadHandler.text);
            StartCoroutine(DownloadAxieImage(wwwImage.downloadHandler.text));
        }
    }

    private IEnumerator DownloadAxieImage(string url)
    {
        UnityWebRequest wwwTexture = UnityWebRequestTexture.GetTexture(url);
        
        // Wait for download to complete
        yield return wwwTexture.SendWebRequest();

        AxieManager.Manager.GetAxie(currentId).axieTexture = DownloadHandlerTexture.GetContent(wwwTexture);

        Debug.Log("Axie image downloaded");

        byte[] bytes = AxieManager.Manager.GetAxie(currentId).axieTexture.EncodeToPNG();
        //write to project
        File.WriteAllBytes(Application.dataPath + "/axieImages/axieSpriteSheet" + currentId + ".png", bytes);
        Debug.LogFormat("{0}/axieImages/axieSpriteSheet{1}.png has been written", Application.dataPath, currentId);

        StartCoroutine(GetAxieImageAtlasUrl(currentId));
    }

    private IEnumerator GetAxieImageAtlasUrl(string type = "axie")
    {
        UnityWebRequest wwwAtlasUrl = UnityWebRequest.Get("https://us-central1-axieinfinityar.cloudfunctions.net/getAxieImageAtlas?axieId=" + currentId + " &type=" + type);
        yield return wwwAtlasUrl.SendWebRequest();

        if (wwwAtlasUrl.isNetworkError || wwwAtlasUrl.isHttpError)
            Debug.Log(wwwAtlasUrl.error);
        else
        {
            StartCoroutine(DownloadAxieImageAtlas(wwwAtlasUrl.downloadHandler.text));
        }
    }

    private IEnumerator DownloadAxieImageAtlas(string url)
    {
        UnityWebRequest wwwAtlas = UnityWebRequest.Get(url);

        yield return wwwAtlas.SendWebRequest();

        string path = Application.dataPath + "/axieImages/axieAtlas" + currentId + ".txt";

        File.WriteAllBytes(path, wwwAtlas.downloadHandler.data);
        AxieManager.Manager.GetAxie(currentId).atlas = File.ReadAllLines(path);

        ProcessAtlas(path, currentId);
    }

    private void ProcessAtlas(string path, string currentId)
    {
        List<string> atlasTemp = new List<string>();

        //remove unwanted lines
        for (int i = 0; i < AxieManager.Manager.GetAxie(currentId).atlas.Length - 1; i++)
        {
            if (i < 6)
                continue;

            atlasTemp.Add(AxieManager.Manager.GetAxie(currentId).atlas[i]);
        }

        //write file with changes
        AxieManager.Manager.GetAxie(currentId).atlas = atlasTemp.ToArray();
        File.WriteAllLines(path, AxieManager.Manager.GetAxie(currentId).atlas);

        string currentPartName = "";
        Rect currentPartRect = new Rect(0,0,0,0);
        //Loop through each part and create sprite
        for(int i = 0; i < atlasTemp.Count; i++)
        {
            if (string.IsNullOrEmpty(atlasTemp[i]))
                continue;

            if(atlasTemp[i][0] == ' ')//if first character in line is a space
            {
                string line = atlasTemp[i].Replace(" ", "");
                if (line.Contains("xy"))
                {
                    string[] xyValues = line.Split(':')[1].Split(','); //Gets string array where value at 0 is x pos and value at 1 is y pos
                    currentPartRect.position = new Vector2(int.Parse(xyValues[0]), int.Parse(xyValues[1])); //set rect position
                    Debug.LogFormat("position for {0} = ({1},{2})", currentPartName, xyValues[0], xyValues[1]);
                }
                else if (line.Contains("size"))
                {
                    string[] xyValues = line.Split(':')[1].Split(','); //Gets string array where value at 0 is x size and value at 1 is y size
                    currentPartRect.size = new Vector2(int.Parse(xyValues[0]), int.Parse(xyValues[1])); //set rect size
                    Debug.LogFormat("size for {0} = ({1},{2})", currentPartName, xyValues[0], xyValues[1]);
                }
            }
            else
            {
                if (currentPartRect.size.magnitude != 0)
                    AxieManager.Manager.GetAxie(currentId).parts.Add(CreatePart(AxieManager.Manager.GetAxie(currentId).axieTexture, currentPartRect, currentPartName));
                currentPartName = atlasTemp[i];
                currentPartRect = new Rect();
            }
        }
        AxieManager.Manager.GetAxie(currentId).parts.Add(CreatePart(AxieManager.Manager.GetAxie(currentId).axieTexture, currentPartRect, currentPartName)); //create last sprite

        StartCoroutine(GetAxieSpine("bones"));
    }

    private AxiePart CreatePart(Texture2D texture, Rect rect, string partName)
    {
        //Do operations to fix position
        Rect finalRect = new Rect();
        finalRect.size = rect.size;
        finalRect.position = new Vector2(rect.position.x, texture.height - rect.position.y - rect.size.y);

        Sprite newSprite = Sprite.Create(texture, finalRect, new Vector2(0.5f, 0.5f), scaleFactor);
        Debug.LogFormat("{0} sprite created", partName);

        return new AxiePart(partName, newSprite);
    }

    private IEnumerator GetAxieSpine(string type)
    {
        UnityWebRequest wwwBones = UnityWebRequest.Get("https://us-central1-axieinfinityar.cloudfunctions.net/getAxieSpineModel?axieId=" + currentId + "&type=" + type);
        yield return wwwBones.SendWebRequest();

        AxieManager.Manager.GetAxie(currentId).bones = JsonConvert.DeserializeObject<List<AxieBone>>(wwwBones.downloadHandler.text);
        ConstructAxie();
    }   

    private void ConstructAxie()
    {
        Axie axie = AxieManager.Manager.GetAxie(currentId);

        if (axie == null)
        {
            Debug.LogFormat("Could not construct axie, Id: \"{0}\" was not found", currentId);
            return;
        }

        GameObject newAxieObj = new GameObject("axie" + currentId);

        foreach (AxieBone bone in axie.bones)
        {
            string boneName = bone.name.Replace("@", "");
            GameObject newBone = new GameObject("bone-" + boneName);
            if (newBone.name == "root")
                newBone.transform.parent = newAxieObj.transform;
            else
            {
                newBone.transform.parent = newAxieObj.transform.FindDeepChild(bone.parent.Replace("@", ""));
                SpriteRenderer sr = newBone.AddComponent<SpriteRenderer>();
                sr.sprite = axie.GetPartSprite(boneName);

                newBone.transform.position = new Vector2(bone.x/scaleFactor, bone.y/ scaleFactor);


                if (bone.rotation != 0) {
                    float parentRot = 0;
                    if (transform.parent)
                        parentRot = transform.parent.rotation.eulerAngles.z;

                    newBone.transform.Rotate(Vector3.forward, (bone.rotation - 180f) - parentRot);
                }
            }
        }
    }
}
