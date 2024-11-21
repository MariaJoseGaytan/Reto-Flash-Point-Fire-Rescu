using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{
    public ServerManager serverManager;

    // Referencias a los prefabs
    public GameObject cellPrefab;         // Prefab del suelo
    public GameObject wallPrefab;         // Prefab de las paredes
    public GameObject doorPrefab;         // Prefab de las puertas
    public GameObject fMarkerPrefab;      // Prefab para marker_type "f"
    public GameObject vMarkerPrefab;      // Prefab para marker_type "v"
    public GameObject fireMarkerPrefab;   // Prefab para FireMarkerAgent

    // Padre para organizar las celdas
    public Transform boardParent;

    // Tamaño de la celda (escala del prefab)
    private float cellSize = 19.96322f;

    // Diccionarios para almacenar datos de celdas y puertas
    private Dictionary<(int, int), AgentData> cellAgentsDict = new Dictionary<(int, int), AgentData>();
    private HashSet<string> processedDoors = new HashSet<string>();

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
        cellAgentsDict.Clear();
        processedDoors.Clear();

        // Primero, almacenar los CellAgents en el diccionario
        foreach (CellState cell in gameState)
        {
            int fila = cell.cell_position[0];
            int columna = cell.cell_position[1];

            foreach (AgentData agent in cell.agents)
            {
                if (agent.type == "CellAgent")
                {
                    cellAgentsDict[(fila, columna)] = agent;
                    break;
                }
            }
        }

        // Procesar las puertas y actualizar las paredes
        foreach (CellState cell in gameState)
        {
            int fila = cell.cell_position[0];
            int columna = cell.cell_position[1];

            foreach (AgentData agent in cell.agents)
            {
                if (agent.type == "DoorAgent")
                {
                    ProcessDoor(agent, fila, columna);
                }
            }
        }

        // Construir el tablero
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

            // Crear las paredes y colocar agentes
            foreach (AgentData agent in cell.agents)
            {
                if (agent.type == "CellAgent")
                {
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

    // Método para procesar las puertas
    void ProcessDoor(AgentData agent, int fila, int columna)
    {
        int connectedFila = agent.connected_cell[0];
        int connectedColumna = agent.connected_cell[1];

        // Crear una clave única para la puerta
        string doorKey = CreateDoorKey(fila, columna, connectedFila, connectedColumna);

        // Verificar si ya hemos procesado esta puerta
        if (processedDoors.Contains(doorKey))
        {
            // Puerta ya procesada, omitir
            return;
        }

        // Marcar la puerta como procesada
        processedDoors.Add(doorKey);

        // Instanciar la puerta
        InstantiateDoor(fila, columna, connectedFila, connectedColumna);
    }

    // Método para crear una clave única para cada puerta
    string CreateDoorKey(int fila1, int columna1, int fila2, int columna2)
    {
        // Ordenar las posiciones para asegurar que la clave sea única sin importar el orden
        if (fila1 < fila2 || (fila1 == fila2 && columna1 < columna2))
        {
            return $"{fila1}_{columna1}_{fila2}_{columna2}";
        }
        else
        {
            return $"{fila2}_{columna2}_{fila1}_{columna1}";
        }
    }

    // Método para instanciar la puerta en el lugar correcto
    void InstantiateDoor(int fila, int columna, int connectedFila, int connectedColumna)
    {
        float doorHeight = 7.1f; // Altura de la puerta

        // Desplazamientos basados en cálculos
        float offsetX_ParedArriba = 6.8f;
        float offsetX_ParedAbajo = 9.16322f;
        float offsetX_ParedIzquierda = -2.2f;
        float offsetX_ParedDerecha = -1.8f;

        float offsetZ_ParedArriba = -2.8f;
        float offsetZ_ParedIzquierda = 7.2f;
        float offsetZ_ParedAbajo = -2.8f;
        float offsetZ_ParedDerecha = -11f;

        int filaDiff = connectedFila - fila;
        int columnaDiff = connectedColumna - columna;

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        // Determinar posición y rotación según la dirección de conexión
        if (filaDiff == -1 && columnaDiff == 0)
        {
            // Puerta en la pared superior
            float x = columna * cellSize + offsetX_ParedArriba;
            float z = -fila * cellSize + offsetZ_ParedArriba;
            position = new Vector3(x, doorHeight, z);
            rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (filaDiff == 1 && columnaDiff == 0)
        {
            // Puerta en la pared inferior
            float x = columna * cellSize + offsetX_ParedAbajo;
            float z = -fila * cellSize + offsetZ_ParedAbajo;
            position = new Vector3(x, doorHeight, z);
            rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (filaDiff == 0 && columnaDiff == -1)
        {
            // Puerta en la pared izquierda
            float x = columna * cellSize + offsetX_ParedIzquierda;
            float z = -fila * cellSize + offsetZ_ParedIzquierda;
            position = new Vector3(x, doorHeight, z);
            rotation = Quaternion.Euler(0, 90, 0);
        }
        else if (filaDiff == 0 && columnaDiff == 1)
        {
            // Puerta en la pared derecha
            float x = columna * cellSize + offsetX_ParedDerecha;
            float z = -fila * cellSize + offsetZ_ParedDerecha;
            position = new Vector3(x, doorHeight, z);
            rotation = Quaternion.Euler(0, 90, 0);
        }
        else
        {
            Debug.LogWarning($"[DEBUG] Dirección de la puerta no reconocida entre ({fila}, {columna}) y ({connectedFila}, {connectedColumna})");
            return;
        }

        // Instanciar la puerta
        GameObject door = Instantiate(doorPrefab, position, rotation, boardParent);
        door.name = $"Door_{fila}_{columna}_to_{connectedFila}_{connectedColumna}";
        Debug.Log($"Puerta instanciada entre ({fila}, {columna}) y ({connectedFila}, {connectedColumna}) en posición {position}");
    }

    // Método para obtener el AgentData de una celda específica
    AgentData GetCellAgent(int fila, int columna)
    {
        if (cellAgentsDict.TryGetValue((fila, columna), out AgentData agent))
        {
            return agent;
        }
        else
        {
            return null;
        }
    }

    void CreateWalls(AgentData agent, int fila, int columna)
    {
        float wallHeight = 7.1f; // Altura de las paredes

        // Desplazamientos basados en cálculos
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

        // Crear las paredes basándose en las paredes ajustadas
        if (walls[0] == '1')
        {
            // Pared superior
            float x = offsetX_ParedArriba + columna * cellSize;
            float z = offsetZ_ParedArriba - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateWall(position, Quaternion.identity, "Pared superior");
        }

        if (walls[1] == '1')
        {
            // Pared izquierda
            float x = offsetX_ParedIzquierda + columna * cellSize;
            float z = offsetZ_ParedIzquierda - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateWall(position, rotation, "Pared izquierda");
        }

        if (walls[2] == '1')
        {
            // Pared inferior
            float x = offsetX_ParedAbajo + (columna - 1) * cellSize;
            float z = offsetZ_ParedAbajo - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateWall(position, Quaternion.identity, "Pared inferior");
        }

        if (walls[3] == '1')
        {
            // Pared derecha
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
