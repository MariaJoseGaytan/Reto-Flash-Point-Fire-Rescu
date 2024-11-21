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
            int fila = cell.cell_position[0];
            int columna = cell.cell_position[1];

            // Calculamos la posición de la celda
            float x = columna * cellSize;
            float z = -fila * cellSize;
            Vector3 cellPosition = new Vector3(x, 0, z);

            Instantiate(cellPrefab, cellPosition, Quaternion.identity, boardParent);
            Debug.Log($"Celda creada en fila: {fila}, columna: {columna}, posición: {cellPosition}");

            // Crear las paredes basándose en las celdas y agentes
            foreach (AgentData agent in cell.agents)
            {
                if (agent.type == "CellAgent")
                {
                    CreateWalls(agent, fila, columna); // Evaluar paredes
                }
            }
        }
    }

    void CreateWalls(AgentData agent, int fila, int columna)
    {
        float wallHeight = 7.1f; // Altura de las paredes
        float cellSize = 19.96322f;

        // Desplazamientos iniciales basados en tus cálculos
        float offsetX_ParedArriba = 6.8f;
        float offsetX_ParedAbajo = 9.16322f;
        float offsetX_ParedIzquierda = -2.2f;
        float offsetX_ParedDerecha = -1.8f;

        float offsetZ_ParedArriba = -2.8f;
        float offsetZ_ParedIzquierda = 7.2f;
        float offsetZ_ParedAbajo = -2.8f;
        float offsetZ_ParedDerecha = -11f;

        // Pared superior
        if (agent.walls[0] == '1')
        {
            float x = offsetX_ParedArriba + columna * cellSize;
            float z = offsetZ_ParedArriba - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateWall(position, Quaternion.identity, "Pared superior");
        }

        // Pared izquierda
        if (agent.walls[1] == '1')
        {
            float x = offsetX_ParedIzquierda + columna * cellSize;
            float z = offsetZ_ParedIzquierda - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateWall(position, rotation, "Pared izquierda");
        }

        // Pared inferior
        if (agent.walls[2] == '1')
        {
            float x = offsetX_ParedAbajo + columna * cellSize;
            float z = offsetZ_ParedAbajo - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateWall(position, Quaternion.identity, "Pared inferior");
        }

        // Pared derecha
        if (agent.walls[3] == '1')
        {
            float x = offsetX_ParedDerecha + columna * cellSize;
            float z = offsetZ_ParedDerecha - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
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
