
# The automated tests for the Dijkstra route algorithm

import time
import unittest

from route_service import dijkstra, generate_graph


class DijkstraTests(unittest.TestCase):

    def setUp(self):
        # Sample RPG map used by the tests.
        self.game_map = {
            "Village": [("Forest Path", 3), ("Mountain Path", 7)],
            "Forest Path": [("Village", 3), ("Cave", 4)],
            "Mountain Path": [("Village", 7), ("Cave", 2)],
            "Cave": [("Forest Path", 4), ("Mountain Path", 2)],
        }

    def test_1_shortest_route(self):
        route, cost = dijkstra(
            self.game_map, "Village", "Cave"
        )

        self.assertEqual(
            route,
            ["Village", "Forest Path", "Cave"]
        )
        self.assertEqual(cost, 7)

    def test_2_same_start_and_destination(self):
        route, cost = dijkstra(
            self.game_map, "Village", "Village"
        )

        self.assertEqual(route, ["Village"])
        self.assertEqual(cost, 0)

    def test_3_unreachable_destination(self):
        graph = {
            "Village": [],
            "Cave": []
        }

        route, cost = dijkstra(graph, "Village", "Cave")

        self.assertIsNone(route)
        self.assertEqual(cost, float("inf"))

    def test_4_invalid_destination(self):
        with self.assertRaises(ValueError):
            dijkstra(self.game_map, "Village", "Castle")

    def test_5_negative_route_cost(self):
        graph = {
            "Village": [("Cave", -5)],
            "Cave": []
        }

        with self.assertRaises(ValueError):
            dijkstra(graph, "Village", "Cave")

    def test_6_empty_graph(self):
        with self.assertRaises(ValueError):
            dijkstra({}, "Village", "Cave")

    def test_7_unknown_connection(self):
        graph = {
            "Village": [("Castle", 2)]
        }

        with self.assertRaises(ValueError):
            dijkstra(graph, "Village", "Village")

    def test_8_route_cost_matches_connections(self):
        route, total_cost = dijkstra(
            self.game_map, "Village", "Cave"
        )

        calculated_cost = 0

        for i in range(len(route) - 1):
            current = route[i]
            next_location = route[i + 1]

            connection_costs = [
                cost
                for neighbor, cost in self.game_map[current]
                if neighbor == next_location
            ]

            self.assertTrue(connection_costs)

            calculated_cost += min(connection_costs)

        self.assertEqual(calculated_cost, total_cost)

    def test_9_performance_smoke_check(self):
        graph = generate_graph(2000)

        start_time = time.perf_counter()

        route, cost = dijkstra(
            graph, "Location_0", "Location_1999"
        )

        elapsed_time = time.perf_counter() - start_time

        self.assertIsNotNone(route)
        self.assertEqual(route[0], "Location_0")
        self.assertEqual(route[-1], "Location_1999")
        self.assertGreaterEqual(cost, 0)

        # A limit to catch a slow execution
        self.assertLess(elapsed_time, 2.0)


if __name__ == "__main__":
    unittest.main(verbosity=2)
