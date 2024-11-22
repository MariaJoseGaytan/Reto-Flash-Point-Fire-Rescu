using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ServerManager : MonoBehaviour
{
    private string url = "http://127.0.0.1:5000/state"; // URL del servidor tablero

    public IEnumerator GetGameState(System.Action<string> callback)
{
    UnityWebRequest www = UnityWebRequest.Get("http://127.0.0.1:5000/state");
    yield return www.SendWebRequest();

    if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
    {
        Debug.LogError($"Error al conectar al servidor: {www.error}");
    }
    else
    {
        Debug.Log($"Datos JSON recibidos: {www.downloadHandler.text}");
        callback(www.downloadHandler.text);
    }
}

}
