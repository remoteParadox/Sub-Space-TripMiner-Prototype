using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LegacyUiTextMirror : MonoBehaviour
{
    public Text[] sources;
    public TextMeshProUGUI[] mirrors;

    private void LateUpdate()
    {
        if (sources == null || mirrors == null) return;
        int count = Mathf.Min(sources.Length, mirrors.Length);
        for (int i = 0; i < count; i++)
        {
            Text source = sources[i];
            TextMeshProUGUI mirror = mirrors[i];
            if (source == null || mirror == null) continue;
            mirror.text = source.text;
            mirror.color = source.color;
            mirror.fontSize = source.fontSize;
            mirror.fontStyle = ConvertStyle(source.fontStyle);
            mirror.alignment = ConvertAlignment(source.alignment);
            mirror.enableWordWrapping = source.horizontalOverflow == HorizontalWrapMode.Wrap;
            mirror.raycastTarget = false;
        }
    }

    private static FontStyles ConvertStyle(FontStyle style)
    {
        switch (style)
        {
            case FontStyle.Bold: return FontStyles.Bold;
            case FontStyle.Italic: return FontStyles.Italic;
            case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
            default: return FontStyles.Normal;
        }
    }

    private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
    {
        switch (alignment)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }
}