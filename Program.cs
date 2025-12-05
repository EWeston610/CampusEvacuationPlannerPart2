using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        WeightedList list = new WeightedList();

        BuildingData.LoadGraph(list);

        list.FindAndDisplayQuickestExits();

        var results = list.ComputeAllShortestPaths();
        var chokeUsers = list.CountChokePointUsage(results);

        Console.WriteLine("\nChoke Usage:");
        foreach (var kvp in chokeUsers)
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");

        var penalties = list.ComputeChokePenalties(chokeUsers);

        Console.WriteLine("\nPenalties:");
        foreach (var kvp in penalties)
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");

        //Overloaded choke points
        Console.WriteLine("\nOverloaded choke points:");

        // same capacity table used for penalty computation
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

        foreach (var kv in chokeUsers)
        {
            string choke = kv.Key;
            int users = kv.Value;

            // Ignore choke points that aren’t in the capacity table
            if (!capacity.TryGetValue(choke, out int cap))
                continue;

            if (users > cap)
            {
                double pen = penalties.GetValueOrDefault(choke, 0);

                Console.WriteLine(
                    $"{choke}: users = {users}, capacity = {cap}, " +
                    $"OVERLOADED by {users - cap}, penalty = {pen:F2}"
                );
            }
        }

        var adjusted = list.ComputeAdjustedTimes(results, penalties);

        Console.WriteLine("\nAdjusted evacuation times:");
        foreach (var kvp in results)
        {
            string room = kvp.Key;
            var res = kvp.Value;
            double adj = adjusted[room];
            double penaltySum = list.ComputePenaltySumForPath(res.Path, penalties);

            Console.WriteLine(
                $"Room {room}: base = {res.Distance}, exit = {res.Exit}, penaltySum = {penaltySum:F2}, adjusted = {adj:F2}, path = ({string.Join(" to ", res.Path)})");
        }

        var bestPaths = list.ComputeBestAdjustedPathsForAllRooms(penalties);

        Console.WriteLine("\nBest Adjusted Routes:");
        foreach (var kv in bestPaths)
        {
            string room = kv.Key;

            var (bestAdj, baseDist, path, exit) = kv.Value;

            Console.WriteLine(
                $"Room {room}: bestExit = {exit}, base = {baseDist}, adjusted = {bestAdj:F2}, path = ({string.Join(" to ", path)})"
            );
        }

    }


}