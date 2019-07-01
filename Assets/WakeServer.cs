using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WakeServer : MonoBehaviour {
    private string url;
    void Start() {
        url = Interface.Release ? "https://red-doright-23845.herokuapp.com" : "https://google.com";
        StartCoroutine(GetRequest(url));
    }

    IEnumerator GetRequest(string uri) {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri)) {
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError) {
                Debug.Log("Error");
            }
            else {
                Debug.Log("Connected to " + url);
            }
        }
    }
}
