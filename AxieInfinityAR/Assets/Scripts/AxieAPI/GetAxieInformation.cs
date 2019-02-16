using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

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

public class GetAxieInformation : MonoBehaviour
{
    [SerializeField]
    private string axieId;

    [SerializeField]
    private Renderer r;

    private Texture2D axieTexture;
    private string[] atlas;

    [SerializeField]
    private List<AxiePart> parts = new List<AxiePart>();

    private void Start()
    {
        StartCoroutine(GetAxieImageUrl(axieId));
    }

    private IEnumerator GetAxieImageUrl(string axieId, string type = "axie"){
        UnityWebRequest wwwImage = UnityWebRequest.Get("https://us-central1-axieinfinityar.cloudfunctions.net/getAxieImage?axieId=" + axieId + "&type=" + type);
        yield return wwwImage.SendWebRequest();

        if (wwwImage.isNetworkError || wwwImage.isHttpError)
            Debug.Log(wwwImage.error);
        else
        {
            Debug.LogFormat("downloading axie image from {0}", wwwImage.downloadHandler.text);
            StartCoroutine(DownloadAxieImage(wwwImage.downloadHandler.text, axieId));
        }
    }

    private IEnumerator DownloadAxieImage(string url, string axieId)
    {
        UnityWebRequest wwwTexture = UnityWebRequestTexture.GetTexture(url);
        
        // Wait for download to complete
        yield return wwwTexture.SendWebRequest();

        axieTexture = DownloadHandlerTexture.GetContent(wwwTexture);

        Debug.Log("Axie image downloaded");

        r.material.SetTexture("_MainTex", axieTexture);

        byte[] bytes = axieTexture.EncodeToPNG();
        //write to project
        File.WriteAllBytes(Application.dataPath + "/axieImages/axieSpriteSheet" + axieId + ".png", bytes);
        Debug.LogFormat("{0}/axieImages/axieSpriteSheet{1}.png has been written", Application.dataPath, axieId);

        StartCoroutine(GetAxieImageAtlasUrl(axieId));
    }

    private IEnumerator GetAxieImageAtlasUrl(string axieId, string type = "axie")
    {
        UnityWebRequest wwwAtlasUrl = UnityWebRequest.Get("https://us-central1-axieinfinityar.cloudfunctions.net/getAxieImageAtlas?axieId=" + axieId + " &type=" + type);
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

        string path = Application.dataPath + "/axieImages/axieAtlas" + axieId + ".txt";

        File.WriteAllBytes(path, wwwAtlas.downloadHandler.data);
        atlas = File.ReadAllLines(path);

        ProcessAtlas(path);
    }

    private void ProcessAtlas(string path)
    {
        List<string> atlasTemp = new List<string>();

        //remove unwanted lines
        for (int i = 0; i < atlas.Length - 1; i++)
        {
            if (i < 6)
                continue;

            atlasTemp.Add(atlas[i]);
        }

        //write file with changes
        atlas = atlasTemp.ToArray();
        File.WriteAllLines(path, atlas);

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
                    CreateSprite(axieTexture, currentPartRect, currentPartName);
                currentPartName = atlasTemp[i];
                currentPartRect = new Rect();
            }
        }
        CreateSprite(axieTexture, currentPartRect, currentPartName); //create last sprite
    }

    private void CreateSprite(Texture2D texture, Rect rect, string partName)
    {
        //Do operations to fix position
        Rect finalRect = new Rect();
        finalRect.size = rect.size;
        finalRect.position = new Vector2(rect.position.x, texture.height - rect.position.y - rect.size.y);

        Sprite newSprite = Sprite.Create(texture, finalRect, new Vector2(0f, 0f), 500);
        parts.Add(new AxiePart(partName, newSprite));
        Debug.LogFormat("{0} sprite created", partName);
    }

    private void ConstructAxie()
    {

    }
}
