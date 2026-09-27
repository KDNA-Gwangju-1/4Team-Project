using UnityEngine;

/// <summary>병원 시작 조작 안내의 내용. 그리는 건 ControlGuideBuilder, 입력과 타이밍은 ControlGuideUI 가 가진다.</summary>
public static class ControlGuideSkin
{
    /// <summary>안내창을 새로 그리고, 깜빡일 하단 "시작" 줄을 돌려준다.</summary>
    public static CanvasGroup Apply(RectTransform root)
    {
        if (root == null || root.Find("GuidePanel") != null) return null;

        // Keep the serialized guide and its callback intact; only its old visuals are hidden.
        for (int i = 0; i < root.childCount; i++) root.GetChild(i).gameObject.SetActive(false);

        return ControlGuideBuilder.Build(root, ControlGuideBuilder.Theme.Daily, new ControlGuideBuilder.Content
        {
            title = "본관 3층 병동",
            subtitle = "301호 병실을 찾아 아이에게 다가가 보세요.",
            leftHeader = "움직이기",
            left = new[]
            {
                new ControlGuideBuilder.Row("이동", "앞뒤 좌우로 걷기", "WASD"),
                new ControlGuideBuilder.Row("빠르게 걷기", "누르고 있는 동안 빨라져요", "Shift"),
                ControlGuideBuilder.Row.Mouse("M", "둘러보기", "마우스를 움직여 시점 전환"),
            },
            rightHeader = "행동",
            right = new[]
            {
                new ControlGuideBuilder.Row("상호작용", "안내가 뜨면 눌러서 살펴보기", "E"),
                new ControlGuideBuilder.Row("대사 넘기기", "다음 대사로 넘어가요", "Space"),
                new ControlGuideBuilder.Row("일시정지", "설정 · 메인 메뉴", "ESC"),
            },
            continueKey = "Space",
            continueText = "또는 아무 키나 눌러 시작",
        });
    }
}
