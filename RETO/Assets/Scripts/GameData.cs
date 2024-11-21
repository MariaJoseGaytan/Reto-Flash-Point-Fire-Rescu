using System.Collections.Generic;

// Clase que representa una celda en el tablero
public class CellState
{
    public List<int> cell_position; // [x, y]
    public List<AgentData> agents; // Lista de agentes en la celda
}

// Clase que representa un agente en el tablero
public class AgentData
{
    public string type; // CellAgent, MarkerAgent, FireMarkerAgent, etc.
    public string unique_id; // ID único del agente
    public List<int> position; // [x, y]
    public string walls; // Para CellAgent, cadena de 4 dígitos indicando paredes
    public string marker_type; // Para MarkerAgent: "v" o "f"
    public List<int> connected_cell; // Para DoorAgent: posición de la celda conectada
     public bool is_entrance;         // Indica si la celda es una entrada (true) o no (false)
    
}
