using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class StepServer : MonoBehaviour
{
    public string serverUrl = "http://127.0.0.1:5001/"; // URL del servidor
    public StepData stepData; // Referencia a la clase que manejará los datos

    void Start()
    {
        StartCoroutine(GetStepDataFromServer());
    }

    IEnumerator GetStepDataFromServer()
    {
        // Realiza la solicitud al servidor
        UnityWebRequest request = UnityWebRequest.Get(serverUrl);

        yield return request.SendWebRequest();

        // Verifica si hubo un error
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error connecting to server: {request.error}");
        }
        else
        {
            // Obtiene los datos JSON como string
            string json = request.downloadHandler.text;

            // Llama a la clase StepData para manejar los datos
            stepData.ProcessStepData(json);
        }
    }
}
