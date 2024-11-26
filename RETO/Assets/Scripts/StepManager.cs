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

    // Variable para indicar si los datos han sido cargados
    private bool dataLoaded = false;

    // Tiempo acumulado desde el último incremento de paso
    private float timeSinceLastStep = 0f;

    // Intervalo de tiempo entre pasos (10 segundos)
    public float stepInterval = 10f;

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

                // Asegurarnos de que el currentStep inicial está en el rango válido
                currentStep = Mathf.Clamp(currentStep, 0, totalSteps - 1);

                // Actualizar los contadores y el tablero para el paso inicial
                UpdateCounters();
                UpdateBoard();

                // Indicar que los datos han sido cargados
                dataLoaded = true;
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
    // Verifica que los datos estén cargados antes de continuar
    if (!dataLoaded)
    {
        Debug.Log("[UPDATE] Los datos no han sido cargados.");
        return;
    }

    // Incrementar el tiempo transcurrido
    timeSinceLastStep += Time.deltaTime;

    // Mostrar log para depuración
    Debug.Log($"[UPDATE] Tiempo acumulado: {timeSinceLastStep}/{stepInterval}, Paso actual: {currentStep}");

    // Detectar y loggear cada segundo acumulado
    int secondsElapsed = Mathf.FloorToInt(timeSinceLastStep);
    if (secondsElapsed > 0 && timeSinceLastStep < stepInterval)
    {
        Debug.Log($"[TIMER] Han transcurrido {secondsElapsed} segundos del intervalo de {stepInterval}.");
    }

    // Verificar si el tiempo acumulado ha alcanzado el intervalo de paso
    if (timeSinceLastStep >= stepInterval)
    {
        // Reiniciar el temporizador
        timeSinceLastStep = 0f;

        // Incrementar el paso actual
        currentStep++;

        // Verificar que el paso actual no exceda el total de pasos disponibles
        if (currentStep >= totalSteps)
        {
            Debug.Log("[UPDATE] Se ha alcanzado el último paso. Deteniendo el avance.");
            currentStep = totalSteps - 1;
            return; // Salir para evitar avanzar más
        }

        // Actualizar contadores y tablero para el nuevo paso
        Debug.Log($"[UPDATE] Cambiando al paso: {currentStep}");
        UpdateCounters(); // Actualizar los contadores
        UpdateBoard();    // Actualizar el tablero
    }
}



    private int GetStructuralDamageForStep(int step)
    {
        if (stepData.mapData.structuralDamageDict != null && stepData.mapData.structuralDamageDict.ContainsKey(step))
        {
            return stepData.mapData.structuralDamageDict[step].value;
        }
        return 0; // Devuelve 0 si no hay datos para el paso
    }

    private int GetSavedLivesForStep(int step)
    {
        if (stepData.mapData.savedLifesDict != null && stepData.mapData.savedLifesDict.ContainsKey(step))
        {
            return stepData.mapData.savedLifesDict[step].count;
        }
        return 0; // Devuelve 0 si no hay datos para el paso
    }

    private int GetVictimsDeadForStep(int step)
    {
        if (stepData.mapData.victimsDeadDict != null && stepData.mapData.victimsDeadDict.ContainsKey(step))
        {
            return stepData.mapData.victimsDeadDict[step].count;
        }
        return 0; // Devuelve 0 si no hay datos para el paso
    }

    private int GetAgentsDeadForStep(int step)
    {
        if (stepData.mapData.agentsDeadDict != null && stepData.mapData.agentsDeadDict.ContainsKey(step))
        {
            return stepData.mapData.agentsDeadDict[step].count;
        }
        return 0; // Devuelve 0 si no hay datos para el paso
    }

    void UpdateCounters()
    {
        Debug.Log($"[COUNTERS] Actualizando contadores para el paso {currentStep}.");

        structuralDamage = GetStructuralDamageForStep(currentStep);
        rescuedPeople = GetSavedLivesForStep(currentStep);
        deadPeople = GetVictimsDeadForStep(currentStep);
        deadAgents = GetAgentsDeadForStep(currentStep);

        Debug.Log($"[COUNTERS] Paso {currentStep}: Daño estructural = {structuralDamage}, Personas rescatadas = {rescuedPeople}, Personas muertas = {deadPeople}, Agentes muertos = {deadAgents}");
    }

    void UpdateBoard()
    {
        Debug.Log($"[BOARD] Actualizando tablero para el paso {currentStep}.");

        if (gameManager != null)
        {
            gameManager.UpdateBoardState(currentStep);
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
        GUI.Label(new Rect(20, 20, 280, 25), $"Hielo Descongelado: {structuralDamage}", styleWhite);
        GUI.Label(new Rect(20, 50, 280, 25), $"Puffles Rescatadas: {rescuedPeople}", styleYellow);
        GUI.Label(new Rect(20, 80, 280, 25), $"Puffles Muertas: {deadPeople}", styleRed);
        GUI.Label(new Rect(20, 110, 280, 25), $"Pingüinos Muertos: {deadAgents}", styleRed);

        // Mostrar el número de paso
        GUI.Label(new Rect(Screen.width / 2 - 100, 10, 200, 40), $"Paso: {currentStep}", styleStep);
    }
}
