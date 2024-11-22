using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{
    public ServerManager serverManager;

    // Referencias a los prefabs
    public GameObject cellPrefab;      // Prefab del suelo
    public GameObject wallPrefab;      // Prefab de las paredes
    public GameObject fMarkerPrefab;   // Prefab para marker_type "f"
    public GameObject vMarkerPrefab;   // Prefab para marker_type "v"
    public GameObject fireMarkerPrefab; // Prefab para FireMarkerAgent
    public GameObject doorPrefab;      // Prefab para las puertas

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
                    ValidateDoors(agent, cell); // Validar puertas y ajustar paredes
                    CreateWalls(agent, fila, columna); // Evaluar paredes
                }
                else if (agent.type == "MarkerAgent")
                {
                    PlaceMarker(agent, fila, columna); // Colocar marcador
                }
                else if (agent.type == "FireMarkerAgent")
                {
                    PlaceFireMarker(agent, cellPosition); // Colocar marcador de fuego
                }
            }
        }
    }

    void ValidateDoors(AgentData cellAgent, CellState cell)
    {
        foreach (AgentData agent in cell.agents)
        {
            // Verifica que sea un DoorAgent y tenga una celda conectada
            if (agent.type == "DoorAgent" && agent.connected_cell != null)
            {
                List<int> currentPosition = cell.cell_position;  // Posición actual de la celda
                List<int> connectedPosition = agent.connected_cell; // Posición de la celda conectada

                char[] walls = cellAgent.walls.ToCharArray(); // Obtener las paredes actuales como un arreglo de caracteres

                // Determinar la ubicación de la puerta según las diferencias en x (fila) y y (columna)
                int dx = connectedPosition[0] - currentPosition[0]; // Cambio en x (fila)
                int dy = connectedPosition[1] - currentPosition[1]; // Cambio en y (columna)

                string direction = ""; // Variable para almacenar la dirección detectada

                // Ajustar las direcciones según tu lógica corregida
                if (dx == 1 && dy == 0) // Puerta a la derecha
                {
                    walls[3] = '2';
                    direction = "derecha";
                }
                else if (dx == -1 && dy == 0) // Puerta a la izquierda
                {
                    walls[1] = '2';
                    direction = "izquierda";
                }
                else if (dx == 0 && dy == 1) // Puerta arriba
                {
                    walls[0] = '2';
                    direction = "arriba";
                }
                else if (dx == 0 && dy == -1) // Puerta abajo
                {
                    walls[2] = '2';
                    direction = "abajo";
                }

                // Log para depuración
                Debug.Log($"[PUERTA DETECTADA] Dirección: {direction}, Pared actualizada: {new string(walls)}");
                Debug.Log($"[DETALLES] Posición actual (x, y): ({currentPosition[0]}, {currentPosition[1]}), Conectada (x, y): ({connectedPosition[0]}, {connectedPosition[1]})");
                Debug.Log($"[DIFERENCIA] dx: {dx}, dy: {dy}");

                // Actualiza las paredes en el agente de celda
                cellAgent.walls = new string(walls);
            }
        }
    }


    void CreateWalls(AgentData agent, int fila, int columna)
    {
        float wallHeight = 7.1f; // Altura de las paredes
        float cellSize = 19.96322f;

        // Desplazamientos iniciales basados en cálculos
        float offsetX_ParedArriba = 6.8f;
        float offsetX_ParedAbajo = 9.16322f;
        float offsetX_ParedIzquierda = -2.2f;
        float offsetX_ParedDerecha = -1.8f;

        float offsetZ_ParedArriba = -2.8f;
        float offsetZ_ParedIzquierda = 7.2f;
        float offsetZ_ParedAbajo = -2.8f;
        float offsetZ_ParedDerecha = -11f;

        // Crear una copia mutable de las paredes
        char[] walls = agent.walls.ToCharArray();

        // Variables temporales para la modificación de paredes
        int filaMod = columna;     
        int columnaMod = fila;

        // Ajustar paredes si la celda es una entrada
        if (agent.is_entrance)
        {
            if (filaMod == 5)
            {
                walls[0] = '0'; // Quitar pared superior
                Debug.Log($"[DEBUG] Pared superior eliminada en fila: {filaMod}, columna: {columnaMod}");
            }
            if (filaMod == 0)
            {
                walls[2] = '0'; // Quitar pared inferior
                Debug.Log($"[DEBUG] Pared inferior eliminada en fila: {filaMod}, columna: {columnaMod}");
            }
            if (columnaMod == 0)
            {
                walls[1] = '0'; // Quitar pared izquierda
                Debug.Log($"[DEBUG] Pared izquierda eliminada en fila: {filaMod}, columna: {columnaMod}");
            }
            if (columnaMod == 7)
            {
                walls[3] = '0'; // Quitar pared derecha
                Debug.Log($"[DEBUG] Pared derecha eliminada en fila: {filaMod}, columna: {columnaMod}");
            }
        }

        Debug.Log($"[DEBUG] Paredes ajustadas para celda en fila: {fila}, columna: {columna}, resultado: {new string(walls)}");

        // Usar las paredes ajustadas para crear las paredes
        // Aquí utilizamos las variables originales de fila y columna

        // Pared superior
        if (walls[0] == '1')
        {
            float x = offsetX_ParedArriba + columna * cellSize;
            float z = offsetZ_ParedArriba - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateWall(position, Quaternion.identity, "Pared superior");
        }
        else if (walls[0] == '2') // Puerta superior
        {
            float x = offsetX_ParedArriba + columna * cellSize;
            float z = offsetZ_ParedArriba - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateDoor(position, Quaternion.identity, "Puerta superior");
        }

        // Pared izquierda
        if (walls[1] == '1')
        {
            float x = offsetX_ParedIzquierda + columna * cellSize;
            float z = offsetZ_ParedIzquierda - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateWall(position, rotation, "Pared izquierda");
        }
        else if (walls[1] == '2') // Puerta izquierda
        {
            float x = offsetX_ParedIzquierda + columna * cellSize;
            float z = offsetZ_ParedIzquierda - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateDoor(position, rotation, "Puerta izquierda");
        }

        // Pared inferior
        if (walls[2] == '1')
        {
            float x = offsetX_ParedAbajo + (columna - 1) * cellSize;
            float z = offsetZ_ParedAbajo - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateWall(position, Quaternion.identity, "Pared inferior");
        }
        else if (walls[2] == '2') // Puerta inferior
        {
            float x = offsetX_ParedAbajo + (columna - 1) * cellSize;
            float z = offsetZ_ParedAbajo - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateDoor(position, Quaternion.identity, "Puerta inferior");
        }

        // Pared derecha
        if (walls[3] == '1')
        {
            float x = offsetX_ParedDerecha + columna * cellSize;
            float z = offsetZ_ParedDerecha - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateWall(position, rotation, "Pared derecha");
        }
        else if (walls[3] == '2') // Puerta derecha
        {
            float x = offsetX_ParedDerecha + columna * cellSize;
            float z = offsetZ_ParedDerecha - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateDoor(position, rotation, "Puerta derecha");
        }
    }

    void InstantiateWall(Vector3 position, Quaternion rotation, string wallName)
    {
        GameObject wall = Instantiate(wallPrefab, position, rotation, boardParent);
        wall.name = wallName;
        Debug.Log($"{wallName} creada en: {position}");
    }

    void InstantiateDoor(Vector3 position, Quaternion rotation, string doorName)
    {
        GameObject door = Instantiate(doorPrefab, position, rotation, boardParent);
        door.name = doorName;
        Debug.Log($"{doorName} creada en: {position}");
    }

    // Método para colocar los marcadores (víctimas)
    void PlaceMarker(AgentData agent, int fila, int columna)
    {
        float markerHeight = 7.3f; // Altura del marcador

        // Calculamos la posición central de la celda
        float x = columna * cellSize;
        float z = -fila * cellSize;
        Vector3 markerPosition = new Vector3(x, markerHeight, z);

        // Seleccionar el prefab según el marker_type
        GameObject markerPrefab = null;
        if (agent.marker_type == "f")
        {
            markerPrefab = fMarkerPrefab;
        }
        else if (agent.marker_type == "v")
        {
            markerPrefab = vMarkerPrefab;
        }
        else
        {
            Debug.LogWarning($"Tipo de marcador desconocido: {agent.marker_type} en posición ({fila}, {columna})");
            return;
        }

        // Instanciar el marcador
        GameObject marker = Instantiate(markerPrefab, markerPosition, Quaternion.identity, boardParent);
        marker.name = $"Marker_{agent.unique_id}";
        Debug.Log($"Marcador '{agent.marker_type}' instanciado en: {markerPosition}");
    }

    void PlaceFireMarker(AgentData agent, Vector3 cellPosition)
    {
        float markerHeight = 7.3f; // Altura del marcador de fuego

        // Posición del marcador: centro de la celda, a una altura específica
        Vector3 markerPosition = new Vector3(cellPosition.x, markerHeight, cellPosition.z);

        // Instanciar el marcador de fuego
        GameObject marker = Instantiate(fireMarkerPrefab, markerPosition, Quaternion.identity, boardParent);
        marker.name = $"FireMarker_{agent.unique_id}";
        Debug.Log($"FireMarker instanciado en: {markerPosition}");
    }
}
