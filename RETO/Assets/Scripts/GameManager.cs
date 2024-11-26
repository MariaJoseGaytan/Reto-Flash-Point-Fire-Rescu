using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{
    public ServerManager serverManager;

    // Referencias a los prefabs
    public GameObject cellPrefab;          // Prefab del suelo
    public GameObject wallPrefab;          // Prefab de las paredes
    public GameObject fMarkerPrefab;       // Prefab para marker_type "f" y "false_alarm"
    public GameObject vMarkerPrefab;       // Prefab para marker_type "v" y "victim"
    public GameObject fireMarkerPrefab;    // Prefab para FireMarkerAgent
    public GameObject smokeMarkerPrefab;   // Prefab para SmokeMarkerAgent
    public GameObject doorPrefab;          // Prefab para las puertas
    public GameObject penguinPrefab;       // Prefab del pingüino

    // Padre para organizar las celdas
    public Transform boardParent;

    // Tamaño de la celda (escala del prefab)
    private float cellSize = 19.96322f;

    // Variable para almacenar el estado actual del juego
    public MapData currentGameState;

    // Diccionarios y conjuntos para rastrear objetos instanciados y sus posiciones
    private Dictionary<int, GameObject> penguinGameObjects = new Dictionary<int, GameObject>();
    private Dictionary<string, GameObject> wallGameObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> wallPositions = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> fireMarkers = new Dictionary<string, GameObject>();
    private HashSet<string> currentFirePositions = new HashSet<string>();
    private Dictionary<string, GameObject> smokeMarkers = new Dictionary<string, GameObject>();
    private HashSet<string> currentSmokePositions = new HashSet<string>();
    private Dictionary<string, GameObject> poiMarkers = new Dictionary<string, GameObject>();
    private HashSet<string> currentPoiPositions = new HashSet<string>();
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
        // Crear un diccionario para mapear las posiciones de las celdas a sus CellState
        Dictionary<(int, int), CellState> cellDictionary = gameState.ToDictionary(
            cell => (cell.cell_position[0], cell.cell_position[1]),
            cell => cell
        );

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
                    ValidateDoors(agent, cell, cellDictionary); // Validar puertas y ajustar paredes
                    CreateWalls(agent, fila, columna); // Evaluar paredes
                }
                else if (agent.type == "MarkerAgent")
                {
                    // Si deseas crear marcadores iniciales, descomenta la siguiente línea
                    // PlaceMarker(agent, fila, columna); // Colocar marcador
                }
            }
        }
    }

    void ValidateDoors(AgentData cellAgent, CellState cell, Dictionary<(int, int), CellState> cellDictionary)
    {
        foreach (AgentData agent in cell.agents)
        {
            if (agent.type == "DoorAgent" && agent.connected_cell != null)
            {
                List<int> currentPosition = cell.cell_position;
                List<int> connectedPosition = agent.connected_cell;

                string doorKey = GetDoorKey(currentPosition, connectedPosition);

                if (processedDoors.Contains(doorKey))
                {
                    continue;
                }
                processedDoors.Add(doorKey);

                string direction = GetDirectionBetweenCells(currentPosition.ToArray(), connectedPosition.ToArray());
                int wallIndex = GetWallIndex(direction);

                // Actualizar paredes en la celda actual
                char[] walls = cellAgent.walls.ToCharArray();
                walls[wallIndex] = '2';
                cellAgent.walls = new string(walls);

                // Actualizar paredes en la celda conectada
                if (cellDictionary.TryGetValue((connectedPosition[0], connectedPosition[1]), out CellState connectedCell))
                {
                    AgentData connectedCellAgent = connectedCell.agents.FirstOrDefault(a => a.type == "CellAgent");
                    if (connectedCellAgent != null)
                    {
                        char[] connectedWalls = connectedCellAgent.walls.ToCharArray();
                        int oppositeWallIndex = GetOppositeWallIndex(wallIndex);
                        connectedWalls[oppositeWallIndex] = '2'; // Marcar como puerta
                        connectedCellAgent.walls = new string(connectedWalls);
                        Debug.Log($"[PUERTA EN CELDA CONECTADA] Pared actualizada en celda conectada ({connectedPosition[0]}, {connectedPosition[1]}): {new string(connectedWalls)}");
                    }
                }
                else
                {
                    Debug.LogWarning($"No se encontró la celda conectada en posición ({connectedPosition[0]}, {connectedPosition[1]})");
                }
            }
        }
    }

    string GetDirectionBetweenCells(int[] cell1, int[] cell2)
    {
        int dx = cell2[0] - cell1[0];
        int dy = cell2[1] - cell1[1];

        if (dx == 1 && dy == 0)
            return "right";
        else if (dx == -1 && dy == 0)
            return "left";
        else if (dx == 0 && dy == 1)
            return "up";
        else if (dx == 0 && dy == -1)
            return "down";
        else
            return null;
    }

    int GetWallIndex(string direction)
    {
        switch (direction)
        {
            case "up": return 0;
            case "left": return 1;
            case "down": return 2;
            case "right": return 3;
            default: return -1;
        }
    }

    int GetOppositeWallIndex(int index)
    {
        switch (index)
        {
            case 0: return 2; // up -> down
            case 1: return 3; // left -> right
            case 2: return 0; // down -> up
            case 3: return 1; // right -> left
            default: return -1;
        }
    }

    string GetDoorKey(List<int> cell1, List<int> cell2)
    {
        // Ordenamos las celdas para que la clave sea consistente sin importar el orden
        int cell1X = cell1[0];
        int cell1Y = cell1[1];
        int cell2X = cell2[0];
        int cell2Y = cell2[1];

        if (cell1X > cell2X || (cell1X == cell2X && cell1Y > cell2Y))
        {
            // Intercambiamos si cell1 es mayor que cell2
            int tempX = cell1X;
            int tempY = cell1Y;
            cell1X = cell2X;
            cell1Y = cell2Y;
            cell2X = tempX;
            cell2Y = tempY;
        }

        return $"{cell1X}_{cell1Y}_{cell2X}_{cell2Y}";
    }

    void CreateWalls(AgentData agent, int fila, int columna)
    {
        float wallHeight = 7.1f; // Altura de las paredes

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

        // Pared superior
        if (walls[0] == '1')
        {
            float x = offsetX_ParedArriba + columna * cellSize;
            float z = offsetZ_ParedArriba - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateWall(position, Quaternion.identity, "Pared superior", fila, columna, "up");
        }
        else if (walls[0] == '2') // Puerta superior
        {
            float x = offsetX_ParedArriba + columna * cellSize;
            float z = offsetZ_ParedArriba - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateDoor(position, Quaternion.identity, "Puerta superior", fila, columna, "up");
        }

        // Pared izquierda
        if (walls[1] == '1')
        {
            float x = offsetX_ParedIzquierda + columna * cellSize;
            float z = offsetZ_ParedIzquierda - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateWall(position, rotation, "Pared izquierda", fila, columna, "left");
        }
        else if (walls[1] == '2') // Puerta izquierda
        {
            float x = offsetX_ParedIzquierda + columna * cellSize;
            float z = offsetZ_ParedIzquierda - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateDoor(position, rotation, "Puerta izquierda", fila, columna, "left");
        }

        // Pared inferior
        if (walls[2] == '1')
        {
            float x = offsetX_ParedAbajo + (columna - 1) * cellSize;
            float z = offsetZ_ParedAbajo - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateWall(position, Quaternion.identity, "Pared inferior", fila, columna, "down");
        }
        else if (walls[2] == '2') // Puerta inferior
        {
            float x = offsetX_ParedAbajo + (columna - 1) * cellSize;
            float z = offsetZ_ParedAbajo - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            InstantiateDoor(position, Quaternion.identity, "Puerta inferior", fila, columna, "down");
        }

        // Pared derecha
        if (walls[3] == '1')
        {
            float x = offsetX_ParedDerecha + columna * cellSize;
            float z = offsetZ_ParedDerecha - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateWall(position, rotation, "Pared derecha", fila, columna, "right");
        }
        else if (walls[3] == '2') // Puerta derecha
        {
            float x = offsetX_ParedDerecha + columna * cellSize;
            float z = offsetZ_ParedDerecha - fila * cellSize;
            Vector3 position = new Vector3(x, wallHeight, z);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            InstantiateDoor(position, rotation, "Puerta derecha", fila, columna, "right");
        }
    }

    string GetWallPositionKey(Vector3 position)
    {
        // Redondear las posiciones para evitar problemas de precisión en coma flotante
        float x = Mathf.Round(position.x * 1000f) / 1000f;
        float y = Mathf.Round(position.y * 1000f) / 1000f;
        float z = Mathf.Round(position.z * 1000f) / 1000f;
        return $"{x}_{y}_{z}";
    }

    void InstantiateWall(Vector3 position, Quaternion rotation, string wallName, int fila, int columna, string direction)
    {
        string positionKey = GetWallPositionKey(position);
        if (wallPositions.ContainsKey(positionKey))
        {
            // Ya existe una pared en esta posición, no instanciar la nueva pared
            Debug.Log($"Ya existe una pared en la posición {position}. No se instanciará {wallName}.");
            return;
        }
        GameObject wall = Instantiate(wallPrefab, position, rotation, boardParent);
        wall.name = wallName;
        string wallKey = GetWallKey(fila, columna, direction);
        wallGameObjects[wallKey] = wall;
        wallPositions[positionKey] = wall;
        Debug.Log($"{wallName} creada en: {position}");
        Debug.Log($"[CREACIÓN] wallKey: {wallKey}, positionKey: {positionKey}, fila: {fila}, columna: {columna}, dirección: {direction}");
    }

    void InstantiateDoor(Vector3 position, Quaternion rotation, string doorName, int fila, int columna, string direction)
    {
        string positionKey = GetWallPositionKey(position);
        if (wallPositions.ContainsKey(positionKey))
        {
            // Ya existe una pared o puerta en esta posición, no instanciar la nueva puerta
            Debug.Log($"Ya existe una pared en la posición {position}. No se instanciará {doorName}.");
            return;
        }
        GameObject door = Instantiate(doorPrefab, position, rotation, boardParent);
        door.name = doorName;
        string wallKey = GetWallKey(fila, columna, direction);
        wallGameObjects[wallKey] = door;
        wallPositions[positionKey] = door;
        Debug.Log($"{doorName} creada en: {position}");
        Debug.Log($"[CREACIÓN] wallKey: {wallKey}, positionKey: {positionKey}, fila: {fila}, columna: {columna}, dirección: {direction}");
    }

    string GetWallKey(int fila, int columna, string direction)
    {
        return $"{fila}_{columna}_{direction}";
    }

    // Método para colocar los marcadores (víctimas)
    void PlaceMarker(AgentData agent, int fila, int columna)
    {
        float markerHeight = -1.624f; // Altura del marcador

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

    public void UpdateBoardState(int step)
    {
        Debug.Log($"Actualizando el tablero para el paso: {step}");
        if (currentGameState == null)
        {
            Debug.LogError("El estado actual del juego no está disponible.");
            return;
        }

        // Actualizar la posición de los pingüinos
        if (currentGameState.agents != null && currentGameState.agents.Length > 0)
        {
            if (step < 0 || step >= currentGameState.agents.Length)
            {
                Debug.LogError($"Paso inválido: {step}. Debe estar entre 0 y {currentGameState.agents.Length - 1}");
                return;
            }

            var currentStepData = currentGameState.agents[step];

            foreach (var agentData in currentStepData.data)
            {
                int agentId = agentData.agent_id;
                int[] position = agentData.position;

                // Restar 1 a cada coordenada y luego intercambiarlas
                int adjustedRow = position[1] - 1;
                int adjustedColumn = position[0] - 1;
                Vector3 worldPosition = ConvertGridPositionToWorldPosition(adjustedRow, adjustedColumn);

                if (penguinGameObjects.ContainsKey(agentId))
                {
                    // Si el pingüino ya existe, moverlo a la nueva posición
                    GameObject penguin = penguinGameObjects[agentId];
                    penguin.transform.position = worldPosition;
                    Debug.Log($"Moviendo pingüino {agentId} a posición {worldPosition}");
                }
                else
                {
                    // Si el pingüino no existe, instanciarlo
                    GameObject penguin = Instantiate(penguinPrefab, worldPosition, Quaternion.identity, boardParent);
                    penguin.name = $"Penguin_{agentId}";
                    penguinGameObjects.Add(agentId, penguin);
                    Debug.Log($"Creando pingüino {agentId} en posición {worldPosition}");
                }
            }
        }

        // Procesar paredes destruidas
        if (currentGameState.destroyed_walls != null)
        {
            var destroyedWallsStep = currentGameState.destroyed_walls.FirstOrDefault(dw => dw.step == step);
            if (destroyedWallsStep != null && destroyedWallsStep.data != null)
            {
                foreach (var destroyedWall in destroyedWallsStep.data)
                {
                    int[] cellPosition = destroyedWall.cell;
                    string direction = destroyedWall.direction;

                    // Obtener las coordenadas de la celda
                    int fila = cellPosition[0];
                    int columna = cellPosition[1];

                    // Generar la clave de la pared utilizando las mismas variables que al crearla
                    string wallKey = GetWallKey(fila, columna, direction);

                    Debug.Log($"[DESTRUCCIÓN PARED] wallKey: {wallKey}, fila: {fila}, columna: {columna}, dirección: {direction}");

                    if (wallGameObjects.ContainsKey(wallKey))
                    {
                        // Eliminar la pared del diccionario y destruir el GameObject
                        GameObject wall = wallGameObjects[wallKey];
                        Destroy(wall);
                        wallGameObjects.Remove(wallKey);
                        Debug.Log($"Pared destruida en celda ({fila}, {columna}) dirección {direction}");
                    }
                    else
                    {
                        Debug.LogWarning($"No se encontró la pared en celda ({fila}, {columna}) dirección {direction} para destruir.");
                    }
                }
            }
        }

        // Procesar puertas destruidas
        if (currentGameState.destroyed_doors != null)
        {
            var destroyedDoorsStep = currentGameState.destroyed_doors.FirstOrDefault(dd => dd.step == step);
            if (destroyedDoorsStep != null && destroyedDoorsStep.data != null)
            {
                foreach (var destroyedDoor in destroyedDoorsStep.data)
                {
                    int[] cell1 = destroyedDoor.cell1;
                    int[] cell2 = destroyedDoor.cell2;
                    string direction = GetDirectionBetweenCells(cell1, cell2);

                    int fila = cell1[0];
                    int columna = cell1[1];

                    string doorKey = GetWallKey(fila, columna, direction);

                    Debug.Log($"[DESTRUCCIÓN PUERTA] doorKey: {doorKey}, fila: {fila}, columna: {columna}, dirección: {direction}");

                    if (wallGameObjects.ContainsKey(doorKey))
                    {
                        // Eliminar la puerta del diccionario y destruir el GameObject
                        GameObject door = wallGameObjects[doorKey];
                        Destroy(door);
                        wallGameObjects.Remove(doorKey);
                        Debug.Log($"Puerta destruida entre celdas ({cell1[0]}, {cell1[1]}) y ({cell2[0]}, {cell2[1]}) dirección {direction}");
                    }
                    else
                    {
                        Debug.LogWarning($"No se encontró la puerta entre celdas ({cell1[0]}, {cell1[1]}) y ({cell2[0]}, {cell2[1]}) dirección {direction} para destruir.");
                    }
                }
            }
        }

        // Procesar puertas abiertas (también las destruimos)
        if (currentGameState.open_doors != null)
        {
            var openDoorsStep = currentGameState.open_doors.FirstOrDefault(od => od.step == step);
            if (openDoorsStep != null && openDoorsStep.data != null)
            {
                foreach (var doorPair in openDoorsStep.data)
                {
                    int[] cell1 = doorPair[0];
                    int[] cell2 = doorPair[1];

                    string direction = GetDirectionBetweenCells(cell1, cell2);

                    int fila = cell1[0];
                    int columna = cell1[1];

                    string doorKey = GetWallKey(fila, columna, direction);

                    Debug.Log($"[PUERTA ABIERTA] doorKey: {doorKey}, fila: {fila}, columna: {columna}, dirección: {direction}");

                    if (wallGameObjects.ContainsKey(doorKey))
                    {
                        // Eliminar la puerta del diccionario y destruir el GameObject
                        GameObject door = wallGameObjects[doorKey];
                        Destroy(door);
                        wallGameObjects.Remove(doorKey);
                        Debug.Log($"Puerta abierta y destruida entre celdas ({cell1[0]}, {cell1[1]}) y ({cell2[0]}, {cell2[1]}) dirección {direction}");
                    }
                    else
                    {
                        Debug.LogWarning($"No se encontró la puerta entre celdas ({cell1[0]}, {cell1[1]}) y ({cell2[0]}, {cell2[1]}) dirección {direction} para abrir/destruir.");
                    }
                }
            }
        }

        // Actualizar los marcadores de fuego
        UpdateFireMarkers(step);

        // Actualizar los marcadores de humo
        UpdateSmokeMarkers(step);

        // Actualizar los marcadores de POIs
        UpdatePoiMarkers(step);
    }

    void UpdateFireMarkers(int step)
    {
        if (currentGameState.fire_expansion != null)
        {
            var fireExpansionStep = currentGameState.fire_expansion.FirstOrDefault(f => f.step == step);
            if (fireExpansionStep != null && fireExpansionStep.data != null)
            {
                HashSet<string> newFirePositions = new HashSet<string>();

                foreach (var fireData in fireExpansionStep.data)
                {
                    int[] position = fireData.position;

                    // Restar 1 a cada coordenada y **intercambiarlas**
                    int adjustedRow = position[1] - 1;
                    int adjustedColumn = position[0] - 1;

                    // Crear una clave única para esta posición
                    string positionKey = $"{adjustedRow}_{adjustedColumn}";

                    newFirePositions.Add(positionKey);

                    // Si no existe ya un marcador de fuego en esta posición, instanciarlo
                    if (!currentFirePositions.Contains(positionKey))
                    {
                        Vector3 worldPosition = ConvertGridPositionToWorldPosition(adjustedRow, adjustedColumn);
                        PlaceFireMarkerAtPosition(worldPosition, positionKey);
                    }
                }

                // Encontrar marcadores de fuego que ya no existen y eliminarlos
                foreach (var positionKey in currentFirePositions)
                {
                    if (!newFirePositions.Contains(positionKey))
                    {
                        if (fireMarkers.ContainsKey(positionKey))
                        {
                            GameObject marker = fireMarkers[positionKey];
                            Destroy(marker);
                            fireMarkers.Remove(positionKey);
                            Debug.Log($"Marcador de fuego eliminado en posición {positionKey}");
                        }
                    }
                }

                // Actualizar el conjunto de posiciones actuales de fuego
                currentFirePositions = newFirePositions;
            }
        }
    }

    void UpdateSmokeMarkers(int step)
    {
        if (currentGameState.smoke_expansion != null)
        {
            var smokeExpansionStep = currentGameState.smoke_expansion.FirstOrDefault(s => s.step == step);
            if (smokeExpansionStep != null && smokeExpansionStep.data != null)
            {
                HashSet<string> newSmokePositions = new HashSet<string>();

                foreach (var smokeData in smokeExpansionStep.data)
                {
                    int[] position = smokeData.position;

                    // Restar 1 a cada coordenada y **intercambiarlas**
                    int adjustedRow = position[1] - 1;
                    int adjustedColumn = position[0] - 1;

                    // Crear una clave única para esta posición
                    string positionKey = $"{adjustedRow}_{adjustedColumn}";

                    newSmokePositions.Add(positionKey);

                    // Si no existe ya un marcador de humo en esta posición, instanciarlo
                    if (!currentSmokePositions.Contains(positionKey))
                    {
                        Vector3 worldPosition = ConvertGridPositionToWorldPosition(adjustedRow, adjustedColumn);
                        PlaceSmokeMarkerAtPosition(worldPosition, positionKey);
                    }
                }

                // Encontrar marcadores de humo que ya no existen y eliminarlos
                foreach (var positionKey in currentSmokePositions)
                {
                    if (!newSmokePositions.Contains(positionKey))
                    {
                        if (smokeMarkers.ContainsKey(positionKey))
                        {
                            GameObject marker = smokeMarkers[positionKey];
                            Destroy(marker);
                            smokeMarkers.Remove(positionKey);
                            Debug.Log($"Marcador de humo eliminado en posición {positionKey}");
                        }
                    }
                }

                // Actualizar el conjunto de posiciones actuales de humo
                currentSmokePositions = newSmokePositions;
            }
        }
    }

    void UpdatePoiMarkers(int step)
    {
        if (currentGameState.pois != null)
        {
            var poiStepData = currentGameState.pois.FirstOrDefault(p => p.step == step);
            if (poiStepData != null && poiStepData.data != null)
            {
                HashSet<string> newPoiPositions = new HashSet<string>();

                foreach (var poiData in poiStepData.data)
                {
                    int[] position = poiData.position;

                    // Restar 1 a cada coordenada y **intercambiarlas**
                    int adjustedRow = position[1] - 1;
                    int adjustedColumn = position[0] - 1;

                    // Crear una clave única para esta posición y tipo
                    string positionKey = $"{adjustedRow}_{adjustedColumn}_{poiData.type}";

                    newPoiPositions.Add(positionKey);

                    // Si no existe ya un marcador de POI en esta posición y tipo, instanciarlo
                    if (!currentPoiPositions.Contains(positionKey))
                    {
                        Vector3 worldPosition = ConvertGridPositionToWorldPosition(adjustedRow, adjustedColumn);
                        PlacePoiMarkerAtPosition(worldPosition, positionKey, poiData.type);
                    }
                }

                // Encontrar marcadores de POIs que ya no existen y eliminarlos
                foreach (var positionKey in currentPoiPositions)
                {
                    if (!newPoiPositions.Contains(positionKey))
                    {
                        if (poiMarkers.ContainsKey(positionKey))
                        {
                            GameObject marker = poiMarkers[positionKey];
                            Destroy(marker);
                            poiMarkers.Remove(positionKey);
                            Debug.Log($"Marcador de POI eliminado en posición {positionKey}");
                        }
                    }
                }

                // Actualizar el conjunto de posiciones actuales de POIs
                currentPoiPositions = newPoiPositions;
            }
            else
            {
                // Si no hay datos para este paso, eliminar todos los marcadores de POIs existentes
                foreach (var marker in poiMarkers.Values)
                {
                    Destroy(marker);
                }
                poiMarkers.Clear();
                currentPoiPositions.Clear();
            }
        }
    }

    void PlaceFireMarkerAtPosition(Vector3 worldPosition, string positionKey)
    {
        float markerHeight = 7.3f; // Altura del marcador de fuego
        Vector3 markerPosition = new Vector3(worldPosition.x, markerHeight, worldPosition.z);

        GameObject marker = Instantiate(fireMarkerPrefab, markerPosition, Quaternion.identity, boardParent);
        marker.name = $"FireMarker_{positionKey}";

        fireMarkers[positionKey] = marker;
        Debug.Log($"Marcador de fuego instanciado en: {markerPosition} para posición {positionKey}");
    }

    void PlaceSmokeMarkerAtPosition(Vector3 worldPosition, string positionKey)
    {
        float markerHeight = -2.5f; // Altura del marcador de humo
        Vector3 markerPosition = new Vector3(worldPosition.x, markerHeight, worldPosition.z);

        GameObject marker = Instantiate(smokeMarkerPrefab, markerPosition, Quaternion.identity, boardParent);
        marker.name = $"SmokeMarker_{positionKey}";

        smokeMarkers[positionKey] = marker;
        Debug.Log($"Marcador de humo instanciado en: {markerPosition} para posición {positionKey}");
    }

    void PlacePoiMarkerAtPosition(Vector3 worldPosition, string positionKey, string poiType)
    {
        float markerHeight = -1.624f; // Altura del marcador de POI
        Vector3 markerPosition = new Vector3(worldPosition.x, markerHeight, worldPosition.z);

        GameObject markerPrefab = null;
        if (poiType == "false_alarm")
        {
            markerPrefab = fMarkerPrefab;
        }
        else if (poiType == "victim")
        {
            markerPrefab = vMarkerPrefab;
        }
        else
        {
            Debug.LogWarning($"Tipo de POI desconocido: {poiType} en posición {positionKey}");
            return;
        }

        GameObject marker = Instantiate(markerPrefab, markerPosition, Quaternion.identity, boardParent);
        marker.name = $"PoiMarker_{positionKey}";

        poiMarkers[positionKey] = marker;
        Debug.Log($"Marcador de POI '{poiType}' instanciado en: {markerPosition} para posición {positionKey}");
    }

    // Método para convertir las coordenadas de la cuadrícula a posiciones en el mundo
    private Vector3 ConvertGridPositionToWorldPosition(int row, int column)
    {
        float x = column * cellSize;
        float z = -row * cellSize;
        float y = 0; // Ajusta la altura si es necesario
        return new Vector3(x, y, z);
    }

    public void SetMapData(MapData mapData)
    {
        this.currentGameState = mapData;
    }
}
