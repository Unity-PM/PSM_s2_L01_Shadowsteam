using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class AIUIGenerator : EditorWindow
{
    private string _prompt = "";
    private string _apiKey = "";
    private string _outputPath = "Assets/UI/Generated";
    private string _status = "Ready";
    private bool _isLoading = false;

    // store key in EditorPrefs so it survives editor restarts and stays out of source control
    private const string API_KEY_PREF = "AIUIGen_GeminiApiKey";

    [MenuItem("Window/UI Toolkit/AI Generator")]
    public static void Open() =>
        GetWindow<AIUIGenerator>("AI UI Generator");

    void OnEnable()
    {
        _apiKey = EditorPrefs.GetString(API_KEY_PREF, "");
    }

    void OnGUI()
    {
        GUILayout.Label("AI UI Generator (Gemini)", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        EditorGUI.BeginChangeCheck();
        _apiKey = EditorGUILayout.PasswordField("Gemini API Key", _apiKey);
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetString(API_KEY_PREF, _apiKey);

        _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);
        EditorGUILayout.Space(8);

        GUILayout.Label("Describe your panel:");
        _prompt = EditorGUILayout.TextArea(_prompt, GUILayout.Height(80));
        EditorGUILayout.Space(8);

        EditorGUI.BeginDisabledGroup(_isLoading ||
            string.IsNullOrEmpty(_prompt) ||
            string.IsNullOrEmpty(_apiKey));

        if (GUILayout.Button(_isLoading ? "Generating..." : "Generate"))
            EditorCoroutineUtility.StartCoroutine(Generate(), this);

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(_status, MessageType.None);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Get a free API key at aistudio.google.com",
            EditorStyles.miniLabel);
    }

    // ─────────────────────────────────────────────────────────────
    // Request
    // ─────────────────────────────────────────────────────────────

    private IEnumerator Generate()
    {
        _isLoading = true;
        _status = "Calling Gemini API...";
        Repaint();

        // Gemini endpoint — responseMimeType forces pure JSON output, no markdown fences
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/" +
                  $"gemini-1.5-flash:generateContent?key={_apiKey}";

        var requestBody = BuildRequestBody(_prompt);
        var req = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            _status = $"Error: {req.error}\n{req.downloadHandler.text}";
            _isLoading = false;
            Repaint();
            yield break;
        }

        _status = "Parsing response...";
        Repaint();

        var json = ParseGeminiResponse(req.downloadHandler.text);
        if (json == null)
        {
            _status = "Failed to parse Gemini response. Check console.";
            _isLoading = false;
            Repaint();
            yield break;
        }

        WriteFiles(json);

        _status = "Done! Assets created.";
        _isLoading = false;
        Repaint();
    }

    // ─────────────────────────────────────────────────────────────
    // Build request body
    // ─────────────────────────────────────────────────────────────

    private string BuildRequestBody(string userPrompt)
    {
        // system prompt is prepended to the user message as a "user" turn
        // because Gemini 1.5 Flash supports a systemInstruction field — we use that
        var systemInstruction = @"You are a Unity UI Toolkit code generator.
The project uses a factory system with these registered widget types:
  stat-bar   -> StatBar(label, color)
  gold-label -> GoldLabel(value)
  spacer     -> Spacer()
  header     -> HeaderLabel(value)

When the user describes a UI panel, respond ONLY with a JSON object.
No explanation, no markdown, no backticks. Exactly this shape:
{
  ""panelName"": ""snake-case-name"",
  ""rootUssClass"": ""css-class"",
  ""widgets"": [
    { ""typeId"": ""stat-bar"", ""name"": ""hp-bar"",
      ""value"": ""HP"", ""color"": ""#E85555"", ""ussClass"": """" }
  ],
  ""newWidgets"": [
    {
      ""className"": ""MyWidget"",
      ""typeId"":    ""my-widget"",
      ""code"": ""[UIWidget(\\""my-widget\\"")] public class MyWidget : VisualElement, IConfigurable { }""
    }
  ]
}
newWidgets is only populated when the user needs a widget type
that does not exist in the registered list above.";

        // Gemini request shape
        return $@"{{
    ""systemInstruction"": {{
        ""parts"": [{{ ""text"": ""{EscapeJson(systemInstruction)}"" }}]
    }},
    ""contents"": [{{
        ""parts"": [{{ ""text"": ""{EscapeJson(userPrompt)}"" }}]
    }}],
    ""generationConfig"": {{
        ""responseMimeType"": ""application/json""
    }}
}}";
    }

    private string EscapeJson(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\n", "\\n")
         .Replace("\r", "");

    // ─────────────────────────────────────────────────────────────
    // Parse Gemini response envelope
    // ─────────────────────────────────────────────────────────────

    private string ParseGeminiResponse(string raw)
    {
        try
        {
            var response = JsonUtility.FromJson<GeminiResponse>(raw);
            var text = response.candidates[0].content.parts[0].text;
            return SanitizeJson(text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIGenerator] Failed to parse Gemini envelope: {e.Message}\nRaw: {raw}");
            return null;
        }
    }

    // strip accidental ```json ... ``` wrapping just in case
    private string SanitizeJson(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("```"))
        {
            int firstNewline = raw.IndexOf('\n');
            if (firstNewline >= 0) raw = raw.Substring(firstNewline + 1);
            int lastFence = raw.LastIndexOf("```");
            if (lastFence >= 0) raw = raw.Substring(0, lastFence);
        }
        return raw.Trim();
    }

    // ─────────────────────────────────────────────────────────────
    // Write generated files
    // ─────────────────────────────────────────────────────────────

    private void WriteFiles(string json)
    {
        GeneratedPanel result;
        try
        {
            result = JsonUtility.FromJson<GeneratedPanel>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIGenerator] Failed to parse generated JSON: {e.Message}\nJSON: {json}");
            _status = "JSON parse error — check console.";
            return;
        }

        if (string.IsNullOrEmpty(result.panelName))
        {
            Debug.LogError("[AIGenerator] panelName is empty — generation may have failed.");
            _status = "Bad response — panelName missing.";
            return;
        }

        Directory.CreateDirectory(_outputPath);

        // ── 1. create the ScriptableObject asset ──
        var so = ScriptableObject.CreateInstance<PanelDefinitionSO>();
        so.panelName = result.panelName;
        so.rootUssClass = result.rootUssClass;
        so.widgets = result.widgets;

        var soPath = $"{_outputPath}/{result.panelName}.asset";
        AssetDatabase.CreateAsset(so, soPath);
        Debug.Log($"[AIGenerator] created SO: {soPath}");

        // ── 2. write any new widget .cs files ──
        if (result.newWidgets != null)
        {
            foreach (var w in result.newWidgets)
            {
                if (string.IsNullOrEmpty(w.className) || string.IsNullOrEmpty(w.code))
                    continue;
                var csPath = $"{_outputPath}/{w.className}.cs";
                File.WriteAllText(csPath, w.code);
                Debug.Log($"[AIGenerator] wrote widget: {csPath}");
            }
        }

        // ── 3. hot-reload ──
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = so;
    }

    // ─────────────────────────────────────────────────────────────
    // Serialization helpers — Gemini envelope
    // ─────────────────────────────────────────────────────────────

    [System.Serializable] class GeminiResponse { public GeminiCandidate[] candidates; }
    [System.Serializable] class GeminiCandidate { public GeminiContent content; }
    [System.Serializable] class GeminiContent { public GeminiPart[] parts; }
    [System.Serializable] class GeminiPart { public string text; }

    // ─────────────────────────────────────────────────────────────
    // Serialization helpers — generated panel schema
    // ─────────────────────────────────────────────────────────────

    [System.Serializable]
    class GeneratedPanel
    {
        public string panelName;
        public string rootUssClass;
        public List<WidgetConfig> widgets;
        public List<NewWidget> newWidgets;
    }

    [System.Serializable]
    class NewWidget
    {
        public string className;
        public string typeId;
        public string code;
    }
}