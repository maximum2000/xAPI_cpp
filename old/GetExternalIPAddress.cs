using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GetExternalIPAddress : MonoBehaviour
{
    public string externalip = "";
    public string geolocation = "";
    public string city = "";

    void Start()
    {
        StartCoroutine(GetIPAddress());
    }

    //Taken from Goodgulf
    IEnumerator GetIPAddress()
    {
        UnityWebRequest www = UnityWebRequest.Get("https://ipinfo.io/ip");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            string result = www.downloadHandler.text;
            //Debug.Log("External IP Address = " + a4);
            externalip = result;
            if (externalip != "")
            {
                StartCoroutine(GetGeo());
            }
        }
    }

    IEnumerator GetGeo()
    {
        UnityWebRequest www = UnityWebRequest.Get("https://ipinfo.io/"+ externalip + "?token=47359222585d92");
        yield return www.SendWebRequest();

        geolocation = "";

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            string result = www.downloadHandler.text;
            SimpleJSON.JSONNode rootNode = SimpleJSON.JSON.Parse(result);
            if (rootNode["loc"].IsString == true)
            {
                geolocation = rootNode["loc"];
            }
            if (rootNode["city"].IsString == true)
            {
                city = rootNode["city"];
            }

        }

        /*
        {
            "ip": "85.140.13.200",
            "hostname": "200.mtsnet.ru",
            "city": "Yekaterinburg",
            "region": "Sverdlovsk",
            "country": "RU",
            "loc": "56.8519,60.6122",
            "org": "AS8359 MTS PJSC",
            "postal": "620000",
            "timezone": "Asia/Yekaterinburg"
        }
        */

    }
}