using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoxManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public PoolGroceriesBox poolGroceriesBox;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        LoadSavedBoxes();
    }

    public void LoadSavedBoxes()
    {
        var existingBoxes = FindObjectsByType<BoxControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var box in existingBoxes)
        {
            if (box.gameObject.activeInHierarchy || box.gameObject.activeSelf)
            {
                Destroy(box.gameObject);
            }
        }
        List<BoxData> savedBoxes = DataManager.Instance.LoadBoxes();

        foreach (BoxData data in savedBoxes)
        {

            Grocery grocery = System.Array.Find(poolGroceriesBox.groceriesToPool,
                g => string.Equals(g.name, data.groceryName, System.StringComparison.Ordinal)); // Case-sensitive match
            if (grocery == null)
            {
                continue;
            }



            if (grocery != null && grocery.GroceryBox != null)
            {

                GameObject box = Instantiate(grocery.GroceryBox,
                    data.position,
                    data.rotation);

                BoxControl boxControl = box.GetComponent<BoxControl>();
                if (boxControl == null)
                {
                    continue;
                }
                if (boxControl != null)
                {
                    boxControl.isStored = data.isStored;
                    boxControl.grocery = grocery;
                    boxControl.boxGenere = data.boxGenere;
                    boxControl.Product = data.product;
                    if (data.isStored)
                    {
                        Rigidbody rb2 = box.GetComponent<Rigidbody>();
                        if (rb2 != null)
                        {
                            rb2.isKinematic = true;
                            rb2.useGravity = false;
                        }

                        BoxCollider collider2 = box.GetComponent<BoxCollider>();
                        if (collider2 != null) collider2.enabled = false;
                    }

                    // Parent to Placeholder II if applicable
                    if (data.parentPlaceholderPosition != Vector3.zero)
                    {
                        PlaceHolderOverlapeII[] placeholders = FindObjectsByType<PlaceHolderOverlapeII>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        foreach (var ph in placeholders)
                        {
                            if (Vector3.Distance(ph.transform.position, data.parentPlaceholderPosition) < 0.1f)
                            {
                                // Parent the box to the placeholder's target position
                                box.transform.SetParent(ph.boxTargetPosition);
                                box.transform.localPosition = Vector3.zero;
                                box.transform.localRotation = Quaternion.identity;

                                // Update references
                                boxControl.storedPlaceholderII = ph;
                                ph.isEmpty = false;
                                break;
                            }
                        }
                    }
                    // Disable physics
                    Rigidbody rb = box.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        //rb.linearVelocity = Vector3.zero;
                        if (!rb.isKinematic) Debug.LogError("Box physics not disabled!");
                    }

                    BoxCollider collider = box.GetComponent<BoxCollider>();
                    if (collider != null) collider.enabled = true;
                }
            }
        }
    }
}
[System.Serializable]
public class BoxData // Changed from struct
{
    public string groceryName;
    public Vector3 position;
    public Quaternion rotation;
    public BoxControl.BoxGenere boxGenere;
    public string product;
    public bool isStored;
    public Vector3 parentPlaceholderPosition;
}