"""Static info cards for the submission video (1920x1080 RGBA)."""
import io
import subprocess

from PIL import Image, ImageDraw, ImageFilter, ImageEnhance

from vidlib import *

YEL, RED, BLU, GRN = (252, 181, 43), (252, 88, 71), (44, 146, 248), (50, 190, 128)
INK, MUTED, DIM = (240, 238, 248), (176, 172, 198), (120, 116, 142)
PANEL = (22, 19, 40)
LOGO_FULL = None


def grab(t, size=None):
    """One frame of the gameplay recording at t seconds."""
    data = subprocess.run([FF, "-hide_banner", "-loglevel", "error", "-ss", str(t), "-i", SRC,
                           "-frames:v", "1", "-f", "image2pipe", "-vcodec", "png", "-"],
                          capture_output=True).stdout
    im = Image.open(io.BytesIO(data)).convert("RGB")
    return im.resize(size, Image.LANCZOS) if size else im


def backdrop(t, blur=22, dark=.30):
    im = grab(t).filter(ImageFilter.GaussianBlur(blur))
    im = ImageEnhance.Brightness(im).enhance(dark)
    im = im.convert("RGBA")
    # vignette + navy tint
    tint = Image.new("RGBA", (W, H), (12, 10, 26, 150))
    im.alpha_composite(tint)
    return im


def logo_image(height):
    global LOGO_FULL
    if LOGO_FULL is None:
        import logo_anim
        pieces = logo_anim.extract_pieces()
        x0 = min(p["box"][0] for p in pieces); x1 = max(p["box"][2] for p in pieces)
        y0 = min(p["box"][1] for p in pieces); y1 = max(p["box"][3] for p in pieces)
        full = Image.new("RGBA", (x1 - x0, y1 - y0), (0, 0, 0, 0))
        for p in pieces:
            full.alpha_composite(p["img"], (p["box"][0] - x0, p["box"][1] - y0))
        LOGO_FULL = full
    s = height / LOGO_FULL.height
    return LOGO_FULL.resize((int(LOGO_FULL.width * s), height), Image.LANCZOS)


def header(im, num, label, title, accent=YEL):
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((120, 92, 128, 142), 3, fill=accent)
    d.text((148, 90), f"{num:02d}", font=font(26, True), fill=accent)
    d.text((196, 90), label, font=font(26, True), fill=MUTED)
    d.text((146, 128), title, font=font(58, True), fill=INK)
    return d


def panel(im, box, fill=PANEL + (225,), outline=(255, 255, 255, 28), r=22):
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rounded_rectangle(box, r, fill=fill, outline=outline, width=2)
    shadowed(im, layer, offset=(0, 8), blur=14, alpha=120)


def wrap(d, s, f, width):
    """Greedy wrap that respects Korean (per character) and spaces."""
    lines, cur = [], ""
    for para in s.split("\n"):
        cur = ""
        for ch in para:
            test = cur + ch
            if d.textlength(test, font=f) > width and cur:
                # break at the last space if there is one on this line
                cut = cur.rfind(" ")
                if cut > len(cur) * .5:
                    lines.append(cur[:cut]); cur = cur[cut + 1:] + ch
                else:
                    lines.append(cur); cur = ch
            else:
                cur = test
        lines.append(cur)
    return lines


def text_block(d, xy, s, f, fill, width, spacing=1.45):
    x, y = xy
    for line in wrap(d, s, f, width):
        d.text((x, y), line, font=f, fill=fill)
        y += int(f.size * spacing)
    return y


def thumb(im, t, box, label=None, accent=YEL):
    x0, y0, x1, y1 = box
    g = grab(t, (x1 - x0, y1 - y0)).convert("RGBA")
    mask = Image.new("L", g.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, g.width - 1, g.height - 1), 16, fill=255)
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    layer.paste(g, (x0, y0), mask)
    shadowed(im, layer, offset=(0, 8), blur=14, alpha=140)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle(box, 16, outline=(255, 255, 255, 60), width=2)
    if label:
        f = font(24, True)
        tw = d.textlength(label, font=f)
        d.rounded_rectangle((x0 + 14, y1 - 50, x0 + 34 + tw, y1 - 14), 10, fill=(10, 9, 20, 210))
        d.rounded_rectangle((x0 + 14, y1 - 50, x0 + 20, y1 - 14), 3, fill=accent)
        d.text((x0 + 28, y1 - 47), label, font=f, fill=INK)


# ---------------------------------------------------------------- keycaps
def keycap(d, x, y, key, h=48):
    f = font(22 if len(key) > 1 else 24, True)
    w = max(h, int(d.textlength(key, font=f)) + 30)
    d.rounded_rectangle((x, y, x + w, y + h), 10, fill=(40, 36, 66), outline=(10, 9, 20), width=2)
    d.rounded_rectangle((x + 2, y + 2, x + w - 2, y + h - 8), 9, fill=(236, 234, 246))
    d.text((x + w / 2, y + (h - 6) / 2), key, font=f, fill=(30, 28, 50), anchor="mm")
    return w


def mouse_icon(d, x, y, button, h=52, accent=YEL):
    w = int(h * .66)
    d.rounded_rectangle((x, y, x + w, y + h), w // 2, fill=(236, 234, 246), outline=(10, 9, 20), width=2)
    split = y + h * .42
    if button in ("L", "R"):
        bx0, bx1 = (x + 3, x + w / 2) if button == "L" else (x + w / 2, x + w - 3)
        d.rounded_rectangle((bx0, y + 3, bx1, split), 8, fill=accent)
    d.line((x + 2, split, x + w - 2, split), fill=(10, 9, 20), width=2)
    d.line((x + w / 2, y + 2, x + w / 2, split), fill=(10, 9, 20), width=2)
    d.rounded_rectangle((x + w / 2 - 3, y + 10, x + w / 2 + 3, y + 22), 3, fill=(80, 76, 110))
    if button == "M":
        d.polygon([(x - 16, y + h / 2), (x - 6, y + h / 2 - 8), (x - 6, y + h / 2 + 8)], fill=accent)
        d.polygon([(x + w + 16, y + h / 2), (x + w + 6, y + h / 2 - 8), (x + w + 6, y + h / 2 + 8)], fill=accent)
    return w


# ---------------------------------------------------------------- cards
def card_title():
    im = backdrop(3, blur=26, dark=.40)
    d = ImageDraw.Draw(im)
    lg = logo_image(120)
    im.alpha_composite(lg, (W // 2 - lg.width // 2, 250))
    d.text((W / 2, 520), "Re:Dream", font=font(150, True), fill=INK, anchor="mm")
    d.text((W / 2, 640), "리드림  ·  게임 소개 영상", font=font(40), fill=MUTED, anchor="mm")
    d.rounded_rectangle((W / 2 - 60, 705, W / 2 + 60, 709), 2, fill=YEL)
    d.text((W / 2, 760), "NC-AI 4팀  4TEAM", font=font(30, True), fill=(220, 214, 240), anchor="mm")
    return im


MEMBERS = [
    ("이원주", "팀장", "프로젝트 총괄", YEL),
    ("주서연", "꿈 1 개발 및 기획", "챕터 1 · 밝은 꿈 (3D)", RED),
    ("임현규", "꿈 1 개발 및 기획", "챕터 1 · 밝은 꿈 (3D)", RED),
    ("윤남기", "꿈 2 개발 및 기획", "챕터 2 · 어두운 꿈 (2D)", BLU),
    ("김슬빈", "꿈 2 개발 및 기획", "챕터 2 · 어두운 꿈 (2D)", BLU),
    ("김명주", "개발 및 디자인", "메인 화면 · 병원 · 로딩", GRN),
]


def card_team():
    im = backdrop(3)
    header(im, 1, "팀 정보", "팀원 소개")
    panel(im, (120, 250, 620, 950))
    lg = logo_image(300)
    im.alpha_composite(lg, (370 - lg.width // 2, 330))
    d = ImageDraw.Draw(im)
    d.text((370, 720), "4TEAM", font=font(60, True), fill=INK, anchor="mm")
    d.text((370, 790), "NC-AI 4팀", font=font(30), fill=MUTED, anchor="mm")
    d.text((370, 860), "팀원 6명 · 2026.09.15 ~ 09.28", font=font(24), fill=DIM, anchor="mm")
    for i, (name, role, area, col) in enumerate(MEMBERS):
        cx, cy = 680 + (i % 2) * 560, 250 + (i // 2) * 240
        panel(im, (cx, cy, cx + 530, cy + 210))
        d = ImageDraw.Draw(im)
        d.rounded_rectangle((cx + 28, cy + 36, cx + 36, cy + 174), 4, fill=col)
        d.text((cx + 64, cy + 34), name, font=font(46, True), fill=INK)
        d.text((cx + 64, cy + 104), role, font=font(30, True), fill=col)
        d.text((cx + 64, cy + 148), area, font=font(24), fill=MUTED)
    return im


def card_overview():
    im = backdrop(100)
    header(im, 2, "게임 개요", "Re:Dream", accent=RED)
    rows = [
        ("게임 제목", "Re:Dream (리드림)"),
        ("장르", "3D 1인칭 탐색 · 전투  +  2D 횡스크롤 액션 어드벤처"),
        ("컨셉", "원인 불명의 '꿈병'에 걸려 잠든 쌍둥이 자매. 꿈을 부숴야만 깨어날 수 있다. "
                 "꿈탐정이 되어 언니와 동생의 꿈에 차례로 들어간다."),
        ("핵심 재미", "하나의 게임, 서로 다른 두 꿈 — 3D 정원에서 단서를 추리하고, "
                    "2D 악몽에서는 손전등 빛으로 숨은 길과 약점을 드러낸다."),
    ]
    panel(im, (120, 250, 1080, 950))
    d = ImageDraw.Draw(im)
    y = 290
    for k, v in rows:
        d.text((170, y), k, font=font(28, True), fill=RED)
        y2 = text_block(d, (360, y - 2), v, font(30), INK, 670)
        y = max(y + 60, y2) + 38
    thumb(im, 100, (1140, 250, 1800, 590), "챕터 1  언니의 꿈 · 3D", RED)
    thumb(im, 330, (1140, 610, 1800, 950), "챕터 2  동생의 꿈 · 2D", BLU)
    return im


def card_controls():
    im = backdrop(300)
    header(im, 3, "조작 방법", "키보드 · 마우스", accent=BLU)
    cols = [
        ("병원 (현실 · 3D)", GRN, [
            (["WASD"], None, "이동"), (["Shift"], None, "빠르게 걷기"), ([], "M", "둘러보기"),
            (["E"], None, "상호작용"), (["Space"], None, "대사 넘기기")]),
        ("챕터 1  밝은 꿈 (3D)", RED, [
            (["WASD"], None, "이동"), (["Shift"], None, "달리기"), (["Space"], None, "점프"),
            ([], "M", "시점 전환"), (["E"], None, "단서 조사 · 줍기"), ([], "L", "정화총 발사")]),
        ("챕터 2  어두운 꿈 (2D)", BLU, [
            (["A", "D"], None, "좌우 이동"), (["Space"], None, "점프 · 벽차기"), (["Shift"], None, "대시"),
            ([], "R", "손전등 (마우스 조준)"), ([], "L", "광탄 발사"), (["E"], None, "상호작용")]),
    ]
    for i, (title, col, rows) in enumerate(cols):
        x0 = 120 + i * 565
        panel(im, (x0, 250, x0 + 535, 880))
        d = ImageDraw.Draw(im)
        d.rounded_rectangle((x0 + 30, 282, x0 + 36, 318), 3, fill=col)
        d.text((x0 + 52, 280), title, font=font(30, True), fill=INK)
        y = 350
        for keys, mouse, action in rows:
            x = x0 + 40
            if keys == ["WASD"]:
                for j, k in enumerate("WASD"):
                    keycap(d, x + j * 50, y, k, 44)
                x += 200
            else:
                for k in keys:
                    x += keycap(d, x, y, k, 48) + 8
            if mouse:
                x += mouse_icon(d, x + 18, y - 2, mouse, 52, col) + 36
            d.text((x0 + 270, y + 24), action, font=font(28, True), fill=INK, anchor="lm")
            y += 84
    d = ImageDraw.Draw(im)
    d.text((W / 2, 930), "공통    ESC  일시정지 · 설정 · 메인 메뉴        R  재시작 (게임 오버 시)",
           font=font(28, True), fill=MUTED, anchor="mm")
    return im


def card_flow():
    im = backdrop(560)
    header(im, 4, "게임 흐름", "시작부터 엔딩까지", accent=GRN)
    steps = [("시작 화면", 3, YEL), ("병원 · 현실", 80, GRN), ("챕터 1 · 밝은 꿈", 150, RED),
             ("챕터 2 · 어두운 꿈", 330, BLU), ("엔딩", 548, YEL)]
    x = 120
    bw = 300
    for i, (label, t, col) in enumerate(steps):
        thumb(im, t, (x, 420, x + bw, 590))
        d = ImageDraw.Draw(im)
        d.text((x + bw / 2, 640), label, font=font(30, True), fill=INK, anchor="mm")
        d.rounded_rectangle((x + bw / 2 - 30, 676, x + bw / 2 + 30, 681), 2, fill=col)
        if i < len(steps) - 1:
            ax = x + bw + 20
            d.polygon([(ax, 490), (ax + 22, 505), (ax, 520)], fill=MUTED)
        x += bw + 55
    d = ImageDraw.Draw(im)
    d.text((W / 2, 800), "지금부터 실제 플레이 영상으로 처음부터 끝까지 따라가 봅니다.",
           font=font(34), fill=INK, anchor="mm")
    d.text((W / 2, 860), "화면 아래 자막에서 목표 · 규칙 · 구현한 기능을 설명합니다.",
           font=font(28), fill=MUTED, anchor="mm")
    return im


def card_features():
    im = backdrop(232)
    header(im, 5, "주요 기능 및 시스템", "팀이 구현한 핵심 기능", accent=YEL)
    tiles = [
        (104, "단서 조사 · 음성 나레이션", "E로 조사하면 기억이 재생", RED),
        (156, "정화총 · 몬스터 정화", "제한 시간 안에 인형 정화", RED),
        (232, "유니콘 보스 3페이즈", "레이저 · 별똥별 · 내려찍기", RED),
        (332, "손전등 · 숨은 발판", "빛을 비추면 길이 드러남", BLU),
        (464, "2D 보스 2페이즈", "빛으로 비추고 광탄 공격", BLU),
        (266, "컷씬 · 대사 시스템", "챕터별 대사창 · 연출", YEL),
    ]
    for i, (t, title, sub, col) in enumerate(tiles):
        x0, y0 = 120 + (i % 3) * 565, 240 + (i // 3) * 330
        thumb(im, t, (x0, y0, x0 + 535, y0 + 220))
        d = ImageDraw.Draw(im)
        d.rounded_rectangle((x0, y0 + 236, x0 + 6, y0 + 300), 3, fill=col)
        d.text((x0 + 20, y0 + 232), title, font=font(30, True), fill=INK)
        d.text((x0 + 20, y0 + 274), sub, font=font(24), fill=MUTED)
    d = ImageDraw.Draw(im)
    d.text((W / 2, 945), "공통 시스템   체크포인트 리스폰 · 타임어택 타이머 · 일시정지/설정 · 로딩 팁 · 챕터별 통일 UI",
           font=font(27, True), fill=MUTED, anchor="mm")
    return im


def card_tools():
    im = backdrop(300)
    header(im, 6, "개발 과정", "사용 엔진 · 도구", accent=BLU)
    tools = [
        ("Unity 6", "6000.3 · 3D 파트와 2D 파트를 한 프로젝트에서 개발", YEL),
        ("C#", "게임 로직 · UI · 컷신 · 에디터 도구 스크립트", BLU),
        ("VARCO 3D", "유니콘 보스 등 3D 캐릭터 모델 제작 · 리깅", RED),
        ("VARCO Sound", "배경음 · 효과음 제작", GRN),
        ("GitHub", "챕터별 브랜치 → 통합 브랜치 병합, 버전 관리", MUTED),
        ("Notion", "역할 분담 · 진행 상황 관리", MUTED),
    ]
    for i, (name, desc, col) in enumerate(tools):
        x0, y0 = 120 + (i % 2) * 850, 250 + (i // 2) * 170
        panel(im, (x0, y0, x0 + 820, y0 + 145))
        d = ImageDraw.Draw(im)
        d.rounded_rectangle((x0 + 26, y0 + 34, x0 + 32, y0 + 110), 3, fill=col)
        d.text((x0 + 56, y0 + 26), name, font=font(40, True), fill=INK)
        d.text((x0 + 56, y0 + 88), desc, font=font(25), fill=MUTED)
    d = ImageDraw.Draw(im)
    stats = [("2주", "개발 기간"), ("369", "커밋"), ("156", "C# 스크립트"), ("2.4만", "코드 줄 수")]
    for i, (v, k) in enumerate(stats):
        cx = 330 + i * 420
        d.text((cx, 800), v, font=font(64, True), fill=YEL, anchor="mm")
        d.text((cx, 870), k, font=font(26), fill=MUTED, anchor="mm")
    return im


ROLES = [
    [
        ("이원주", "팀장", YEL, [(95, "챕터 1 3D 에셋"), (310, "챕터 2 픽셀 아트")], [
            "프로젝트 총괄 · 챕터 1(3D) · 챕터 2(2D) 통합 브랜치 병합",
            "챕터 1 3D 에셋 — 정원 건물 · 소품 · 보스 · 단서 오브젝트 (VARCO 3D)",
            "챕터 2 픽셀 아트 — 탐정 · 몬스터 · 보스 스프라이트와 애니메이션",
            "챕터 2 배경 · 기억 액자 · 동화풍 문 · 발판 타일",
            "오프닝 · 엔딩 시네마틱 원화, 챕터 2 균열 오프닝 연출",
            "챕터별 대사창 · HUD · 로딩 UI 스킨과 폰트 통일, 사운드 작업",
        ]),
        ("주서연 · 임현규", "꿈 1 개발 및 기획", RED, [(100, "단서 조사"), (230, "유니콘 보스")], [
            "밝은 꿈 정원 맵 레이아웃 · 지형 · 돌길 · 온실 호수와 다리",
            "단서 조사 · 은은한 외곽선 발광, 기억의 단서 편지 선택 개선",
            "정화총 획득 · 조준점, 하트 체력 UI, 몬스터 장애물 회피",
            "유니콘 보스 리깅 모델 · 전투 모션, 3페이즈 기믹(레이저 · 별똥별)",
            "단서 수집 후 다리 컷씬 · 보스 아레나 잠금 · 오염 흔적 데칼",
            "스테이지별 체크포인트 리스폰 · 타임어택 타이머 · 디버그 치트키",
        ]),
    ],
    [
        ("윤남기 · 김슬빈", "꿈 2 개발 및 기획", BLU, [(332, "손전등 발판"), (464, "2D 보스전")], [
            "2D 플레이어 이동 · 점프 · 벽차기 · 대시, 저중력 조작감",
            "손전등 빛 판정 · 숨은 발판, 손전등 옆 사용법 쪽지",
            "스테이지 2 수직 통로 · 큰 점프맵, 몬스터 배치 · 낙하 구제",
            "몬스터 리스폰 · 매복 함정, 액자 · 문 등 스테이지 에셋 배치",
            "언니 형상 보스 1·2페이즈 촉수 패턴, 2페이즈 리트라이",
            "배경음 · 발소리 · 손전등 · 몬스터 효과음, HUD · 대사창 정리",
        ]),
        ("김명주", "개발 및 디자인 · 메인 QA", GRN, [(3, "메인 화면"), (74, "병실")], [
            "메인 화면(메인 허브) 구현",
            "병원 복도 · 병실 인게임 씬 제작, 호실 이슈 수정",
            "로딩 화면 · 조작법 안내 · 컷씬 스킵 버튼",
            "메인 QA — 전 챕터를 반복 플레이하며 자잘한 오류 발견 · 제보",
            "UI 겹침 · 진행 막힘 등 버그 재현과 수정 확인",
            "시작 화면부터 엔딩까지 전체 흐름 점검",
        ]),
    ],
]


def card_roles(page):
    im = backdrop(150 if page == 0 else 400)
    header(im, 7, "개발 과정", f"팀원별 작업 분담 ({page + 1}/2)", accent=GRN)
    for i, (name, role, col, shots, items) in enumerate(ROLES[page]):
        x0 = 120 + i * 850
        panel(im, (x0, 250, x0 + 820, 950))
        d = ImageDraw.Draw(im)
        d.rounded_rectangle((x0 + 30, 290, x0 + 38, 400), 4, fill=col)
        d.text((x0 + 64, 282), name, font=font(48, True), fill=INK)
        d.text((x0 + 64, 352), role, font=font(30, True), fill=col)
        # longer lists use a smaller size so they still fit above the screenshots
        fs, gap = (30, 22) if len(items) <= 4 else (24, 9)
        y = 450 if len(items) <= 4 else 432
        for it in items:
            dy = int(fs * .47)
            d.ellipse((x0 + 64, y + dy - 6, x0 + 76, y + dy + 6), fill=col)
            y = text_block(d, (x0 + 96, y), it, font(fs), INK, 690) + gap
        for j, (t, label) in enumerate(shots):
            tx = x0 + 40 + j * 380
            thumb(im, t, (tx, 725, tx + 360, 928), label, col)
    return im


PROBLEMS = [
    ("3D와 2D 파트의 입력 방식 충돌",
     "챕터 1은 레거시 Input, 챕터 2는 Input System을 써서 병합 후 키가 먹지 않는 구간이 생김",
     "입력 처리를 두 방식 동시 사용으로 두고, 조작키를 E · 좌클릭 · 우클릭 · Space · R로 전 챕터 통일"),
    ("챕터마다 제각각이던 UI",
     "팀원별로 만든 하트 · 목표 · 대사창의 크기와 폰트가 달라 한 게임처럼 보이지 않음",
     "공용 스킨 코드(ChapterHudStyle · ChapterDialogueSkin)로 하트 5칸 · 오른쪽 위 목표 · 대사창을 통일"),
    ("보스전에서 죽으면 처음부터 다시",
     "긴 보스전 · 스테이지를 매번 처음부터 반복해야 해서 흐름이 끊김",
     "스테이지별 체크포인트 리스폰, 2D 보스는 2페이즈에서 죽으면 2페이즈부터 재시작"),
    ("공중에서 착지 자세로 멈추는 애니메이션",
     "점프가 길어지면 착지 프레임에 멈춘 채 떠 있음 (저중력 구간)",
     "타이머 대신 수직 속도로 공중 프레임을 고르도록 바꿔 상승 · 정점 · 하강을 자연스럽게 표시"),
]


def card_problems():
    im = backdrop(470)
    header(im, 8, "개발 과정", "겪은 문제와 해결 방법", accent=RED)
    for i, (title, prob, sol) in enumerate(PROBLEMS):
        y0 = 240 + i * 180
        panel(im, (120, y0, 1800, y0 + 160))
        d = ImageDraw.Draw(im)
        d.text((160, y0 + 22), f"{i + 1}", font=font(52, True), fill=RED)
        d.text((220, y0 + 26), title, font=font(32, True), fill=INK)
        d.text((220, y0 + 76), "문제", font=font(22, True), fill=RED)
        text_block(d, (290, y0 + 74), prob, font(24), MUTED, 640)
        d.text((990, y0 + 76), "해결", font=font(22, True), fill=GRN)
        text_block(d, (1060, y0 + 74), sol, font(24), INK, 700)
    return im


def card_wrapup():
    im = backdrop(548)
    header(im, 9, "마무리", "자평과 추후 개선 계획", accent=YEL)
    left = [
        "3D 탐색과 2D 액션을 '두 자매의 꿈'이라는 하나의 이야기로 연결했습니다.",
        "시작 화면부터 엔딩 · 에필로그까지 끊김 없이 플레이할 수 있습니다.",
        "챕터마다 다른 분위기를 살리면서 조작키와 UI는 하나로 통일했습니다.",
        "아쉬운 점: 일부 효과음 · 음악이 임시 사운드이고, 챕터 2 난이도 편차가 있습니다.",
    ]
    right = [
        "임시 사운드를 정식 음원 · 성우 녹음으로 교체",
        "난이도 선택과 저장 · 이어하기 기능",
        "다른 환자의 꿈을 다루는 새 챕터 추가",
        "빌드 최적화와 배포 (PC)",
    ]
    for i, (title, items, col) in enumerate([("자평", left, YEL), ("추후 개선 계획", right, GRN)]):
        x0 = 120 + i * 850
        panel(im, (x0, 250, x0 + 820, 950))
        d = ImageDraw.Draw(im)
        d.rounded_rectangle((x0 + 30, 290, x0 + 38, 340), 4, fill=col)
        d.text((x0 + 60, 282), title, font=font(44, True), fill=INK)
        y = 390
        for it in items:
            d.ellipse((x0 + 64, y + 14, x0 + 76, y + 26), fill=col)
            y = text_block(d, (x0 + 96, y), it, font(30), INK, 680) + 30
    return im


if __name__ == "__main__":
    import sys
    os.makedirs(os.path.join(OUT, "cards"), exist_ok=True)
    fns = dict(title=card_title, team=card_team, overview=card_overview, controls=card_controls, flow=card_flow,
               features=card_features, tools=card_tools, roles0=lambda: card_roles(0), roles1=lambda: card_roles(1),
               problems=card_problems, wrapup=card_wrapup)
    names = sys.argv[1:] or list(fns)
    for n in names:
        fns[n]().convert("RGB").save(os.path.join(OUT, "cards", n + ".png"))
        print("card", n)
