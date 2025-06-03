using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using MelonLoader.Utils;
using Newtonsoft.Json;

#if MONO
using ScheduleOne.UI;
using TMPro;
using Object = System.Object;
#else
using Il2CppScheduleOne.UI;
using Il2CppTMPro;
using Object = Il2CppSystem.Object;
#endif


[assembly:
    MelonInfo(typeof(TextDecorator.TextDecorator), TextDecorator.BuildInfo.Name, TextDecorator.BuildInfo.Version,
        TextDecorator.BuildInfo.Author)]
[assembly: MelonColor(1, 255, 215, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace TextDecorator
{
    public static class BuildInfo
    {
        public const string Name = "Text Decorator";
        public const string Description = "Adds text formatting options to the text input screen.";
        public const string Author = "k073l";
        public const string Version = "0.6.0";
    }


    [Serializable]
    public class ColorData
    {
        public int R;
        public int G;
        public int B;

        public ColorData(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }

        public static ColorData FromColor(Color color)
        {
            return new ColorData(
                Mathf.RoundToInt(color.r * 255),
                Mathf.RoundToInt(color.g * 255),
                Mathf.RoundToInt(color.b * 255));
        }

        public Color ToColor()
        {
            return new Color(R / 255f, G / 255f, B / 255f);
        }
    }

    [Serializable]
    public class FormatButtonData
    {
        public string Label;
        public string TagKey;
        public ColorData ButtonColorData;

        [JsonIgnore]
        public Color ButtonColor => ButtonColorData.ToColor();

        public FormatButtonData(string label, string tagKey, Color buttonColor)
        {
            Label = label;
            TagKey = tagKey;
            ButtonColorData = ColorData.FromColor(buttonColor);
        }
    }
    
    [Serializable]
    public class CustomColorConfig
    {
        public System.Collections.Generic.List<FormatButtonData> customColors = [];
    }


    public class TextDecorator : MelonMod
    {
        private static MelonLogger.Instance MelonLogger { get; set; }

        public override void OnInitializeMelon()
        {
            MelonLogger = LoggerInstance;
            MelonLogger.Msg("TextDecorator initialized");
        }


        [HarmonyPatch(typeof(TextInputScreen))]
        public static class TextInputScreenPatch
        {
            private static System.Collections.Generic.Dictionary<string, Button> formatButtons = new System.Collections.Generic.Dictionary<string, Button>();

            private static System.Collections.Generic.List<FormatButtonData> colorButtons = new System.Collections.Generic.List<FormatButtonData>
            {
                new FormatButtonData("R", "color=#FF5555", new Color(1, 0.33f, 0.33f)),
                new FormatButtonData("G", "color=#55FF55", new Color(0.33f, 1, 0.33f)),
                new FormatButtonData("B", "color=#5555FF", new Color(0.33f, 0.33f, 1)),
                new FormatButtonData("Y", "color=#FFFF55", new Color(1, 1, 0.33f)),
                new FormatButtonData("P", "color=#FF55FF", new Color(1, 0.33f, 1)),
            };

            private static readonly System.Collections.Generic.Dictionary<string, string> formatTags = new System.Collections.Generic.Dictionary<string, string>
            {
                { "bold", "b" },
                { "italic", "i" },
                { "underline", "u" },
                { "strikethrough", "s" }
            };

            private static TMP_InputField inputField;
            private static TextMeshProUGUI warningText;

            private static readonly string ConfigFilePath =
                Path.Combine(MelonEnvironment.UserDataDirectory, "TextDecoratorColors.json");

            private static void SaveCustomColors()
            {
                var config = new CustomColorConfig
                {
                    customColors = colorButtons.Where(b => !IsDefaultColor(b.TagKey)).ToList()
                };

                try
                {
                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(ConfigFilePath, json);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Failed to save custom colors: {ex.Message}");
                }
            }

            private static void LoadCustomColors(GameObject buttonPanel)
            {
                if (!File.Exists(ConfigFilePath)) return;

                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var config = JsonConvert.DeserializeObject<CustomColorConfig>(json);
                    foreach (var btn in config.customColors)
                    {
                        formatTags[btn.TagKey] = btn.TagKey;
                        if (colorButtons.Any(b => b.TagKey == btn.TagKey))
                        {
                            continue;
                        }
                        colorButtons.Add(btn);
                        CreateColorButton(buttonPanel, btn.Label, btn.TagKey, btn.ButtonColor, true);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Failed to load custom colors: {ex.Message}");
                }
            }

            private static bool IsDefaultColor(string tagKey)
            {
                return tagKey switch
                {
                    "color=#FF5555" or "color=#55FF55" or "color=#5555FF" or
                        "color=#FFFF55" or "color=#FF55FF" => true,
                    _ => false
                };
            }


            [HarmonyPostfix]
            [HarmonyPatch("Open")]
            public static void AddFormattingUI(TextInputScreen __instance)
            {
                inputField = __instance.InputField;
                inputField.characterLimit = 10000;
                formatButtons.Clear();

                GameObject editorContainer = new GameObject("FormattingEditorContainer");
                editorContainer.transform.SetParent(__instance.Canvas.transform, false);

                RectTransform containerTransform = editorContainer.AddComponent<RectTransform>();
                containerTransform.anchorMin = new Vector2(0.5f, 0);
                containerTransform.anchorMax = new Vector2(0.5f, 0);
                containerTransform.pivot = new Vector2(0.5f, 0);
                containerTransform.anchoredPosition = new Vector2(0, 160);
                containerTransform.sizeDelta = new Vector2(480, 110);

                var verticalLayout = editorContainer.AddComponent<VerticalLayoutGroup>();
                verticalLayout.spacing = 5;
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.childForceExpandWidth = true;
                verticalLayout.childForceExpandHeight = false;

                // add warning text
                GameObject warningTextObj = new GameObject("WarningText");
                warningTextObj.transform.SetParent(editorContainer.transform, false);
                RectTransform warningTextRect = warningTextObj.AddComponent<RectTransform>();
                warningTextRect.sizeDelta = new Vector2(480, 35);
                warningText = warningTextObj.AddComponent<TextMeshProUGUI>();
                warningText.text = "Warning: Text formatting works only on selections.";
                warningText.fontSize = 14;
                warningText.color = new Color(1, 1, 1);
                warningText.alignment = TextAlignmentOptions.Center;
                warningText.fontStyle = FontStyles.Bold;

                RectTransform warningTextRectTransform = warningText.GetComponent<RectTransform>();
                warningTextRectTransform.anchorMin = Vector2.zero;
                warningTextRectTransform.anchorMax = Vector2.one;
                warningTextRectTransform.offsetMin = Vector2.zero;
                warningTextRectTransform.offsetMax = Vector2.zero;

                GameObject buttonPanel = new GameObject("FormattingButtons");
                buttonPanel.transform.SetParent(editorContainer.transform, false);

                RectTransform rectTransform = buttonPanel.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(480, 50);

                var panelImage = buttonPanel.AddComponent<Image>();
                panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                var layout = buttonPanel.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.padding = new RectOffset(10, 10, 5, 5);

                CreateFormatButton(buttonPanel, "B", "bold");
                CreateFormatButton(buttonPanel, "I", "italic");
                CreateFormatButton(buttonPanel, "U", "underline");
                CreateFormatButton(buttonPanel, "S", "strikethrough");

                CreateDivider(buttonPanel);

                foreach (var btn in colorButtons)
                {
                    if (!formatTags.ContainsKey(btn.TagKey))
                    {
                        formatTags[btn.TagKey] = btn.TagKey;
                    }

                    if (IsDefaultColor(btn.TagKey))
                        CreateColorButton(buttonPanel, btn.Label, btn.TagKey, btn.ButtonColor);
                    else
                        CreateColorButton(buttonPanel, btn.Label, btn.TagKey, btn.ButtonColor, true);
                }

                GameObject customColorPanel = new GameObject("CustomColorPanel");
                customColorPanel.transform.SetParent(editorContainer.transform, false);
                RectTransform customColorRect = customColorPanel.AddComponent<RectTransform>();
                customColorRect.sizeDelta = new Vector2(480, 40);

                HorizontalLayoutGroup customColorLayout = customColorPanel.AddComponent<HorizontalLayoutGroup>();
                customColorLayout.spacing = 10;
                customColorLayout.childAlignment = TextAnchor.MiddleCenter;
                customColorLayout.childControlHeight = false;
                customColorLayout.childControlWidth = false;
                customColorLayout.padding = new RectOffset(10, 10, 5, 5);

                GameObject hexInputObj = new GameObject("HexInputField");
                hexInputObj.transform.SetParent(customColorPanel.transform, false);
                RectTransform hexInputRect = hexInputObj.AddComponent<RectTransform>();
                hexInputRect.sizeDelta = new Vector2(150, 35);

                TMP_InputField hexInput = hexInputObj.AddComponent<TMP_InputField>();
                RectTransform viewport = new GameObject("Viewport").AddComponent<RectTransform>();
                viewport.SetParent(hexInput.transform, false);
                hexInput.textViewport = viewport;

                TextMeshProUGUI textComponent = new GameObject("Text").AddComponent<TextMeshProUGUI>();
                textComponent.transform.SetParent(viewport, false);
                textComponent.fontSize = 14;
                textComponent.alignment = TextAlignmentOptions.MidlineLeft;
                hexInput.textComponent = textComponent;

                hexInput.text = "#FFFFFF";

                // adding custom color button
                GameObject addButtonObj = new GameObject("AddColorButton");
                addButtonObj.transform.SetParent(customColorPanel.transform, false);
                RectTransform addButtonRect = addButtonObj.AddComponent<RectTransform>();
                addButtonRect.sizeDelta = new Vector2(100, 35);

                Button addButton = addButtonObj.AddComponent<Button>();
                Image addButtonImage = addButtonObj.AddComponent<Image>();
                addButtonImage.color = new Color(0.3f, 0.3f, 0.3f);

                GameObject addButtonTextObj = new GameObject("Text");
                addButtonTextObj.transform.SetParent(addButtonObj.transform, false);
                TextMeshProUGUI addButtonText = addButtonTextObj.AddComponent<TextMeshProUGUI>();
                addButtonText.text = "Add Color";
                addButtonText.fontSize = 16;
                addButtonText.color = Color.white;
                addButtonText.alignment = TextAlignmentOptions.Center;
                RectTransform addButtonTextRect = addButtonText.GetComponent<RectTransform>();
                addButtonTextRect.anchorMin = Vector2.zero;
                addButtonTextRect.anchorMax = Vector2.one;
                addButtonTextRect.offsetMin = Vector2.zero;
                addButtonTextRect.offsetMax = Vector2.zero;

                addButton.onClick.AddListener((UnityAction)(() =>
                {
                    string hex = hexInput.text.Trim();
                    if (!hex.StartsWith("#")) hex = "#" + hex;

                    if (ColorUtility.TryParseHtmlString(hex, out Color newColor))
                    {
                        string tagKey = $"color={hex}";
                        if (!formatTags.ContainsKey(tagKey))
                        {
                            string label = hex.Substring(1, 2).ToUpper();
                            formatTags[tagKey] = tagKey;
                            
                            if (colorButtons.Any(b => b.TagKey == tagKey))
                            {
                                return;
                            }

                            var newBtn = new FormatButtonData(label, tagKey, newColor);
                            colorButtons.Add(newBtn);
                            CreateColorButton(buttonPanel, label, tagKey, newColor, true);

                            MelonLogger.Msg($"Added custom color button: {hex}");
                            SaveCustomColors();
                        }
                    }
                    else
                    {
                        MelonLogger.Msg($"Invalid HEX color: {hexInput.text}");
                    }
                }));
                LoadCustomColors(buttonPanel);
            }

            private static void FlashWarningText()
            {
                if (warningText == null) return;
                MelonLogger.Msg("No selection, flashing warning text!");
                MelonCoroutines.Start(FlashTextCoroutine());
            }

            private static IEnumerator FlashTextCoroutine()
            {
                warningText.color = Color.yellow;
                yield return new WaitForSeconds(0.5f);
                warningText.color = Color.white;
            }

            private static void CreateDivider(GameObject parent)
            {
                GameObject divider = new GameObject("Divider");
                divider.transform.SetParent(parent.transform, false);

                Image dividerImage = divider.AddComponent<Image>();
                dividerImage.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

                RectTransform dividerRect = divider.GetComponent<RectTransform>();
                dividerRect.sizeDelta = new Vector2(2, 40);
            }

            private static void CreateFormatButton(GameObject parent, string label, string formatKey)
            {
                GameObject buttonObj = new GameObject(label + "Button");
                buttonObj.transform.SetParent(parent.transform, false);

                Button button = buttonObj.AddComponent<Button>();
                Image image = buttonObj.AddComponent<Image>();
                image.color = new Color(0.3f, 0.3f, 0.3f);

                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(35, 35);

                Outline outline = buttonObj.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.5f);
                outline.effectDistance = new Vector2(1, -1);

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = label;
                text.fontSize = 20;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;

                RectTransform textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                button.onClick.AddListener((UnityAction)(() => { ApplyFormatting(formatKey); }));

                formatButtons[formatKey] = button;
            }

            private static void CreateColorButton(GameObject parent, string label, string colorKey, Color buttonColor,
                bool isCustom = false)
            {
                GameObject buttonObj = new GameObject(label + "Button");
                buttonObj.transform.SetParent(parent.transform, false);

                RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(35, 35);

                Button button = buttonObj.AddComponent<Button>();
                Image image = buttonObj.AddComponent<Image>();
                image.color = buttonColor;

                Outline outline = buttonObj.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.5f);
                outline.effectDistance = new Vector2(1, -1);

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = label;
                text.fontSize = 20;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;

                RectTransform textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                button.onClick.AddListener((UnityAction)(() => ApplyFormatting(colorKey)));
                formatButtons[colorKey] = button;

                if (isCustom)
                {
                    GameObject removeBtnObj = new GameObject("RemoveButton");
                    removeBtnObj.transform.SetParent(buttonObj.transform, false);

                    RectTransform removeRect = removeBtnObj.AddComponent<RectTransform>();
                    removeRect.anchorMin = new Vector2(0, 1);
                    removeRect.anchorMax = new Vector2(0, 1);
                    removeRect.pivot = new Vector2(0, 1);
                    removeRect.anchoredPosition = new Vector2(2, -2);
                    removeRect.sizeDelta = new Vector2(12, 12);

                    Button removeButton = removeBtnObj.AddComponent<Button>();
                    Image removeImage = removeBtnObj.AddComponent<Image>();
                    removeImage.color = new Color(1f, 0.2f, 0.2f); // red

                    GameObject removeTextObj = new GameObject("Text");
                    removeTextObj.transform.SetParent(removeBtnObj.transform, false);
                    TextMeshProUGUI removeText = removeTextObj.AddComponent<TextMeshProUGUI>();
                    removeText.text = "X";
                    removeText.fontSize = 10;
                    removeText.alignment = TextAlignmentOptions.Center;
                    removeText.color = Color.white;

                    RectTransform removeTextRect = removeText.GetComponent<RectTransform>();
                    removeTextRect.anchorMin = Vector2.zero;
                    removeTextRect.anchorMax = Vector2.one;
                    removeTextRect.offsetMin = Vector2.zero;
                    removeTextRect.offsetMax = Vector2.zero;

                    removeButton.onClick.AddListener((UnityAction)(() =>
                    {
                        GameObject.Destroy(buttonObj);
                        formatButtons.Remove(colorKey);
                        colorButtons.RemoveAll(b => b.TagKey == colorKey);
                        SaveCustomColors();
                        MelonLogger.Msg($"Removed custom color button: {colorKey}");
                    }));
                }
            }


            private static void ApplyFormatting(string formatKey)
            {
                if (inputField == null) return;

                int start = inputField.selectionAnchorPosition;
                int end = inputField.selectionFocusPosition;

                if (start > end)
                    (start, end) = (end, start);

                if (start == end)
                {
                    FlashWarningText();
                    return;
                }

                string tagType = formatTags[formatKey];
                string openTag = $"<{tagType}>";
                string closeTag = formatKey.StartsWith("color") ? "</color>" : $"</{tagType}>";

                MelonLogger.Msg($"Applying formatting: {formatKey} from {start} to {end}");
                MelonLogger.Msg($"Tags: {openTag} {closeTag}");

                string oldText = inputField.text;
                string newText = TextFormatterUtils.ApplyTag(oldText, start, end, openTag, closeTag);

                MelonLogger.Msg($"Old text: {oldText}; New text: {newText}");
                inputField.text = newText;
            }

            [HarmonyPrefix]
            [HarmonyPatch("Close")]
            public static void CleanupFormattingUI()
            {
                inputField = null;
                formatButtons.Clear();

                GameObject container = GameObject.Find("FormattingEditorContainer");
                if (container != null)
                {
                    GameObject.Destroy(container);
                }
            }
        }
    }
}