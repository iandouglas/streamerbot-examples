using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        string countsJson = CPH.GetGlobalVar<string>("highGroundClaimCounts", true);
        var counts = string.IsNullOrWhiteSpace(countsJson)
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : JsonConvert.DeserializeObject<Dictionary<string, int>>(countsJson)
              ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (counts.Count == 0)
        {
            CPH.SendMessage("No one has claimed the high ground yet.", true);
            return true;
        }

        int topCount = 3;

        var leaders = counts
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Take(topCount)
            .ToList();

        string leaderboard = BuildLeaderboard(leaders);
        CPH.SendMessage($"High Ground Leaders: {leaderboard}", true);
        return true;
    }

    private string BuildLeaderboard(List<KeyValuePair<string, int>> leaders)
    {
        var parts = new List<string>();

        for (int index = 0; index < leaders.Count; index++)
        {
            var leader = leaders[index];
            parts.Add($"Position {index + 1}: {leader.Key}, {leader.Value} time{(leader.Value == 1 ? "" : "s")}");
        }

        return string.Join(" ... ", parts);
    }
}
