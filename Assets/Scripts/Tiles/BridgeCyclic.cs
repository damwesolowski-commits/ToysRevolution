using UnityEngine;
using System.Collections;

[ExecuteAlways] // Dzięki temu Cycle Time aktualizuje się też w edytorze
public class BridgeCyclic : MonoBehaviour
{
    [Header("Czas działania mostu (prędkość jednostki 0,6 sek/pole")]

    [Tooltip("Opóźnienie przed rozpoczęciem pierwszego cyklu (w sekundach)")]
    public float delayTime = 0f;

    [Tooltip("Całkowity czas jednego cyklu (Visible + Hidden)")]
    [SerializeField, ReadOnlyInspector]
    private float cycleTime;

    [Tooltip("Czas, przez jaki most pozostaje widoczny (w sekundach)")]
    public float visibleTime = 2.0f;

    [Tooltip("Czas, przez jaki most pozostaje ukryty (w sekundach)")]
    public float hiddenTime = 3.5f;

    [Header("Efekty")]
    [Tooltip("Dźwięk odtwarzany przy otwieraniu/zamykaniu mostu")]
    public AudioClip bridgeSound;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool isVisible = true; // zawsze zaczyna jako widoczny
    private Vector3 bridgeWorldPos;

    void OnValidate()
    {
        // Automatycznie przelicz Cycle Time przy każdej zmianie w Inspectorze
        cycleTime = visibleTime + hiddenTime;
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("❌ BridgeCyclic: Brak SpriteRenderer!");
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("⚠️ BridgeCyclic: Brak AudioSource – dźwięk nie zostanie odtworzony.");
        }

        // zapamiętaj pozycję mostu
        bridgeWorldPos = transform.position;

        // ustaw stan początkowy (zawsze widoczny)
        isVisible = true;
        ApplyState();

        // uruchom cykl po opóźnieniu
        StartCoroutine(StartAfterDelay());
    }

    private IEnumerator StartAfterDelay()
    {
        if (delayTime > 0)
            yield return new WaitForSeconds(delayTime);

        StartCoroutine(CycleBridge());
    }

    private IEnumerator CycleBridge()
    {
        while (true)
        {
            yield return new WaitForSeconds(isVisible ? visibleTime : hiddenTime);
            ToggleBridge();
        }
    }

    private void ToggleBridge()
    {
        isVisible = !isVisible;
        ApplyState();

        if (audioSource != null && bridgeSound != null)
        {
            audioSource.PlayOneShot(bridgeSound);
        }
    }

    private void ApplyState()
    {
        if (spriteRenderer == null) return;

        // Zmieniamy przezroczystość mostu
        Color c = spriteRenderer.color;
        c.a = isVisible ? 1f : 0.2f;
        spriteRenderer.color = c;

        // Aktualizujemy neutralizację deadly tiles
        if (TileLogicManager.Instance != null)
        {
            TileLogicManager.Instance.SetTileNeutralized(bridgeWorldPos, isVisible);

            // Gdy most znika → od razu wymuś sprawdzenie kto stoi na polu
            if (!isVisible)
            {
                Vector3Int cell = Vector3Int.FloorToInt(bridgeWorldPos);
                TileLogicManager.Instance.RefreshTileLogic(cell);
            }
            // 🟫 Sprawdź skrzynie na tym samym polu co most (bez użycia fizyki)
            Vector3Int bridgeCell = TileLogicManager.Instance.groundTilemap.WorldToCell(bridgeWorldPos);

            foreach (var chest in GameObject.FindGameObjectsWithTag("Chest"))
            {
                Vector3Int chestCell = TileLogicManager.Instance.groundTilemap.WorldToCell(chest.transform.position);
                if (chestCell == bridgeCell)
                {
                    TileLogicManager.Instance.HandleUnitOnTile(chest);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // dla ułatwienia testów w edytorze
        Gizmos.color = isVisible ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
    }
}

// 🔒 Klasa pomocnicza, by pole Cycle Time było tylko do odczytu w Inspectorze
public class ReadOnlyInspectorAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyInspectorAttribute))]
public class ReadOnlyInspectorDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif
