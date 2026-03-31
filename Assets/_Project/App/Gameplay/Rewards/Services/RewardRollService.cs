using System.Collections.Generic;
using UnityEngine;

public sealed class RewardRollService
{
    private readonly RewardDatabase _database;

    public RewardRollService(RewardDatabase database)
    {
        _database = database;
    }

    public List<RewardChoiceData> Roll3()
    {
        var result = new List<RewardChoiceData>();
        var pool = _database.Modifiers;

        for (int i = 0; i < 3; i++)
        {
            var random = pool[Random.Range(0, pool.Count)];
            result.Add(new RewardChoiceData(random));
        }

        return result;
    }
}