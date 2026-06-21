using System.Collections;
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

    private const string API_KEY_PREF = "AIUIGen_GroqApiKey";

    [MenuItem("Window/UI Toolkit/AI Generator")]
    public static void Open() =>
        GetWindow<AIUIGenerator>("AI UI Generator");

    void OnEnable()
    {
        _apiKey = EditorPrefs.GetString(API_KEY_PREF, "");
    }

    void OnGUI()
    {
        GUILayout.Label("AI UI Generator (Groq)", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        EditorGUI.BeginChangeCheck();
        _apiKey = EditorGUILayout.PasswordField("Groq API Key", _apiKey);
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetString(API_KEY_PREF, _apiKey);

        _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);
        EditorGUILayout.Space(8);

        GUILayout.Label("Describe your UI:");
        _prompt = EditorGUILayout.TextArea(_prompt, GUILayout.Height(100));
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
        EditorGUILayout.LabelField("Get a free API key at console.groq.com",
            EditorStyles.miniLabel);
    }

    // ─────────────────────────────────────────────────────────────
    // Request
    // ─────────────────────────────────────────────────────────────

    private IEnumerator Generate()
    {
        _isLoading = true;
        _status = "Calling Groq API...";
        Repaint();

        var req = new UnityWebRequest(
            "https://api.groq.com/openai/v1/chat/completions", "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(BuildRequestBody(_prompt));
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {_apiKey}");

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

        var json = ParseGroqResponse(req.downloadHandler.text);
        if (json == null)
        {
            _status = "Failed to parse response. Check console.";
            _isLoading = false;
            Repaint();
            yield break;
        }

        WriteFiles(json);

        _status = "Done! Check console for next steps.";
        _isLoading = false;
        Repaint();
    }

    // ─────────────────────────────────────────────────────────────
    // Build request body
    // ─────────────────────────────────────────────────────────────

    private string BuildRequestBody(string userPrompt)
    {
        var systemPrompt = @"You are a Unity UI Toolkit file generator.
You output three files: a UXML layout file, a USS stylesheet, and a C# MonoBehaviour.

Unity UI Toolkit rules you must follow:

UXML rules:
- Root element is always <ui:UXML xmlns:ui=""UnityEngine.UIElements"">
- Use only: <ui:VisualElement> <ui:Label> <ui:Button> <ui:ScrollView> <ui:TextField> <ui:Toggle> <ui:Slider>
- Attach stylesheet as first child: <Style src=""PanelName.uss"" />
- Every element that needs styling gets a class attribute
- Every button gets a name attribute matching its action e.g. name=""close-btn""

USS rules:
- Flexbox only. Do NOT write display:flex — every VisualElement is already flex
- flex-direction: row or column
- To center on screen: position:absolute; left:50%; top:50%; translate:-50% -50%
- To anchor bottom: position:absolute; bottom:0; left:0; width:100%
- Spacing between siblings: use margin-bottom or margin-right on children — NOT the gap property
- Text alignment: -unity-text-align only — NOT text-align
- Valid -unity-text-align values: upper-left upper-center upper-right middle-left middle-center middle-right lower-left lower-center lower-right
- Supported pseudo-classes: :hover :active :focus
- NOT supported: CSS Grid, calc(), ::before, ::after, gap, display:flex, transform, nth-child

C# rules:
- Class name matches panelName, inherits MonoBehaviour
- OnEnable: get root via GetComponent<UIDocument>().rootVisualElement, then Q<Button> each named button, register clicked listeners
- OnDisable: call rootVisualElement.Clear() to clean up
- Every button has its own private void handler method with a TODO comment
- Include a public void Refresh() stub for updating labels or values later
- Organize with #region: Fields / Lifecycle / Handlers / Data

Respond ONLY with a JSON object. No explanation, no markdown, no backticks.
Exactly this shape:
{
  ""panelName"": ""PascalCaseName"",
  ""uxml"": ""<ui:UXML xmlns:ui=\""UnityEngine.UIElements\"">\n  ...\n</ui:UXML>"",
  ""uss"": "".root {\n    ...\n}"",
  ""cs"": ""using UnityEngine;\nusing UnityEngine.UIElements;\n...""
}
Use \n for newlines and \t for indentation inside all three file strings.
Every button in UXML must have a matching clicked listener in the C# file.
The USS must cover every class used in the UXML.";

        return $@"{{
    ""model"": ""llama-3.3-70b-versatile"",
    ""messages"": [
        {{ ""role"": ""system"", ""content"": ""{EscapeJson(systemPrompt)}"" }},
        {{ ""role"": ""user"",   ""content"": ""{EscapeJson(userPrompt)}"" }}
    ],
    ""response_format"": {{ ""type"": ""json_object"" }},
    ""temperature"": 0.2,
    ""max_tokens"": 4096
}}";
    }

    private string EscapeJson(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\n", "\\n")
         .Replace("\r", "");

    // ─────────────────────────────────────────────────────────────
    // Parse response
    // ─────────────────────────────────────────────────────────────

    private string ParseGroqResponse(string raw)
    {
        try
        {
            var response = JsonUtility.FromJson<GroqResponse>(raw);
            var text = response.choices[0].message.content;
            return SanitizeJson(text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIGenerator] Parse failed: {e.Message}\nRaw: {raw}");
            return null;
        }
    }

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
    // Write files
    // ─────────────────────────────────────────────────────────────

    private void WriteFiles(string json)
    {
        GeneratedFiles result;
        try
        {
            result = JsonUtility.FromJson<GeneratedFiles>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIGenerator] JSON parse failed: {e.Message}\nJSON: {json}");
            _status = "JSON parse error — check console.";
            return;
        }

        if (string.IsNullOrEmpty(result.panelName))
        {
            Debug.LogError("[AIGenerator] panelName missing.");
            _status = "Bad response — panelName missing.";
            return;
        }

        Directory.CreateDirectory(_outputPath);

        var uxml = Unescape(result.uxml);
        var uss = Unescape(result.uss);
        var cs = Unescape(result.cs);

        var uxmlPath = $"{_outputPath}/{result.panelName}.uxml";
        var ussPath = $"{_outputPath}/{result.panelName}.uss";
        var csPath = $"{_outputPath}/{result.panelName}.cs";

        File.WriteAllText(uxmlPath, uxml);
        File.WriteAllText(ussPath, uss);
        File.WriteAllText(csPath, cs);

        Debug.Log($"[AIGenerator] wrote:\n  {uxmlPath}\n  {ussPath}\n  {csPath}");

        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        AssetDatabase.ImportAsset(uxmlPath);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(uxmlPath);

        Debug.Log(
            $"[AIGenerator] Done!\n\n" +
            $"Next steps:\n" +
            $"  1. Wait for recompile\n" +
            $"  2. Create empty GameObject → rename '{result.panelName}UI'\n" +
            $"  3. Add Component → UI Document\n" +
            $"  4. Assign Panel Settings to UI Document\n" +
            $"  5. Drag '{result.panelName}.uxml' into Source Asset slot\n" +
            $"  6. Add Component → {result.panelName}\n" +
            $"  7. Hit Play\n\n" +
            $"Edit your files:\n" +
            $"  Layout  → {result.panelName}.uxml\n" +
            $"  Styles  → {result.panelName}.uss\n" +
            $"  Logic   → {result.panelName}.cs"
        );
    }

    private string Unescape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"");
    }

    // ─────────────────────────────────────────────────────────────
    // Serialization
    // ─────────────────────────────────────────────────────────────

    [System.Serializable] class GroqResponse { public GroqChoice[] choices; }
    [System.Serializable] class GroqChoice { public GroqMessage message; }
    [System.Serializable] class GroqMessage { public string content; }

    [System.Serializable]
    class GeneratedFiles
    {
        public string panelName;
        public string uxml;
        public string uss;
        public string cs;
    }
}