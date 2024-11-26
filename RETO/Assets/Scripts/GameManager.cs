using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{
    public ServerManager serverManager;
    public GameObject cellPrefab;  
    public Transform boardParent;
    private float cellSize = 19.96322f;

    void Start()
    {
        StartCoroutine(serverManager.GetGameState(OnGameStateReceived));
    }

    void OnGameStateReceived(string json)
    {
        Debug.Log($"JSON recibido en GameManager: {json}");

        // Transformar el JSON para manejar los valores de `pos` y `door`
        var transformedJson = TransformJson(json);

        // Deserializar el JSON transformado
        Dictionary<string, CellState> gameState = JsonConvert.DeserializeObject<Dictionary<string, CellState>>(transformedJson);

        if (gameState != null && gameState.Count > 0)
        {
            Debug.Log($"Número de celdas procesadas: {gameState.Count}");
            BuildBoard(gameState); // Construir el tablero
        }
        else
        {
            Debug.LogError("El JSON no se pudo deserializar o está vacío.");
        }
    }

    string TransformJson(string json)
    {
        var parsedJson = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);

        foreach (var cell in parsedJson.Values)
        {
            // Convertir `pos` de cadena a lista de enteros
            if (cell.ContainsKey("pos") && cell["pos"] is string posString)
            {
                var coordinates = posString.Trim('(', ')').Split(',');
                if (coordinates.Length == 2 && 
                    int.TryParse(coordinates[0].Trim(), out int posX) && 
                    int.TryParse(coordinates[1].Trim(), out int posY))
                {
                    cell["pos"] = new List<int> { posX, posY };
                }
                else
                {
                    Debug.LogError($"Error al parsear 'pos' con valor: {posString}");
                    cell["pos"] = new List<int> { 0, 0 }; // Valor por defecto o manejar el error según sea necesario
                }
            }

            // Convertir `door` de cadena a lista (si aplica)
            if (cell.ContainsKey("door") && cell["door"] is string doorString)
            {
                if (!string.IsNullOrWhiteSpace(doorString) && doorString != "[]")
                {
                    var doors = new List<List<int>>();
                    var doorEntries = doorString.Trim('[', ']').Split(new string[] { "),(" }, System.StringSplitOptions.RemoveEmptyEntries);

                    foreach (var entry in doorEntries)
                    {
                        var doorCoords = entry.Trim('(', ')').Split(',');
                        if (doorCoords.Length == 2 && 
                            int.TryParse(doorCoords[0].Trim(), out int doorX) && 
                            int.TryParse(doorCoords[1].Trim(), out int doorY))
                        {
                            doors.Add(new List<int> { doorX, doorY });
                        }
                        else
                        {
                            Debug.LogError($"Error al parsear 'door' con valor: {entry}");
                        }
                    }
                    cell["door"] = doors;
                }
                else
                {
                    cell["door"] = new List<List<int>>(); // Lista vacía correctamente tipada
                }
            }
        }

        return JsonConvert.SerializeObject(parsedJson);
    }

    void BuildBoard(Dictionary<string, CellState> gameState)
{
    foreach (var cellEntry in gameState)
    {
        string positionKey = cellEntry.Key; // Clave como "(x, y, z)"
        CellState cellData = cellEntry.Value;

        // Extrae las coordenadas de la clave
        string[] positionParts = positionKey.Trim('(', ')').Split(',');
        if (positionParts.Length == 3 && 
            int.TryParse(positionParts[0].Trim(), out int floor) && // x
            int.TryParse(positionParts[1].Trim(), out int row) &&   // y
            int.TryParse(positionParts[2].Trim(), out int column))  // z
        {
            // Mapea 'row' a X y 'column' a Z
            Vector3 cellPosition = new Vector3(row * cellSize, 0, -column * cellSize);

            // Instancia el suelo en la posición calculada
            Instantiate(cellPrefab, cellPosition, Quaternion.identity, boardParent);

            Debug.Log($"Celda creada en posición: {cellPosition}");
        }
        else
        {
            Debug.LogError($"Error al parsear la clave de posición: {positionKey}");
        }
    }
}

}
