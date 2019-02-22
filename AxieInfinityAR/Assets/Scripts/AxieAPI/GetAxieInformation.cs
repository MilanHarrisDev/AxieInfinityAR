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
    public List<AxieSkin> skins;

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
    public bool rotate;
    public Sprite sprite;

    public AxiePart(string name, Sprite sprite, bool rotate)
    {
        this.name = name;
        this.sprite = sprite;
        this.rotate = rotate;
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

[System.Serializable]
public class AxieSkin 
{
    public string name;
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
        bool rotate = false;

        //Loop through each part and create sprite
        for (int i = 0; i < atlasTemp.Count; i++)
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
                    //Debug.LogFormat("position for {0} = ({1},{2})", currentPartName, xyValues[0], xyValues[1]);
                }
                else if (line.Contains("size"))
                {
                    string[] xyValues = line.Split(':')[1].Split(','); //Gets string array where value at 0 is x size and value at 1 is y size
                    currentPartRect.size = new Vector2(int.Parse(xyValues[0]), int.Parse(xyValues[1])); //set rect size
                    //Debug.LogFormat("size for {0} = ({1},{2})", currentPartName, xyValues[0], xyValues[1]);
                }
                else if (line.Contains("rotate"))
                {
                    rotate = line.Contains("true") || rotate;
                    Debug.LogFormat("<color=yellow>rotate = {0}</color>", rotate);
                }
            }
            else
            {
                if (currentPartRect.size.magnitude != 0)
                {
                    if(rotate)
                    {
                        Debug.Log("<color=green>Before Rotation</color>:");
                        Debug.LogFormat("size for {0} = ({1},{2})", currentPartName, currentPartRect.size.x, currentPartRect.size.y);

                        Vector2 newSize = new Vector2(currentPartRect.size.y, currentPartRect.size.x);
                        currentPartRect.size = newSize;
                        Debug.Log("<color=red>After Rotation</color>:");

                    }

                    AxieManager.Manager.GetAxie(currentId).parts.Add(CreatePart(AxieManager.Manager.GetAxie(currentId).axieTexture, currentPartRect, currentPartName, rotate));

                    Debug.LogFormat("size for {0} = ({1},{2})", currentPartName, currentPartRect.size.x, currentPartRect.size.y);
                    Debug.LogFormat("pos for {0} = ({1},{2})", currentPartName, currentPartRect.position.x, currentPartRect.position.y);
                }

                currentPartName = atlasTemp[i];
                currentPartRect = new Rect();
                rotate = false;
            }
        }

        AxieManager.Manager.GetAxie(currentId).parts.Add(CreatePart(AxieManager.Manager.GetAxie(currentId).axieTexture, currentPartRect, currentPartName, rotate)); //create last sprite

        StartCoroutine(GetAxieBoneSpine());
    }

    private AxiePart CreatePart(Texture2D texture, Rect rect, string partName, bool rotate = false)
    {
        //Do operations to fix position
        Rect finalRect = new Rect();
        finalRect.size = rect.size;
        finalRect.position = new Vector2(rect.position.x, texture.height - rect.position.y - rect.size.y);

        Sprite newSprite = Sprite.Create(texture, finalRect, new Vector2(0.5f, 0.5f), scaleFactor);
        Debug.LogFormat("{0} sprite created", partName);

        return new AxiePart(partName, newSprite, rotate);
    }

    private IEnumerator GetAxieBoneSpine()
    {
        UnityWebRequest wwwBones = UnityWebRequest.Get("https://us-central1-axieinfinityar.cloudfunctions.net/getAxieSpineModel?axieId=" + currentId + "&type=bones");
        yield return wwwBones.SendWebRequest();

        AxieManager.Manager.GetAxie(currentId).bones = JsonConvert.DeserializeObject<List<AxieBone>>(wwwBones.downloadHandler.text);
        StartCoroutine(GetAxieSkinSpine());
    }

    private IEnumerator GetAxieSkinSpine()
    {
        UnityWebRequest wwwSkins = UnityWebRequest.Get("https://us-central1-axieinfinityar.cloudfunctions.net/getAxieSpineModel?axieId=" + currentId + "&type=skins");
        yield return wwwSkins.SendWebRequest();

        AxieManager.Manager.GetAxie(currentId).skins = JsonConvert.DeserializeObject<List<AxieSkin>>(wwwSkins.downloadHandler.text);
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
            if (newBone.name == "bone-root")
                newBone.transform.parent = newAxieObj.transform;
            else
            {
                newBone.transform.parent = newAxieObj.transform.FindDeepChild(bone.parent.Replace("@", "bone-"));
                newBone.transform.localPosition = new Vector2(bone.x/scaleFactor, bone.y/ scaleFactor);

                if (bone.rotation != 0) {
                    float parentRot = 0;
                    if (transform.parent)
                        parentRot = transform.parent.rotation.eulerAngles.z;

                    newBone.transform.Rotate(Vector3.forward, bone.rotation - parentRot /*(bone.rotation - 180f) - parentRot*/);
                }
            }
        }

        foreach(AxieSkin skin in axie.skins)
        {
            string skinName = skin.name;
            GameObject newSkin = new GameObject("skin-" + skinName);

            newSkin.transform.parent = newAxieObj.transform.FindDeepChild("bone-" + skin.name);


            bool partRotate = false;

            foreach (AxiePart part in axie.parts)
            {
                if (part.name.Equals(skinName))
                    partRotate = part.rotate;
            }


            Vector2 newPos = new Vector2(skin.x / scaleFactor, skin.y / scaleFactor);

            Transform newTransform = newSkin.transform.parent.Find("bone-" + skin.name);
            if (newTransform)
            {
                newPos = partRotate ? new Vector2(skin.y / scaleFactor, skin.x / scaleFactor) :
                    newPos;
                newSkin.transform.parent = newTransform;
            }

            if (newTransform)
                newPos = new Vector2(-newPos.x, -newPos.y);

            if (skinName.Contains("tail"))
                newPos = new Vector2(-newPos.x, -newPos.y);

            newSkin.transform.localPosition = newPos;

            if (partRotate)
                newSkin.transform.Rotate(Vector3.forward, -90);

            //partRotate = false;

           
            /*if (skin.rotation != 0f)
            {
                float parentRot = 0;
                if (transform.parent)
                    parentRot = transform.parent.rotation.eulerAngles.z;
                //newSkin.transform.Rotate(Vector3.forward, skin.rotation (skin.rotation - 180f) - parentRot);
            }
            */

            /*
             * if (skin.name.Contains("ear") || skin.name.Contains("mouth"))
                newSkin.transform.Rotate(Vector3.forward, -90);
            */

            SpriteRenderer sr = newSkin.AddComponent<SpriteRenderer>();
            sr.sprite = axie.GetPartSprite(skinName);

        }
    }
}
