using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레거시 UI Text 에 한글 폰트를 끼워 주는 도우미.
///
/// 유니티가 기본으로 주는 LegacyRuntime.ttf 에는 한글 글자가 들어 있지 않아서
/// 한글을 넣으면 네모(두부)로 깨진다.
/// 그래서 운영체제에 깔려 있는 한글 폰트를 런타임에 끌어와 갈아 끼운다.
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

    /// <summary>한글이 나오는 폰트를 하나 돌려준다. 못 찾으면 null.</summary>
    public static Font Get()
    {
        if (_font != null) return _font;

        _font = Font.CreateDynamicFontFromOSFont(Candidates, 32);
        return _font;
    }

    /// <summary>Text 하나에 한글 폰트를 적용한다. null 을 넣어도 안전하다.</summary>
    public static void Apply(Text text)
    {
        if (text == null) return;

        var font = Get();
        if (font != null) text.font = font;
    }

    /// <summary>자식에 있는 Text 전부에 한글 폰트를 적용한다.</summary>
    public static void ApplyAll(GameObject root)
    {
        if (root == null) return;

        var texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++) Apply(texts[i]);
    }
}
