using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{
    public ServerManager serverManager;

    // Referencias a los prefabs
    public GameObject cellPrefab;  // Prefab del suelo
    public GameObject wallPrefab;  // Prefab de las paredes

    // Padre para organizar las celdas
    public Transform boardParent;

    // Tamaño de la celda (escala del prefab)
    private float cellSize = 19.96322f;

    void Start()
    {
        // Solicitar el estado inicial del juego al servidor
        StartCoroutine(serverManager.GetGameState(OnGameStateReceived));
    }

    void OnGameStateReceived(string json)
    {
        Debug.Log($"JSON recibido en GameManager: {json}");

        // Deserializar el JSON para obtener el estado del tablero
        List<CellState> gameState = JsonConvert.DeserializeObject<List<CellState>>(json);

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

    void BuildBoard(List<CellState> gameState)
    {
        foreach (CellState cell in gameState)
        {
            // Calcular la posición del suelo
            Vector3 cellPosition = new Vector3(cell.cell_position[1] * cellSize, 0, -cell.cell_position[0] * cellSize);

            // Crear el suelo para todas las celdas
            Instantiate(cellPrefab, cellPosition, Quaternion.identity, boardParent);
            Debug.Log($"Celda creada en: {cellPosition}");

            // Crear las paredes solo para la celda en [0,0]
            if (cell.cell_position[0] == 0 && cell.cell_position[1] == 0)
            {
                foreach (AgentData agent in cell.agents)
                {
                    if (agent.type == "CellAgent")
                    {
                        CreateWalls(agent, cellPosition); // Crear paredes para la celda [0,0]
                    }
                }
            }
        }
    }

   void CreateWalls(AgentData agent, Vector3 cellPosition)
{
    float wallHeight = 7.1f; // Altura de las paredes
    float offsetX = 2.2f;    // Desplazamiento en X para las paredes izquierda y derecha
    float offsetZ = 7.2f;    // Desplazamiento en Z para las paredes superior e inferior
    float lowerOffsetX = -11.2f; // Ajuste en X para la pared inferior
    float lowerOffsetZ = -2.8f;  // Ajuste en Z para la pared inferior

    // Pared superior (arriba)
    if (agent.walls[0] == '1')
    {
        Vector3 position = cellPosition + new Vector3(0, wallHeight, offsetZ);
        Quaternion rotation = Quaternion.identity; // Sin rotación
        InstantiateWall(position, rotation, "Pared superior");
    }

    // Pared izquierda
    if (agent.walls[1] == '1')
    {
        Vector3 position = cellPosition + new Vector3(-offsetX, wallHeight, offsetZ); // Ajuste en Z
        Quaternion rotation = Quaternion.Euler(0, 90, 0); // Rotación 90 grados
        InstantiateWall(position, rotation, "Pared izquierda");
    }

    // Pared inferior (ajustando X y Z)
    if (agent.walls[2] == '1')
    {
        Vector3 position = cellPosition + new Vector3(lowerOffsetX, wallHeight, lowerOffsetZ); // Ajuste en X y Z
        Quaternion rotation = Quaternion.identity; // Sin rotación
        InstantiateWall(position, rotation, "Pared inferior");
    }

    // Pared derecha
    if (agent.walls[3] == '1')
    {
        Vector3 position = cellPosition + new Vector3(offsetX, wallHeight, 0);
        Quaternion rotation = Quaternion.Euler(0, 90, 0); // Rotación 90 grados
        InstantiateWall(position, rotation, "Pared derecha");
    }
}

void InstantiateWall(Vector3 position, Quaternion rotation, string wallName)
{
    GameObject wall = Instantiate(wallPrefab, position, rotation, boardParent);
    wall.name = wallName;
    Debug.Log($"{wallName} creada en: {position}");
}


}
