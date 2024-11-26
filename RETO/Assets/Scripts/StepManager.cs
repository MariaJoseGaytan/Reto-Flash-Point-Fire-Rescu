using UnityEngine;

public class StepManager : MonoBehaviour
{
    // Referencia al StepData para acceder a los datos deserializados
    public StepData stepData;

    // Referencia al servidor para cargar los datos JSON
    public ServerManager serverManager;

    // Variable para rastrear el paso actual
    private int currentStep = 0;

    // Total de pasos disponibles
    private int totalSteps = 0;

    // Variables para almacenar los contadores
    private int structuralDamage = 0;
    private int rescuedPeople = 0;
    private int deadPeople = 0;
    private int deadAgents = 0;

    // Referencia a GameManager para actualizar el tablero
    public GameManager gameManager;

    // Variable pública para modificar manualmente el paso desde el Inspector
    public int debugStep = 0;

    void Start()
    {
        if (serverManager != null)
        {
            Debug.Log("[START] Cargando datos desde el servidor...");
            StartCoroutine(serverManager.GetGameState(OnGameStateReceived));
        }
        else
        {
            Debug.LogError("[START] ServerManager no está asignado en StepManager.");
        }
    }

    // Callback para recibir el JSON desde el servidor
    private void OnGameStateReceived(string json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            Debug.Log("[SERVER] Datos recibidos desde el servidor.");
            stepData.ProcessStepData(json); // Procesar y asignar el JSON a `StepData`

            if (stepData.mapData != null && stepData.mapData.agents != null)
            {
                totalSteps = stepData.mapData.agents.Length;
                Debug.Log($"[SERVER] Número total de pasos cargados: {totalSteps}");
                UpdateCounters();
                UpdateBoard();
            }
            else
            {
                Debug.LogError("[SERVER] El JSON procesado no contiene datos válidos.");
            }
        }
        else
        {
            Debug.LogError("[SERVER] No se recibieron datos válidos desde el servidor.");
        }
    }

    void Update()
    {
        // Sincronizar manualmente el paso con debugStep
        if (debugStep != currentStep)
        {
            Debug.Log($"[UPDATE] Cambio detectado en debugStep. debugStep: {debugStep}, currentStep: {currentStep}");
            currentStep = Mathf.Clamp(debugStep, 0, totalSteps - 1); // Asegura que debugStep esté en el rango válido
            Debug.Log($"[UPDATE] Paso actualizado manualmente a: {currentStep}");
            UpdateCounters();
            UpdateBoard();
        }
    }

    private int GetStructuralDamageForStep(int step)
    {
        if (stepData.mapData.structuralDamageDict != null && stepData.mapData.structuralDamageDict.ContainsKey(step))
        {
            int damage = stepData.mapData.structuralDamageDict[step].value;
            Debug.Log($"[DATA] Daño estructural encontrado para el paso {step}: {damage}");
            return damage;
        }
        Debug.LogWarning($"[DATA] No se encontró información de daño estructural para el paso {step}.");
        return 0;
    }

    private int GetSavedLivesForStep(int step)
    {
        if (stepData.mapData.savedLifesDict != null && stepData.mapData.savedLifesDict.ContainsKey(step))
        {
            int count = stepData.mapData.savedLifesDict[step].count;
            Debug.Log($"[DATA] Vidas salvadas encontradas para el paso {step}: {count}");
            return count;
        }
        Debug.LogWarning($"[DATA] No se encontró información de vidas salvadas para el paso {step}.");
        return 0;
    }

    private int GetVictimsDeadForStep(int step)
    {
        if (stepData.mapData.victimsDeadDict != null && stepData.mapData.victimsDeadDict.ContainsKey(step))
        {
            int count = stepData.mapData.victimsDeadDict[step].count;
            Debug.Log($"[DATA] Víctimas muertas encontradas para el paso {step}: {count}");
            return count;
        }
        Debug.LogWarning($"[DATA] No se encontró información de víctimas muertas para el paso {step}.");
        return 0;
    }

    private int GetAgentsDeadForStep(int step)
    {
        if (stepData.mapData.agentsDeadDict != null && stepData.mapData.agentsDeadDict.ContainsKey(step))
        {
            int count = stepData.mapData.agentsDeadDict[step].count;
            Debug.Log($"[DATA] Agentes muertos encontrados para el paso {step}: {count}");
            return count;
        }
        Debug.LogWarning($"[DATA] No se encontró información de agentes muertos para el paso {step}.");
        return 0;
    }

    void UpdateCounters()
    {
        Debug.Log($"[COUNTERS] Actualizando contadores para el paso {currentStep}.");

        structuralDamage = GetStructuralDamageForStep(currentStep);
        Debug.Log($"[COUNTERS] Daño estructural para el paso {currentStep}: {structuralDamage}");

        rescuedPeople = GetSavedLivesForStep(currentStep);
        Debug.Log($"[COUNTERS] Personas rescatadas para el paso {currentStep}: {rescuedPeople}");

        deadPeople = GetVictimsDeadForStep(currentStep);
        Debug.Log($"[COUNTERS] Personas muertas para el paso {currentStep}: {deadPeople}");

        deadAgents = GetAgentsDeadForStep(currentStep);
        Debug.Log($"[COUNTERS] Agentes muertos para el paso {currentStep}: {deadAgents}");
    }

    void UpdateBoard()
    {
        Debug.Log($"[BOARD] Actualizando tablero para el paso {currentStep}.");

        if (gameManager != null)
        {
            gameManager.UpdateBoardState(currentStep);
            Debug.Log($"[BOARD] Tablero actualizado para el paso {currentStep}.");
        }
        else
        {
            Debug.LogError("[BOARD] GameManager no está asignado en StepManager.");
        }
    }

    void OnGUI()
    {
        // Estilos de texto
        GUIStyle styleYellow = new GUIStyle();
        styleYellow.fontSize = 16;
        styleYellow.normal.textColor = Color.yellow;

        GUIStyle styleRed = new GUIStyle();
        styleRed.fontSize = 16;
        styleRed.normal.textColor = Color.red;

        GUIStyle styleWhite = new GUIStyle();
        styleWhite.fontSize = 16;
        styleWhite.normal.textColor = Color.white;

        // Estilo para el número de paso
        GUIStyle styleStep = new GUIStyle();
        styleStep.fontSize = 20;
        styleStep.normal.textColor = Color.cyan;
        styleStep.alignment = TextAnchor.MiddleCenter;

        // Fondo para los contadores
        Color originalColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f); // Semi-transparente
        GUI.Box(new Rect(10, 10, 300, 200), "");
        GUI.color = originalColor;

        // Mostrar los contadores
        GUI.Label(new Rect(20, 20, 280, 25), $"Daño Estructural: {structuralDamage}", styleWhite);
        GUI.Label(new Rect(20, 50, 280, 25), $"Personas Rescatadas: {rescuedPeople}", styleYellow);
        GUI.Label(new Rect(20, 80, 280, 25), $"Personas Muertas: {deadPeople}", styleRed);
        GUI.Label(new Rect(20, 110, 280, 25), $"Agentes Muertos: {deadAgents}", styleRed);

        // Mostrar el número de paso
        GUI.Label(new Rect(Screen.width / 2 - 100, 10, 200, 40), $"Paso: {currentStep}", styleStep);
    }
}
