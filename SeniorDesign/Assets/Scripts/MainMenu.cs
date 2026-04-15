using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TrainingRunsBrowser trainingRunsBrowser;
    [SerializeField] private bool unlockAllLevelsForNow = true;

    [TextArea(2, 6)]
    [SerializeField] private string creditsBody =
        "Senior Design — guide agents with placement flags + ML-Agents.\nAdd your team names here (Inspector on Main Menu).";

    private void Start()
    {
        SettingsStore.ApplyToAudioListener();
        EnsureSettingsExtras();

        if (unlockAllLevelsForNow)
        {
            LevelProgress.UnlockAllLevels();
            PlayMenu menu = FindFirstObjectByType<PlayMenu>(FindObjectsInactive.Include);
            if (menu != null)
            {
                menu.RefreshLevelButtons();
            }
        }
#if UNITY_EDITOR
        EnsureTrainingRunsEntry();
#endif
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ResetLevelProgress()
    {
        LevelProgress.ResetAllProgress();
        PlayMenu menu = FindFirstObjectByType<PlayMenu>(FindObjectsInactive.Include);
        if (menu != null)
        {
            menu.RefreshLevelButtons();
        }
    }

    public void EnableInferenceBestModel()
    {
#if UNITY_EDITOR
        InferenceModeController.EnableInferenceFromBest(InferenceModeController.DefaultBehaviorName);
#else
        Debug.LogWarning("EnableInferenceBestModel: results/best is not available in player builds. Import a model into Assets and assign it to Behavior Parameters instead.");
#endif
    }

    public void DisableInferenceMode()
    {
        InferenceModeController.DisableInferenceForAll(InferenceModeController.DefaultBehaviorName);
    }

    /// <summary>
    /// Hook a UI button to this in the Main Menu. Lists results/ runs and can launch the ML smoke script (Editor).
    /// </summary>
    public void OpenTrainingRuns()
    {
        TrainingRunsBrowser browser = trainingRunsBrowser
            ?? FindFirstObjectByType<TrainingRunsBrowser>(FindObjectsInactive.Include);
        if (browser == null)
        {
            Debug.LogWarning("OpenTrainingRuns: add a TrainingRunsBrowser to the scene or assign it on MainMenu.");
            return;
        }

        browser.ShowAndRefresh();
    }

    private void EnsureSettingsExtras()
    {
        Transform root = GameObject.Find("SettingsMenu")?.transform;
        if (root == null || root.Find("VolumeSliderRow") != null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject row = new GameObject("VolumeSliderRow", typeof(RectTransform));
        row.transform.SetParent(root, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = new Vector2(0f, 80f);
        rowRt.sizeDelta = new Vector2(420f, 200f);

        GameObject labelGo = new GameObject("VolLabel", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(row.transform, false);
        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.5f, 1f);
        lrt.anchorMax = new Vector2(0.5f, 1f);
        lrt.pivot = new Vector2(0.5f, 1f);
        lrt.anchoredPosition = Vector2.zero;
        lrt.sizeDelta = new Vector2(400f, 32f);
        Text lt = labelGo.GetComponent<Text>();
        lt.font = font;
        lt.text = "Master volume";
        lt.fontSize = 22;
        lt.color = Color.black;
        lt.alignment = TextAnchor.MiddleCenter;

        GameObject sliderGo = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(row.transform, false);
        RectTransform srt = sliderGo.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.sizeDelta = new Vector2(360f, 22f);
        srt.anchoredPosition = new Vector2(0f, 20f);
        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = SettingsStore.MasterVolume;
        slider.onValueChanged.AddListener(v => SettingsStore.MasterVolume = v);

        GameObject resetGo = new GameObject("ResetProgressBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        resetGo.transform.SetParent(row.transform, false);
        RectTransform rrt = resetGo.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.5f, 0f);
        rrt.anchorMax = new Vector2(0.5f, 0f);
        rrt.pivot = new Vector2(0.5f, 0f);
        rrt.anchoredPosition = new Vector2(0f, 56f);
        rrt.sizeDelta = new Vector2(260f, 34f);
        resetGo.GetComponent<Image>().color = new Color(0.28f, 0.3f, 0.42f, 1f);
        Button rb = resetGo.GetComponent<Button>();
        rb.onClick.AddListener(ResetLevelProgress);

        GameObject rbTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        rbTextGo.transform.SetParent(resetGo.transform, false);
        RectTransform trt = rbTextGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        Text rtex = rbTextGo.GetComponent<Text>();
        rtex.font = font;
        rtex.text = "Reset level unlocks";
        rtex.fontSize = 16;
        rtex.color = Color.white;
        rtex.alignment = TextAnchor.MiddleCenter;

        GameObject infGo = new GameObject("InferenceBestBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        infGo.transform.SetParent(row.transform, false);
        RectTransform irt = infGo.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 0f);
        irt.anchorMax = new Vector2(0.5f, 0f);
        irt.pivot = new Vector2(0.5f, 0f);
        irt.anchoredPosition = new Vector2(0f, 16f);
        irt.sizeDelta = new Vector2(260f, 34f);
        infGo.GetComponent<Image>().color = new Color(0.22f, 0.48f, 0.35f, 1f);
        Button ib = infGo.GetComponent<Button>();
        ib.onClick.AddListener(EnableInferenceBestModel);

        GameObject ibTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        ibTextGo.transform.SetParent(infGo.transform, false);
        RectTransform itrt = ibTextGo.GetComponent<RectTransform>();
        itrt.anchorMin = Vector2.zero;
        itrt.anchorMax = Vector2.one;
        itrt.offsetMin = Vector2.zero;
        itrt.offsetMax = Vector2.zero;
        Text itex = ibTextGo.GetComponent<Text>();
        itex.font = font;
        itex.text = "Use best model (inference)";
        itex.fontSize = 16;
        itex.color = Color.white;
        itex.alignment = TextAnchor.MiddleCenter;

        GameObject infOffGo = new GameObject("InferenceOffBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        infOffGo.transform.SetParent(row.transform, false);
        RectTransform iort = infOffGo.GetComponent<RectTransform>();
        iort.anchorMin = new Vector2(0.5f, 0f);
        iort.anchorMax = new Vector2(0.5f, 0f);
        iort.pivot = new Vector2(0.5f, 0f);
        iort.anchoredPosition = new Vector2(0f, -24f);
        iort.sizeDelta = new Vector2(260f, 34f);
        infOffGo.GetComponent<Image>().color = new Color(0.28f, 0.3f, 0.42f, 1f);
        Button iob = infOffGo.GetComponent<Button>();
        iob.onClick.AddListener(DisableInferenceMode);

        GameObject iobTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        iobTextGo.transform.SetParent(infOffGo.transform, false);
        RectTransform iobtrt = iobTextGo.GetComponent<RectTransform>();
        iobtrt.anchorMin = Vector2.zero;
        iobtrt.anchorMax = Vector2.one;
        iobtrt.offsetMin = Vector2.zero;
        iobtrt.offsetMax = Vector2.zero;
        Text iobtex = iobTextGo.GetComponent<Text>();
        iobtex.font = font;
        iobtex.text = "Use trainer / default behavior";
        iobtex.fontSize = 16;
        iobtex.color = Color.white;
        iobtex.alignment = TextAnchor.MiddleCenter;

        GameObject creditsGo = new GameObject("CreditsBlock", typeof(RectTransform), typeof(Text));
        creditsGo.transform.SetParent(root, false);
        RectTransform crt = creditsGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(0f, -60f);
        crt.sizeDelta = new Vector2(720f, 200f);
        Text ctx = creditsGo.GetComponent<Text>();
        ctx.font = font;
        ctx.text = creditsBody;
        ctx.fontSize = 17;
        ctx.color = Color.black;
        ctx.alignment = TextAnchor.MiddleCenter;
        ctx.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

#if UNITY_EDITOR
    static Sprite s_uiBlockSprite;

    static Sprite GetUiBlockSprite()
    {
        if (s_uiBlockSprite != null)
        {
            return s_uiBlockSprite;
        }

        Texture2D t = Texture2D.whiteTexture;
        s_uiBlockSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        return s_uiBlockSprite;
    }

    /// <summary>
    /// Runtime-created Training runs button: Unity 6+ no longer ships builtin UI sprites (Knob, etc.);
    /// using them logs errors even when null-checked. Use a flat sprite + opaque ColorTint instead.
    /// </summary>
    static void ApplyMainMenuPrimaryButtonStyle(Image image, Button button)
    {
        image.sprite = GetUiBlockSprite();
        image.type = Image.Type.Simple;
        image.color = Color.white;
        button.targetGraphic = image;

        ColorBlock c = button.colors;
        c.normalColor = new Color(1f, 1f, 1f, 0.94f);
        c.highlightedColor = new Color(0.93f, 0.94f, 0.97f, 1f);
        c.pressedColor = new Color(0.84f, 0.86f, 0.91f, 1f);
        c.selectedColor = new Color(0.93f, 0.94f, 0.97f, 1f);
        c.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.45f);
        c.colorMultiplier = 1f;
        c.fadeDuration = 0.1f;
        button.colors = c;
    }

    private void EnsureTrainingRunsEntry()
    {
        if (trainingRunsBrowser == null)
        {
            Transform existing = transform.Find("TrainingRunsBrowserHost");
            if (existing != null)
            {
                trainingRunsBrowser = existing.GetComponent<TrainingRunsBrowser>();
            }
        }

        if (trainingRunsBrowser == null)
        {
            GameObject host = new GameObject("TrainingRunsBrowserHost");
            host.transform.SetParent(transform, false);
            RectTransform hrt = host.AddComponent<RectTransform>();
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = Vector2.zero;
            hrt.anchoredPosition = Vector2.zero;
            trainingRunsBrowser = host.AddComponent<TrainingRunsBrowser>();
        }

        Transform oldTrainingBtn = transform.Find("TrainingRunsButton");
        if (oldTrainingBtn != null)
        {
            Destroy(oldTrainingBtn.gameObject);
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject btnGo = new GameObject("TrainingRunsButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(transform, false);
        RectTransform brt = btnGo.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0f, -120f);
        brt.sizeDelta = new Vector2(160f, 30f);
        Image img = btnGo.GetComponent<Image>();
        Button btn = btnGo.GetComponent<Button>();
        ApplyMainMenuPrimaryButtonStyle(img, btn);
        btn.onClick.AddListener(OpenTrainingRuns);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        Text tx = textGo.GetComponent<Text>();
        tx.font = font;
        tx.text = "Training runs";
        tx.fontSize = 14;
        tx.color = Color.black;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.raycastTarget = false;
    }
#endif
}
