using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor-focused UI: lists <c>results/*</c> runs with metrics from <c>timers.json</c>,
/// and launches the platform-appropriate smoke script (Windows: <c>Start-MLSmoke.ps1</c>, macOS/Linux: <c>Start-MLSmoke.sh</c>)
/// with resume, overwrite, or a new run id.
/// In a player build the project <c>results</c> folder is not available; controls stay disabled.
/// </summary>
public class TrainingRunsBrowser : MonoBehaviour
{
    [SerializeField] private int timeScale = 10;
    [SerializeField] private bool hideWhenCreated = true;
    [SerializeField] private bool allowDeleteBestOrCurrent = false;

    private Font _font;
    private GameObject _panelRoot;
    private RectTransform _listContent;
    private InputField _newRunIdField;
    private Text _statusText;
    private readonly List<GameObject> _rowPool = new List<GameObject>();

    private GameObject _confirmRoot;
    private Text _confirmText;
    private string _pendingDeleteRunId;

    private void Awake()
    {
        if (!Application.isEditor)
        {
            enabled = false;
            return;
        }

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUi();
        if (hideWhenCreated && _panelRoot != null)
        {
            _panelRoot.SetActive(false);
        }
    }

    public void ShowAndRefresh()
    {
        if (_panelRoot == null)
        {
            return;
        }

        _panelRoot.SetActive(true);
        RefreshList();
        if (_newRunIdField != null && string.IsNullOrWhiteSpace(_newRunIdField.text))
        {
            _newRunIdField.text = SuggestNewRunId();
        }

        SetStatus(Application.isEditor
            ? "Pick a run below or start a new run id. This launches the external ML smoke script."
            : "Training launcher is only meant for use in the Unity Editor (project results/ folder).");
    }

    public void HidePanel()
    {
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(false);
        }
    }

    public void RefreshList()
    {
        if (_listContent == null)
        {
            return;
        }

        foreach (GameObject row in _rowPool)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }

        _rowPool.Clear();

        if (!Application.isEditor)
        {
            return;
        }

        List<MlRunSummary> runs = MlRunsScanner.ListRuns();
        if (runs.Count == 0)
        {
            AddInfoRow(_listContent, "No runs found under results/. Train once with Start-MLSmoke (script in repo root), then refresh.");
            return;
        }

        foreach (MlRunSummary run in runs)
        {
            CreateRunRow(_listContent, run);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
    }

    public void OnClickStartNewRun()
    {
        if (!EnsureEditorAndScript())
        {
            return;
        }

        string id = _newRunIdField != null ? _newRunIdField.text.Trim() : "";
        if (string.IsNullOrEmpty(id))
        {
            SetStatus("Enter a run id for the new run.");
            return;
        }

        LaunchSmoke(id, resume: false, noOverwrite: true);
    }

    void LaunchResume(string runId)
    {
        if (!EnsureEditorAndScript())
        {
            return;
        }

        LaunchSmoke(runId, resume: true, noOverwrite: false);
    }

    void LaunchOverwrite(string runId)
    {
        if (!EnsureEditorAndScript())
        {
            return;
        }

        LaunchSmoke(runId, resume: false, noOverwrite: false);
    }

    bool EnsureEditorAndScript()
    {
        if (!Application.isEditor)
        {
            SetStatus("Only available in the Unity Editor.");
            return false;
        }

        string ps1 = MlRunsScanner.GetStartScriptPath();
        if (string.IsNullOrEmpty(ps1) || !File.Exists(ps1))
        {
            SetStatus($"Start-MLSmoke script not found next to Assets. Expected: {ps1}");
            return false;
        }

        return true;
    }

    void LaunchSmoke(string runId, bool resume, bool noOverwrite)
    {
        string scriptPath = MlRunsScanner.GetStartScriptPath();
        string root = MlRunsScanner.GetProjectRoot();
        bool isWindows =
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer;

        try
        {
            ProcessStartInfo psi = BuildStartProcessInfo(scriptPath, root, runId, timeScale, resume, noOverwrite);

            if (isWindows)
            {
                Process.Start(psi);
            }
            else
            {
                // On macOS/Linux, capture immediate script failure output (e.g., missing venv / wrong python)
                // so the user isn't left guessing when nothing happens.
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;

                using (Process p = Process.Start(psi))
                {
                    if (p == null)
                    {
                        throw new Exception("Process failed to start.");
                    }

                    string stdout = p.StandardOutput.ReadToEnd();
                    string stderr = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    if (p.ExitCode != 0)
                    {
                        string msg = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                        msg = string.IsNullOrWhiteSpace(msg) ? "Unknown error." : msg.Trim();
                        SetStatus($"Start failed: {msg}");
                        return;
                    }
                }
            }

            string mode = resume ? "resume" : (noOverwrite ? "new (no overwrite)" : "overwrite");
            SetStatus($"Started training ({mode}): {runId}");
        }
        catch (Exception e)
        {
            SetStatus($"Failed to start training script: {e.Message}");
            UnityEngine.Debug.LogException(e);
        }
    }

    static ProcessStartInfo BuildStartProcessInfo(
        string scriptPath,
        string root,
        string runId,
        int timeScale,
        bool resume,
        bool noOverwrite)
    {
        bool isWindows =
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer;

        if (isWindows)
        {
            var args = new List<string>
            {
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                scriptPath,
                "-RunId",
                runId,
                "-TimeScale",
                timeScale.ToString()
            };

            if (resume)
            {
                args.Add("-Resume");
            }
            else if (noOverwrite)
            {
                args.Add("-NoOverwrite");
            }

            string argLine = BuildArgumentLine(args);
            return new ProcessStartInfo
            {
                FileName = "powershell.exe",
                WorkingDirectory = root,
                UseShellExecute = true,
                Arguments = argLine
            };
        }

        // macOS/Linux: run Start-MLSmoke.sh via bash and pass settings via environment variables.
        // The script itself backgrounds trainer/tensorboard and writes Logs/*.pid for Stop-MLSmoke.sh.
        string mode = resume ? "resume" : (noOverwrite ? "no_overwrite" : "force");
        var psiUnix = new ProcessStartInfo
        {
            FileName = "/usr/bin/env",
            WorkingDirectory = root,
            UseShellExecute = false
        };

        psiUnix.ArgumentList.Add("bash");
        psiUnix.ArgumentList.Add(scriptPath);
        psiUnix.EnvironmentVariables["RUN_ID"] = runId;
        psiUnix.EnvironmentVariables["TIME_SCALE"] = timeScale.ToString();
        psiUnix.EnvironmentVariables["MODE"] = mode;

        return psiUnix;
    }

    static string BuildArgumentLine(List<string> args)
    {
        var parts = new List<string>(args.Count);
        foreach (string a in args)
        {
            parts.Add(a.IndexOf(' ') >= 0 ? $"\"{a.Replace("\"", "\\\"")}\"" : a);
        }

        return string.Join(" ", parts);
    }

    void SetStatus(string msg)
    {
        if (_statusText != null)
        {
            _statusText.text = msg;
        }
    }

    static string SuggestNewRunId()
    {
        return "actor-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
    }

    void BuildUi()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            UnityEngine.Debug.LogWarning("TrainingRunsBrowser: no Canvas found.");
            return;
        }

        _panelRoot = new GameObject("TrainingRunsPanel", typeof(RectTransform), typeof(Image));
        _panelRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = _panelRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        _panelRoot.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

        GameObject window = new GameObject("Window", typeof(RectTransform), typeof(Image));
        window.transform.SetParent(_panelRoot.transform, false);
        RectTransform winRt = window.GetComponent<RectTransform>();
        winRt.anchorMin = new Vector2(0.5f, 0.5f);
        winRt.anchorMax = new Vector2(0.5f, 0.5f);
        winRt.sizeDelta = new Vector2(920f, 560f);
        winRt.anchoredPosition = Vector2.zero;
        window.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.97f, 1f);

        GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text));
        title.transform.SetParent(window.transform, false);
        RectTransform titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -12f);
        titleRt.sizeDelta = new Vector2(-24f, 36f);
        Text titleTx = title.GetComponent<Text>();
        titleTx.font = _font;
        titleTx.fontSize = 22;
        titleTx.color = Color.black;
        titleTx.alignment = TextAnchor.MiddleLeft;
        titleTx.text = "ML-Agents training runs";

        GameObject closeBtnGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGo.transform.SetParent(window.transform, false);
        RectTransform closeRt = closeBtnGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-8f, -8f);
        closeRt.sizeDelta = new Vector2(88f, 32f);
        closeBtnGo.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.4f, 1f);
        closeBtnGo.GetComponent<Button>().onClick.AddListener(HidePanel);
        AddButtonLabel(closeBtnGo.transform, "Close", _font);

        GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(window.transform, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0.28f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(16f, 16f);
        scrollRt.offsetMax = new Vector2(-16f, -52f);
        scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(4f, 4f);
        vpRt.offsetMax = new Vector2(-4f, -4f);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        _listContent = content.GetComponent<RectTransform>();
        _listContent.anchorMin = new Vector2(0f, 1f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.pivot = new Vector2(0.5f, 1f);
        _listContent.anchoredPosition = Vector2.zero;
        _listContent.sizeDelta = new Vector2(0f, 0f);
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.content = _listContent;
        scroll.viewport = vpRt;
        scroll.vertical = true;
        scroll.horizontal = false;

        GameObject footer = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        footer.transform.SetParent(window.transform, false);
        RectTransform footRt = footer.GetComponent<RectTransform>();
        footRt.anchorMin = new Vector2(0f, 0f);
        footRt.anchorMax = new Vector2(1f, 0f);
        footRt.pivot = new Vector2(0.5f, 0f);
        footRt.anchoredPosition = new Vector2(0f, 12f);
        footRt.sizeDelta = new Vector2(-32f, 120f);
        var hlg = footer.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight = true;
        hlg.childControlWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.padding = new RectOffset(12, 12, 8, 8);

        GameObject newLabel = new GameObject("NewLabel", typeof(RectTransform), typeof(Text));
        newLabel.transform.SetParent(footer.transform, false);
        Text nl = newLabel.GetComponent<Text>();
        nl.font = _font;
        nl.fontSize = 16;
        nl.color = Color.black;
        nl.text = "New run id:";
        LayoutElement leN = newLabel.AddComponent<LayoutElement>();
        leN.minWidth = 90f;
        leN.preferredWidth = 90f;

        _newRunIdField = CreateInputField(footer.transform, _font, SuggestNewRunId());
        LayoutElement leI = _newRunIdField.gameObject.AddComponent<LayoutElement>();
        leI.flexibleWidth = 1f;
        leI.minWidth = 200f;

        GameObject startNewGo = CreateFooterButton(footer.transform, "Start new", () => OnClickStartNewRun());
        LayoutElement leB = startNewGo.AddComponent<LayoutElement>();
        leB.minWidth = 100f;

        GameObject refreshGo = CreateFooterButton(footer.transform, "Refresh", RefreshList);
        refreshGo.AddComponent<LayoutElement>().minWidth = 88f;

        GameObject pruneGo = CreateFooterButton(footer.transform, "Prune runs", PruneRunsKeepBestCurrent);
        pruneGo.AddComponent<LayoutElement>().minWidth = 120f;

        GameObject statusGo = new GameObject("Status", typeof(RectTransform), typeof(Text));
        statusGo.transform.SetParent(window.transform, false);
        RectTransform stRt = statusGo.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 0f);
        stRt.anchorMax = new Vector2(1f, 0f);
        stRt.pivot = new Vector2(0.5f, 0f);
        stRt.anchoredPosition = new Vector2(0f, 132f);
        stRt.sizeDelta = new Vector2(-32f, 44f);
        _statusText = statusGo.GetComponent<Text>();
        _statusText.font = _font;
        _statusText.fontSize = 14;
        _statusText.color = new Color(0.15f, 0.15f, 0.2f);
        _statusText.alignment = TextAnchor.MiddleLeft;
        _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _statusText.text = "";

        BuildConfirmDialog(_panelRoot.transform);
    }

    void CreateRunRow(Transform parent, MlRunSummary run)
    {
        GameObject row = new GameObject($"Row_{run.RunId}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var rowLe = row.GetComponent<LayoutElement>();
        rowLe.minHeight = 40f;
        rowLe.preferredHeight = 40f;
        var rowH = row.GetComponent<HorizontalLayoutGroup>();
        rowH.spacing = 8f;
        rowH.childAlignment = TextAnchor.MiddleLeft;
        rowH.childControlHeight = true;
        rowH.childControlWidth = false;
        rowH.childForceExpandHeight = true;
        rowH.childForceExpandWidth = false;
        rowH.padding = new RectOffset(6, 6, 4, 4);
        Image rim = row.AddComponent<Image>();
        rim.color = new Color(1f, 1f, 1f, 0.55f);

        GameObject textGo = new GameObject("Summary", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(row.transform, false);
        Text tx = textGo.GetComponent<Text>();
        tx.font = _font;
        tx.fontSize = 14;
        tx.color = Color.black;
        tx.alignment = TextAnchor.MiddleLeft;
        tx.horizontalOverflow = HorizontalWrapMode.Wrap;
        tx.text = run.BuildDisplayLine();
        LayoutElement leT = textGo.AddComponent<LayoutElement>();
        leT.flexibleWidth = 1f;
        leT.minWidth = 320f;

        string rid = run.RunId;
        GameObject resumeGo = CreateRowButton(row.transform, _font, "Resume", () => LaunchResume(rid));
        LayoutElement resumeLe = resumeGo.AddComponent<LayoutElement>();
        resumeLe.minWidth = 88f;
        GameObject overGo = CreateRowButton(row.transform, _font, "Overwrite", () => LaunchOverwrite(rid));
        LayoutElement overLe = overGo.AddComponent<LayoutElement>();
        overLe.minWidth = 96f;

        GameObject delGo = CreateRowButton(row.transform, _font, "Delete", () => ConfirmDeleteRun(rid));
        delGo.GetComponent<Image>().color = new Color(0.72f, 0.25f, 0.25f, 1f);
        LayoutElement delLe = delGo.AddComponent<LayoutElement>();
        delLe.minWidth = 80f;

        _rowPool.Add(row);
    }

    static void AddButtonLabel(Transform parent, string label, Font font)
    {
        GameObject t = new GameObject("Text", typeof(RectTransform), typeof(Text));
        t.transform.SetParent(parent, false);
        RectTransform trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        Text tex = t.GetComponent<Text>();
        tex.font = font;
        tex.text = label;
        tex.fontSize = 15;
        tex.color = Color.white;
        tex.alignment = TextAnchor.MiddleCenter;
    }

    static GameObject CreateRowButton(Transform parent, Font font, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.42f, 0.65f, 1f);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        AddButtonLabel(go.transform, label, font);
        return go;
    }

    static GameObject CreateFooterButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 36f);
        go.GetComponent<Image>().color = new Color(0.22f, 0.48f, 0.35f, 1f);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        AddButtonLabel(go.transform, label, f);
        return go;
    }

    static InputField CreateInputField(Transform parent, Font font, string initial)
    {
        GameObject go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = Color.white;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280f, 36f);
        InputField field = go.GetComponent<InputField>();
        field.lineType = InputField.LineType.SingleLine;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 4f);
        trt.offsetMax = new Vector2(-8f, -4f);
        Text tx = textGo.GetComponent<Text>();
        tx.font = font;
        tx.fontSize = 15;
        tx.color = Color.black;
        tx.supportRichText = false;
        field.textComponent = tx;

        GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderGo.transform.SetParent(go.transform, false);
        RectTransform prt = placeholderGo.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(8f, 4f);
        prt.offsetMax = new Vector2(-8f, -4f);
        Text pt = placeholderGo.GetComponent<Text>();
        pt.font = font;
        pt.fontSize = 15;
        pt.color = new Color(0.4f, 0.4f, 0.4f);
        pt.text = "run-id";
        field.placeholder = pt;

        field.text = initial;
        return field;
    }

    void AddInfoRow(Transform parent, string message)
    {
        GameObject row = new GameObject("InfoRow", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().minHeight = 28f;
        Text tx = row.GetComponent<Text>();
        tx.font = _font;
        tx.fontSize = 15;
        tx.color = new Color(0.2f, 0.2f, 0.25f);
        tx.text = message;
        _rowPool.Add(row);
    }

    void BuildConfirmDialog(Transform parent)
    {
        _confirmRoot = new GameObject("ConfirmDeleteDialog", typeof(RectTransform), typeof(Image));
        _confirmRoot.transform.SetParent(parent, false);
        RectTransform rt = _confirmRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        _confirmRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(_confirmRoot.transform, false);
        RectTransform brt = box.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(720f, 180f);
        brt.anchoredPosition = Vector2.zero;
        box.GetComponent<Image>().color = new Color(0.96f, 0.96f, 0.98f, 1f);

        GameObject msg = new GameObject("Message", typeof(RectTransform), typeof(Text));
        msg.transform.SetParent(box.transform, false);
        RectTransform mrt = msg.GetComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0f, 1f);
        mrt.anchorMax = new Vector2(1f, 1f);
        mrt.pivot = new Vector2(0.5f, 1f);
        mrt.anchoredPosition = new Vector2(0f, -16f);
        mrt.sizeDelta = new Vector2(-24f, 88f);
        _confirmText = msg.GetComponent<Text>();
        _confirmText.font = _font;
        _confirmText.fontSize = 16;
        _confirmText.color = Color.black;
        _confirmText.alignment = TextAnchor.UpperLeft;
        _confirmText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _confirmText.text = "";

        GameObject row = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(box.transform, false);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.5f, 0f);
        rrt.anchorMax = new Vector2(0.5f, 0f);
        rrt.pivot = new Vector2(0.5f, 0f);
        rrt.anchoredPosition = new Vector2(0f, 14f);
        rrt.sizeDelta = new Vector2(520f, 44f);
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        GameObject cancel = CreateFooterButton(row.transform, "Cancel", CancelDelete);
        cancel.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 36f);
        cancel.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.4f, 1f);

        GameObject confirm = CreateFooterButton(row.transform, "Delete run", DeletePendingRun);
        confirm.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 36f);
        confirm.GetComponent<Image>().color = new Color(0.72f, 0.25f, 0.25f, 1f);

        _confirmRoot.SetActive(false);
    }

    void ConfirmDeleteRun(string runId)
    {
        if (!Application.isEditor)
        {
            SetStatus("Only available in the Unity Editor.");
            return;
        }

        if (string.IsNullOrEmpty(runId))
        {
            return;
        }

        if (!allowDeleteBestOrCurrent && (string.Equals(runId, "best", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(runId, "current", StringComparison.OrdinalIgnoreCase)))
        {
            SetStatus("Refusing to delete 'best' or 'current'. Toggle allowDeleteBestOrCurrent on TrainingRunsBrowser if you really want this.");
            return;
        }

        _pendingDeleteRunId = runId;
        if (_confirmText != null)
        {
            _confirmText.text =
                $"Delete run '{runId}'?\n\n" +
                "This permanently deletes the folder under results/ (including TensorBoard logs and checkpoints).";
        }

        if (_confirmRoot != null)
        {
            _confirmRoot.SetActive(true);
        }
    }

    void CancelDelete()
    {
        _pendingDeleteRunId = null;
        if (_confirmRoot != null)
        {
            _confirmRoot.SetActive(false);
        }
    }

    void DeletePendingRun()
    {
        string runId = _pendingDeleteRunId;
        CancelDelete();

        string results = MlRunsScanner.GetResultsDirectory();
        if (string.IsNullOrEmpty(results))
        {
            SetStatus("Could not locate results/ directory.");
            return;
        }

        string dir = Path.Combine(results, runId);
        if (!Directory.Exists(dir))
        {
            SetStatus($"Run folder not found: {dir}");
            RefreshList();
            return;
        }

        try
        {
            Directory.Delete(dir, recursive: true);
            SetStatus($"Deleted run: {runId}");
        }
        catch (Exception e)
        {
            SetStatus($"Delete failed: {e.Message}");
            UnityEngine.Debug.LogException(e);
        }

        RefreshList();
    }

    void PruneRunsKeepBestCurrent()
    {
        if (!Application.isEditor)
        {
            SetStatus("Only available in the Unity Editor.");
            return;
        }

        string results = MlRunsScanner.GetResultsDirectory();
        if (string.IsNullOrEmpty(results) || !Directory.Exists(results))
        {
            SetStatus("No results/ directory found.");
            return;
        }

        int deleted = 0;
        foreach (string dir in Directory.GetDirectories(results))
        {
            string name = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (string.Equals(name, "best", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "current", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                Directory.Delete(dir, recursive: true);
                deleted++;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"Prune runs: failed to delete {dir}: {e.Message}");
            }
        }

        SetStatus(deleted > 0
            ? $"Pruned {deleted} runs (kept best/current)."
            : "Nothing to prune (only best/current found).");

        RefreshList();
    }
}
