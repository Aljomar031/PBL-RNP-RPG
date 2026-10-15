# The Dijkstra Algorithm
# It should give the shortest route

import heapq
import random


def dijkstra(graph, start, destination):
    # Check whether the starting point and destination exist.
    if start not in graph or destination not in graph:
        raise ValueError("Invalid starting or destination location")

    # Check for invalid connections and negative route costs.
    for node in graph:
        for neighbor, cost in graph[node]:
            if neighbor not in graph:
                raise ValueError("Edge points to an unknown location")

            if cost < 0:
                raise ValueError("Negative route costs are not allowed")

    # Prepare the shortest distance and previous location records.
    distances = {node: float("inf") for node in graph}
    previous = {node: None for node in graph}

    distances[start] = 0
    priority_queue = [(0, start)]

    # Search for the shortest route.
    while priority_queue:
        current_distance, current_node = heapq.heappop(priority_queue)

        if current_distance > distances[current_node]:
            continue

        if current_node == destination:
            break

        for neighbor, cost in graph[current_node]:
            new_distance = current_distance + cost

            if new_distance < distances[neighbor]:
                distances[neighbor] = new_distance
                previous[neighbor] = current_node

                heapq.heappush(
                    priority_queue,
                    (new_distance, neighbor)
                )

    # Return no route if the destination cannot be reached.
    if distances[destination] == float("inf"):
        return None, float("inf")

    # Reconstruct the route from destination to starting point.
    route = []
    current = destination

    while current is not None:
        route.append(current)
        current = previous[current]

    route.reverse()

    return route, distances[destination]


def generate_graph(number_of_nodes, extra_edges=2, seed=42):
    # To generate a connected graph for testing and benchmarking

    if number_of_nodes < 1:
        raise ValueError("The graph must contain at least one location")

    rng = random.Random(seed)

    graph = {
        f"Location_{i}": []
        for i in range(number_of_nodes)
    }

    # Connect each location to the next one.
    for i in range(number_of_nodes - 1):
        first = f"Location_{i}"
        second = f"Location_{i + 1}"
        cost = rng.randint(1, 10)

        graph[first].append((second, cost))
        graph[second].append((first, cost))

    # Add extra connections to create alternative routes.
    for _ in range(extra_edges * number_of_nodes):
        first_index = rng.randrange(number_of_nodes)
        second_index = rng.randrange(number_of_nodes)

        if first_index == second_index:
            continue

        first = f"Location_{first_index}"
        second = f"Location_{second_index}"
        cost = rng.randint(1, 10)

        graph[first].append((second, cost))
        graph[second].append((first, cost))

    return graph


# The sample test map
if __name__ == "__main__":
    game_map = {
        "Village": [("Forest Path", 3), ("Mountain Path", 7)],
        "Forest Path": [("Village", 3), ("Cave", 4)],
        "Mountain Path": [("Village", 7), ("Cave", 2)],
        "Cave": [("Forest Path", 4), ("Mountain Path", 2)],
    }

    route, cost = dijkstra(game_map, "Village", "Cave")

    print("Recommended route:", route)
    print("Total route cost:", cost)
