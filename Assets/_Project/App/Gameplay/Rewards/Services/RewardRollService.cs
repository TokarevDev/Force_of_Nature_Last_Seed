using System.Collections.Generic;
using UnityEngine;

public sealed class RewardRollService
{
    private const int MAX_CHOICES = 3;

    private readonly RewardDatabase _database;

    public RewardRollService(RewardDatabase database)
    {
        _database = database;
    }

    public List<RewardChoiceData> Roll3()
    {
        var result = new List<RewardChoiceData>(MAX_CHOICES);
        var source = _database.Rewards;

        if (source == null || source.Count == 0)
        {
            Debug.LogWarning("Reward database is empty.");
            return result;
        }

        var pool = new List<RewardModifierEntry>(source);
        int count = Mathf.Min(MAX_CHOICES, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            RewardModifierEntry selected = pool[randomIndex];

            result.Add(new RewardChoiceData(selected));
            pool.RemoveAt(randomIndex);
        }

        return result;
    }
}