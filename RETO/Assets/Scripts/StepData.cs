using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json; // Asegúrate de incluir este using

[System.Serializable]
public class StepData : MonoBehaviour
{
    // Hacer MapData pública para que otros scripts puedan acceder a ella
    public MapData mapData { get; private set; }

    // Método para procesar el JSON descargado
    public void ProcessStepData(string json)
    {
        Debug.Log("Procesando JSON...");
        
        // Log del JSON recibido
        Debug.Log($"[StepData] JSON recibido: {json}");

        // Intentar deserializar usando Newtonsoft.Json
        try
        {
            mapData = JsonConvert.DeserializeObject<MapData>(json);
            Debug.Log("JSON deserializado correctamente con Newtonsoft.Json.");
            mapData.InitializeDictionaries(); // Inicializar diccionarios

            if (mapData.agents != null && mapData.agents.Length > 0)
            {
                Debug.Log($"Número de pasos cargados: {mapData.agents.Length}");
                foreach (var agent in mapData.agents[0].data)
                {
                    string targetStr = agent.target != null ? $"({agent.target[0]}, {agent.target[1]})" : "null";
                    Debug.Log($"Agente {agent.agent_id} en posición inicial ({agent.position[0]}, {agent.position[1]}) con objetivo {targetStr}");
                }
            }
            else
            {
                Debug.LogError("No se cargaron datos de agentes.");
            }

            // Mostrar los contadores
            Debug.Log("Mostrando datos de contadores:");

            // Daño Estructural
            if (mapData.structural_damage_left != null && mapData.structural_damage_left.Length > 0)
            {
                foreach (var damageData in mapData.structural_damage_left)
                {
                    Debug.Log($"Paso {damageData.step}: Daño estructural restante = {damageData.value}");
                }
            }
            else
            {
                Debug.Log("No se encontraron datos de daño estructural.");
            }

            // Víctimas Muertas
            if (mapData.victims_dead != null && mapData.victims_dead.Length > 0)
            {
                foreach (var victimsData in mapData.victims_dead)
                {
                    Debug.Log($"Paso {victimsData.step}: Víctimas muertas = {victimsData.count}");
                }
            }
            else
            {
                Debug.Log("No se encontraron datos de víctimas muertas.");
            }

            // Agentes Muertos
            if (mapData.agents_dead != null && mapData.agents_dead.Length > 0)
            {
                foreach (var agentsData in mapData.agents_dead)
                {
                    Debug.Log($"Paso {agentsData.step}: Agentes muertos = {agentsData.count}");
                }
            }
            else
            {
                Debug.Log("No se encontraron datos de agentes muertos.");
            }

            // Vidas Salvadas
            if (mapData.saved_lifes != null && mapData.saved_lifes.Length > 0)
            {
                foreach (var savedData in mapData.saved_lifes)
                {
                    Debug.Log($"Paso {savedData.step}: Vidas salvadas = {savedData.count}");
                }
            }
            else
            {
                Debug.Log("No se encontraron datos de vidas salvadas.");
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al deserializar con Newtonsoft.Json: {ex.Message}");
        }
    }
}


// Clase principal que modela el JSON completo
[Serializable]
public class MapData
{
    public AgentsStepData[] agents;                  
    public FireStepData[] fire_expansion;            
    public SmokeStepData[] smoke_expansion;          
    public POIStepData[] pois;                       
    public StepCountData[] victims_dead;             
    public StepCountData[] agents_dead;              
    public StepCountData[] saved_lifes;              
    public StructuralDamageData[] structural_damage_left;
    public DestroyedDoorsStepData[] destroyed_doors; 
    public DestroyedWallsStepData[] destroyed_walls; 
    public OpenDoorsStepData[] open_doors;           

    // Diccionarios para acceso rápido
    [NonSerialized] public Dictionary<int, StepCountData> savedLifesDict;
    [NonSerialized] public Dictionary<int, StepCountData> victimsDeadDict;
    [NonSerialized] public Dictionary<int, StepCountData> agentsDeadDict;
    [NonSerialized] public Dictionary<int, StructuralDamageData> structuralDamageDict;

    public void InitializeDictionaries()
    {
        savedLifesDict = new Dictionary<int, StepCountData>();
        if (saved_lifes != null)
        {
            foreach (var item in saved_lifes)
            {
                if (!savedLifesDict.ContainsKey(item.step))
                    savedLifesDict.Add(item.step, item);
            }
        }

        victimsDeadDict = new Dictionary<int, StepCountData>();
        if (victims_dead != null)
        {
            foreach (var item in victims_dead)
            {
                if (!victimsDeadDict.ContainsKey(item.step))
                    victimsDeadDict.Add(item.step, item);
            }
        }

        agentsDeadDict = new Dictionary<int, StepCountData>();
        if (agents_dead != null)
        {
            foreach (var item in agents_dead)
            {
                if (!agentsDeadDict.ContainsKey(item.step))
                    agentsDeadDict.Add(item.step, item);
            }
        }

        structuralDamageDict = new Dictionary<int, StructuralDamageData>();
        if (structural_damage_left != null)
        {
            foreach (var item in structural_damage_left)
            {
                if (!structuralDamageDict.ContainsKey(item.step))
                    structuralDamageDict.Add(item.step, item);
            }
        }
    }
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

// Subclase para puertas destruidas
[Serializable]
public class DestroyedDoorData
{
    public int[] cell1;        // Primera celda
    public int[] cell2;        // Segunda celda
    public string direction;   // Dirección de la puerta destruida
}

[Serializable]
public class DestroyedDoorsStepData
{
    public int step;                        // Número del paso
    public DestroyedDoorData[] data;        // Lista de puertas destruidas
}

// Subclase para paredes destruidas
[Serializable]
public class DestroyedWallData
{
    public int[] cell;          // Celda donde se destruyó la pared
    public string direction;    // Dirección de la pared destruida
    public int[] neighbor;      // Vecino afectado
}

[Serializable]
public class DestroyedWallsStepData
{
    public int step;                        // Número del paso
    public DestroyedWallData[] data;        // Lista de paredes destruidas
}

// Subclase para puertas abiertas
[Serializable]
public class OpenDoorsStepData
{
    public int step;                        // Número del paso
    public List<List<int[]>> data;          // Lista de pares de celdas abiertas
}
