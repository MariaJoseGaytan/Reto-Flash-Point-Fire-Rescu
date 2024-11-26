using UnityEngine;

[System.Serializable]
public class StepData : MonoBehaviour
{
    // Clase principal que contendrá los datos deserializados del JSON
    private MapData mapData;

    // Método para procesar el JSON descargado
    public void ProcessStepData(string json)
    {
        // Deserializa el JSON en la clase MapData
        mapData = JsonUtility.FromJson<MapData>(json);

        // Verifica que se haya deserializado correctamente
        Debug.Log("Data processed successfully!");

        // Ejemplo: Imprimir datos de agentes del primer paso
        if (mapData.agents != null && mapData.agents.Length > 0)
        {
            foreach (var agent in mapData.agents[0].data)
            {
                Debug.Log($"Agent {agent.agent_id} at position ({agent.position[0]}, {agent.position[1]})");
            }
        }

        // Ejemplo: Imprimir daño estructural restante del paso 0
        if (mapData.structural_damage_left != null && mapData.structural_damage_left.Length > 0)
        {
            Debug.Log($"Structural damage left at step 0: {mapData.structural_damage_left[0].value}");
        }
    }
}

// Clase principal que modela el JSON completo
[System.Serializable]
public class MapData
{
    public AgentsStepData[] agents;                  // Datos de agentes por paso
    public FireStepData[] fire_expansion;            // Expansión del fuego
    public SmokeStepData[] smoke_expansion;          // Expansión del humo
    public POIStepData[] pois;                       // Puntos de interés (POIs)
    public StepCountData[] victims_dead;             // Muertes de víctimas
    public StepCountData[] agents_dead;              // Muertes de agentes
    public StepCountData[] saved_lifes;              // Vidas salvadas
    public StructuralDamageData[] structural_damage_left; // Daño estructural restante
    public GenericStepData[] destroyed_doors;        // Puertas destruidas
    public DestroyedWallsStepData[] destroyed_walls; // Paredes destruidas
    public GenericStepData[] open_doors;             // Puertas abiertas
}

// Subclase para agentes
[System.Serializable]
public class AgentsStepData
{
    public int step;            // Número del paso
    public AgentDataStep[] data;    // Lista de agentes
}

[System.Serializable]
public class AgentDataStep
{
    public int agent_id;        // ID del agente
    public int carry_state;     // Estado de carga
    public int[] position;      // Posición actual [x, y]
    public int[] target;        // Objetivo actual [x, y] o null
}

// Subclase para expansión de fuego
[System.Serializable]
public class FireStepData
{
    public int step;            // Número del paso
    public FireData[] data;     // Lista de expansiones de fuego
}

[System.Serializable]
public class FireData
{
    public int[] position;      // Posición del fuego [x, y]
    public string state;        // Estado del fuego (e.g., "fire")
}

// Subclase para expansión de humo
[System.Serializable]
public class SmokeStepData
{
    public int step;            // Número del paso
    public SmokeData[] data;    // Lista de expansiones de humo
}

[System.Serializable]
public class SmokeData
{
    public int[] position;      // Posición del humo [x, y]
    public string state;        // Estado del humo (e.g., "smoke")
}

// Subclase para puntos de interés
[System.Serializable]
public class POIStepData
{
    public int step;            // Número del paso
    public POIData[] data;      // Lista de POIs
}

[System.Serializable]
public class POIData
{
    public int[] position;      // Posición del POI [x, y]
    public string type;         // Tipo de POI (e.g., "victim", "false_alarm")
}

// Subclase para conteos genéricos (muertes y vidas salvadas)
[System.Serializable]
public class StepCountData
{
    public int step;            // Número del paso
    public int count;           // Conteo
}

// Subclase para daño estructural
[System.Serializable]
public class StructuralDamageData
{
    public int step;            // Número del paso
    public int value;           // Daño estructural restante
}

// Subclase para puertas destruidas y abiertas
[System.Serializable]
public class GenericStepData
{
    public int step;            // Número del paso
    public GenericData[] data;  // Lista de datos genéricos
}

[System.Serializable]
public class GenericData
{
    public int[] cell1;         // Primera celda afectada
    public int[] cell2;         // Segunda celda afectada (si aplica)
    public string direction;    // Dirección (e.g., "left", "right")
}

// Subclase para paredes destruidas
[System.Serializable]
public class DestroyedWallsStepData
{
    public int step;            // Número del paso
    public DestroyedWallData[] data; // Lista de paredes destruidas
}

[System.Serializable]
public class DestroyedWallData
{
    public int[] cell;          // Celda donde se destruyó la pared
    public string direction;    // Dirección de la pared destruida
    public int[] neighbor;      // Vecino afectado
}
