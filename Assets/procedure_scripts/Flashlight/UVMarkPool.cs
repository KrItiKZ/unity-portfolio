
using UnityEngine;
using System.Collections.Generic;

public class UVMarkPool : MonoBehaviour
{
    public static UVMarkPool Instance;

    [Header("Pool Settings")]
    public GameObject uvMarkPrefab;
    public int initialPoolSize = 20;
    public int maxPoolSize = 50;

    private Queue<GameObject> availableMarks = new Queue<GameObject>();
    private List<GameObject> allMarks = new List<GameObject>();
    private Transform poolParent;

    private void Awake()
    {
        Instance = this;
        poolParent = new GameObject("UVMarkPool").transform;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewMark();
        }
    }

    private GameObject CreateNewMark()
    {
        if (allMarks.Count >= maxPoolSize) return null;

        GameObject mark = Instantiate(uvMarkPrefab, poolParent);
        mark.SetActive(false);
        availableMarks.Enqueue(mark);
        allMarks.Add(mark);

        return mark;
    }

    public GameObject GetUVMark(Transform doorTransform, Vector2 localOffset)
    {
        GameObject mark = null;

        if (availableMarks.Count > 0)
        {
            mark = availableMarks.Dequeue();
        }
        else if (allMarks.Count < maxPoolSize)
        {
            mark = CreateNewMark();
        }

        if (mark != null)
        {
            SetupMark(mark, doorTransform, localOffset);
        }

        return mark;
    }

    private void SetupMark(GameObject mark, Transform doorTransform, Vector2 localOffset)
    {
        mark.transform.SetParent(doorTransform);
        Vector3 localPosition = new Vector3(localOffset.x, localOffset.y, -0.1f);
        mark.transform.localPosition = localPosition;
        mark.transform.localRotation = Quaternion.identity;

        float randomScaleMultiplier = Random.Range(0.8f, 1.2f);
        mark.transform.localScale = Vector3.one * 0.1f * randomScaleMultiplier;

        float randomRotation = Random.Range(-30f, 30f);
        mark.transform.Rotate(0f, 0f, randomRotation, Space.Self);

        SpriteRenderer renderer = mark.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = new Color(1f, 1f, 1f, 0f);

        mark.SetActive(true);
    }

    public void ReturnUVMark(GameObject mark)
    {
        if (mark != null && allMarks.Contains(mark))
        {
            mark.transform.SetParent(poolParent);
            mark.SetActive(false);

            UvMark uvMarkComponent = mark.GetComponent<UvMark>();
            if (uvMarkComponent != null)
            {
                uvMarkComponent.ForceHide();
            }

            availableMarks.Enqueue(mark);
        }
    }

    public void ClearAllMarks()
    {
        foreach (GameObject mark in allMarks)
        {
            if (mark != null && mark.activeInHierarchy)
            {
                ReturnUVMark(mark);
            }
        }
    }
}