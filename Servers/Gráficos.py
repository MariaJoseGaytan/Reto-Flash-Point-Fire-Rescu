# Librerías
from mesa import Agent, Model
from mesa.space import MultiGrid
from mesa.time import RandomActivation

import matplotlib.pyplot as plt
from matplotlib.colors import ListedColormap
import numpy as np
import json
import random

# Clase Cell que nos ayuda a guardar información
class Cell():
    def __init__(self, x, y, wall):
        self.pos = (x, y)
        self.wall_health = [0, 0, 0, 0]
        # lectura de muros del txt 0100 (arriba,izq,abajo,derecha)
        # pared arriba
        if wall[0] == '1':
            self.up = True
            self.wall_health[0] = 2
        else:
            self.up = False
        # pared izq
        if wall[1] == '1':
            self.left = True
            self.wall_health[1] = 2
        else:
            self.left = False
        # pared abajo
        if wall[2] == '1':
            self.down = True
            self.wall_health[2] = 2
        else:
            self.down = False
        # pared derecha
        if wall[3] == '1':
            self.right = True
            self.wall_health[3] = 2
        else:
            self.right = False

        self.poi = 0  # usa 1 si es falsa alarma, 2 si es una víctima
        self.fire = 0  # 1 si es humo, 2 si es fuego

        # Arreglo con la posición de la casilla donde se conecta con puerta
        self.door = []

        self.entrance = False  # es una entrada?

        self.inside_agents = 0  # num de agentes en la celda

# Clase Agente
class PenguinAgent(Agent):
    def __init__(self, unique_id, model, target=None):
        super().__init__(unique_id, model)
        self.target = target
        self.action_points = 4
        self.lleva_puffle = 1  # 1 nada, 2 es víctima esto es para que el num de acciones de movimientos se multiplique y no tenga que haber un if
        self.path = []

    def step(self):
        if self.target is not None:
            # Calculate the path to the target and total steps
            self.path, total_steps = self.dijkstra(self.pos, self.target.pos)

            # Move along the path as long as there are action points and the target hasn't been reached
            while self.action_points >= 1 and self.pos != self.target.pos and self.path:
                cost = self.clear_path(self.pos, self.path[0])
                if self.action_points < cost:
                    break  # si los puntos de acción exceden el costo de limpiar el camino no avanza
                self.action_points -= cost
                self.model.grid.move_agent(self, self.path.pop(0))

        # Al llegar al objetivo
        if self.target is not None and self.pos == self.target.pos:
            # si es una víctima (2)
            if self.target.poi == 2:
                self.lleva_puffle = 2  # El agente lleva al puffle
                closest_exit = self.find_closest_exit()
                self.model.cells[self.pos[0]][self.pos[1]].poi = 0
                self.remove_from_interest_points(self.target)
                self.path, _ = self.dijkstra(self.pos, closest_exit.pos)
                self.target = closest_exit

            # si el objetivo es una salida y lleva un puffle
            elif self.model.cells[self.pos[0]][self.pos[1]] in self.model.outside and self.lleva_puffle == 2:
                self.lleva_puffle = 1  # Drop the victim
                self.model.saved_lifes += 1
                self.target = None

            # Case: Target is a false alarm
            elif self.target.poi == 1:
                self.model.cells[self.pos[0]][self.pos[1]].poi = 0
                self.remove_from_interest_points(self.target)
                self.target = None

            # Case: Target is a fire
            elif self.target.fire == 2:
                self.target = None

        # Replenish action points
        self.calculate_action_points()

    def find_closest_exit(self):
        """Find the closest exit to the agent's current position."""
        closest_exit = None
        min_distance = float('inf')
        for exit_cell in self.model.outside:
            distance = self.dijkstra(self.pos, exit_cell.pos)[1]
            if distance < min_distance:
                min_distance = distance
                closest_exit = exit_cell
        return closest_exit

    def remove_from_interest_points(self, point):
        """Remove a point from the interest points list if it exists."""
        if point in self.model.interest_points:
            self.model.interest_points.remove(point)

    def dijkstra(self, start, end):
        # Crear un mapa para almacenar información de cada celda (pasos y celda previa en el camino)
        dijkstra_map = {}
        path = []

        # Inicializar el mapa de Dijkstra para todas las celdas del grid
        for x in range(self.model.grid.height):
            for y in range(self.model.grid.width):
                dijkstra_map[(y, x)] = {"previous_cell": None, "steps": None}

        # Verificar que los puntos de inicio y fin sean válidos y distintos
        if start in dijkstra_map and end in dijkstra_map and start != end:
            # Inicializar el punto de partida
            dijkstra_map[start]["steps"] = 0
            dijkstra_map[start]["previous_cell"] = start
            queue = [start]  # Cola para explorar celdas

            # Recorrer las celdas en la cola mientras haya celdas por explorar
            while queue:
                current_cell = queue.pop(0)  # Obtener la celda actual de la cola

                # Obtener los vecinos de la celda actual
                neighbors = self.model.grid.get_neighborhood(current_cell, moore=False)

                for neighbor in neighbors:
                    # Verificar si el vecino está dentro de los límites del grid
                    if 0 <= neighbor[0] < self.model.grid.width and 0 <= neighbor[1] < self.model.grid.height:
                        # Calcular el costo de moverse hacia el vecino
                        cost_to_neighbor = self.calculate_steps(current_cell, neighbor)

                        # Si el vecino no ha sido visitado, calcular su costo total
                        if dijkstra_map[neighbor]["steps"] is None and cost_to_neighbor is not None:
                            dijkstra_map[neighbor]["steps"] = dijkstra_map[current_cell]["steps"] + cost_to_neighbor
                            dijkstra_map[neighbor]["previous_cell"] = current_cell
                            queue.append(neighbor)

                        # Si ya fue visitado, actualizar si el nuevo costo es menor
                        elif dijkstra_map[neighbor]["steps"] > dijkstra_map[current_cell]["steps"] + cost_to_neighbor:
                            dijkstra_map[neighbor]["steps"] = dijkstra_map[current_cell]["steps"] + cost_to_neighbor
                            dijkstra_map[neighbor]["previous_cell"] = current_cell
                            queue.append(neighbor)

            # Reconstruir el camino desde el punto final hasta el inicial
            current_position = end
            while current_position != start and dijkstra_map[current_position]["previous_cell"] is not None:
                path.insert(0, current_position)  # Agregar al inicio de la lista
                current_position = dijkstra_map[current_position]["previous_cell"]

            # Retornar el camino y el costo total
            total_cost = dijkstra_map[end]["steps"] if dijkstra_map[end]["steps"] is not None else 0
            return path, total_cost
        else:
            # Si los puntos son inválidos o son iguales, devolver el punto final y un costo 0
            return [end], 0

    def calculate_steps(self, start, end):
        # Inicializar el costo de puntos de acción
        action_points_cost = 0

        # Verificar que las coordenadas del destino estén dentro del grid
        if 0 <= end[0] < len(self.model.cells) and 0 <= end[1] < len(self.model.cells[0]):
            # Determinar la dirección del movimiento
            if start[0] < end[0]:  # Moverse hacia abajo
                action_points_cost += self.calculate_vertical_cost(start, end, "down")
            elif start[0] > end[0]:  # Moverse hacia arriba
                action_points_cost += self.calculate_vertical_cost(start, end, "up")
            elif start[1] < end[1]:  # Moverse hacia la derecha
                action_points_cost += self.calculate_horizontal_cost(start, end, "right")
            elif start[1] > end[1]:  # Moverse hacia la izquierda
                action_points_cost += self.calculate_horizontal_cost(start, end, "left")

            # Si la celda destino tiene fuego, añadir un costo adicional
            if self.model.cells[end[0]][end[1]].fire == 2:
                action_points_cost += 1

            # Añadir un costo adicional por llevar una víctima
            action_points_cost += 1 * self.lleva_puffle

        return action_points_cost

    def calculate_vertical_cost(self, start, end, direction):
        """Calcula el costo de moverse en dirección vertical (arriba o abajo)."""
        cost = 0
        if direction == "down":  # Moverse hacia abajo
            if self.model.cells[end[0]][end[1]].up or self.model.cells[start[0]][start[1]].down:
                # Verificar si es necesario romper una pared o usar una puerta
                cost += 4.1 if end not in self.model.cells[start[0]][start[1]].door else 1
        elif direction == "up":  # Moverse hacia arriba
            if self.model.cells[end[0]][end[1]].down or self.model.cells[start[0]][start[1]].up:
                cost += 4.1 if end not in self.model.cells[start[0]][start[1]].door else 1
        return cost

    def calculate_horizontal_cost(self, start, end, direction):
        """Calcula el costo de moverse en dirección horizontal (izquierda o derecha)."""
        cost = 0
        if direction == "right":  # Moverse hacia la derecha
            if self.model.cells[end[0]][end[1]].left or self.model.cells[start[0]][start[1]].right:
                # Verificar si es necesario romper una pared o usar una puerta
                cost += 4.1 if end not in self.model.cells[start[0]][start[1]].door else 1
        elif direction == "left":  # Moverse hacia la izquierda
            if self.model.cells[end[0]][end[1]].right or self.model.cells[start[0]][start[1]].left:
                cost += 4.1 if end not in self.model.cells[start[0]][start[1]].door else 1
        return cost

    def calculate_action_points(self):
        if self.action_points + 4 > 8:
            self.action_points = 8
        else:
            self.action_points += 4

    def remove_wall(self, end):
        if self.pos[0] < end[0]:
            direction = "up"
            self.model.cells[end[0]][end[1]].up = False
            self.model.cells[self.pos[0]][self.pos[1]].down = False
        elif self.pos[0] > end[0]:
            direction = "down"
            self.model.cells[end[0]][end[1]].down = False
            self.model.cells[self.pos[0]][self.pos[1]].up = False
        elif self.pos[1] < end[1]:
            direction = "left"
            self.model.cells[end[0]][end[1]].left = False
            self.model.cells[self.pos[0]][self.pos[1]].right = False
        elif self.pos[1] > end[1]:
            direction = "right"
            self.model.cells[end[0]][end[1]].right = False
            self.model.cells[self.pos[0]][self.pos[1]].left = False

        self.model.structural_damage_left -= 2

        # Registrar la pared destruida
        wall_info = {
            "cell": self.pos,
            "neighbor": end,
            "direction": direction
        }
        self.model.destroyed_walls.append(wall_info)

        print(f"Pared removida por explosión en {self.pos} hacia {direction} con vecino {end}")

    def clear_path(self, start, end):
        """Calcula el costo de despejar el camino de start a end, incluyendo puertas, paredes y fuego."""
        action_points_cost = 0

        # Verificar que las coordenadas del destino estén dentro de los límites
        if not (0 <= end[0] < len(self.model.cells) and 0 <= end[1] < len(self.model.cells[0])):
            return action_points_cost

        # Determinar la dirección del movimiento
        if start[0] < end[0]:  # Moverse hacia abajo
            action_points_cost += self.calculate_cost_and_clear(start, end, "down", "up")
        elif start[0] > end[0]:  # Moverse hacia arriba
            action_points_cost += self.calculate_cost_and_clear(start, end, "up", "down")
        elif start[1] < end[1]:  # Moverse hacia la derecha
            action_points_cost += self.calculate_cost_and_clear(start, end, "right", "left")
        elif start[1] > end[1]:  # Moverse hacia la izquierda
            action_points_cost += self.calculate_cost_and_clear(start, end, "left", "right")

        # Agregar costo adicional si el destino tiene fuego
        if self.model.cells[end[0]][end[1]].fire == 2:
            action_points_cost += 1
            if self.model.cells[end[0]][end[1]] in self.model.fire_points:
                self.model.fire_points.remove(self.model.cells[end[0]][end[1]])
            self.model.cells[end[0]][end[1]].fire = 0

        # Añadir el costo de llevar una víctima
        action_points_cost += 1 * self.lleva_puffle

        return action_points_cost

    def calculate_cost_and_clear(self, start, end, direction, opposite_direction):
        """
        Calcula el costo de moverse en una dirección específica y realiza las operaciones necesarias
        para despejar puertas o paredes.
        """
        cost = 0
        current_cell = self.model.cells[start[0]][start[1]]
        target_cell = self.model.cells[end[0]][end[1]]

        # Si hay una pared o puerta en el camino
        if getattr(target_cell, opposite_direction) or getattr(current_cell, direction):
            # Verificar si es una puerta o una pared
            if end not in current_cell.door:
                # Es una pared, cuesta más romperla
                cost += 4
                self.remove_wall(end)
            else:
                # Es una puerta, cuesta menos cruzarla
                cost += 1
                current_cell.door.remove(end)
                target_cell.door.remove(start)
                self.remove_wall(end)

        return cost

# Clase Model
class MapModel(Model):
    def __init__(self, num_agents):
        super().__init__()
        self.steps = 0
        self.smokes = []
        self.saved_lifes = 0
        self.dead_lifes = 0
        self.dead_agents = 0
        self.width = 10
        self.height = 8
        self.structural_damage_left = 24
        self.num_agents = num_agents
        self.grid = MultiGrid(self.height, self.width, False)
        self.cells, self.outside = self.read_map_data()
        self.inside = [cell for row in self.cells for cell in row if cell not in self.outside]
        self.put_entrance_doors()
        self.interest_points = [cell for row in self.cells for cell in row if cell.poi != 0]
        self.fire_points = [cell for row in self.cells for cell in row if cell.fire == 2]
        self.schedule = RandomActivation(self)
        self.running = True

        # Inicializar listas para rastrear destrucciones
        self.destroyed_doors = []
        self.destroyed_walls = []

        for i in range(self.num_agents):
            agent = PenguinAgent(i, self)
            self.schedule.add(agent)
            self.grid.place_agent(agent, (0, 0))
            self.position_agent(agent)

    # Esta función posiciona a los agentes en una celda aleatoria fuera de la casa
    def position_agent(self, agent):
        random_pos = self.random.choice(self.outside)
        self.grid.move_agent(agent, random_pos.pos)

    # Esta función lee el archivo de texto de entrada y coloca la información en una matriz y un arreglo (cells y outside)
    def read_map_data(self):
        # Simulación simplificada para evitar dependencias de archivos
        # Reemplaza este bloque con tu lógica de lectura de 'final.txt'
        walls = ["0100"] * (8 * 6)  # Ejemplo: todas las celdas con pared arriba
        alerts = [("3", "4", "v"), ("5", "6", "f"), ("7", "8", "v")]
        fires = [("2", "3"), ("4", "5"), ("6", "7"), ("8", "9"), ("1", "2"),
                 ("3", "4"), ("5", "6"), ("7", "8"), ("9", "0"), ("0", "1")]
        doors = [(("1", "1"), ("1", "2")), (("2", "2"), ("2", "3")), (("3", "3"), ("3", "4")),
                 (("4", "4"), ("4", "5")), (("5", "5"), ("5", "6")), (("6", "6"), ("6", "7")),
                 (("7", "7"), ("7", "8")), (("8", "8"), ("8", "9"))]
        exits = [("1", "0"), ("6", "9"), ("7", "0"), ("7", "9")]

        cells = []
        for i in range(6):
            for j in range(8):
                cell_walls = walls[0]
                del walls[0]

                c = Cell(i + 1, j + 1, cell_walls)
                cells.append(c)

                if (str(i + 1), str(j + 1), 'v') in alerts:
                    c.poi = 2
                elif (str(i + 1), str(j + 1), 'f') in alerts:
                    c.poi = 1

                if (str(i + 1), str(j + 1)) in fires:
                    c.fire = 2

                for d in doors:
                    if (str(i + 1), str(j + 1)) == d[0]:
                        c.door.append((int(d[1][0]), int(d[1][1])))
                    elif (str(i + 1), str(j + 1)) == d[1]:
                        c.door.append((int(d[0][0]), int(d[0][1])))

                if (str(i + 1), str(j + 1)) in exits:
                    c.entrance = True

        # Agregar celdas exteriores
        new_cells = [
            Cell(0, 0, "0000"),
            Cell(0, 1, "0010"),
            Cell(0, 2, "0010"),
            Cell(0, 3, "0010"),
            Cell(0, 4, "0010"),
            Cell(0, 5, "0010"),
            Cell(0, 6, "0010"),
            Cell(0, 7, "0010"),
            Cell(0, 8, "0010"),
            Cell(0, 9, "0000"),
        ]
        outside = new_cells.copy()
        cells = new_cells + cells
        for i in range(1, 7):
            c = Cell(i, 0, "0001")
            cells.insert(i * 10, c)
            outside.append(c)
            c = Cell(i, 9, "0100")
            cells.insert((i * 10) + 9, c)
            outside.append(c)
        new_cells = [
            Cell(7, 0, "0000"),
            Cell(7, 1, "1000"),
            Cell(7, 2, "1000"),
            Cell(7, 3, "1000"),
            Cell(7, 4, "1000"),
            Cell(7, 5, "1000"),
            Cell(7, 6, "1000"),
            Cell(7, 7, "1000"),
            Cell(7, 8, "1000"),
            Cell(7, 9, "0000"),
        ]
        outside = outside + new_cells
        cells = cells + new_cells
        map_grid = [[None for _ in range(10)] for _ in range(8)]
        for cell in cells:
            y, x = cell.pos
            if 0 <= y < 8 and 0 <= x < 10:
                map_grid[y][x] = cell
        return map_grid, outside

    # Esta función indica las puertas de entrada en la matriz cells
    def put_entrance_doors(self):
        for row in self.cells:
            for cell in row:
                if cell.entrance:
                    if cell.pos[0] == 1:
                        cell.up = False
                        self.cells[cell.pos[0] - 1][cell.pos[1]].down = False
                    elif cell.pos[0] == 6:
                        cell.down = False
                        self.cells[cell.pos[0] + 1][cell.pos[1]].up = False
                    elif cell.pos[1] == 1:
                        cell.left = False
                        self.cells[cell.pos[0]][cell.pos[1] - 1].right = False
                    elif cell.pos[1] == 8:
                        cell.right = False
                        self.cells[cell.pos[0]][cell.pos[1] + 1].left = False

    # Esta función indica en qué celda cae nieve y qué pasa de acuerdo al estado de la celda
    def snowfall(self):
        flat_cells = [cell for row in self.cells for cell in row]
        random_cell = self.random.choice(list(filter(lambda cell: cell not in self.outside, flat_cells)))
        if random_cell.fire == 0:
            random_cell.fire = 1
            print(f"Snowfall en: {random_cell.pos}, snow hill generado")
        elif random_cell.fire == 1:
            random_cell.fire = 2
            print(f"Snowfall en: {random_cell.pos}, snow mountain generado")
        elif random_cell.fire == 2:
            for i in range(4):
                self.avalanche_dir(i, random_cell)
            print(f"Snowfall en: {random_cell.pos}, avalanche generada")

    # Esta función modela el comportamiento de las avalanchas
    def avalanche_dir(self, direction, cell):
        """
        Maneja la propagación de una avalancha en la dirección especificada, afectando paredes, puertas y propagando fuego.
        """
        if cell not in self.inside:
            return  # No hace nada si la celda está fuera de la estructura.

        # Mapeo de direcciones con ajustes de coordenadas y atributos de pared.
        directions = {
            0: {"neighbor_offset": (-1, 0), "wall_index": 0, "opposite_wall_index": 2, "wall_attr": "up", "opposite_wall_attr": "down"},
            1: {"neighbor_offset": (0, -1), "wall_index": 1, "opposite_wall_index": 3, "wall_attr": "left", "opposite_wall_attr": "right"},
            2: {"neighbor_offset": (1, 0), "wall_index": 2, "opposite_wall_index": 0, "wall_attr": "down", "opposite_wall_attr": "up"},
            3: {"neighbor_offset": (0, 1), "wall_index": 3, "opposite_wall_index": 1, "wall_attr": "right", "opposite_wall_attr": "left"},
        }

        # Obtener los datos de la dirección.
        dir_data = directions[direction]
        neighbor_offset = dir_data["neighbor_offset"]
        wall_index = dir_data["wall_index"]
        opposite_wall_index = dir_data["opposite_wall_index"]
        wall_attr = dir_data["wall_attr"]
        opposite_wall_attr = dir_data["opposite_wall_attr"]

        # Identificar la celda vecina.
        neighbor_pos = (cell.pos[0] + neighbor_offset[0], cell.pos[1] + neighbor_offset[1])
        if not (0 <= neighbor_pos[0] < self.height and 0 <= neighbor_pos[1] < self.width):
            return  # Fuera de los límites
        neighbor_cell = self.cells[neighbor_pos[0]][neighbor_pos[1]]

        if cell.fire == 2:  # Caso: La celda está en fuego.
            if neighbor_cell.pos in cell.door:
                self.remove_door(cell, neighbor_cell, direction)
            elif getattr(cell, wall_attr):  # Caso: Hay una pared.
                # Reducir la salud de la pared en ambas celdas.
                cell.wall_health[wall_index] -= 1
                neighbor_cell.wall_health[opposite_wall_index] -= 1

                # Si la pared colapsa, eliminarla y actualizar el daño estructural.
                if cell.wall_health[wall_index] == 0:
                    setattr(cell, wall_attr, False)
                    setattr(neighbor_cell, opposite_wall_attr, False)
                    self.structural_damage_left -= 2
                    self.remove_wall(cell.pos, neighbor_pos)
                    print(f"Pared removida por avalancha en {cell.pos}")
            else:
                # Propagación recursiva si no hay obstáculos.
                self.avalanche_dir(direction, neighbor_cell)
        else:
            # Si no hay fuego en la celda, asignarlo.
            self.assign_fire(cell)

    def remove_door(self, cell1, cell2, direction):
        self.cells[cell1.pos[0]][cell1.pos[1]].door.remove(cell2.pos)
        self.cells[cell2.pos[0]][cell2.pos[1]].door.remove(cell1.pos)
        if direction == 0:
            self.cells[cell1.pos[0]][cell1.pos[1]].up = False
            self.cells[cell2.pos[0]][cell2.pos[1]].down = False
        elif direction == 1:
            self.cells[cell1.pos[0]][cell1.pos[1]].left = False
            self.cells[cell2.pos[0]][cell2.pos[1]].right = False
        elif direction == 2:
            self.cells[cell1.pos[0]][cell1.pos[1]].down = False
            self.cells[cell2.pos[0]][cell2.pos[1]].up = False
        elif direction == 3:
            self.cells[cell1.pos[0]][cell1.pos[1]].right = False
            self.cells[cell2.pos[0]][cell2.pos[1]].left = False

        # Registrar la puerta destruida
        door_info = {
            "cell1": cell1.pos,
            "cell2": cell2.pos,
            "direction": ["up", "left", "down", "right"][direction]
        }
        self.destroyed_doors.append(door_info)

        print(f"Puerta removida por explosión en {cell1.pos} y {cell2.pos} en dirección {door_info['direction']}")

    # Esta función asigna el estado de fuego a una celda
    def assign_fire(self, cell):
        self.cells[cell.pos[0]][cell.pos[1]].fire = 2

    # Esta función verifica si los humos tienen fuegos alrededor para convertirse en fuegos
    def check_smokes(self):
        """
        Returns:
            bool: False si se encontró humo que se convirtió en fuego; True si no hubo cambios.
        """
        # Crear una copia de la lista de humos para evitar problemas durante la iteración.
        for smoke in self.smokes.copy():
            x, y = smoke.pos  # Posición del humo.

            # Verificar fuego en las celdas adyacentes (arriba, abajo, izquierda, derecha).
            if (
                x > 0 and  # Arriba
                self.cells[x - 1][y].fire == 2 and
                not smoke.up
            ):
                self._convert_smoke_to_fire(smoke)
                return False
            elif (
                x < self.height - 1 and  # Abajo
                self.cells[x + 1][y].fire == 2 and
                not smoke.down
            ):
                self._convert_smoke_to_fire(smoke)
                return False
            elif (
                y > 0 and  # Izquierda
                self.cells[x][y - 1].fire == 2 and
                not smoke.left
            ):
                self._convert_smoke_to_fire(smoke)
                return False
            elif (
                y < self.width - 1 and  # Derecha
                self.cells[x][y + 1].fire == 2 and
                not smoke.right
            ):
                self._convert_smoke_to_fire(smoke)
                return False

        # Si no hubo cambios, retornar True.
        return True

    def _convert_smoke_to_fire(self, smoke):
        smoke.fire = 2  # Convertir el humo en fuego.
        self.fire_points.append(smoke)  # Agregar la celda a los puntos de fuego.
        self.smokes.remove(smoke)  # Eliminar la celda de la lista de humos.

    # Esta función determina el final de la simulación
    def end_sim(self):
        if self.structural_damage_left <= 0:
            print("Derrota: Demasiado daño a la cueva")
            self.running = False
        elif self.dead_lifes >= 4:
            print("Derrota: Demasiadas pérdidas")
            self.running = False
        elif self.saved_lifes >= 7:
            print("Victoria: Puffles rescatados")
            self.running = False

    # Esta función hace un gráfico del estado del mapa y los agentes
    def plot_grid(self):
        grid = np.zeros((self.grid.width, self.grid.height))
        for cell in self.grid.coord_iter():
            contents = cell[0]
            y, x = cell[1]
            if contents:
                grid[y][x] = 1  # negro donde están los agentes
            elif self.cells[y][x].fire == 2:
                grid[y][x] = 2  # rojo donde hay fuego
            elif self.cells[y][x].fire == 1:
                grid[y][x] = 3  # gris donde hay humo
            elif self.cells[y][x].poi != 0:
                grid[y][x] = 4  # azul donde hay puntos de interés

        cmap = ListedColormap(['white', 'black', 'red', 'gray', 'blue'])
        plt.imshow(grid, cmap=cmap)
        plt.title(f"Paso: {self.steps}")
        plt.show()
        self.steps += 1

    def get_open_doors(self):
        """
        Retorna una lista de puertas abiertas actuales.
        Cada puerta se representa como una tupla de posiciones ordenadas.
        """
        doors = set()
        for row in self.cells:
            for cell in row:
                for door_pos in cell.door:
                    # Ordenar las posiciones para evitar duplicados (puerta A-B y B-A)
                    door = tuple(sorted([cell.pos, door_pos]))
                    doors.add(door)
        return list(doors)

    # Esta función genera un nuevo punto de interés
    def generate_new_interest_point(self):
        flat_cells = [cell for row in self.cells for cell in row]
        random_cell = self.random.choice(list(filter(lambda cell: cell not in self.outside
                                                    and cell not in self.interest_points
                                                    and cell not in self.smokes
                                                    and cell not in self.fire_points, flat_cells)))
        random_cell.poi = self.random.randint(1, 2)
        return random_cell

    # Esta función asigna los puntos de interés o fuegos a cada agente
    def assign_points(self):
        while len(self.interest_points) < 3:
            self.interest_points.append(self.generate_new_interest_point())
        interest_points = self.interest_points.copy()
        for agent in self.schedule.agents:
            if agent.target in interest_points:
                interest_points.remove(agent.target)
        for interest_point in interest_points:
            closest_agent = None
            min_steps = 100
            for agent in self.schedule.agents:
                if agent.target is None:
                    steps = agent.dijkstra(agent.pos, interest_point.pos)[1]
                    if steps < min_steps:
                        min_steps = steps
                        closest_agent = agent.unique_id
            if closest_agent is not None:
                for agent in self.schedule.agents:
                    if agent.unique_id == closest_agent:
                        agent.target = interest_point
                        break
        if len(self.fire_points) > 3:
            left_fire_points = self.fire_points.copy()
            for agent in self.schedule.agents:
                if agent.target is None and len(left_fire_points) > 0:
                    closest_fire = None
                    min_steps_fire = 100
                    for fire in left_fire_points:
                        steps = agent.dijkstra(agent.pos, fire.pos)[1]
                        if steps < min_steps_fire:
                            min_steps_fire = steps
                            closest_fire = fire
                    if closest_fire is not None:
                        agent.target = closest_fire
                        left_fire_points.remove(closest_fire)
        else:
            for agent in self.schedule.agents:
                if agent.target is None:
                    agent.target = agent.target  # Mantener como está

    # Esta función indica las acciones que se toman en cada paso de la simulación
    def step_simulation(self):
        self.end_sim()
        print(f"Vidas salvadas: {self.saved_lifes}, Muertes: {self.dead_lifes}, Daño Estructural Restante: {self.structural_damage_left}, Agentes muertos: {self.dead_agents}")
        if self.running:
            if self.steps > 0:
                self.snowfall()
            self.smokes = [cell for row in self.cells for cell in row if cell.fire == 1]
            self.fire_points = [cell for row in self.cells for cell in row if cell.fire == 2]
            smokes_checked = False
            while not smokes_checked:
                smokes_checked = self.check_smokes()
            for agent in list(self.schedule.agents):  # Convertir a lista para iterar de manera segura
                current_cell = self.cells[agent.pos[0]][agent.pos[1]]
                if current_cell in self.fire_points:
                    if agent.lleva_puffle == 2:
                        agent.lleva_puffle = 1
                        self.dead_lifes += 1
                        self.dead_agents += 1
                        self.position_agent(agent)
                        print(f"Agente {agent.unique_id} quemado mientras rescataba en {agent.pos}")
                    else:
                        self.dead_agents += 1
                        self.position_agent(agent)
                        print(f"Agente {agent.unique_id} ha muerto quemado en {agent.pos}")
            if len(self.interest_points) > 0:
                for interest_point in self.interest_points.copy():  # Usar .copy() para evitar errores durante la iteración
                    if interest_point in self.fire_points:
                        if interest_point.poi == 2:
                            self.dead_lifes += 1
                            print(f"Puffle revelado y perdido en {interest_point.pos} por chispa")
                        elif interest_point.poi == 1:
                            print(f"Falsa alarma revelada en {interest_point.pos} por fuego")
                        self.interest_points.remove(interest_point)
            self.assign_points()
            # self.plot_grid()
            for punto_interes in self.interest_points:
                print(f"Puntos de interés en: {punto_interes.pos}")
            print("\n")
            for punto_fuego in self.fire_points:
                print(f"Fuego en: {punto_fuego.pos}")
            print("\n")
            for agent in self.schedule.agents:
                target_pos = list(agent.target.pos) if agent.target else None
                print(f"Agente: {agent.unique_id} Posición: {agent.pos} Yendo a: {target_pos}")
            print("\n")
            self.schedule.step()

# Función para ejecutar una simulación y devolver el resultado
def run_single_simulation():
    model = MapModel(6)
    
    while model.running:
        model.step_simulation()
        model.steps += 1

    # Determinar si fue victoria o derrota
    if model.saved_lifes >= 7:
        return {"resultado": "victoria", "motivo": None, "rescates": model.saved_lifes}
    else:
        # Determinar el motivo de la derrota
        if model.structural_damage_left <= 0:
            motivo = "Daño estructural"
        elif model.dead_lifes >= 4:
            motivo = "Demasiadas muertes"
        else:
            motivo = "Otro"

        return {
            "resultado": "derrota",
            "motivo": motivo,
            "rescates": model.saved_lifes
        }

# Ejecutar múltiples simulaciones y recolectar resultados detallados
def run_multiple_simulations(num_runs=500):
    resultados = {
        "victoria": 0,
        "derrota": 0,
        "motivos_derrota": {
            "Daño estructural": 0,
            "Demasiadas muertes": 0,
            "Otro": 0
        },
        "rescates_derrota": []
    }

    for i in range(1, num_runs + 1):
        print(f"--- Simulación {i} ---")
        resultado = run_single_simulation()
        
        if resultado["resultado"] == "victoria":
            resultados["victoria"] += 1
        else:
            resultados["derrota"] += 1
            resultados["motivos_derrota"][resultado["motivo"]] += 1
            resultados["rescates_derrota"].append(resultado["rescates"])
        
        print(f"Resultado de la simulación {i}: {resultado['resultado']}\n")
    
    print(f"Total de simulaciones ejecutadas: {num_runs}")
    print(f"Victorias: {resultados['victoria']}")
    print(f"Derrotas: {resultados['derrota']}")
    
    # Guardar los resultados en un archivo JSON (opcional)
    with open('resultados_simulaciones_detallados.json', 'w') as json_file:
        json.dump(resultados, json_file, indent=4)
    
    return resultados

# Función para generar las gráficas
def generar_graficas(resultados):
    # 1. Gráfica de Victorias vs Derrotas
    etiquetas = ['Victorias', 'Derrotas']
    conteos = [resultados['victoria'], resultados['derrota']]
    
    plt.figure(figsize=(10, 6))
    barras = plt.bar(etiquetas, conteos, color=['green', 'red'])
    plt.title('Victorias vs Derrotas')
    plt.ylabel('Cantidad')
    plt.xlabel('Resultado')
    plt.ylim(0, max(conteos) + max(conteos)*0.1)
    
    # Añadir etiquetas de conteo encima de las barras
    for barra in barras:
        altura = barra.get_height()
        plt.text(barra.get_x() + barra.get_width()/2., altura + max(conteos)*0.01,
                 '%d' % int(altura), ha='center', va='bottom')
    
    plt.show()
    
    # 2. Gráfica de Motivos de Derrota
    motivos = list(resultados['motivos_derrota'].keys())
    motivos_conteos = list(resultados['motivos_derrota'].values())
    
    plt.figure(figsize=(8, 8))
    colores = ['orange', 'purple', 'grey']
    plt.pie(motivos_conteos, labels=motivos, autopct='%1.1f%%', colors=colores, startangle=140)
    plt.title('Motivos de Derrota')
    plt.axis('equal')  # Para que el pastel sea circular
    plt.show()
    
    # 3. Gráfica de Víctimas Rescatadas en Derrotas
    if resultados['derrota'] > 0 and len(resultados['rescates_derrota']) > 0:
        plt.figure(figsize=(10, 6))
        bins = range(0, max(resultados['rescates_derrota']) + 2)  # +2 para incluir el último bin
        plt.hist(resultados['rescates_derrota'], bins=bins, color='blue', edgecolor='black', align='left')
        plt.title('Número de Víctimas Rescatadas en Derrotas')
        plt.xlabel('Número de Víctimas Rescatadas')
        plt.ylabel('Frecuencia')
        plt.xticks(bins)
        plt.show()
    else:
        print("No hubo derrotas con rescates para mostrar la gráfica de víctimas rescatadas.")

# Ejecutar las simulaciones y generar las gráficas
if __name__ == '__main__':
    # Ejecutar las simulaciones y obtener los resultados detallados
    resultados_finales = run_multiple_simulations(100)  # Cambia a 100 o el número que desees
    
    # Generar las gráficas basadas en los resultados
    generar_graficas(resultados_finales)
