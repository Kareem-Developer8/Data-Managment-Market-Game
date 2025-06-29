using UnityEngine;
using TMPro;

public class DaySystem : MonoBehaviour
{
    public int dayCounter { get; private set; } = 0;

    [Header("Display Settings")]
    [SerializeField] private TextMeshProUGUI timeDisplay; // Assign in Inspector
    [SerializeField] private int marketOpenHour = 8;      // Start at 8:00 AM

    [Header("Time Settings")]
    [SerializeField] private float realSecondsPerGameHour = 60f; // 1 game hour = 60 real seconds

    private float timeAccumulator;
    public int currentGameMinutes; // Tracks minutes since market opened
    public event System.Action OnDayStarted;
    public event System.Action OnDayEnded;
    public bool isDayActive;

    private void Start()
    {
        //ResetDay();
        dayCounter = DataManager.Instance.LoadDayCounter();
        currentGameMinutes = DataManager.Instance.LoadGameTime();
        timeAccumulator = 0f;
    }

    private void Update()
    {
        UpdateGameTime();
        UpdateTimeDisplay();
    }
    public void StartDay()
    {
        Debug.Log("Day Started");
        isDayActive = true;
        currentGameMinutes = 0;
        timeAccumulator = 0f;
        dayCounter++;
        if (ShopperReview.Instance != null)
        {
            ShopperReview.Instance.ResetCounter();
        }
        DataManager.Instance.SaveDayCounter(dayCounter);
        OnDayStarted?.Invoke();
    }

    public void EndDay()
    {
        isDayActive = false;
        OnDayEnded?.Invoke();
    }
    private void UpdateGameTime()
    {
        if (!isDayActive)
        {
            Debug.Log("Day not active");
            return;
        }
        timeAccumulator += Time.deltaTime;

        if (timeAccumulator >= 1f)
        {
            currentGameMinutes++;
            timeAccumulator -= 1f;

            // Reset after 16 hours (960 minutes)
            if (currentGameMinutes >= 960)
            {
                EndDay();
            }
        }
    }

    private void UpdateTimeDisplay()
    {
        if (timeDisplay == null) return;

        int totalHours = marketOpenHour + (currentGameMinutes / 60);
        int displayHours = totalHours % 24;
        int minutes = currentGameMinutes % 60;

        timeDisplay.text = $"{displayHours:00}:{minutes:00}";
    }

    private void ResetDay()
    {
        currentGameMinutes = 0;
        timeAccumulator = 0f;
        dayCounter++;
    }

    public bool IsMarketOpen()
    {
        return currentGameMinutes < 960; // Market is open first 16 game hours
    }

    public float GetCurrentHour()
    {
        return marketOpenHour + (currentGameMinutes / 60f);
    }
    private void OnApplicationQuit()
    {
        DataManager.Instance.SaveGameTime(currentGameMinutes, dayCounter);
    }


}