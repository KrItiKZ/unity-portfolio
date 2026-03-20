using UnityEngine;
using System.Collections;

public class BookController : MonoBehaviour
{
    [Header("Кадры анимации (ПЕРЕТАЩИ СЮДА OBJ)")]
    public GameObject[] openFrameObjects = new GameObject[40];
    public GameObject[] closeFrameObjects = new GameObject[40];

    [Header("Референс для материалов (ОДИН OBJ с MTL)")]
    public GameObject materialReferenceFrame;

    [Header("Размер книги")]
    public Vector3 bookScale = new Vector3(1f, 1f, 1f);

    [Header("Поворот")]
    public Vector3 hiddenRotationOffset = new Vector3(30f, 0f, 0f);
    public Vector3 visibleRotationOffset = new Vector3(-10f, 0f, 0f);

    [Header("Скорость")]
    public float moveDuration = 0.6f;
    public float animFps = 30f;

    [Header("Позиции")]
    public Vector3 hiddenPosition = new Vector3(0f, -0.4f, 0.3f);
    public Vector3 visiblePosition = new Vector3(0f, -0.05f, 0.55f);

    private MeshFilter meshFilter;
    private Mesh[] openMeshes;
    private Mesh[] closeMeshes;
    private Material[] referenceMaterials;
    private Quaternion baseRotation;
    private Coroutine currentAnim;
    private System.Action onFinished;

    public enum BookState { Hidden, Opening, Open, Closing }
    public BookState currentState = BookState.Hidden;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();

        ExtractMeshesAndMaterials();

        transform.localScale = bookScale;
        transform.localRotation = baseRotation;

        if (openMeshes.Length > 0 && openMeshes[0] != null)
            meshFilter.sharedMesh = openMeshes[0];

        ApplyReferenceMaterials();           

        transform.localPosition = hiddenPosition;
        gameObject.SetActive(false);
    }

    private void ExtractMeshesAndMaterials()
    {
        openMeshes = new Mesh[openFrameObjects.Length];
        closeMeshes = new Mesh[closeFrameObjects.Length];

        if (materialReferenceFrame != null)
        {
            MeshFilter mf = materialReferenceFrame.GetComponentInChildren<MeshFilter>(true);
            MeshRenderer mr = materialReferenceFrame.GetComponentInChildren<MeshRenderer>(true);

            if (mf != null) baseRotation = mf.transform.localRotation;
            if (mr != null) referenceMaterials = mr.sharedMaterials;
        }

        for (int i = 0; i < openFrameObjects.Length; i++)
        {
            if (openFrameObjects[i] != null)
            {
                MeshFilter mf = openFrameObjects[i].GetComponentInChildren<MeshFilter>(true);
                openMeshes[i] = mf ? mf.sharedMesh : null;
            }
        }

        for (int i = 0; i < closeFrameObjects.Length; i++)
        {
            if (closeFrameObjects[i] != null)
            {
                MeshFilter mf = closeFrameObjects[i].GetComponentInChildren<MeshFilter>(true);
                closeMeshes[i] = mf ? mf.sharedMesh : null;
            }
        }
    }

    private void ApplyReferenceMaterials()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (referenceMaterials != null && referenceMaterials.Length > 0)
        {
            renderer.materials = referenceMaterials;   
        }
    }

    public void StartOpeningAnimation(System.Action callback)
    {
        if (currentState != BookState.Hidden) return;
        gameObject.SetActive(true);
        onFinished = callback;
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(OpeningSequence());
    }

    public void StartClosingAnimation(System.Action callback)
    {
        if (currentState != BookState.Open) return;
        onFinished = callback;
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(ClosingSequence());
    }

    private IEnumerator OpeningSequence()
    {
        currentState = BookState.Opening;

        float t = 0f;
        Vector3 startPos = hiddenPosition;
        Quaternion startRot = baseRotation * Quaternion.Euler(hiddenRotationOffset);
        Vector3 endPos = visiblePosition;
        Quaternion endRot = baseRotation * Quaternion.Euler(visibleRotationOffset);

        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float progress = t / moveDuration;
            transform.localPosition = Vector3.Lerp(startPos, endPos, progress);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, progress);
            meshFilter.sharedMesh = openMeshes[0];
            ApplyReferenceMaterials();           
            yield return null;
        }

        float frameTime = 1f / animFps;
        for (int i = 0; i < openMeshes.Length; i++)
        {
            if (openMeshes[i] != null)
            {
                meshFilter.sharedMesh = openMeshes[i];
                ApplyReferenceMaterials();       
            }
            yield return new WaitForSeconds(frameTime);
        }

        currentState = BookState.Open;
        onFinished?.Invoke();
        currentAnim = null;
    }

    private IEnumerator ClosingSequence()
    {
        currentState = BookState.Closing;

        float frameTime = 1f / animFps;
        for (int i = 0; i < closeMeshes.Length; i++)
        {
            if (closeMeshes[i] != null)
            {
                meshFilter.sharedMesh = closeMeshes[i];
                ApplyReferenceMaterials();       
            }
            yield return new WaitForSeconds(frameTime);
        }

        float t = 0f;
        Vector3 startPos = visiblePosition;
        Quaternion startRot = baseRotation * Quaternion.Euler(visibleRotationOffset);
        Vector3 endPos = hiddenPosition;
        Quaternion endRot = baseRotation * Quaternion.Euler(hiddenRotationOffset);

        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float progress = t / moveDuration;
            transform.localPosition = Vector3.Lerp(startPos, endPos, progress);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, progress);
            yield return null;
        }

        gameObject.SetActive(false);
        currentState = BookState.Hidden;
        onFinished?.Invoke();
        currentAnim = null;
    }
}