# 장난감 총 레퍼런스 프롬프트 (Dream1 / 밝은 꿈)

## 설계 방침
사용자가 준 레퍼런스 5장(버즈 블래스터 / 해피타임 물총 / 공룡 물총 /
별·날개 총 / 달팽이 물총)은 **실루엣만** 참고한다.
재질은 전부 Dream1 의 v3 펠트-CG 톤으로 치환한다. 플라스틱 광택은
기존 몹 5종·벽면 에셋과 충돌하므로 프롬프트에서 명시적으로 배제한다.

## 몹 프롬프트와 다른 점
| | 몹 5종 | 장난감 총 |
|---|---|---|
| 포즈 | T-포즈 강제 (리깅용) | 해당 없음 |
| 시점 | 정면 | **3/4 정면** |
| tPose | 1 | 0 (리깅 안 함) |
| 배제 | — | `No hands, no character` |

3/4 각도를 쓰는 이유: 정면 평면 이미지는 두께 정보가 없어 Generate3D 가
큐브로 만들어 버린다 (01_wall_sky_panel / 02_wall_corner 에서 확인된 사례).
총은 볼륨이 있는 프롭이라 옆면 프로파일이 보여야 한다.

## STYLE 블록 (프롭용 공통 꼬리말)
```
3D CG render in the style of a children's television craft world: everything is built from
felt, fabric, yarn and buttons with visible stitched seams and dashed stitch lines, but
rendered as clean stylized computer graphics - NOT a photograph of a real handmade craft
object, and NOT smooth plastic or vinyl. Bright saturated candy colors, plump rounded toy
shapes, soft even studio lighting. Single object, centered, the entire object fully visible
and not cropped, seen from a three-quarter front angle so its depth and side profile are
clearly readable. Plain flat white background, isolated asset render. No hands, no
character, no watermark, no extra props.
```

---

## 01. 솜뭉치 블래스터 (cotton blaster)
원본 실루엣: 버즈 라이트이어 블래스터 — 둥근 벌브 총구, 통통한 몸통.

A toy ray-gun blaster made entirely of felt and fabric. It has a chunky rounded body of
deep royal blue felt, a fat bulbous muzzle of cream white felt with spiral stitched ridges
wrapped around it, lime green felt fins on the sides, a small round porthole window on the
body made of a big glossy button, and a plump red felt trigger. Visible dashed stitch lines
run along every seam of the body. Soft, stuffed and pillowy, like a sewn plush prop.
[STYLE]

## 02. 펠트 물총 (felt squirt pistol)
원본 실루엣: 해피타임 물총 — 고전 스퀴트건, 상단 물탱크.

A classic toy squirt water pistol made entirely of felt and fabric. Apple green felt body
with a rounded nozzle tip of sky blue felt, a quilted pale mint felt tank panel on top
stitched with little circles like bubbles, an orange felt grip patch on the handle, a small
purple felt dial on the side, and a small orange felt trigger inside a rounded trigger
guard. Chunky, plump and rounded, with visible dashed stitch seams along the top and around
the tank panel.
[STYLE]

## 03. 공룡 아가리 총 (dino maw blaster)
원본 실루엣: 공룡 물총 — 벌린 입이 총구.

A toy blaster shaped like a cute dinosaur head, made entirely of felt and fabric. The body
is a big plump teal-green felt dinosaur head with its mouth wide open as the muzzle, small
white felt triangle teeth stitched around the mouth opening, a red felt tongue inside the
mouth, two large white button eyes with black centres on top of the head, tiny stitched
nostril dots, and a short stubby handle below with a small red felt trigger knob. Visible
dashed stitch seams run around the head and along the jaw.
[STYLE]

## 04. 별날개 요술총 (star-wing wand blaster)
원본 실루엣: 파스텔 별·날개 총 — 장식적, 구름·별·날개.

A cute toy magic blaster made entirely of felt and fabric, in pastel pink and baby blue.
The long body is soft pink felt decorated with baby blue felt stars and white felt cloud
puffs stitched onto it, a pair of small white felt wings sewn on near the muzzle, a baby
blue felt star sitting on the tip of the barrel, and a handle wound like a soft-serve swirl
in white and pink yarn. Visible dashed stitch lines outline every appliqued shape. Dreamy
and sweet.
[STYLE]

## 05. 달팽이 사탕총 (snail lollipop squirter)
원본 실루엣: 달팽이 물총 — 나선 껍데기가 물탱크.

A toy water pistol shaped like a cheerful snail, made entirely of felt and wool yarn. The
snail's body is bright yellow felt with a small stitched smiling face and rosy cheeks, two
short antennae topped with round beads, and its huge shell is a thick spiral coil of teal
and white wool yarn wound flat like a lollipop, forming the tank of the gun. A short yellow
felt handle below with a small red felt trigger knob. Visible dashed stitch seams along the
body.
[STYLE]

---

## 크레딧
GenerateImage (V2) 20 × 5 = **100 크레딧**
Generate3D 는 채택본만 진행 (200 크레딧/개)

## 3D 단계 설정 (프롭용)
polygonCount 20000 / topology tri / **tPose 0** / usePbrTexture 1 / textureSize 1024
리깅하지 않으므로 tPose 는 0. 몹(1)과 다르다.

## 스케일 기준
몹 0.900 m 기준, 손에 드는 장난감 총은 **0.20 ~ 0.25 m** 가 적정.
3D 생성 후 check_scale.py 로 검증할 것.

---

# 2차 — 비눗방울 총 5종 (2026-09-21)

## 설계 과제
비눗방울은 투명한데 Dream1 은 펠트·털실이다. 방울을 그대로 투명하게 그리면
재질이 튄다. 그래서 **방울을 공예 재료로 치환**했다.

| 방울 표현 | 사용처 |
|---|---|
| 흰 펠트 원 | 06 07 08 09 |
| 폼폼 | 06 07 08 10 |
| 홀로그램 스팽글 | 06 |
| 투명 비즈 | 09 |
| 튤 메시 | 10 |

각 프롬프트 끝에 `not real transparent soap bubbles` 를 명시했다.

## 각도 지시 강화
1차에서 02·05 가 정측면으로 흘렀다. 2차는 총구 방향과 기울기까지 지정했다.

```
seen from a three-quarter front angle with the barrel pointing to the left and
tilted slightly toward the camera, so that both the long side of the body and
the front face are clearly visible at once.
```

→ 5종 모두 의도한 각도로 나왔다. 이 문구를 앞으로 표준으로 쓴다.

---

## 06. 펠트 버블 블래스터
A toy bubble-blowing gun made entirely of felt and fabric. It has a chunky rounded body of
sky blue felt, and at the front a large open ring wand made of a padded white felt hoop
rimmed with tiny iridescent sequins. On top sits a rounded tank made of pale mint sheer
organza, and inside the tank are small white felt circles and fluffy pom-poms standing in
for soap bubbles. A plump yellow felt trigger below. A few white felt bubble circles of
different sizes are stitched onto the side of the body. All bubbles are craft-made from
felt, pom-poms and sequins - not real transparent soap bubbles.
[STYLE-2]

## 07. 링 다발 버블건
A toy bubble gun with a multi-ring wand, made entirely of felt and fabric. The body is
lavender purple felt, and at the front is a flat round disc holding six small open hoops of
white felt arranged like flower petals, each hoop rimmed with pale yellow stitching. A small
pale yellow felt tank sits under the barrel. A mint green felt trigger below. Tiny white
felt bubble circles and small pom-poms are stitched around the disc. All bubbles are
craft-made from felt and pom-poms - not real transparent soap bubbles.
[STYLE-2]

## 08. 고래 버블건
A toy bubble blower shaped like a cheerful whale, made entirely of felt and fabric. The
whale's plump body is navy and sky blue felt with a stitched smiling mouth, one large white
button eye, and a small felt tail fin at the back. On top of its head is a blowhole ring of
white felt, with a cluster of fluffy white pom-poms and white felt circles rising out of it
as bubbles. A short sky blue felt handle below with a red felt trigger knob. Visible dashed
stitch seams run along the belly and around the blowhole. All bubbles are craft-made from
pom-poms and felt circles - not real transparent soap bubbles.
[STYLE-2 / snout pointing to the left]

## 09. 비눗방울 지팡이총
A toy bubble wand blaster made entirely of felt and wool yarn. It has a slim cream felt body
and a long barrel ending in a very large open hoop tightly wrapped in mint green and white
wool yarn. Small clear round beads are threaded onto the hoop like tiny bubbles. A small
quilted cream felt pouch tank sits behind the hoop, and the handle is cream felt with a mint
green felt trigger. A few white felt bubble circles are stitched along the barrel. All
bubbles are craft-made from beads and felt circles - not real transparent soap bubbles.
[STYLE-2]

## 10. 통통 버블 저그
A chubby retro toy bubble gun made entirely of felt and fabric. It has a short fat body of
coral orange felt, and a big round dome tank on top made of white tulle mesh with fluffy
white pom-poms visible inside it as bubbles. At the front is a stubby wide ring wand of
cream felt. A teal felt grip patch on the handle and a small teal felt trigger. Visible
dashed stitch seams run around the body and the base of the dome. All bubbles are
craft-made from pom-poms and tulle mesh - not real transparent soap bubbles.
[STYLE-2]

## 3D 진행 전 주의 — 떠 있는 방울

06 · 07 · 08 은 **몸통에서 떨어져 공중에 뜬 방울**이 그려졌다.
이미지로는 예쁘지만 Generate3D 에 넣으면 셋 중 하나가 된다.
  - 방울이 통째로 누락된다
  - 허공에 떠 있는 별개 덩어리가 되어 메시가 지저분해진다
  - 몸통과 억지로 이어져 혹처럼 붙는다

떠 있는 방울을 지우고 재생성하거나, 3D 후 Blender 에서 분리 오브젝트를
삭제하는 편이 안전하다. 게임에서 방울은 어차피 파티클로 낼 것이므로
메시에 박아 둘 이유가 없다.

09 의 얇은 후프도 20,000 폴리에서 뭉개질 수 있다. 확인 필요.
10 이 3D 적합도가 가장 높다 — 볼륨이 꽉 차 있고 부속이 전부 몸통에 붙어 있다.
