using System;
using System.Collections.Generic;
using System.Linq;
public class RoomResult
{
    public int Distance { get; set; }
    public List<string> Path { get; set; }
    public string Exit { get; set; }
}
class WeightedList
{
    Dictionary<string, List<(string neighbor, int weight)>> graph = new();

    public void AddEdge(string from, string to, int weight)
    {
        if (!graph.ContainsKey(from))
        {
            graph[from] = new List<(string neighbor, int weight)>();
        }

        if (!graph.ContainsKey(to))
        {
            graph[to] = new List<(string neighbor, int weight)>();
        }

        graph[from].Add((to, weight));
        graph[to].Add((from, weight));
    }

    public void AddExit(string hallwayNode, string exitName, int weight)
    {
        AddEdge(hallwayNode, exitName, weight);
    }

    public void AddSpecialNode(string hallwayNode, string roomName, int weight)
    {
        AddEdge(hallwayNode, roomName, weight);
    }

    public void DisplayAll()
    {
        foreach (var node in graph)
        {
            Console.Write($"{node.Key}: ");

            foreach (var (neighbor, weight) in node.Value)
            {
                Console.Write($"[{neighbor}, w={weight}] ");
            }
            Console.WriteLine();
        }

    }

    public void DisplaySummary()
    {
        foreach (var node in graph)
        {
            Console.WriteLine($"{node.Key} to {node.Value.Count} neighbors");
        }
    }

    public HashSet<string> GetExitNodes()
    {
        var exits = new HashSet<string>();

        foreach (var node in graph.Keys)
        {
            if (node.StartsWith("Exit"))
            {
                exits.Add(node);
            }
        }

        return exits;
    }

    public List<string> GetRoomNodes()
    {
        var rooms = new List<string>();

        foreach (var node in graph.Keys)
        {
            bool isRoom = node.All(char.IsDigit);

            if (isRoom)
            {
                rooms.Add(node);
            }
        }

        return rooms;
    }

    public int ComputeTotalEvacuationTime()
    {
        var rooms = GetRoomNodes();
        var exits = GetExitNodes();

        int total = 0;

        foreach (var room in rooms)
        {
            var (distance, path) = Dijkstra(room, exits);
            total += distance;
        }

        return total;
    }


    public void FindAndDisplayQuickestExits()
    {
        var rooms = GetRoomNodes();
        var exits = GetExitNodes();

        foreach (var room in rooms)
        {
            var (distance, path) = Dijkstra(room, exits);

            Console.WriteLine($"Room {room}: distance = {distance}, path = ({string.Join(" to ", path)})");
        }
    }

    public (int distance, List<string> path) Dijkstra(string start, HashSet<string> exits)
    {
        var dist = new Dictionary<string, int>();

        foreach (var node in graph.Keys)
        {
            dist[node] = int.MaxValue;
        }

        dist[start] = 0;

        var pq = new PriorityQueue<string, int>();

        pq.Enqueue(start, 0);

        var visited = new HashSet<string>();
        var prev = new Dictionary<string, string>();

        string reachedExit = null;

        while (pq.Count > 0)
        {
            string current = pq.Dequeue();

            if (visited.Contains(current))
            {
                continue;
            }

            visited.Add(current);

            if (exits.Contains(current))
            {
                reachedExit = current;
                break;
            }



            foreach (var (neighbor, weight) in graph[current])
            {
                int newDist = dist[current] + weight;

                if (newDist < dist[neighbor])
                {
                    dist[neighbor] = newDist;
                    prev[neighbor] = current;
                    pq.Enqueue(neighbor, newDist);
                }
            }
        }

        if (reachedExit == null)
        {
            return (int.MaxValue, new List<string>());
        }

        var path = new List<string>();
        string currentNode = reachedExit;

        while (currentNode != start)
        {
            path.Add(currentNode);
            currentNode = prev[currentNode];
        }

        path.Add(start);

        path.Reverse();

        int finalDistance = dist[reachedExit];

        return (finalDistance, path);
    }
    public Dictionary<string, RoomResult> ComputeAllShortestPaths()
    {
        var rooms = GetRoomNodes();
        var exits = GetExitNodes();

        var results = new Dictionary<string, RoomResult>();

        foreach (var room in rooms)
        {
            var (distance, path) = Dijkstra(room, exits);

            results[room] = new RoomResult
            {
                Distance = distance,
                Path = path,
                Exit = path.Last()
            };
        }

        return results;
    }

    public Dictionary<string, int> CountChokePointUsage(Dictionary<string, RoomResult> results)
    {
        var chokeUsers = new Dictionary<string, int>();

        void Add(string choke, int amount)
        {
            if (!chokeUsers.ContainsKey(choke))
                chokeUsers[choke] = 0;

            chokeUsers[choke] += amount;
        }

        const int StudentsPerRoom = 30;

        foreach (var kvp in results)
        {
            var path = kvp.Value.Path;
            string exitNode = kvp.Value.Exit;

            //Count exit use
            Add(exitNode, StudentsPerRoom);

            if (path.Contains("E") || path.Contains("DD") || path.Contains("CCC"))
                Add("CenterStair", StudentsPerRoom);

            if (path.Contains("A") || path.Contains("AA") || path.Contains("AAA"))
                Add("SideStairLeft", StudentsPerRoom);

            if (path.Contains("GG") || path.Contains("GGG"))
                Add("SideStairRight", StudentsPerRoom);
        }

        return chokeUsers;
    }

    public Dictionary<string, double> ComputeChokePenalties(Dictionary<string, int> chokeUsers)
    {
        var capacity = new Dictionary<string, int>
    {
        { "CenterStair", 200 },
        { "SideStairLeft", 100 },
        { "SideStairRight", 100 },

        { "Exit1", 300 },
        { "Exit2", 300 },
        { "Exit3", 300 },
        { "Exit4", 300 },
        { "Exit5", 300 }
    };

        var penalties = new Dictionary<string, double>();

        foreach (var keyValuePair in chokeUsers)
        {
            string choke = keyValuePair.Key;
            int users = keyValuePair.Value;

            //skip choke points not in capacity table
            if (!capacity.ContainsKey(choke))
                continue;

            int cap = capacity[choke];

            if (users <= cap)
            {
                penalties[choke] = 0;
            }
            else
            {
                penalties[choke] = (users - cap) / (double)cap;
            }
        }

        return penalties;
    }

    public Dictionary<string, double> ComputeAdjustedTimes(
    Dictionary<string, RoomResult> results,
    Dictionary<string, double> penalties)
    {
        var adjusted = new Dictionary<string, double>();

        foreach (var kvp in results)
        {
            string room = kvp.Key;
            var result = kvp.Value;

            double penaltySum = 0;
            var path = result.Path;

            // Check center stair
            if (path.Contains("E") || path.Contains("DD") || path.Contains("CCC"))
                penaltySum += penalties.GetValueOrDefault("CenterStair", 0);

            // Check left stair
            if (path.Contains("A") || path.Contains("AA") || path.Contains("AAA"))
                penaltySum += penalties.GetValueOrDefault("SideStairLeft", 0);

            // Check right stair
            if (path.Contains("GG") || path.Contains("GGG"))
                penaltySum += penalties.GetValueOrDefault("SideStairRight", 0);

            //exit penalty
            penaltySum += penalties.GetValueOrDefault(result.Exit, 0);

            // Compute final evacuation time
            double adjustedTime = result.Distance * (1 + penaltySum);

            adjusted[room] = adjustedTime;
        }

        return adjusted;
    }

    public double ComputePenaltySumForPath(List<string> path, Dictionary<string, double> penalties)
    {
        double sum = 0;

        // center stair: E, DD, CCC
        if (path.Contains("E") || path.Contains("DD") || path.Contains("CCC"))
            sum += penalties.GetValueOrDefault("CenterStair", 0);

        // left stair: A, AA, AAA
        if (path.Contains("A") || path.Contains("AA") || path.Contains("AAA"))
            sum += penalties.GetValueOrDefault("SideStairLeft", 0);

        // right stair: GG, GGG
        if (path.Contains("GG") || path.Contains("GGG"))
            sum += penalties.GetValueOrDefault("SideStairRight", 0);

        // exit: always last node
        string exitNode = path.Last();
        sum += penalties.GetValueOrDefault(exitNode, 0);

        return sum;
    }

    public (double adjustedTime, int baseDistance, List<string> path, string exit)
    ComputeBestAdjustedPathForRoom(string room,
    Dictionary<string, double> penalties)
    {
        var exits = GetExitNodes();
        double bestAdjusted = double.PositiveInfinity;
        int bestBaseDist = int.MaxValue;
        List<string> bestPath = null;
        string bestExit = null;

        foreach (var exit in exits)
        {
            // Run Dijkstra targeting only this exit
            var (dist, path) = Dijkstra(room, new HashSet<string> { exit });

            if (dist == int.MaxValue)
                continue; // no path

            // Compute penalties for this path
            double penaltySum = ComputePenaltySumForPath(path, penalties);

            double adjusted = dist * (1 + penaltySum);

            if (adjusted < bestAdjusted)
            {
                bestAdjusted = adjusted;
                bestBaseDist = dist;
                bestPath = path;
                bestExit = exit;
            }
        }

        return (bestAdjusted, bestBaseDist, bestPath, bestExit);
    }

    public Dictionary<string, (double adjusted, int baseDist, List<string> path, string exit)>
    ComputeBestAdjustedPathsForAllRooms(Dictionary<string, double> penalties)
    {
        var rooms = GetRoomNodes();
        var result = new Dictionary<string, (double, int, List<string>, string)>();

        foreach (var room in rooms)
        {
            var best = ComputeBestAdjustedPathForRoom(room, penalties);
            result[room] = best;
        }

        return result;
    }



}
