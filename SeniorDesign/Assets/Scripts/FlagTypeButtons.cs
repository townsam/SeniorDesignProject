using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FlagTypeButtons : MonoBehaviour
{
    [Header("UI Setup")]
    public Transform buttonContainer;
    [Tooltip("Optional legacy prefab; toolbar is built at runtime if container is set.")]
    public Button buttonPrefab;

    [Header("Docking")]
    [Tooltip("Fixed pixel width of the tool column (icons + labels only).")]
    [SerializeField] private float columnWidth = 112f;
    [SerializeField] private bool dockToRightEdge = true;
    [SerializeField] private float verticalMargin = 16f;
    [SerializeField] private float edgeInset = 12f;

    [Header("Layout")]
    [SerializeField] private float slotSpacing = 14f;
    [SerializeField] private float squareSize = 64f;

    [Header("Flag Prefabs")]
    public GameObject slowZonePrefab;
    public GameObject rewardBoostPrefab;
    public GameObject jumpBoostPrefab;

    [Header("Hints")]
    [SerializeField] private Text hintLine;

    private const string DefaultHint =
        "Hover a tool for details. Cost and uses are under each icon. Delete refunds supplies.";

    private PlacementManager placement;
    private Font uiFont;
    private Text budgetLabel;
    private readonly List<ToolbarSlot> slots = new List<ToolbarSlot>();

    private sealed class ToolbarSlot
    {
        public string ShortTitle;
        public string LongTitle;
        public Text TitleLabel;
        public Text StatsLabel;
        public Button SquareButton;
        public Image Frame;
        public Image IconBg;
        public Text Glyph;
        public GameObject TargetPrefab;
    }

    private void Start()
    {
        placement = FindFirstObjectByType<PlacementManager>();
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (buttonContainer == null)
        {
            UnityEngine.Debug.LogWarning("FlagTypeButtons: buttonContainer not assigned. Skipping UI setup.");
            return;
        }

        ClearContainerChildren();
        if (dockToRightEdge)
        {
            ApplyRightDockLayout();
        }

        BuildToolbar();
        EnsureHintLine();
        SetHint(DefaultHint);

        if (placement != null)
        {
            placement.EconomyChanged += RefreshToolbar;
            placement.SelectedPrefabChanged += OnSelectedPrefabChanged;
        }

        RefreshToolbar();
    }

    private void OnDestroy()
    {
        if (placement != null)
        {
            placement.EconomyChanged -= RefreshToolbar;
            placement.SelectedPrefabChanged -= OnSelectedPrefabChanged;
        }
    }

    private void OnSelectedPrefabChanged(GameObject _)
    {
        RefreshToolbar();
    }

    private void ApplyRightDockLayout()
    {
        var rt = buttonContainer as RectTransform;
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = new Vector2(-columnWidth - edgeInset, verticalMargin);
        rt.offsetMax = new Vector2(-edgeInset, -verticalMargin);
    }

    private void ClearContainerChildren()
    {
        for (int i = buttonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(buttonContainer.GetChild(i).gameObject);
        }
    }

    private void BuildToolbar()
    {
        float innerW = Mathf.Max(72f, columnWidth - 16f);

        var root = new GameObject("FlagToolbar", typeof(RectTransform), typeof(VerticalLayoutGroup));
        root.transform.SetParent(buttonContainer, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var v = root.GetComponent<VerticalLayoutGroup>();
        v.spacing = slotSpacing;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = false;
        v.childForceExpandHeight = false;
        v.padding = new RectOffset(8, 8, 10, 10);

        var budgetGo = new GameObject("BudgetLabel", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        budgetGo.transform.SetParent(root.transform, false);
        var budgetLe = budgetGo.GetComponent<LayoutElement>();
        budgetLe.preferredWidth = innerW;
        budgetLe.minWidth = innerW;
        budgetLe.flexibleWidth = 0f;
        budgetLe.preferredHeight = 46f;
        budgetLabel = budgetGo.GetComponent<Text>();
        budgetLabel.font = uiFont;
        budgetLabel.fontSize = 13;
        budgetLabel.fontStyle = FontStyle.Bold;
        budgetLabel.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        budgetLabel.alignment = TextAnchor.MiddleCenter;
        budgetLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        budgetLabel.verticalOverflow = VerticalWrapMode.Overflow;

        slots.Add(CreateToolCard(root.transform, innerW, "Select", "Select", "+", null, new Color(0.48f, 0.52f, 0.6f),
            "Click a flag to select it. Delete removes it and refunds supplies.",
            () => SelectCursor()));

        if (slowZonePrefab != null)
        {
            slots.Add(CreateToolCard(root.transform, innerW, "Slow", "Slow Zone", "S", slowZonePrefab, new Color(0.2f, 0.4f, 0.9f),
                "Slows actors inside the zone. Spends supplies when placed.",
                () => SelectSlow()));
        }

        if (rewardBoostPrefab != null)
        {
            slots.Add(CreateToolCard(root.transform, innerW, "Boost", "Reward Boost", "R", rewardBoostPrefab, new Color(0.16f, 0.68f, 0.34f),
                "Increases rewards inside the zone. Spends supplies when placed.",
                () => SelectBoost()));
        }

        if (jumpBoostPrefab != null)
        {
            slots.Add(CreateToolCard(root.transform, innerW, "Jump", "Jump Boost", "J", jumpBoostPrefab, new Color(0.95f, 0.55f, 0.12f),
                "Stronger jumps inside the zone. Spends supplies when placed.",
                () => SelectJump()));
        }
    }

    private ToolbarSlot CreateToolCard(Transform parent, float cardWidth, string shortTitle, string longTitle, string glyphChar,
        GameObject prefab, Color iconColor, string longHint, System.Action onPick)
    {
        const float titleH = 22f;
        const float statsH = 18f;
        const float gap = 6f;
        float cardHeight = titleH + gap + squareSize + gap + statsH;

        var card = new GameObject(shortTitle + "Card", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        card.transform.SetParent(parent, false);
        var cardLe = card.GetComponent<LayoutElement>();
        cardLe.preferredWidth = cardWidth;
        cardLe.minWidth = cardWidth;
        cardLe.flexibleWidth = 0f;
        cardLe.preferredHeight = cardHeight;
        cardLe.minHeight = cardHeight;

        var cardV = card.GetComponent<VerticalLayoutGroup>();
        cardV.spacing = gap;
        cardV.childAlignment = TextAnchor.UpperCenter;
        cardV.childControlWidth = true;
        cardV.childControlHeight = true;
        cardV.childForceExpandWidth = false;
        cardV.childForceExpandHeight = false;

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        titleGo.transform.SetParent(card.transform, false);
        var titleLe = titleGo.GetComponent<LayoutElement>();
        titleLe.preferredHeight = titleH;
        titleLe.preferredWidth = cardWidth;
        titleLe.minWidth = cardWidth;
        titleLe.flexibleWidth = 0f;
        var titleLabel = titleGo.GetComponent<Text>();
        titleLabel.font = uiFont;
        titleLabel.text = shortTitle;
        titleLabel.fontSize = 12;
        titleLabel.fontStyle = FontStyle.Bold;
        titleLabel.color = new Color(0.15f, 0.15f, 0.17f, 1f);
        titleLabel.alignment = TextAnchor.MiddleCenter;
        titleLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        titleLabel.verticalOverflow = VerticalWrapMode.Truncate;

        var squareGo = new GameObject("Square", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        squareGo.transform.SetParent(card.transform, false);
        var sqLe = squareGo.GetComponent<LayoutElement>();
        sqLe.preferredWidth = squareSize;
        sqLe.preferredHeight = squareSize;
        sqLe.minWidth = squareSize;
        sqLe.minHeight = squareSize;
        sqLe.flexibleWidth = 0f;
        sqLe.flexibleHeight = 0f;

        var frame = squareGo.GetComponent<Image>();
        frame.color = new Color(0.11f, 0.12f, 0.16f, 1f);
        frame.raycastTarget = true;
        var outline = squareGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var btn = squareGo.GetComponent<Button>();
        btn.targetGraphic = frame;
        btn.onClick.AddListener(() =>
        {
            onPick?.Invoke();
            RefreshToolbar();
        });

        var iconGo = new GameObject("IconBg", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(squareGo.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        float inset = 6f;
        iconRt.sizeDelta = new Vector2(squareSize - inset * 2f, squareSize - inset * 2f);
        iconRt.anchoredPosition = Vector2.zero;
        var iconBg = iconGo.GetComponent<Image>();
        iconBg.sprite = ToolbarUiSprites.RoundedTile;
        iconBg.type = Image.Type.Sliced;
        iconBg.color = iconColor;
        iconBg.raycastTarget = false;

        var glyphGo = new GameObject("Glyph", typeof(RectTransform), typeof(Text));
        glyphGo.transform.SetParent(squareGo.transform, false);
        var gRt = glyphGo.GetComponent<RectTransform>();
        gRt.anchorMin = Vector2.zero;
        gRt.anchorMax = Vector2.one;
        gRt.offsetMin = new Vector2(4f, 4f);
        gRt.offsetMax = new Vector2(-4f, -4f);
        var glyph = glyphGo.GetComponent<Text>();
        glyph.font = uiFont;
        glyph.text = glyphChar;
        glyph.fontSize = 26;
        glyph.fontStyle = FontStyle.Bold;
        glyph.color = new Color(1f, 1f, 1f, 0.98f);
        glyph.alignment = TextAnchor.MiddleCenter;

        var statsGo = new GameObject("Stats", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        statsGo.transform.SetParent(card.transform, false);
        var statsLe = statsGo.GetComponent<LayoutElement>();
        statsLe.preferredHeight = statsH;
        statsLe.preferredWidth = cardWidth;
        statsLe.minWidth = cardWidth;
        statsLe.flexibleWidth = 0f;
        var statsLabel = statsGo.GetComponent<Text>();
        statsLabel.font = uiFont;
        statsLabel.fontSize = 11;
        statsLabel.fontStyle = FontStyle.Bold;
        statsLabel.color = new Color(0.22f, 0.22f, 0.26f, 1f);
        statsLabel.alignment = TextAnchor.MiddleCenter;
        statsLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        statsLabel.verticalOverflow = VerticalWrapMode.Truncate;

        var slot = new ToolbarSlot
        {
            ShortTitle = shortTitle,
            LongTitle = longTitle,
            TitleLabel = titleLabel,
            StatsLabel = statsLabel,
            SquareButton = btn,
            Frame = frame,
            IconBg = iconBg,
            Glyph = glyph,
            TargetPrefab = prefab
        };

        WireHover(slot, longHint);
        return slot;
    }

    private void WireHover(ToolbarSlot slot, string longHint)
    {
        Button b = slot.SquareButton;
        EventTrigger trigger = b.gameObject.GetComponent<EventTrigger>() ?? b.gameObject.AddComponent<EventTrigger>();

        void OnEnter(BaseEventData _)
        {
            slot.TitleLabel.text = slot.LongTitle;
            SetHint(longHint);
        }

        void OnExit(BaseEventData _)
        {
            slot.TitleLabel.text = slot.ShortTitle;
            SetHint(DefaultHint);
        }

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(OnEnter);
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(OnExit);
        trigger.triggers.Add(exit);
    }

    private void RefreshToolbar()
    {
        if (placement == null)
        {
            return;
        }

        GameObject sel = placement.selectedPrefab;
        if (sel == slowZonePrefab && !placement.CanAffordSlowPlacement())
        {
            placement.SelectObject(null);
            sel = null;
        }
        else if (sel == rewardBoostPrefab && !placement.CanAffordBoostPlacement())
        {
            placement.SelectObject(null);
            sel = null;
        }
        else if (sel == jumpBoostPrefab && !placement.CanAffordJumpPlacement())
        {
            placement.SelectObject(null);
            sel = null;
        }

        if (budgetLabel != null)
        {
            if (placement.UsesSupplyBudget)
            {
                budgetLabel.text = $"Supplies\n{placement.ToolBudgetRemaining} / {placement.ToolBudgetTotal}";
            }
            else
            {
                budgetLabel.text = "Supplies\n∞";
            }
        }

        sel = placement.selectedPrefab;

        foreach (ToolbarSlot s in slots)
        {
            bool isSel = (s.TargetPrefab == null && sel == null) || (s.TargetPrefab != null && s.TargetPrefab == sel);
            s.Frame.color = isSel ? new Color(0.18f, 0.24f, 0.38f, 1f) : new Color(0.11f, 0.12f, 0.16f, 1f);

            if (s.TargetPrefab == null)
            {
                s.StatsLabel.text = "—  ·  ∞ uses";
                Color c = s.IconBg.color;
                c.a = 1f;
                s.IconBg.color = c;
                continue;
            }

            int left;
            bool canPick;
            if (s.TargetPrefab == slowZonePrefab)
            {
                left = placement.SlowSlotsRemaining;
                canPick = placement.CanAffordSlowPlacement();
            }
            else if (s.TargetPrefab == jumpBoostPrefab)
            {
                left = placement.JumpSlotsRemaining;
                canPick = placement.CanAffordJumpPlacement();
            }
            else
            {
                left = placement.BoostSlotsRemaining;
                canPick = placement.CanAffordBoostPlacement();
            }

            string costStr;
            if (placement.UsesSupplyBudget)
            {
                int c = s.TargetPrefab == slowZonePrefab
                    ? placement.slowPlacementCost
                    : s.TargetPrefab == jumpBoostPrefab
                        ? placement.jumpPlacementCost
                        : placement.boostPlacementCost;
                costStr = $"{c} pt";
            }
            else
            {
                costStr = "—";
            }

            string usesStr = left > 0 ? $"{left} left" : "0 left";
            s.StatsLabel.text = $"{costStr}  ·  {usesStr}";

            Color ic = s.IconBg.color;
            ic.a = canPick && left > 0 ? 1f : 0.38f;
            s.IconBg.color = ic;
        }
    }

    private void SelectCursor()
    {
        if (placement == null)
        {
            return;
        }

        placement.SelectObject(null);
    }

    private void SelectSlow()
    {
        if (placement == null || slowZonePrefab == null)
        {
            return;
        }

        if (!placement.CanAffordSlowPlacement() || placement.SlowSlotsRemaining <= 0)
        {
            return;
        }

        placement.SelectObject(slowZonePrefab);
    }

    private void SelectBoost()
    {
        if (placement == null || rewardBoostPrefab == null)
        {
            return;
        }

        if (!placement.CanAffordBoostPlacement() || placement.BoostSlotsRemaining <= 0)
        {
            return;
        }

        placement.SelectObject(rewardBoostPrefab);
    }

    private void SelectJump()
    {
        if (placement == null || jumpBoostPrefab == null)
        {
            return;
        }

        if (!placement.CanAffordJumpPlacement() || placement.JumpSlotsRemaining <= 0)
        {
            return;
        }

        placement.SelectObject(jumpBoostPrefab);
    }

    private void EnsureHintLine()
    {
        if (hintLine != null)
        {
            return;
        }

        Transform parent = buttonContainer.parent;
        if (parent == null)
        {
            return;
        }

        GameObject go = new GameObject("FlagHintLine", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 20f);
        rt.sizeDelta = new Vector2(-(columnWidth + edgeInset + 24f), 56f);
        hintLine = go.GetComponent<Text>();
        hintLine.font = uiFont;
        hintLine.fontSize = 16;
        hintLine.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        hintLine.alignment = TextAnchor.LowerCenter;
        hintLine.horizontalOverflow = HorizontalWrapMode.Wrap;
        hintLine.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void SetHint(string message)
    {
        if (hintLine == null)
        {
            return;
        }

        hintLine.text = string.IsNullOrEmpty(message) ? DefaultHint : message;
    }
}
