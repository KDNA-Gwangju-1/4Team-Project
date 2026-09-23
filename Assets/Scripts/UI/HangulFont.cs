using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레거시 UI Text 에 한글 폰트를 끼워 주는 도우미.
///
/// 유니티가 기본으로 주는 LegacyRuntime.ttf 에는 한글 글자가 들어 있지 않아서
/// 한글을 넣으면 네모(두부)로 깨진다.
/// 동봉한 Pretendard를 우선 사용하고, 에셋이 누락된 경우에만 OS 폰트로 대체한다.
///
/// 나중에 TextMeshPro + 한글 폰트 에셋으로 갈아탈 거면 이 파일은 지워도 된다.
/// </summary>
public static class HangulFont
{
    /// <summary>찾아볼 폰트 이름들. 위에서부터 있는 것을 쓴다.</summary>
    private static readonly string[] Candidates =
    {
        "Malgun Gothic",      // 윈도우 기본 한글 폰트
        "맑은 고딕",
        "NanumGothic",
        "Noto Sans KR",
        "Gulim",
        "Batang",
        "AppleSDGothicNeo-Regular",
        "Arial Unicode MS",
    };

    private static Font _font;
    private static Font _emphasisFont;

    /// <summary>한글이 나오는 폰트를 하나 돌려준다. 못 찾으면 null.</summary>
    public static Font Get()
    {
        if (_font != null) return _font;

        _font = Resources.Load<Font>("UI/Fonts/Pretendard-Regular");
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont(Candidates, 32);
        return _font;
    }

    public static Font GetEmphasis()
    {
        if (_emphasisFont == null)
            _emphasisFont = Resources.Load<Font>("UI/Fonts/Pretendard-SemiBold");
        return _emphasisFont != null ? _emphasisFont : Get();
    }

    /// <summary>Text 하나에 한글 폰트를 적용한다. null 을 넣어도 안전하다.</summary>
    public static void Apply(Text text)
    {
        if (text == null) return;

        bool bold = text.fontStyle == FontStyle.Bold || text.fontStyle == FontStyle.BoldAndItalic;
        var font = bold ? GetEmphasis() : Get();
        if (font == null) return;
        text.font = font;
        // A real SemiBold face must not receive an additional synthetic bold pass.
        if (bold && _emphasisFont != null)
            text.fontStyle = text.fontStyle == FontStyle.BoldAndItalic ? FontStyle.Italic : FontStyle.Normal;
    }

    /// <summary>자식에 있는 Text 전부에 한글 폰트를 적용한다.</summary>
    public static void ApplyAll(GameObject root)
    {
        if (root == null) return;

        var texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++) Apply(texts[i]);
    }
}
