# Puffle Rescue

## Introducción
El objetivo del proyecto fue simular una solución para el juego de mesa Flash Point Fire Rescue mediante la implementación de un modelo multiagente. Este desafío no solo implicó desarrollar agentes inteligentes capaces de completar las misiones del juego, sino también representar su comportamiento de manera visual, empleando Unity y utilizando assets personalizados para mejorar la comprensión y presentación de la simulación.

## Trasfondo
En la tranquila isla de Club Penguin, una nueva cueva ha sido descubierta, atrayendo la curiosidad de los puffles, quienes rápidamente la adoptaron como un refugio. Sin embargo, la calma no duró mucho. Una fuerte tormenta golpeó la isla, causando que la cueva comenzará a colapsar. Los muros mostraban grietas, y pequeños derrumbes anunciaban que el techo podría ceder en cualquier momento, poniendo en peligro la vida de los puffles que se encontraban dentro.

Ante esta situación, un valiente equipo de pingüinos rescatistas fue desplegado. Su misión: entrar en la cueva, localizar y rescatar a todos los puffles posibles antes de que el colapso se volviera total. Los rescatistas deben enfrentarse no solo a las limitaciones de tiempo y la inminente destrucción de la cueva, sino también a obstáculos como paredes, puertas bloqueadas y la propagación de la nieve, que amenaza con agravar aún más la situación.

El escenario combina elementos de estrategia, riesgo y trabajo en equipo, planteando un desafío emocionante que busca poner a prueba la eficiencia y coordinación de los agentes rescatistas en un entorno crítico.

## Estrategia
### Estrategia desarrollada
Nuestra estrategia tiene como objetivo maximizar el rescate de puffles en peligro mientras gestionamos los riesgos presentes en el entorno dinámico de la simulación. Para lograrlo, hemos implementado un enfoque basado en:

Priorización: Los potenciales puffles en peligro (POI) son nuestra prioridad máxima, mientras que la nieve (fuego) se maneja de forma secundaria, dependiendo de su impacto.
Asignación inteligente de agentes: Cada agente se asigna automáticamente a la tarea más cercana disponible (dos agentes no pueden tener la misma tarea), evitando duplicidad de esfuerzos.

Optimización de recursos: Los agentes optimizan el uso de puntos de acción en movimientos, apertura de puertas y manejo de obstáculos, asegurando que sus acciones sean efectivas (por ejemplo cuando hay pocos fuegos en el tablero, un agente puede guardar todas sus acciones para que su siguiente turno pueda salvar a una víctima o limpiar nieve con mayor facilidad).

Adaptación dinámica: Los objetivos y las rutas de los agentes se recalculan constantemente en función de los cambios en el entorno, como nuevos incendios o aparición de víctimas.

### Eficiencia
Al ejecutar nuestro modelo 500 veces, logramos 241 victorias, lo que representa una eficiencia del 48.2%, un resultado sólido dado el nivel de complejidad de la simulación y las condiciones dinámicas del entorno. Para detalles más específicos, incluyendo gráficos e interpretaciones, consulta la sección Resultados.

## Visualización
Se anexan los videos de la visualización en Unity, donde se puede observar el progreso paso a paso de la simulación. En cada paso se representa:

El movimiento de los 6 agentes.

El daño estructural restante (hielo derretido), cuyo límite determina la derrota si llega a cero.

Eventos clave como:

Rescate de un puffle.

Muerte de un puffle.

Lesión de un pingüino.

En el video adjunto, la simulación muestra una victoria, lograda al rescatar exitosamente 7 puffles antes de que murieran 4 o se agotara el límite de hielo derretido. El progreso del rescate se detalla a continuación:

Paso 2: 1 puffle rescatado.

Paso 3: 1 puffle rescatado.

Paso 5: 1 puffle rescatado.

Paso 6: 1 puffle rescatado.

Paso 8: 1 puffle rescatado.

Paso 9: 1 puffle rescatado.

Paso 10: 1 puffle rescatado.

Sumando un total de 7 puffles, lo cual asegura la victoria. Además durante la simulación, se puede observar cómo el hielo se derrite progresivamente en cada paso, pero nunca llega a cero, permitiendo el rescate de todos los puffles a tiempo.

Nota: Los videos de la simulación están disponibles tanto en YouTube como en Google Drive, y se adjuntan como parte del reporte.

Link youtube: https://www.youtube.com/watch?v=Qi5CxNtYd7Q

Link drive: https://drive.google.com/file/d/1qd20dKbh0uhOatsiydsaUoDhnHmTWYcl/view?usp=sharing

## Resultados
![Figure_1](https://github.com/user-attachments/assets/07756b7d-bac7-47c7-bc40-9afb373ba11c)
Esta gráfica es resultado de simular el juego de Puffle Rescue 500 veces. Como podemos observar obtuvimos 241 victorias de 500 simulaciones lo que representa un porcentaje de victoria de un 48.2%. Esto significa que con la estrategia que implementamos, los agentes son capaces de salvar a 7 o más víctimas casi un 50% de las veces.

![Figure_2](https://github.com/user-attachments/assets/381a922a-148b-41f0-bce2-b56f51aaec50)
Mientras tanto en esta gráfica podemos ver que el 100% de las veces que se perdió fue a causa de daño estructural de la cueva. Esto se debe a distintos factores, ya que en nuestra estrategia los agentes pueden dañar las paredes para crear una ruta mucho más rápida a una salida, con el riesgo de llegar al límite de daño del juego (y no tienen una condición que impida este comportamiento cuando el daño total es alto). Además en nuestra generación de POI, la probabilidad de generar una falsa alarma es de 50% algo mayor a la que sería realmente si se jugara con las fichas. Esto reduce la probabilidad de que se muera una víctima, pero aumenta la probabilidad de recibir más daño ya que los agentes están más tiempo buscando falsas alarmas.

![Figure_3](https://github.com/user-attachments/assets/7d932adb-fb95-416b-81f7-3ad0c52c64a1)
Finalmente, en esta gráfica se observa que, de las 259 derrotas, los resultados de rescate fueron los siguientes:

5 simulaciones lograron salvar 2 puffles.
30 simulaciones salvaron 3 puffles.
65 simulaciones, aproximadamente, salvaron 4 puffles.
76 simulaciones, alrededor, lograron rescatar 5 puffles.
83 simulaciones, aproximadamente, salvaron 6 puffles.

Esto demuestra que, aunque se perdió en estas ocasiones, en la mayoría de las derrotas los agentes lograron salvar entre 4 y 6 puffles, lo que los acerca significativamente al objetivo de ganar el juego. 
Cabe resaltar que, en ninguna de las derrotas, se logró rescatar menos de 2 puffles, lo que demuestra que, incluso en los casos menos exitosos, los agentes siempre lograron salvar al menos 2 puffles, destacando un desempeño consistente a pesar de no alcanzar la victoria.

## Posibles mejoras
### Generación de POI basada en una lista estructurada
En lugar de asignar puntos de interés (POI) con una probabilidad del 50/50, implementar una lista predeterminada que permita una distribución más controlada y estratégica de las víctimas y falsas alarmas, optimizando la dificultad y consistencia de la simulación.

### Estandarización de índices (x, y)
Unificar el manejo de los índices del tablero, asegurando que todos se manejen en la misma base (base 0 o base 1). Esto reduciría confusiones y errores en el procesamiento de coordenadas, especialmente al trabajar con múltiples sistemas como el servidor y la simulación.

### Mejora en la toma de decisiones al romper paredes
Limitar la acción de los agentes para que eviten romper paredes cuando el daño estructural del mapa es alto, priorizando alternativas como el uso de puertas. Esto permitiría una mejor gestión del riesgo, especialmente en situaciones críticas donde el daño acumulado podría causar una derrota.

## Conclusiones
### Logros Generales
Implementamos un modelo funcional que integra agentes inteligentes con un sistema cliente-servidor, demostrando la capacidad del equipo para manejar sistemas complejos en un entorno dinámico.

Conseguimos una eficiencia del 48.2%, lo que implica que en casi la mitad de las simulaciones nuestros agentes logran rescatar al menos 7 puffles antes de que la cueva colapse o se pierdan 4 vidas.

A pesar de las limitaciones, el modelo mostró un comportamiento estratégico sólido, incluyendo rutas óptimas para agentes, priorización de puffles y gestión dinámica del entorno.

### Aprendizajes Generales
Integración cliente-servidor: Falta de compatibilidad inicial complicó paredes y puertas; alineación temprana es clave.

Planeación: Falta de detalle en etapas críticas llevó a retrabajos; es esencial planificar mejor.

Gestión del tiempo: Falta de ajustes por mala organización; planificar tiempos para mejorar resultados.

Estos errores nos dejaron valiosas lecciones para futuros proyectos, como la necesidad de una planificación más detallada, la identificación temprana de posibles inconsistencias y la gestión eficiente del tiempo para abordar ajustes clave.

### Conclusión Majo
El desarrollo de este proyecto ha sido una experiencia sumamente enriquecedora que me ha permitido profundizar en la modelación de sistemas multiagentes y en la implementación de algoritmos de búsqueda como Dijkstra. La simulación del juego "Flash Point Fire Rescue" nos presentó diversos desafíos que requirieron soluciones creativas y un trabajo en equipo sólido.

Estoy especialmente satisfecha con la eficiencia alcanzada en nuestras simulaciones. Ya que obtener una victoria del 48.2% demuestra que, a pesar de las limitaciones y obstáculos, fuimos capaces de diseñar agentes inteligentes que toman decisiones estratégicas efectivas. La integración con Unity para la visualización también añadió un valor significativo al proyecto, permitiéndonos apreciar de manera tangible el comportamiento de los agentes y el impacto de nuestras decisiones de diseño.
Este proyecto me enseñó la importancia de una planificación detallada y de la necesidad de abordar las posibles inconsistencias desde etapas tempranas. Los desafíos enfrentados en la integración cliente-servidor y en la gestión del tiempo fueron lecciones valiosas que contribuirán a mi crecimiento profesional. En resumen, considero que los logros obtenidos reflejan el esfuerzo y la dedicación invertidos, y estoy orgullosa del resultado final.

### Conclusión Maxime
Participar en este proyecto fue muy divertido (aunque algo tedioso) y me sirvió para aplicar conocimientos que adquirimos a lo largo del semestre en un entorno práctico y dinámico. La tarea de simular "Flash Point Fire Rescue" mediante agentes inteligentes fue un gran desafío técnico sino también estratégico, requiriendo una comprensión profunda de algoritmos, optimización y toma de decisiones en tiempo real.

Los resultados obtenidos, (≈50% de victoria), indican que nuestra estrategia fué adecuada. La implementación de estrategias como la priorización de objetivos y la adaptación dinámica de los agentes demostró ser efectiva en escenarios complejos y cambiantes. Además, la experiencia nos resaltó la importancia de la coherencia en los sistemas, especialmente al trabajar con múltiples componentes como servidores y plataformas de visualización.
Las dificultades encontradas, particularmente en la integración y alineación de datos entre sistemas, nos recordaron la necesidad de una planificación meticulosa y una comunicación clara dentro del equipo. Esto es algo que tendremos muy en cuenta en los proyectos futuros.






