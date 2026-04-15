using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelFlowController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private WinZone winZone;
    private ActorSpawner spawner;
    private int levelIndex;
    private float missionTimeLimitSeconds = 75f;

    private bool missionRunning;
    private bool missionSettled;
    private float missionEndTime;

    private GameObject flowCanvasRoot;
    private GameObject winRoot;
    private GameObject loseRoot;
    private GameObject pauseRoot;
    private Text missionTimerText;
    private Text buildHintText;
    private Text phaseLabelText;
    private GameObject phaseActionButtonGo;
    private Text phaseActionButtonLabelText;
    private RectTransform phaseActionButtonRt;
    private RectTransform phaseStripRt;

    private void Awake()
    {
        string scene = SceneManager.GetActiveScene().name;
        levelIndex = LevelCatalog.GetLevelIndex(scene);
        switch (scene)
        {
            case "Level01":
                missionTimeLimitSeconds = 60f;
                break;
            case "Level02":
            case "SampleScene":
                missionTimeLimitSeconds = 75f;
                break;
            case "Level03":
                missionTimeLimitSeconds = 95f;
                break;
            default:
                missionTimeLimitSeconds = 75f;
                break;
        }
    }

    private void Start()
    {
        EnsureGameManagerExists();
        if (GameManager.Instance == null)
        {
            UnityEngine.Debug.LogError("LevelFlowController: No GameManager in scene. Add a GameManager or use a level that includes one.");
            return;
        }

        winZone = FindFirstObjectByType<WinZone>();
        spawner = FindFirstObjectByType<ActorSpawner>();
        if (winZone != null)
        {
            winZone.LevelCompleted += HandleLevelWon;
        }

        BuildRuntimeUi();
        HideResultPanels();
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (winZone != null)
        {
            winZone.LevelCompleted -= HandleLevelWon;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        UpdatePhaseStrip();
        UpdateMissionClock();
        UpdateBuildHints();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseRoot != null && pauseRoot.activeSelf)
            {
                ResumeFromPause();
            }
            else if (!missionSettled && GameManager.Instance.CurrentPhase == GamePhase.Simulation)
            {
                OpenPause();
            }
        }
    }

    private void UpdateMissionClock()
    {
        if (missionSettled)
        {
            return;
        }

        if (GameManager.Instance.CurrentPhase != GamePhase.Simulation)
        {
            missionRunning = false;
            if (missionTimerText != null)
            {
                missionTimerText.gameObject.SetActive(false);
            }

            return;
        }

        if (!missionRunning)
        {
            missionRunning = true;
            missionEndTime = Time.time + missionTimeLimitSeconds;
            if (missionTimerText != null)
            {
                missionTimerText.gameObject.SetActive(true);
            }
        }

        if (missionTimerText != null && missionRunning)
        {
            float left = Mathf.Max(0f, missionEndTime - Time.time);
            missionTimerText.text = $"Time left: {Mathf.CeilToInt(left)}s";
        }

        if (missionRunning && Time.time >= missionEndTime)
        {
            HandleMissionFailed();
        }
    }

    private void UpdateBuildHints()
    {
        if (buildHintText == null)
        {
            return;
        }

        bool build = GameManager.Instance.CurrentPhase == GamePhase.Build;
        buildHintText.gameObject.SetActive(build && !missionSettled);
    }

    private void HandleLevelWon()
    {
        if (missionSettled)
        {
            return;
        }

        missionSettled = true;
        Time.timeScale = 0f;
        if (spawner != null)
        {
            spawner.StopEpisodeLoop();
        }

        LevelProgress.RegisterLevelCleared(levelIndex);
        HideResultPanels();
        if (winRoot != null)
        {
            winRoot.SetActive(true);
        }

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        if (GameSfx.Instance != null)
        {
            GameSfx.Instance.PlayWin();
        }
    }

    private void HandleMissionFailed()
    {
        if (missionSettled)
        {
            return;
        }

        missionSettled = true;
        Time.timeScale = 0f;
        if (spawner != null)
        {
            spawner.StopEpisodeLoop();
        }

        HideResultPanels();
        if (loseRoot != null)
        {
            loseRoot.SetActive(true);
        }

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        if (GameSfx.Instance != null)
        {
            GameSfx.Instance.PlayLose();
        }
    }

    private void OpenPause()
    {
        Time.timeScale = 0f;
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(true);
        }
    }

    private void ResumeFromPause()
    {
        if (missionSettled)
        {
            return;
        }

        Time.timeScale = 1f;
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }
    }

    private static void EnsureGameManagerExists()
    {
        if (GameManager.Instance != null)
        {
            return;
        }

        GameManager existing = FindFirstObjectByType<GameManager>();
        if (existing != null)
        {
            return;
        }

        GameObject host = new GameObject("GameManager");
        host.AddComponent<GameManager>();
    }

    private void UpdatePhaseStrip()
    {
        if (phaseLabelText == null || GameManager.Instance == null)
        {
            return;
        }

        bool settled = missionSettled;
        bool build = GameManager.Instance.CurrentPhase == GamePhase.Build;

        if (settled)
        {
            phaseLabelText.text = build ? "Phase: BUILD" : "Phase: SIMULATION";
            phaseLabelText.color = new Color(0.75f, 0.75f, 0.78f);
            if (phaseActionButtonGo != null)
            {
                phaseActionButtonGo.SetActive(false);
            }

            return;
        }

        phaseLabelText.text = build ? "Phase: BUILD" : "Phase: SIMULATION";
        phaseLabelText.color = build ? new Color(0.45f, 0.9f, 1f) : new Color(1f, 0.82f, 0.45f);

        if (phaseActionButtonGo != null && phaseActionButtonLabelText != null)
        {
            phaseActionButtonGo.SetActive(true);
            phaseActionButtonLabelText.text = build ? "Simulate" : "Back to build";
            phaseActionButtonLabelText.fontSize = phaseActionButtonLabelText.text.Length > 10 ? 15 : 17;
        }
    }

    private void OnPrimaryPhaseActionClicked()
    {
        if (GameManager.Instance == null || missionSettled)
        {
            return;
        }

        if (GameSfx.Instance != null)
        {
            GameSfx.Instance.PlayClick();
        }

        if (GameManager.Instance.CurrentPhase == GamePhase.Build)
        {
            PlacementManager pm = FindFirstObjectByType<PlacementManager>();
            if (pm != null)
            {
                pm.ClearHighlight();
            }

            GameManager.Instance.StartSimulation();
        }
        else
        {
            OnBackToBuildClicked();
        }
    }

    private void OnBackToBuildClicked()
    {
        if (GameManager.Instance == null || missionSettled)
        {
            return;
        }

        Time.timeScale = 1f;
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        GameManager.Instance.StartBuild();
        if (spawner != null)
        {
            spawner.EnterBuildPhaseFromSimulation();
        }

        missionRunning = false;
    }

    private void HideResultPanels()
    {
        if (winRoot != null)
        {
            winRoot.SetActive(false);
        }

        if (loseRoot != null)
        {
            loseRoot.SetActive(false);
        }
    }

    private void ReloadCurrentLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void LoadNextLevel()
    {
        if (!LevelCatalog.TryGetNextScene(levelIndex, out string next))
        {
            LoadMainMenu();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(next);
    }

    private void BuildRuntimeUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        flowCanvasRoot = new GameObject("LevelFlowCanvas");

        var canvas = flowCanvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = flowCanvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        flowCanvasRoot.AddComponent<GraphicRaycaster>();

        missionTimerText = CreateHudText(flowCanvasRoot.transform, font, "Time left: —", TextAnchor.UpperRight, new Vector2(-40f, -40f));

        RectTransform sceneSimulateLayout = TryDisableSceneSimulateButton();
        CreatePrimaryPhaseActionButton(flowCanvasRoot.transform, font, sceneSimulateLayout);
        CreatePhaseStrip(flowCanvasRoot.transform, font);
        LayoutPhaseStripBelowActionButton();

        buildHintText = CreateBuildPhaseHint(flowCanvasRoot.transform, font);

        winRoot = CreateResultPanel(flowCanvasRoot.transform, font, "Level complete!", true);
        loseRoot = CreateResultPanel(flowCanvasRoot.transform, font, "Time's up — try different flag placement.", false);
        pauseRoot = CreatePausePanel(flowCanvasRoot.transform, font);
    }

    private static Text CreateHudText(Transform parent, Font font, string msg, TextAnchor align, Vector2 anchoredPos)
    {
        var go = new GameObject("HudText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = align == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0.5f, 0f);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = align == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(520f, 48f);

        var text = go.GetComponent<Text>();
        text.font = font;
        text.text = msg;
        text.fontSize = 22;
        text.color = Color.white;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    private void CreatePhaseStrip(Transform parent, Font font)
    {
        var strip = new GameObject("PhaseStrip", typeof(RectTransform));
        strip.transform.SetParent(parent, false);
        phaseStripRt = strip.GetComponent<RectTransform>();
        phaseStripRt.anchorMin = new Vector2(0.5f, 1f);
        phaseStripRt.anchorMax = new Vector2(0.5f, 1f);
        phaseStripRt.pivot = new Vector2(0.5f, 1f);
        phaseStripRt.anchoredPosition = new Vector2(0f, -48f);
        phaseStripRt.sizeDelta = new Vector2(720f, 40f);

        var labelGo = new GameObject("PhaseLabel", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(strip.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        phaseLabelText = labelGo.GetComponent<Text>();
        phaseLabelText.font = font;
        phaseLabelText.text = "Phase: BUILD";
        phaseLabelText.fontSize = 20;
        phaseLabelText.color = new Color(0.45f, 0.9f, 1f);
        phaseLabelText.alignment = TextAnchor.MiddleCenter;
        phaseLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;

        var ol = labelGo.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private static RectTransform TryDisableSceneSimulateButton()
    {
        UIManager ui = FindFirstObjectByType<UIManager>();
        if (ui == null || ui.buildUI == null)
        {
            return null;
        }

        Transform start = ui.buildUI.transform.Find("Start");
        if (start == null)
        {
            return null;
        }

        var rt = start.GetComponent<RectTransform>();
        start.gameObject.SetActive(false);
        return rt;
    }

    private void CreatePrimaryPhaseActionButton(Transform parent, Font font, RectTransform layoutSource)
    {
        var go = new GameObject("PhaseActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        phaseActionButtonRt = go.GetComponent<RectTransform>();
        if (layoutSource != null)
        {
            phaseActionButtonRt.anchorMin = layoutSource.anchorMin;
            phaseActionButtonRt.anchorMax = layoutSource.anchorMax;
            phaseActionButtonRt.pivot = layoutSource.pivot;
            phaseActionButtonRt.anchoredPosition = layoutSource.anchoredPosition;
            float w = Mathf.Max(layoutSource.sizeDelta.x, 160f);
            float h = Mathf.Max(layoutSource.sizeDelta.y, 36f);
            phaseActionButtonRt.sizeDelta = new Vector2(w, h);
        }
        else
        {
            phaseActionButtonRt.anchorMin = new Vector2(0.5f, 1f);
            phaseActionButtonRt.anchorMax = new Vector2(0.5f, 1f);
            phaseActionButtonRt.pivot = new Vector2(0.5f, 0.5f);
            phaseActionButtonRt.anchoredPosition = new Vector2(0f, -19f);
            phaseActionButtonRt.sizeDelta = new Vector2(160f, 38f);
        }

        var img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.32f, 0.48f, 0.95f);

        var btn = go.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 0.92f, 1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.8f, 0.9f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.45f);
        btn.colors = colors;
        btn.onClick.AddListener(OnPrimaryPhaseActionClicked);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        phaseActionButtonLabelText = textGo.GetComponent<Text>();
        phaseActionButtonLabelText.font = font;
        phaseActionButtonLabelText.text = "Simulate";
        phaseActionButtonLabelText.fontSize = 17;
        phaseActionButtonLabelText.color = Color.white;
        phaseActionButtonLabelText.alignment = TextAnchor.MiddleCenter;
        phaseActionButtonLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        phaseActionButtonLabelText.verticalOverflow = VerticalWrapMode.Truncate;

        phaseActionButtonGo = go;
    }

    private void LayoutPhaseStripBelowActionButton()
    {
        if (phaseStripRt == null || phaseActionButtonRt == null)
        {
            return;
        }

        const float gap = 8f;
        float py = Mathf.Abs(phaseActionButtonRt.anchoredPosition.y);
        float pivotY = phaseActionButtonRt.pivot.y;
        float h = phaseActionButtonRt.sizeDelta.y;
        float extentBelowPivot = Mathf.Approximately(pivotY, 1f) ? h : (1f - pivotY) * h;
        phaseStripRt.anchoredPosition = new Vector2(phaseStripRt.anchoredPosition.x, -(py + extentBelowPivot + gap));
    }

    private static Text CreateBuildPhaseHint(Transform parent, Font font)
    {
        const string msg =
            "Build: tools cost supplies (bar on the right). Pick a slot, click ground to place; Delete removes a flag and refunds. Use Simulate at the top center to run; the same button returns you to build during simulation. Esc pauses during simulation. WASD pans the camera; R resets the view.";

        var go = new GameObject("BuildHintHud", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -128f);
        rt.sizeDelta = new Vector2(520f, 120f);

        var text = go.GetComponent<Text>();
        text.font = font;
        text.text = msg;
        text.fontSize = 17;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.88f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return text;
    }

    private GameObject CreateResultPanel(Transform parent, Font font, string title, bool isWin)
    {
        var root = new GameObject(isWin ? "WinPanel" : "LosePanel");
        root.transform.SetParent(parent, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        bg.raycastTarget = true;

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.64f);
        titleRt.anchorMax = new Vector2(0.5f, 0.64f);
        titleRt.sizeDelta = new Vector2(900f, 80f);
        titleRt.anchoredPosition = Vector2.zero;
        var titleText = titleGo.GetComponent<Text>();
        titleText.font = font;
        titleText.text = title;
        titleText.fontSize = 42;
        titleText.color = isWin ? new Color(0.6f, 1f, 0.65f) : new Color(1f, 0.55f, 0.5f);
        titleText.alignment = TextAnchor.MiddleCenter;

        const float buttonWidth = 168f;
        const float buttonGap = 22f;
        float step = buttonWidth + buttonGap;
        float rowY = -36f;

        bool showNext = isWin && LevelCatalog.TryGetNextScene(levelIndex, out _);
        if (showNext)
        {
            CreateMenuButton(root.transform, font, "Retry", new Vector2(-step, rowY), ReloadCurrentLevel, buttonWidth);
            CreateMenuButton(root.transform, font, "Menu", new Vector2(0f, rowY), LoadMainMenu, buttonWidth);
            CreateMenuButton(root.transform, font, "Next level", new Vector2(step, rowY), LoadNextLevel, buttonWidth);
        }
        else
        {
            float pair = (buttonWidth + buttonGap) * 0.5f;
            CreateMenuButton(root.transform, font, "Retry", new Vector2(-pair, rowY), ReloadCurrentLevel, buttonWidth);
            CreateMenuButton(root.transform, font, "Menu", new Vector2(pair, rowY), LoadMainMenu, buttonWidth);
        }

        root.SetActive(false);
        return root;
    }

    private GameObject CreatePausePanel(Transform parent, Font font)
    {
        var root = new GameObject("PausePanel");
        root.transform.SetParent(parent, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = true;

        var titleGo = new GameObject("PauseTitle", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.58f);
        titleRt.anchorMax = new Vector2(0.5f, 0.58f);
        titleRt.sizeDelta = new Vector2(600f, 64f);
        var titleText = titleGo.GetComponent<Text>();
        titleText.font = font;
        titleText.text = "Paused (Esc)";
        titleText.fontSize = 38;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;

        const float pauseButtonWidth = 132f;
        const float pauseGap = 16f;
        float pauseStep = pauseButtonWidth + pauseGap;
        CreateMenuButton(root.transform, font, "Resume", new Vector2(-pauseStep, -34f), ResumeFromPause, pauseButtonWidth);
        CreateMenuButton(root.transform, font, "To build", new Vector2(0f, -34f), OnBackToBuildClicked, pauseButtonWidth);
        CreateMenuButton(root.transform, font, "Menu", new Vector2(pauseStep, -34f), LoadMainMenu, pauseButtonWidth);

        root.SetActive(false);
        return root;
    }

    private void CreateMenuButton(Transform parent, Font font, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick, float width = 200f)
    {
        var go = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 46f);
        rt.anchoredPosition = anchoredPos;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.2f, 0.25f, 0.35f, 0.95f);

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (GameSfx.Instance != null)
            {
                GameSfx.Instance.PlayClick();
            }

            onClick?.Invoke();
        });

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var t = textGo.GetComponent<Text>();
        t.font = font;
        t.text = label;
        t.fontSize = label.Length > 11 ? 18 : 20;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
    }
}
