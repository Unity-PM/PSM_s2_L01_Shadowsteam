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

    [MenuItem("Window/UI Toolkit/AI Generator")]
    public static void Open() =>
        GetWindow<AIUIGenerator>("AI UI Generator");

    void OnGUI()
    {
        GUILayout.Label("AI UI Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        _apiKey = EditorGUILayout.PasswordField("API Key", _apiKey);
        _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);
        EditorGUILayout.Space(8);

        GUILayout.Label("Describe your panel:");
        _prompt = EditorGUILayout.TextArea(_prompt,
            GUILayout.Height(80));
        EditorGUILayout.Space(8);

        EditorGUI.BeginDisabledGroup(_isLoading ||
            string.IsNullOrEmpty(_prompt) ||
            string.IsNullOrEmpty(_apiKey));

        if (GUILayout.Button(_isLoading ? "Generating..." : "Generate"))
            EditorCoroutineUtility.StartCoroutine(Generate(), this);

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(_status, MessageType.None);
    }

    private IEnumerator Generate()
    {
        _isLoading = true;
        _status = "Calling Claude API...";
        Repaint();

        var requestBody = BuildRequestBody(_prompt);
        var req = new UnityWebRequest(
            "https://api.anthropic.com/v1/messages",
            "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("x-api-key", _apiKey);
        req.SetRequestHeader("anthropic-version", "2023-06-01");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            _status = $"Error: {req.error}";
            _isLoading = false;
            Repaint();
            yield break;
        }

        _status = "Parsing response...";
        Repaint();

        var json = ParseClaudeResponse(req.downloadHandler.text);
        WriteFiles(json);

        _status = "Done! Assets created.";
        _isLoading = false;
        Repaint();
    }

    private string BuildRequestBody(string userPrompt)
    {
        var systemPrompt = @"You are a Unity UI Toolkit code generator.
The project uses a factory system with these registered widget types:
  stat-bar   → StatBar(label, color)
  gold-label → GoldLabel(value)
  spacer     → Spacer()
  header     → HeaderLabel(value)

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
      ""code"": ""[UIWidget(\""my-widget\"")]\\npublic class MyWidget : VisualElement, IConfigurable { ... }""
    }
  ]
}
newWidgets is only populated when the user needs a widget type
that does not exist in the registered list above.";

        return $@"{{
        ""model"":      ""claude-sonnet-4-20250514"",
        ""max_tokens"": 1024,
        ""system"":     ""{EscapeJson(systemPrompt)}"",
        ""messages"":   [{{""role"":""user"",""content"":""{EscapeJson(userPrompt)}""}}]
    }}";
    }

    private string EscapeJson(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\n", "\\n")
         .Replace("\r", "");

    private string ParseClaudeResponse(string raw)
    {
        var wrapper = JsonUtility.FromJson<ClaudeResponse>(raw);
        return wrapper.content[0].text;
    }

    private void WriteFiles(string json)
    {
        var result = JsonUtility.FromJson<GeneratedPanel>(json);

        Directory.CreateDirectory(_outputPath);
        var so = ScriptableObject.CreateInstance<PanelDefinitionSO>();
        so.panelName = result.panelName;
        so.rootUssClass = result.rootUssClass;
        so.widgets = result.widgets;

        var soPath = $"{_outputPath}/{result.panelName}.asset";
        AssetDatabase.CreateAsset(so, soPath);

        foreach (var w in result.newWidgets)
        {
            var csPath = $"{_outputPath}/{w.className}.cs";
            File.WriteAllText(csPath, w.code);
            Debug.Log($"[AIGenerator] wrote {csPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = so;
    }

    [System.Serializable]
    class ClaudeResponse
    {
        public ContentBlock[] content;
    }
    [System.Serializable]
    class ContentBlock { public string text; }

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