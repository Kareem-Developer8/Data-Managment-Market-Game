using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public static event System.Action OnDataCleared;

    private const string XPKey = "PlayerXP";
    private const string LevelKey = "PlayerLevel";
    private const string DayCounterKey = "DayCounter";
    private const string BalanceKey = "PlayerBalance";
    private const string SavedBoxesKey = "SavedBoxes";
    private const string SavedPlaceholdersKey = "SavedPlaceholders";
    private List<BoxData> savedBoxes = new List<BoxData>();
    [System.Serializable]
    public class PlaceholderData
    {
        public string groceryName;
        public string shelfID;
        public BoxControl.BoxGenere boxGenere;
        public List<ItemData> items = new List<ItemData>();
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class ItemData
    {
        public string groceryName;
        public Vector3 position;
        public Quaternion rotation;
    }
    [System.Serializable]
    private class SavedBoxDataWrapper
    {
        public List<BoxData> boxes = new List<BoxData>();
    }

    void Awake()
    {
        //ClearAllData();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void SaveBox(BoxControl box)
    {
        if (box.grocery == null) // NEW CHECK
        {
            Debug.LogError("Attempted to save box with null grocery!");
            return;
        }

        Debug.Log($"Saving box with grocery: {box.grocery.name}"); // NEW LOG

        List<BoxData> allBoxes = LoadBoxesRaw();

        int existingIndex = allBoxes.FindIndex(b =>
            Vector3.Distance(b.position, box.transform.position) < 0.1f &&
            b.groceryName == box.grocery.name
        );

        if (existingIndex != -1)
        {
            allBoxes[existingIndex] = new BoxData
            {
                groceryName = box.grocery.name,
                position = box.transform.position,
                rotation = box.transform.rotation,
                boxGenere = box.boxGenere,
                product = box.Product,
                isStored = box.isStored,
                parentPlaceholderPosition = box.storedPlaceholderII != null
                ? box.storedPlaceholderII.boxTargetPosition.position
                : Vector3.zero
            };
        }
        else
        {
            allBoxes.Add(new BoxData
            {
                groceryName = box.grocery.name,
                position = box.transform.position,
                rotation = box.transform.rotation,
                boxGenere = box.boxGenere,
                product = box.Product,
                isStored = box.isStored
                ,
                parentPlaceholderPosition = box.storedPlaceholderII != null
                ? box.storedPlaceholderII.boxTargetPosition.position
                : Vector3.zero
            });

        }
        SaveBoxList(allBoxes);

    }
    public void SaveAllBoxes()
    {
        List<BoxData> boxesToSave = new List<BoxData>();
        var storedBoxes = FindObjectsByType<BoxControl>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(b => b.isStored);

        foreach (var box in storedBoxes)
        {
            if (box.grocery == null)
            {
                Debug.LogError("Box has null grocery, skipping save.");
                continue;
            }

            boxesToSave.Add(new BoxData
            {
                groceryName = box.grocery.name,
                position = box.transform.position,
                rotation = box.transform.rotation,
                boxGenere = box.boxGenere,
                product = box.Product,
                isStored = true
            });
        }

        SaveBoxList(boxesToSave);
    }
    [System.Serializable]
    private class SavedPlaceholderDataWrapper
    {
        public List<PlaceholderData> placeholders = new List<PlaceholderData>();
    }

    public void SaveAllPlaceholders()
    {
        Debug.Log("=== SAVING PLACEHOLDERS ===");
        var placeholders = FindObjectsByType<PlaceHolderOverlap>(FindObjectsSortMode.None)
            .Where(p => p.isFull || p.isPartiallyFull).ToList();

        Debug.Log($"Found {placeholders.Count} placeholders to save");

        SavedPlaceholderDataWrapper wrapper = new SavedPlaceholderDataWrapper();
        int totalItemsSaved = 0;

        foreach (var ph in placeholders)
        {
            if (ph.assignedGrocery == null)
            {
                Debug.LogWarning($"Placeholder at {ph.transform.position} has no assigned grocery - skipping");
                continue;
            }
#if UNITY_EDITOR
            string assetName = System.IO.Path.GetFileNameWithoutExtension(UnityEditor.AssetDatabase.GetAssetPath(ph.assignedGrocery));
#endif

            var data = new PlaceholderData
            {
                groceryName = ph.assignedGrocery.name,
                shelfID = ph.shelfID,
                boxGenere = ph.boxGenere,
                position = ph.transform.position,
                rotation = ph.transform.rotation
            };

            Debug.Log($"Saving placeholder: {ph.assignedGrocery.name} at {ph.transform.position}");

            foreach (Transform spot in ph.shelfSpots)
            {
                if (spot.childCount > 0)
                {
                    Transform item = spot.GetChild(0);
                    data.items.Add(new ItemData
                    {
                        groceryName = ph.assignedGrocery.name,
                        position = item.position,
                        rotation = item.rotation
                    });
                    totalItemsSaved++;
                }
            }

            wrapper.placeholders.Add(data);
        }

        string json = JsonUtility.ToJson(wrapper);
        Debug.Log($"Final placeholder JSON: {json}");
        PlayerPrefs.SetString(SavedPlaceholdersKey, json);
        PlayerPrefs.Save();

        Debug.Log($"Saved {wrapper.placeholders.Count} placeholders with {totalItemsSaved} total items");
    }

    public List<PlaceholderData> LoadPlaceholders()
    {
        string json = PlayerPrefs.GetString(SavedPlaceholdersKey, "");
        Debug.Log($"=== LOADING PLACEHOLDERS ===");
        Debug.Log($"Raw placeholder JSON: {(string.IsNullOrEmpty(json) ? "EMPTY" : json)}");

        if (!string.IsNullOrEmpty(json))
        {
            var wrapper = JsonUtility.FromJson<SavedPlaceholderDataWrapper>(json);
            Debug.Log($"Loaded {wrapper.placeholders.Count} placeholders from save");
            return wrapper.placeholders;
        }
        return new List<PlaceholderData>();
    }


    public void Debug_ClearSaveData()
    {
        PlayerPrefs.DeleteKey(SavedBoxesKey);
        PlayerPrefs.Save();
        Debug.Log("Cleared all box save data");
    }

    private void SaveBoxList(List<BoxData> boxes)
    {
        try
        {
            SavedBoxDataWrapper wrapper = new SavedBoxDataWrapper();
            wrapper.boxes = boxes;
            string json = JsonUtility.ToJson(wrapper);

            Debug.Log($"Attempting save to {SavedBoxesKey}");
            PlayerPrefs.SetString(SavedBoxesKey, json);
            bool saveSuccess = PlayerPrefs.HasKey(SavedBoxesKey);
            Debug.Log($"Save verified: {saveSuccess}, Data: {json}");
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }
    }
    private List<BoxData> LoadBoxesRaw()
    {
        string json = PlayerPrefs.GetString(SavedBoxesKey, "");
        return json == "" ? new List<BoxData>()
            : JsonUtility.FromJson<SavedBoxDataWrapper>(json).boxes;
    }
    /*private void SaveBoxes()
    {
        SavedBoxDataWrapper wrapper = new SavedBoxDataWrapper();
        wrapper.boxes = savedBoxes;
        string json = JsonUtility.ToJson(wrapper);

        PlayerPrefs.SetString(SavedBoxesKey, json);
        PlayerPrefs.Save();
    }*/

    public List<BoxData> LoadBoxes()
    {
        string json = PlayerPrefs.GetString(SavedBoxesKey, "");
        Debug.Log($"=== LOADING BOXES ===");
        Debug.Log($"Raw JSON: {json}");
        if (!string.IsNullOrEmpty(json))
        {
            SavedBoxDataWrapper wrapper = JsonUtility.FromJson<SavedBoxDataWrapper>(json);
            Debug.Log($"Deserialized boxes: {wrapper.boxes?.Count ?? 0}");
            savedBoxes = wrapper.boxes; // ← Match the field name
        }
        Debug.Log($"Total boxes loaded: {savedBoxes.Count}");
        return savedBoxes;
    }

    public void ClearBoxes()
    {
        savedBoxes.Clear();
        PlayerPrefs.DeleteKey(SavedBoxesKey);
    }

    private void Start()
    {
    }
    public void SaveProgress(int currentXP, int currentLevel)
    {
        PlayerPrefs.SetInt(XPKey, currentXP);
        PlayerPrefs.SetInt(LevelKey, currentLevel);
        PlayerPrefs.Save();
    }

    public void LoadProgress(out int currentXP, out int currentLevel)
    {
        currentXP = PlayerPrefs.GetInt(XPKey, 0);
        currentLevel = PlayerPrefs.GetInt(LevelKey, 1);
    }
    public void SaveShopkeeperActivation(int index, bool isActive)
    {
        PlayerPrefs.SetInt(GetActivationKey(index), isActive ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool LoadShopkeeperActivation(int index)
    {
        return PlayerPrefs.GetInt(GetActivationKey(index), 0) == 1;
    }

    private string GetActivationKey(int index)
    {
        return $"Shopkeeper_{index}_Active";
    }

    public void SaveGroceryPrice(Grocery grocery)
    {
        if (grocery == null) return; // Add null check

        PlayerPrefs.SetFloat(GetPriceKey(grocery), grocery.ChangedSellingPrice);
        PlayerPrefs.Save();
    }

    public void LoadGroceryPrice(Grocery grocery)
    {
        if (grocery == null) return; // Add null check
        grocery.ChangedSellingPrice = PlayerPrefs.GetFloat(GetPriceKey(grocery), grocery.sellingPrice);
    }

    public void ClearAllData()
    {
        ClearBoxes();
        int hasSavedGame = PlayerPrefs.GetInt("HasSavedGame", 0);
        PlayerPrefs.DeleteKey(DayCounterKey);
        PlayerPrefs.DeleteAll();
        OnDataCleared?.Invoke();
    }
    public void SaveDayCounter(int day)
    {
        PlayerPrefs.SetInt(DayCounterKey, day);
        PlayerPrefs.Save();
    }

    public int LoadDayCounter()
    {
        return PlayerPrefs.GetInt(DayCounterKey, 1); // Default to day 1
    }



    private string GetPriceKey(Grocery grocery)
    {
        return $"{grocery.name}_Price";
    }
    public void SaveGameTime(int minutes, int dayCounter)
    {
        PlayerPrefs.SetInt("GameMinutes", minutes);
        SaveDayCounter(dayCounter);
    }

    public int LoadGameTime()
    {
        return PlayerPrefs.GetInt("GameMinutes", 0);
    }
    public void SaveUnpaidBills(float unpaidElectricity, float unpaidSalaries)
    {
        PlayerPrefs.SetFloat("UnpaidElectricity", unpaidElectricity);
        PlayerPrefs.SetFloat("UnpaidSalaries", unpaidSalaries);
        PlayerPrefs.Save();
    }

    public float LoadUnpaidElectricity()
    {
        return PlayerPrefs.GetFloat("UnpaidElectricity", 0f);
    }

    public float LoadUnpaidSalaries()
    {
        return PlayerPrefs.GetFloat("UnpaidSalaries", 0f);
    }
    public void SaveBillPaymentStatus(bool isElectricityPaid, bool isSalariesPaid)
    {
        PlayerPrefs.SetInt("ElectricityPaid", isElectricityPaid ? 1 : 0);
        PlayerPrefs.SetInt("SalariesPaid", isSalariesPaid ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadBillPaymentStatus(out bool isElectricityPaid, out bool isSalariesPaid)
    {
        isElectricityPaid = PlayerPrefs.GetInt("ElectricityPaid", 0) == 1;
        isSalariesPaid = PlayerPrefs.GetInt("SalariesPaid", 0) == 1;
    }
    public void SaveBalance(float balance)
    {
        PlayerPrefs.SetFloat(BalanceKey, balance);
        PlayerPrefs.Save();
    }

    public float LoadBalance()
    {
        return PlayerPrefs.GetFloat(BalanceKey, 1000f); // Default to 0
    }
    public void SaveExpansionLevel(int level)
    {
        PlayerPrefs.SetInt("ExpansionLevel", level);
        PlayerPrefs.Save();
    }

    public int LoadExpansionLevel()
    {
        return PlayerPrefs.GetInt("ExpansionLevel", 1); // Default to level 1
    }
}