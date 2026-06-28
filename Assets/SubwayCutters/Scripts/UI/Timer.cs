using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Timer : MonoBehaviour
{
    [SerializeField] private float countdownTime = 180.0f;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private UnityEvent onTimerEnd;

    private float _endTime = 0;
    private float RemainingTime => _endTime - Time.time;

    public void StartTimer()
    {
        StartCoroutine(Countdown());
        _endTime = Time.time + countdownTime;
        SetTimerText();
        StartCoroutine(UpdateText());
    }

    private void StopTimer()
    {
        onTimerEnd.Invoke();
    }

    private IEnumerator Countdown()
    {
        yield return new WaitForSeconds(countdownTime);
        StopTimer();
    }

    private IEnumerator UpdateText()
    {
        while (RemainingTime > 0)
        {
            yield return new WaitForSeconds(0.5f);

            SetTimerText();
        }

        timerText.text = "00:00";
    }

    private void SetTimerText()
    {
        int minutes = Mathf.FloorToInt(RemainingTime / 60);
        int seconds = Mathf.FloorToInt(RemainingTime % 60);
            
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}