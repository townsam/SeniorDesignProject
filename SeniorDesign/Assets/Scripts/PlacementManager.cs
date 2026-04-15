using System;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using System.Diagnostics;
using System.ComponentModel;

public class PlacementManager : MonoBehaviour
{
    [Header("Selected Prefab")]
    public GameObject selectedPrefab;

    [Header("Layers")]
    [SerializeField] private LayerMask placementMask = 0; // Ground
    [SerializeField] private LayerMask ignoreMask = 0;    // WinZone
    [SerializeField] private LayerMask flagMask = 0;      // Placed flags

    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;

    [Header("Flag Limits (per level)")]
    public int maxSlowZoneFlags = 3;
    public int maxRewardBoostFlags = 5;
    public int maxJumpBoostFlags = 2;

    [Header("Supply budget (per level)")]
    [Tooltip("Total supply points. When 0, only per-type limits apply (unlimited budget).")]
    public int toolBudgetTotal = 10;
    [Tooltip("Supply cost for each Slow Zone placed.")]
    public int slowPlacementCost = 3;
    [Tooltip("Supply cost for each Reward Boost placed.")]
    public int boostPlacementCost = 2;
    [Tooltip("Supply cost for each Jump Boost placed.")]
    public int jumpPlacementCost = 2;

    [Header("UI Setup")]
    public RectTransform uiPanelParent; // assign a Canvas or Panel in Inspector
    public GameObject uiFieldPrefab;    // a prefab with Text + InputField

    private GameObject currentUI;
    private UnityEngine.Component selectedFlagComponent;

    // Track how many are currently placed
    private int currentSlowZoneFlags = 0;
    private int currentRewardBoostFlags = 0;
    private int currentJumpBoostFlags = 0;

    private int toolBudgetRemaining;

    public event Action EconomyChanged;
    public event Action<GameObject> SelectedPrefabChanged;

    private GameObject previewInstance;
    private GameObject selectedPlacedObject;
    private Material[][] originalMaterials;

    void Start()
    {
        if (placementMask == 0) placementMask = LayerMask.GetMask("Ground");
        if (ignoreMask == 0) ignoreMask = LayerMask.GetMask("WinZone");
        if (flagMask == 0) flagMask = LayerMask.GetMask("Flag");

        toolBudgetRemaining = Mathf.Max(0, toolBudgetTotal);
        NotifyEconomy();
    }

    public bool UsesSupplyBudget => toolBudgetTotal > 0;

    public int ToolBudgetRemaining => toolBudgetRemaining;

    public int ToolBudgetTotal => Mathf.Max(0, toolBudgetTotal);

    public int SlowSlotsRemaining => Mathf.Max(0, maxSlowZoneFlags - currentSlowZoneFlags);

    public int BoostSlotsRemaining => Mathf.Max(0, maxRewardBoostFlags - currentRewardBoostFlags);

    public int JumpSlotsRemaining => Mathf.Max(0, maxJumpBoostFlags - currentJumpBoostFlags);

    public bool CanAffordSlowPlacement()
    {
        if (currentSlowZoneFlags >= maxSlowZoneFlags)
        {
            return false;
        }

        return !UsesSupplyBudget || toolBudgetRemaining >= slowPlacementCost;
    }

    public bool CanAffordBoostPlacement()
    {
        if (currentRewardBoostFlags >= maxRewardBoostFlags)
        {
            return false;
        }

        return !UsesSupplyBudget || toolBudgetRemaining >= boostPlacementCost;
    }

    public bool CanAffordJumpPlacement()
    {
        if (currentJumpBoostFlags >= maxJumpBoostFlags)
        {
            return false;
        }

        return !UsesSupplyBudget || toolBudgetRemaining >= jumpPlacementCost;
    }

    private int GetPlacementCost(IFlagRadius provider)
    {
        return provider switch
        {
            SlowZoneFlag => slowPlacementCost,
            RewardBoostFlag => boostPlacementCost,
            JumpBoostFlag => jumpPlacementCost,
            _ => 0
        };
    }

    private void NotifyEconomy()
    {
        EconomyChanged?.Invoke();
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != GamePhase.Build)
            return;

        if (selectedPlacedObject != null && Input.GetKeyDown(KeyCode.Delete))
        {
            var deletedFlag = selectedPlacedObject.GetComponent<IFlagRadius>();
            if (deletedFlag is SlowZoneFlag)
            {
                currentSlowZoneFlags = Mathf.Max(0, currentSlowZoneFlags - 1);
                RefundIfNeeded(deletedFlag);
            }
            else if (deletedFlag is RewardBoostFlag)
            {
                currentRewardBoostFlags = Mathf.Max(0, currentRewardBoostFlags - 1);
                RefundIfNeeded(deletedFlag);
            }
            else if (deletedFlag is JumpBoostFlag)
            {
                currentJumpBoostFlags = Mathf.Max(0, currentJumpBoostFlags - 1);
                RefundIfNeeded(deletedFlag);
            }

            Destroy(selectedPlacedObject);
            ClearHighlight();
            selectedPlacedObject = null;
            NotifyEconomy();
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            SelectObject(null);
            return;
        }

        // Ignore UI clicks
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HidePreview();
            return;
        }

        if (selectedPrefab != null)
        {
            UpdatePreview();

            if (Input.GetMouseButtonDown(0))
            {
                PlaceObject();
            }
        }
        else
        {
            HidePreview();

            if (Input.GetMouseButtonDown(0))
            {
                TrySelectPlacedObject();
            }
        }
    }

    #region Preview & Placement
    void UpdatePreview()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int rayMask = ~ignoreMask;

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, rayMask);
        var sortedHits = hits.OrderBy(h => h.distance);

        foreach (var hit in sortedHits)
        {
            if (((1 << hit.collider.gameObject.layer) & placementMask) != 0)
            {
                if (previewInstance == null)
                {
                    previewInstance = Instantiate(selectedPrefab);
                    MakePreview(previewInstance);

                    var radiusProvider = previewInstance.GetComponent<IFlagRadius>();
                    if (radiusProvider != null)
                    {
                        DrawCircle(previewInstance, radiusProvider.GetRadius(), Color.green, Vector3.up);
                    }
                }

                // Position and rotation aligned to surface
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * selectedPrefab.transform.rotation;
                previewInstance.transform.position = hit.point;
                previewInstance.transform.rotation = rotation;

                // Adjust height using collider
                var col = previewInstance.GetComponent<Collider>();
                if (col != null)
                {
                    previewInstance.transform.position += hit.normal * col.bounds.extents.y;
                }

                // Update radius circle slope
                var radiusProvider2 = previewInstance.GetComponent<IFlagRadius>();
                if (radiusProvider2 != null)
                {
                    DrawCircle(previewInstance, radiusProvider2.GetRadius(), Color.green, hit.normal);
                }

                return;
            }
        }

        HidePreview();
    }

    void PlaceObject()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int rayMask = ~ignoreMask;

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, rayMask);
        var sortedHits = hits.OrderBy(h => h.distance);

        // Determine type of flag
        var radiusProvider = selectedPrefab.GetComponent<IFlagRadius>();
        if (radiusProvider == null) return; // safety check

        // Check type, count, and supply
        if (radiusProvider is SlowZoneFlag)
        {
            if (currentSlowZoneFlags >= maxSlowZoneFlags)
            {
                UnityEngine.Debug.Log($"Cannot place more Slow Zone flags (max {maxSlowZoneFlags})");
                return;
            }

            if (UsesSupplyBudget && toolBudgetRemaining < slowPlacementCost)
            {
                UnityEngine.Debug.Log($"Not enough supplies for Slow Zone (needs {slowPlacementCost}, have {toolBudgetRemaining}).");
                return;
            }
        }
        else if (radiusProvider is RewardBoostFlag)
        {
            if (currentRewardBoostFlags >= maxRewardBoostFlags)
            {
                UnityEngine.Debug.Log($"Cannot place more Reward Boost flags (max {maxRewardBoostFlags})");
                return;
            }

            if (UsesSupplyBudget && toolBudgetRemaining < boostPlacementCost)
            {
                UnityEngine.Debug.Log($"Not enough supplies for Reward Boost (needs {boostPlacementCost}, have {toolBudgetRemaining}).");
                return;
            }
        }
        else if (radiusProvider is JumpBoostFlag)
        {
            if (currentJumpBoostFlags >= maxJumpBoostFlags)
            {
                UnityEngine.Debug.Log($"Cannot place more Jump Boost flags (max {maxJumpBoostFlags})");
                return;
            }

            if (UsesSupplyBudget && toolBudgetRemaining < jumpPlacementCost)
            {
                UnityEngine.Debug.Log($"Not enough supplies for Jump Boost (needs {jumpPlacementCost}, have {toolBudgetRemaining}).");
                return;
            }
        }

        foreach (var hit in sortedHits)
        {
            if (((1 << hit.collider.gameObject.layer) & placementMask) != 0)
            {
                Vector3 position = hit.point;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * selectedPrefab.transform.rotation;

                var col = selectedPrefab.GetComponent<Collider>();
                if (col != null)
                {
                    position += hit.normal * col.bounds.extents.y;
                }

                GameObject newFlag = Instantiate(selectedPrefab, position, rotation);

                if (radiusProvider is SlowZoneFlag)
                {
                    currentSlowZoneFlags++;
                    SpendIfNeeded(radiusProvider);
                }
                else if (radiusProvider is RewardBoostFlag)
                {
                    currentRewardBoostFlags++;
                    SpendIfNeeded(radiusProvider);
                }
                else if (radiusProvider is JumpBoostFlag)
                {
                    currentJumpBoostFlags++;
                    SpendIfNeeded(radiusProvider);
                }

                NotifyEconomy();
                return;
            }
        }

        UnityEngine.Debug.Log("No valid ground found under cursor.");
    }

    void HidePreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }

    void MakePreview(GameObject obj)
    {
        // Disable colliders
        foreach (var col in obj.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Make semi-transparent
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = 0.5f;
                    mat.color = c;
                }
            }
        }
    }
    #endregion

    #region Selection & Highlight
    void TrySelectPlacedObject()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, flagMask))
        {
            if (hit.collider == null) return;

            GameObject newSelection = hit.collider.transform.root.gameObject;
            if (selectedPlacedObject == newSelection) return;

            ClearHighlight();
            selectedPlacedObject = newSelection;
            ApplyHighlight(selectedPlacedObject);
        }
        else
        {
            ClearHighlight();
            selectedPlacedObject = null;
        }
    }

    void ApplyHighlight(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;

            Material[] newMats = new Material[renderers[i].materials.Length];

            for (int j = 0; j < newMats.Length; j++)
            {
                Material matInstance = new Material(renderers[i].materials[j]);
                if (matInstance.HasProperty("_Color"))
                    matInstance.color = highlightColor;
                newMats[j] = matInstance;
            }

            renderers[i].materials = newMats;
        }

        // Draw radius circle aligned to object up
        var radiusProvider = obj.GetComponent<IFlagRadius>();
        if (radiusProvider != null)
        {
            DrawCircle(obj, radiusProvider.GetRadius(), Color.yellow, obj.transform.up);
        }
        GenerateUIForFlag(obj);
    }

    public void ClearHighlight()
    {
        if (selectedPlacedObject == null || originalMaterials == null) return;

        var renderers = selectedPlacedObject.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length && i < originalMaterials.Length; i++)
        {
            renderers[i].materials = originalMaterials[i];
        }

        // Remove circle
        LineRenderer lr = selectedPlacedObject.GetComponent<LineRenderer>();
        if (lr != null) Destroy(lr);

        originalMaterials = null;
        ClearFlagUI();
    }
    #endregion

    #region Circle Drawing
    void DrawCircle(GameObject obj, float radius, Color color, Vector3 normal)
    {
        LineRenderer lr = obj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = obj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.widthMultiplier = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        lr.startColor = color;
        lr.endColor = color;

        int segments = 50;
        lr.positionCount = segments;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            Vector3 point = new Vector3(x, 0f, z);
            lr.SetPosition(i, obj.transform.position + rotation * point);
        }
    }
    #endregion

    public void SelectObject(GameObject prefab)
    {
        GameObject previous = selectedPrefab;
        selectedPrefab = prefab;
        HidePreview();
        ClearHighlight();
        selectedPlacedObject = null;
        
        if (prefab == null)
            UnityEngine.Debug.Log("Switched to selection mode.");
        else
            UnityEngine.Debug.Log("Placement mode: " + prefab.name);

        if (previous != prefab)
        {
            SelectedPrefabChanged?.Invoke(prefab);
        }
    }

    private void SpendIfNeeded(IFlagRadius placedType)
    {
        if (!UsesSupplyBudget)
        {
            return;
        }

        int cost = GetPlacementCost(placedType);
        toolBudgetRemaining = Mathf.Max(0, toolBudgetRemaining - cost);
    }

    private void RefundIfNeeded(IFlagRadius removedType)
    {
        if (!UsesSupplyBudget)
        {
            return;
        }

        int cost = GetPlacementCost(removedType);
        toolBudgetRemaining = Mathf.Min(ToolBudgetTotal, toolBudgetRemaining + cost);
    }

    void GenerateUIForFlag(GameObject flagObj)
    {
        ClearFlagUI();

        // Find the main component that has public variables (your flag script)
        var flagScript = flagObj.GetComponent<IFlagRadius>() as UnityEngine.Component;
        if (flagScript == null) return;

        selectedFlagComponent = flagScript;

        currentUI = new GameObject("FlagUI");
        currentUI.transform.SetParent(uiPanelParent, false);

        var layout = currentUI.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 5;

        var fields = flagScript.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            GameObject fieldUI = Instantiate(uiFieldPrefab, currentUI.transform);

            var text = fieldUI.GetComponentInChildren<UnityEngine.UI.Text>();
            var input = fieldUI.GetComponentInChildren<UnityEngine.UI.InputField>();

            if (text != null) text.text = field.Name;
            if (input != null)
            {
                object value = field.GetValue(flagScript);
                input.text = value.ToString();

                input.onEndEdit.AddListener((string newValue) =>
                {
                    try
                    {
                        object converted = System.Convert.ChangeType(newValue, field.FieldType);
                        field.SetValue(flagScript, converted);
                    }
                    catch
                    {
                        UnityEngine.Debug.LogWarning($"Invalid value for {field.Name}");
                    }
                });
            }
        }
    }

    void ClearFlagUI()
    {
        if (currentUI != null)
        {
            Destroy(currentUI);
            currentUI = null;
            selectedFlagComponent = null;
        }
    }
}