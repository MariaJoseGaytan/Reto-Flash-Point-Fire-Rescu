using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{
    public ServerManager serverManager;

    // Referencias a los prefabs
    public GameObject cellPrefab;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject avalanchePrefab; // HIELO
    public GameObject victimPrefab; // Puffle
    public GameObject falseAlarmPrefab; // Fantasma

    // Padre para organizar las celdas
    public Transform boardParent;

    void Start()
    {
        StartCoroutine(serverManager.GetGameState(OnGameStateReceived));
    }

    void OnGameStateReceived(string json)
{
    Debug.Log($"JSON recibido en GameManager: {json}");

    // Intenta deserializar el JSON
    List<CellState> gameState = JsonConvert.DeserializeObject<List<CellState>>(json);

    if (gameState != null && gameState.Count > 0)
    {
        Debug.Log($"Número de celdas procesadas: {gameState.Count}");
        BuildBoard(gameState);
    }
    else
    {
        Debug.LogError("El JSON no se pudo deserializar o está vacío.");
    }
}


    void BuildBoard(List<CellState> gameState)
{
    float cellSize = 19.96322f; // Tamaño del piso (Scale del prefab)

    foreach (CellState cell in gameState)
    {
        Vector3 cellPosition = new Vector3(cell.cell_position[1] * cellSize, 0, -cell.cell_position[0] * cellSize);

        Instantiate(cellPrefab, cellPosition, Quaternion.identity, boardParent);

        Debug.Log($"Celda creada en: {cellPosition}");
    }
}






    void CreateWalls(AgentData agent, Vector3 cellPosition)
{
    string walls = agent.walls;
    float wallLength = 1f;

    // Pared arriba
    if (walls[0] == '1')
    {
        Vector3 position = cellPosition + new Vector3(0, 0, wallLength / 2);
        Instantiate(wallPrefab, position, Quaternion.identity, boardParent);
    }
    // Pared izquierda
    if (walls[1] == '1')
    {
        Vector3 position = cellPosition + new Vector3(-wallLength / 2, 0, 0);
        Instantiate(wallPrefab, position, Quaternion.Euler(0, 90, 0), boardParent);
    }
    // Pared abajo
    if (walls[2] == '1')
    {
        Vector3 position = cellPosition + new Vector3(0, 0, -wallLength / 2);
        Instantiate(wallPrefab, position, Quaternion.identity, boardParent);
    }
    // Pared derecha
    if (walls[3] == '1')
    {
        Vector3 position = cellPosition + new Vector3(wallLength / 2, 0, 0);
        Instantiate(wallPrefab, position, Quaternion.Euler(0, 90, 0), boardParent);
    }
}


    void CreateMarker(AgentData agent, Vector3 cellPosition)
    {
        if (agent.marker_type == "v")
        {
            Instantiate(victimPrefab, cellPosition, Quaternion.identity, boardParent);
        }
        else if (agent.marker_type == "f")
        {
            Instantiate(falseAlarmPrefab, cellPosition, Quaternion.identity, boardParent);
        }
    }

    void CreateDoor(AgentData agent, Vector3 cellPosition)
    {
        // Determinar la posición de la puerta
        int x = agent.position[0];
        int y = agent.position[1];
        List<int> connectedCell = agent.connected_cell;

        int dx = connectedCell[0] - x;
        int dy = connectedCell[1] - y;

        Vector3 position = cellPosition;
        Quaternion rotation = Quaternion.identity;

        if (dx == 1) // Puerta a la derecha
        {
            position += new Vector3(0.5f, 0, 0);
            rotation = Quaternion.Euler(0, 90, 0);
        }
        else if (dx == -1) // Puerta a la izquierda
        {
            position += new Vector3(-0.5f, 0, 0);
            rotation = Quaternion.Euler(0, 90, 0);
        }
        else if (dy == 1) // Puerta arriba
        {
            position += new Vector3(0, 0, 0.5f);
        }
        else if (dy == -1) // Puerta abajo
        {
            position += new Vector3(0, 0, -0.5f);
        }

        Instantiate(doorPrefab, position, rotation, boardParent);
    }
}
