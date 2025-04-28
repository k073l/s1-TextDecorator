using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne.UI;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(TextDecorator.TextDecorator), TextDecorator.BuildInfo.Name, TextDecorator.BuildInfo.Version, TextDecorator.BuildInfo.Author)]
[assembly: MelonColor(1, 255, 215, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace TextDecorator
{
    public static class BuildInfo
    {
        public const string Name = "Text Decorator";
        public const string Description = "Text formatting options";
        public const string Author = "k073l";
        public const string Version = "0.1";
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
            private static Dictionary<string, Button> formatButtons = new Dictionary<string, Button>();
            private static Dictionary<string, string> formatTags = new Dictionary<string, string>
            {
                { "bold", "b" },
                { "italic", "i" },
                { "underline", "u" },
                { "strikethrough", "s" },
                { "red", "color=#FF5555" },
                { "green", "color=#55FF55" },
                { "blue", "color=#5555FF" },
                { "yellow", "color=#FFFF55" },
                { "purple", "color=#FF55FF" }
            };

            private static TMP_InputField inputField;

            [HarmonyPostfix]
            [HarmonyPatch("Open")]
            public static void AddFormattingUI(TextInputScreen __instance)
            {
                inputField = __instance.InputField;
                inputField.characterLimit = 10000; // 100 is too little with formatting
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

                CreateColorButton(buttonPanel, "R", "red", new Color(1, 0.33f, 0.33f));
                CreateColorButton(buttonPanel, "G", "green", new Color(0.33f, 1, 0.33f));
                CreateColorButton(buttonPanel, "B", "blue", new Color(0.33f, 0.33f, 1));
                CreateColorButton(buttonPanel, "Y", "yellow", new Color(1, 1, 0.33f));
                CreateColorButton(buttonPanel, "P", "purple", new Color(1, 0.33f, 1));
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

                button.onClick.AddListener((UnityAction)(() =>
                {
                    ApplyFormatting(formatKey);
                }));

                formatButtons[formatKey] = button;
            }

            private static void CreateColorButton(GameObject parent, string label, string colorKey, Color buttonColor)
            {
                GameObject buttonObj = new GameObject(label + "Button");
                buttonObj.transform.SetParent(parent.transform, false);

                Button button = buttonObj.AddComponent<Button>();
                Image image = buttonObj.AddComponent<Image>();
                image.color = buttonColor;

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

                button.onClick.AddListener((UnityAction)(() =>
                {
                    ApplyFormatting(colorKey);
                }));

                formatButtons[colorKey] = button;
            }

            private static void ApplyFormatting(string formatKey)
            {
                if (inputField == null) return;

                int start = inputField.selectionAnchorPosition;
                int end = inputField.selectionFocusPosition;

                if (start > end)
                    (start, end) = (end, start);

                if (start == end) return;

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