using System;
using UnityEngine;

public static class GameState
{
    public static int Score { get; private set; }
    public static bool IsGameOver { get; private set; }

    public static event Action<int> ScoreChanged;

    public static event Action GameOver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayMode()
    {
        Score = 0;
        IsGameOver = false;
        ScoreChanged = null;
        GameOver = null;
    }

    public static void Reset()
    {
        Score = 0;
        IsGameOver = false;
        ScoreChanged?.Invoke(Score);
    }

    public static void AddScore(int amount)
    {
        if (IsGameOver == true)
        {
            return;
        }
        Score += amount;
        ScoreChanged?.Invoke(Score);
        Debug.Log($"[GameState] Score: {Score}");
    }

    public static void TriggerGameOver()
    {
        if (IsGameOver == true)
        {
            return;
        }
        IsGameOver = true;
        GameOver?.Invoke();
        Debug.Log("[GameState] GAME OVER");
    }
}