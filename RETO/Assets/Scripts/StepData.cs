using System;
using System.Collections.Generic;

using UnityEngine;

[System.Serializable]
public class StepData : MonoBehaviour
{
    // Hacer MapData pública para que otros scripts puedan acceder a ella
    public MapData mapData { get; private set; }

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
                string targetStr = agent.target != null ? $"({agent.target[0]}, {agent.target[1]})" : "null";
                Debug.Log($"Agent {agent.agent_id} at position ({agent.position[0]}, {agent.position[1]}) targeting {targetStr}");
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
[Serializable]
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
[Serializable]
public class AgentsStepData
{
    public int step;            // Número del paso
    public AgentDataStep[] data;    // Lista de agentes
}

[Serializable]
public class AgentDataStep
{
    public int agent_id;        // ID del agente
    public int carry_state;     // Estado de carga
    public int[] position;      // Posición actual [x, y]
    public int[] target;        // Objetivo actual [x, y] o null
}

// Subclase para expansión de fuego
[Serializable]
public class FireStepData
{
    public int step;            // Número del paso
    public FireData[] data;     // Lista de expansiones de fuego
}

[Serializable]
public class FireData
{
    public int[] position;      // Posición del fuego [x, y]
    public string state;        // Estado del fuego (e.g., "fire")
}

// Subclase para expansión de humo
[Serializable]
public class SmokeStepData
{
    public int step;            // Número del paso
    public SmokeData[] data;    // Lista de expansiones de humo
}

[Serializable]
public class SmokeData
{
    public int[] position;      // Posición del humo [x, y]
    public string state;        // Estado del humo (e.g., "smoke")
}

// Subclase para puntos de interés
[Serializable]
public class POIStepData
{
    public int step;            // Número del paso
    public POIData[] data;      // Lista de POIs
}

[Serializable]
public class POIData
{
    public int[] position;      // Posición del POI [x, y]
    public string type;         // Tipo de POI (e.g., "victim", "false_alarm")
}

// Subclase para conteos genéricos (muertes y vidas salvadas)
[Serializable]
public class StepCountData
{
    public int step;            // Número del paso
    public int count;           // Conteo
}

// Subclase para daño estructural
[Serializable]
public class StructuralDamageData
{
    public int step;            // Número del paso
    public int value;           // Daño estructural restante
}

// Subclase para puertas destruidas y abiertas
[Serializable]
public class GenericStepData
{
    public List<int[]> data;    // Lista de pares de celdas afectadas
    public int step;            // Número del paso
}

// Subclase para paredes destruidas
[Serializable]
public class DestroyedWallsStepData
{
    public int step;            // Número del paso
    public DestroyedWallData[] data; // Lista de paredes destruidas
}

[Serializable]
public class DestroyedWallData
{
    public int[] cell;          // Celda donde se destruyó la pared
    public string direction;    // Dirección de la pared destruida
    public int[] neighbor;      // Vecino afectado
}
