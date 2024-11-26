using UnityEngine;

public class StepManager : MonoBehaviour
{
    // Referencia al StepData para acceder a los datos deserializados
    public StepData stepData;

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

    void Start()
    {
        if (stepData != null && stepData.mapData != null)
        {
            totalSteps = stepData.mapData.agents.Length; // Asumiendo que 'agents' define el número de pasos
            UpdateCounters();
            UpdateBoard();
        }
        else
        {
            Debug.LogError("StepData o MapData no está asignado o aún no se ha procesado.");
        }
    }

    void Update()
    {
        // Avanzar al siguiente paso presionando la tecla "N"
        if (Input.GetKeyDown(KeyCode.N))
        {
            AdvanceStep();
        }

        // Retroceder al paso anterior presionando la tecla "P"
        if (Input.GetKeyDown(KeyCode.P))
        {
            PreviousStep();
        }
    }

    // Método para avanzar al siguiente paso
    public void AdvanceStep()
    {
        if (currentStep < totalSteps - 1)
        {
            currentStep++;
            UpdateCounters();
            UpdateBoard();
            Debug.Log($"Paso avanzado a: {currentStep}");
        }
        else
        {
            Debug.Log("Has alcanzado el último paso.");
        }
    }

    // Método para retroceder al paso anterior
    public void PreviousStep()
    {
        if (currentStep > 0)
        {
            currentStep--;
            UpdateCounters();
            UpdateBoard();
            Debug.Log($"Paso retrocedido a: {currentStep}");
        }
        else
        {
            Debug.Log("Estás en el primer paso.");
        }
    }

    // Método para actualizar los contadores con los datos del paso actual
    void UpdateCounters()
    {
        if (stepData.mapData == null)
        {
            Debug.LogError("MapData no está disponible.");
            return;
        }

        // Actualizar el daño estructural restante
        if (stepData.mapData.structural_damage_left != null && stepData.mapData.structural_damage_left.Length > currentStep)
        {
            structuralDamage = stepData.mapData.structural_damage_left[currentStep].value;
        }
        else
        {
            structuralDamage = 0;
        }

        // Actualizar personas rescatadas
        if (stepData.mapData.saved_lifes != null && stepData.mapData.saved_lifes.Length > currentStep)
        {
            rescuedPeople = stepData.mapData.saved_lifes[currentStep].count;
        }
        else
        {
            rescuedPeople = 0;
        }

        // Actualizar personas muertas
        if (stepData.mapData.victims_dead != null && stepData.mapData.victims_dead.Length > currentStep)
        {
            deadPeople = stepData.mapData.victims_dead[currentStep].count;
        }
        else
        {
            deadPeople = 0;
        }

        // Actualizar agentes muertos
        if (stepData.mapData.agents_dead != null && stepData.mapData.agents_dead.Length > currentStep)
        {
            deadAgents = stepData.mapData.agents_dead[currentStep].count;
        }
        else
        {
            deadAgents = 0;
        }
    }

    // Método para actualizar el tablero según el paso actual
    void UpdateBoard()
    {
        if (gameManager != null)
        {
            gameManager.UpdateBoardState(currentStep);
        }
        else
        {
            Debug.LogError("GameManager no está asignado en StepManager.");
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

        // Botón para avanzar al siguiente paso
        if (GUI.Button(new Rect(20, 180, 140, 30), "Siguiente Paso"))
        {
            AdvanceStep();
        }

        // Botón para retroceder al paso anterior
        if (GUI.Button(new Rect(180, 180, 140, 30), "Paso Anterior"))
        {
            PreviousStep();
        }
    }
}
