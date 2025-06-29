using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DataManager;

public class PlaceholderManager : MonoBehaviour
{
    public static PlaceholderManager Instance;
    private List<PlaceHolderOverlap> allPlaceholders = new List<PlaceHolderOverlap>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 1) // Game scene
        {
            StartCoroutine(HandleSceneLoad());
        }
    }

    IEnumerator HandleSceneLoad()
    {
        // Clear previous placeholders
        allPlaceholders.Clear();

        // Wait for placeholders to register themselves
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Double buffer

    }

    public void RegisterPlaceholder(PlaceHolderOverlap ph)
    {
        if (!allPlaceholders.Contains(ph))
        {
            allPlaceholders.Add(ph);
        }
    }

    public void LoadSavedPlaceholders(List<PlaceholderData> savedData)
    {
        StartCoroutine(LoadAfterRegistration(savedData));
    }

    private IEnumerator LoadAfterRegistration(List<PlaceholderData> savedData)
    {
        // Wait for all placeholders to register
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        int successfulLoads = 0;
        int failedLoads = 0;

        foreach (var data in savedData)
        {
            var testGrocery = Resources.Load<Grocery>($"Groceries/{data.groceryName}");

            bool foundMatch = false;

            foreach (var ph in allPlaceholders)
            {
                // Use precise position comparison
                if (Vector3.Distance(ph.transform.position, data.position) < 0.01f &&
                    Quaternion.Angle(ph.transform.rotation, data.rotation) < 0.1f)
                {
                    foundMatch = true;

                    // Clear existing items
                    foreach (Transform spot in ph.shelfSpots)
                    {
                        if (spot.childCount > 0) Destroy(spot.GetChild(0).gameObject);
                    }

                    // Load grocery data
                    var grocery = Resources.Load<Grocery>($"Groceries/{data.groceryName}");
                    if (grocery == null)
                    {
                        failedLoads++;
                        continue;
                    }

                    ph.SetShelfID(data.shelfID, grocery, data.boxGenere);

                    // Instantiate items
                    for (int i = 0; i < data.items.Count; i++)
                    {
                        if (i >= ph.shelfSpots.Length) break;

                        var spot = ph.shelfSpots[i];
                        var item = Instantiate(grocery.itemPrefab, spot.position, ph.targetGameObject.transform.rotation);
                        item.transform.SetParent(spot);
                        switch (grocery.placementType)
                        {
                            case Grocery.PlacementType.BakeriesShelf:
                                // Adjust position: +2 on Y-axis
                                item.transform.position += new Vector3(0, .15f, 0);
                                // Set rotation to 90 degrees on Y-axis
                                item.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                                break;

                            case Grocery.PlacementType.Fridge:
                                // Adjust position: +20 on X and Y axes
                                item.transform.position += new Vector3(0f, .45f, 0f);
                                // Add 90 degree rotation on Y-axis
                                item.transform.rotation *= Quaternion.Euler(-90f, -90f, 0f);
                                break;

                            default: // Regular Shelf
                                     // Original position adjustment
                                item.transform.position += new Vector3(0, 0.235f, 0);
                                break;
                        }
                        ph.itemsOnPlaceholder[i] = item;
                    }
                    successfulLoads++;
                    break;
                }
            }

            if (!foundMatch)
            {
                foreach (var ph in allPlaceholders)
                {
                    Debug.Log($"- {ph.transform.position} ({ph.assignedGrocery?.name})");
                }
                failedLoads++;
            }
        }

    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

}