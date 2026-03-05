using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class AISuggester : MonoBehaviour
{
    [System.Serializable]
    public class CarStyleOption
    {
        public string label;
        public string description;
        public Color bodyColor;
    }

    public TMP_InputField themeInput;
    public TextMeshProUGUI suggestionsText;

    [Header("Gemini AI (optional)")]
    public bool useGemini = false;
    [TextArea]
    public string geminiInstructionHint =
        "Design 3–5 distinct car paint jobs (body colors only) that match the user theme. Aim for showroom-ready, cinematic lighting friendly palettes.";
    public string geminiApiKey;
    public string geminiModel = "gemini-1.5-flash";
    [Range(3, 8)]
    public int aiOptionCount = 4;

    readonly List<CarStyleOption> _options = new List<CarStyleOption>();
    int _currentIndex = -1;
    string _lastThemeRaw = string.Empty;

    void Awake()
    {
        if (suggestionsText == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var go = new GameObject("AI Style Suggestions");
                go.layer = canvas.gameObject.layer;
                go.transform.SetParent(canvas.transform, false);

                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0, -40);
                rect.sizeDelta = new Vector2(900, 220);

                var text = go.AddComponent<TextMeshProUGUI>();
                text.fontSize = 26;
                text.alignment = TextAlignmentOptions.Center;
                text.enableWordWrapping = true;
                text.text = "Type a theme like \"sporty\" or \"luxury\" and press STYLE to see AI suggestions.";

                suggestionsText = text;
            }
        }
    }

    public void SuggestStyle()
    {
        var rawTheme = themeInput != null ? themeInput.text : string.Empty;

        // If the user clicks STYLE repeatedly with the same theme,
        // cycle through the existing options instead of regenerating.
        if (!string.IsNullOrEmpty(_lastThemeRaw) &&
            _options.Count > 0 &&
            string.Equals(rawTheme, _lastThemeRaw, System.StringComparison.OrdinalIgnoreCase))
        {
            NextSuggestedStyle();
            return;
        }

        _lastThemeRaw = rawTheme;

        if (useGemini && !string.IsNullOrEmpty(geminiApiKey))
        {
            StartCoroutine(SuggestStyleFromGemini(rawTheme));
        }
        else
        {
            SuggestStyleLocal(rawTheme);
        }
    }

    public void NextSuggestedStyle()
    {
        if (_options.Count == 0)
            return;

        _currentIndex = (_currentIndex + 1) % _options.Count;
        ApplyCurrentOption();

        var rawTheme = themeInput != null ? themeInput.text : string.Empty;
        UpdateSuggestionsText(rawTheme);
    }

    public void ApplyOptionIndex(int optionIndexOneBased)
    {
        if (_options.Count == 0)
            return;

        var idx = Mathf.Clamp(optionIndexOneBased - 1, 0, _options.Count - 1);
        _currentIndex = idx;
        ApplyCurrentOption();

        var rawTheme = themeInput != null ? themeInput.text : string.Empty;
        UpdateSuggestionsText(rawTheme);
    }

    public void ApplyOptionFromDropdown(int dropdownIndex)
    {
        if (_options.Count == 0)
            return;

        var idx = Mathf.Clamp(dropdownIndex, 0, _options.Count - 1);
        _currentIndex = idx;
        ApplyCurrentOption();

        var rawTheme = themeInput != null ? themeInput.text : string.Empty;
        UpdateSuggestionsText(rawTheme);
    }

    public void ChangeTiresStyle()
    {
        var rawTheme = themeInput != null ? themeInput.text : string.Empty;
        var theme = rawTheme.ToLowerInvariant();

        var activeCar = FindActiveCar();
        if (activeCar == null)
            return;

        Color tireColor;

        if (theme.Contains("sport"))
            tireColor = new Color(0.05f, 0.05f, 0.05f);            // deep black performance tire
        else if (theme.Contains("luxury"))
            tireColor = new Color(0.02f, 0.02f, 0.02f);            // piano black
        else if (theme.Contains("military") || theme.Contains("army") || theme.Contains("offroad"))
            tireColor = new Color(0.12f, 0.10f, 0.08f);            // dusty dark brown
        else if (theme.Contains("ice") || theme.Contains("snow"))
            tireColor = new Color(0.12f, 0.12f, 0.14f);            // winter compound grey
        else
            tireColor = new Color(0.08f, 0.08f, 0.08f);            // default dark rubber

        var renderers = activeCar.GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            var goName = r.gameObject.name.ToLowerInvariant();

            var isTireObject =
                goName.Contains("tire") ||
                goName.Contains("tyre") ||
                goName.Contains("wheel") ||
                goName.Contains("rim");

            foreach (var mat in r.materials)
            {
                var matName = mat.name.ToLowerInvariant();
                var isTireMaterial =
                    matName.Contains("tire") ||
                    matName.Contains("tyre") ||
                    matName.Contains("wheel") ||
                    matName.Contains("rim");

                // Fallback for this project where tire materials are generic black ones.
                var looksLikeBlackRubber =
                    matName.Contains("black") &&
                    !matName.Contains("glass") &&
                    !matName.Contains("steel");

                if (!isTireObject && !isTireMaterial && !looksLikeBlackRubber)
                    continue;

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", tireColor);

                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", tireColor);

                if (mat.HasProperty("_Smoothness"))
                {
                    var smooth = theme.Contains("sport") || theme.Contains("luxury") ? 0.7f : 0.4f;
                    mat.SetFloat("_Smoothness", smooth);
                }
            }
        }

        Debug.Log($"AI tire style applied for theme \"{rawTheme}\" with color {tireColor}");
    }

    void SuggestStyleLocal(string rawTheme)
    {
        var theme = rawTheme.ToLowerInvariant().Trim();

        _options.Clear();
        GenerateOptions(theme);

        if (_options.Count == 0)
        {
            AddOption(
                "Surprise Mix",
                "Playful, AI-picked palette based on your theme.",
                Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.4f, 1f)
            );
        }

        _currentIndex = 0;
        ApplyCurrentOption();
        UpdateSuggestionsText(rawTheme);
    }

    void GenerateOptions(string theme)
    {
        if (theme.Contains("sport"))
        {
            AddOption("Track Day Red", "Aggressive racing red with dark details.", new Color(0.85f, 0.1f, 0.1f));
            AddOption("Midnight Racer", "Deep midnight blue for stealth performance.", new Color(0.05f, 0.08f, 0.2f));
            AddOption("Neon Sprint", "Black base with neon accent vibe.", new Color(0.0f, 0.9f, 0.4f));
        }
        else if (theme.Contains("luxury"))
        {
            AddOption("Obsidian Gloss", "High-gloss piano black limousine look.", new Color(0.03f, 0.03f, 0.03f));
            AddOption("Champagne Gold", "Soft metallic gold for a VIP feel.", new Color(0.86f, 0.75f, 0.48f));
            AddOption("Pearl White", "Clean pearl white executive finish.", new Color(0.95f, 0.95f, 0.98f));
        }
        else if (theme.Contains("modern"))
        {
            AddOption("Concrete Grey", "Minimal matte concrete grey.", new Color(0.4f, 0.42f, 0.45f));
            AddOption("Studio White", "Pure gallery-white show car.", Color.white);
            AddOption("Slate Two-Tone", "Dark slate body with subtle highlights.", new Color(0.18f, 0.2f, 0.23f));
        }
        else if (theme.Contains("cyberpunk"))
        {
            AddOption("Neon Violet", "Glowing neon violet hero car.", new Color(0.7f, 0.1f, 0.9f));
            AddOption("Electric Cyan", "Cyan-tinted body with sci‑fi vibe.", new Color(0.0f, 0.8f, 1.0f));
            AddOption("Night City Mix", "Dark chassis with toxic green highlights.", new Color(0.07f, 0.9f, 0.3f));
        }
        else if (theme.Contains("military") || theme.Contains("army"))
        {
            AddOption("Combat Green", "Matte olive green with tactical vibe.", new Color(0.19f, 0.34f, 0.13f));
            AddOption("Desert Camo", "Warm sand‑tan desert patrol look.", new Color(0.66f, 0.58f, 0.38f));
            AddOption("Urban Camo", "Muted grey‑green for city operations.", new Color(0.33f, 0.37f, 0.33f));
        }
        else if (theme.Contains("nature") || theme.Contains("forest"))
        {
            AddOption("Deep Forest", "Rich evergreen off‑road look.", new Color(0.1f, 0.35f, 0.15f));
            AddOption("Sand Trail", "Warm desert sand explorer.", new Color(0.83f, 0.73f, 0.55f));
            AddOption("Ocean Breeze", "Cool teal coastal cruiser.", new Color(0.1f, 0.6f, 0.6f));
        }
        else if (theme.Contains("sunset"))
        {
            AddOption("Sunset Orange", "Bright orange with golden tint.", new Color(1f, 0.5f, 0.1f));
            AddOption("Pink Horizon", "Soft magenta inspired by late sunsets.", new Color(0.9f, 0.35f, 0.6f));
            AddOption("Twilight Purple", "Purple-blue gradient vibe.", new Color(0.35f, 0.1f, 0.5f));
        }
        else
        {
            AddOption("Clean Gloss", "Simple, glossy showroom white.", Color.white);
            AddOption("Stealth", "Understated matte-inspired dark grey.", new Color(0.15f, 0.15f, 0.17f));
            AddOption("Color Pop", "Vibrant, playful random color.", Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.5f, 1f));
        }
    }

    void AddOption(string label, string description, Color bodyColor)
    {
        _options.Add(new CarStyleOption
        {
            label = label,
            description = description,
            bodyColor = bodyColor
        });
    }

    void ApplyCurrentOption()
    {
        if (_currentIndex < 0 || _currentIndex >= _options.Count)
            return;

        var option = _options[_currentIndex];
        var activeCar = FindActiveCar();
        if (activeCar == null)
            return;

        var renderers = activeCar.GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                var matName = mat.name.ToLowerInvariant();
                if (matName.Contains("glass") || matName.Contains("steel") || matName.Contains("tire") || matName.Contains("rubber"))
                    continue;

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", option.bodyColor);

                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", option.bodyColor);
            }
        }

        Debug.Log($"AI style applied: {option.label} ({option.bodyColor})");
    }

    static GameObject FindActiveCar()
    {
        var cars = GameObject.FindGameObjectsWithTag("car");
        foreach (var car in cars)
        {
            if (car.activeInHierarchy)
                return car;
        }

        Debug.LogWarning("AISuggester: No active car found in the scene.");
        return null;
    }

    void UpdateSuggestionsText(string rawTheme)
    {
        if (suggestionsText == null)
            return;

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(rawTheme))
            sb.AppendLine($"Theme: {rawTheme}");

        sb.AppendLine("AI customization options:");

        for (int i = 0; i < _options.Count; i++)
        {
            var o = _options[i];
            sb.AppendLine($"{i + 1}. {o.label} – {o.description}");
        }

        sb.AppendLine();
        sb.AppendLine("Press STYLE again to cycle through these looks.");

        suggestionsText.text = sb.ToString();
    }

    [System.Serializable]
    class GeminiRequest
    {
        public GeminiContent[] contents;
    }

    [System.Serializable]
    class GeminiContent
    {
        public GeminiPart[] parts;
    }

    [System.Serializable]
    class GeminiPart
    {
        public string text;
    }

    [System.Serializable]
    class GeminiRoot
    {
        public GeminiCandidate[] candidates;
    }

    [System.Serializable]
    class GeminiCandidate
    {
        public GeminiContent content;
    }

    [System.Serializable]
    class ColorDto
    {
        public float r;
        public float g;
        public float b;
    }

    [System.Serializable]
    class CarStyleOptionDto
    {
        public string label;
        public string description;
        public ColorDto bodyColor;
    }

    [System.Serializable]
    class GeminiOptionsWrapper
    {
        public CarStyleOptionDto[] options;
    }

    IEnumerator SuggestStyleFromGemini(string rawTheme)
    {
        var theme = string.IsNullOrWhiteSpace(rawTheme) ? "surprise me" : rawTheme.Trim();

        if (suggestionsText != null)
        {
            suggestionsText.text = "Talking to Gemini...\nGenerating car showroom skins...";
        }

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{geminiModel}:generateContent?key={geminiApiKey}";

        var prompt = BuildGeminiPrompt(theme);

        var body = new GeminiRequest
        {
            contents = new[]
            {
                new GeminiContent
                {
                    parts = new[]
                    {
                        new GeminiPart { text = prompt }
                    }
                }
            }
        };

        var json = JsonUtility.ToJson(body);
        var request = new UnityWebRequest(url, "POST");
        var bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Gemini request failed: {request.error}");
            SuggestStyleLocal(rawTheme);
            yield break;
        }

        var responseJson = request.downloadHandler.text;

        GeminiRoot root = null;
        try
        {
            root = JsonUtility.FromJson<GeminiRoot>(responseJson);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to parse Gemini root: " + e.Message);
        }

        var optionsJson = ExtractOptionsJson(root);

        if (string.IsNullOrEmpty(optionsJson))
        {
            Debug.LogWarning("Gemini response did not contain style options JSON. Falling back to local styles.");
            SuggestStyleLocal(rawTheme);
            yield break;
        }

        GeminiOptionsWrapper wrapper = null;
        try
        {
            wrapper = JsonUtility.FromJson<GeminiOptionsWrapper>(optionsJson);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to parse Gemini options JSON: " + e.Message);
        }

        if (wrapper?.options == null || wrapper.options.Length == 0)
        {
            Debug.LogWarning("Gemini returned no usable options. Falling back to local styles.");
            SuggestStyleLocal(rawTheme);
            yield break;
        }

        _options.Clear();
        foreach (var dto in wrapper.options)
        {
            if (dto == null || dto.bodyColor == null)
                continue;

            var color = new Color(dto.bodyColor.r, dto.bodyColor.g, dto.bodyColor.b);
            AddOption(dto.label, dto.description, color);
        }

        if (_options.Count == 0)
        {
            SuggestStyleLocal(rawTheme);
            yield break;
        }

        _currentIndex = 0;
        ApplyCurrentOption();
        UpdateSuggestionsText(rawTheme);
    }

    string BuildGeminiPrompt(string theme)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an automotive visual designer for a real-time 3D car showroom in Unity.");
        sb.AppendLine("Based ONLY on the user theme, design distinct body paint jobs for the car.");
        sb.AppendLine($"User theme: \"{theme}\".");
        sb.AppendLine();
        sb.AppendLine("Return between 3 and 5 options.");
        sb.AppendLine("Output MUST be VALID JSON only, no comments, no markdown, no extra text.");
        sb.AppendLine("Use this EXACT schema and keep all color channel values between 0 and 1:");
        sb.AppendLine("{");
        sb.AppendLine("  \"options\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"label\": \"Short name\",");
        sb.AppendLine("      \"description\": \"One sentence describing the vibe.\",");
        sb.AppendLine("      \"bodyColor\": { \"r\": 1.0, \"g\": 0.1, \"b\": 0.1 }");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Focus on cinematic, showroom-ready colors that look great under neutral lighting.");
        sb.AppendLine(geminiInstructionHint ?? string.Empty);
        return sb.ToString();
    }

    static string ExtractOptionsJson(GeminiRoot root)
    {
        if (root?.candidates == null || root.candidates.Length == 0)
            return null;

        var content = root.candidates[0].content;
        if (content?.parts == null || content.parts.Length == 0)
            return null;

        return content.parts[0].text;
    }
}