# 몹 5종 프롬프트 (VARCO 3D / nano-banana-pro)

## 공통 규칙
- **전원 2족 휴머노이드**. VARCO Rig 는 `humanoid` / `humanoid-fingers` 두 모드밖에 없어서
  4족·거미형은 리깅이 실패한다. 거미/골렘도 팔 2 · 다리 2 실루엣으로 설계했다.
- 이미지 단계에서 **T-포즈**를 반드시 강제해야 Generate3D 의 `tPose:1` 이 제대로 먹는다.
- Generate3D 설정: polygonCount 20000 / topology tri / tPose 1 / usePbrTexture 1 / textureSize 1024
- 스타일 블록(STYLE)은 5종 모두 동일하게 붙인다. 벽면·레벨 에셋과 같은 v3 펠트-CG 톤.

## STYLE 블록 (공통 꼬리말)
```
3D CG render in the style of a children's television craft world: everything is built from
felt, fabric, yarn and buttons with visible stitched seams and dashed stitch lines, but
rendered as clean stylized computer graphics - NOT a photograph of a real handmade craft
object, and NOT smooth plastic or vinyl. Bright saturated candy colors, plump rounded toy
shapes, soft even studio lighting. Single character, centered, entire body fully visible and
not cropped, seen straight from the front. Plain flat white background, isolated asset
render. No watermark, no extra props.
```

## POSE 블록 (공통 머리말 뒤에 붙임)
```
standing upright in a T-pose with both arms stretched straight out to the sides and legs
straight down and slightly apart, facing the camera.
The head, torso, both arms and both legs must all be clearly separated and readable as a
human-like figure.
```

---

## 01. 검은 솜뭉치 병사 (완료)
A small humanoid monster soldier made of dark charcoal felt, [POSE]
It has a rounded body, a clearly separate head with two pointed ears, two big white button
eyes with black centres, a tiny stitched smile, two distinct arms with simple mitten hands,
and two distinct legs with stubby feet. Visible stitched seams run down its body and limbs.
Mischievous but cute, not scary.
[STYLE]

## 02. 천조각 허수아비 (patchwork scarecrow)
A humanoid scarecrow monster stitched together from mismatched patches of burlap and
coloured fabric, [POSE]
Its head is a stuffed sack with a crooked stitched X for one eye and a black button for the
other, a wide zigzag-stitched mouth, and a floppy pointed straw hat sewn on. Loose straw
tufts poke out of the cuffs of both arms and the ankles of both legs. Its torso is a
patchwork of mustard, rust and olive fabric squares joined by thick visible cross-stitches.
Simple mitten hands, stubby feet. Ragged but friendly-creepy.
[STYLE]

## 03. 단추눈 인형 기사 (button-eyed doll knight)
A humanoid rag-doll knight made of cream and pale blue felt, [POSE]
Its head is a round stuffed doll head with two large flat black button eyes and a small
stitched nose and mouth. It wears a felt breastplate cut from silver-grey fabric with
stitched rivet dots, a small felt cape in deep red hanging behind the shoulders, and felt
pauldrons on both shoulders. Its arms end in mitten gauntlets and its legs in short felt
boots. Yarn hair in soft tan tufts. Noble and tidy, a toy soldier made of cloth.
[STYLE]

## 04. 털실 거미 인간 (yarn spiderling)
A humanoid spider monster whose body is wound entirely from thick purple and black wool
yarn, [POSE]
It stands on two legs and has two main arms. Its head is a yarn ball with six small round
glossy black bead eyes arranged in two rows and two tiny fangs of white felt. Four much
smaller decorative spider legs curve out from its back behind the shoulders, clearly behind
the body and not replacing the two main arms. The yarn winding is visible as spiralling
strands, with a few loose fibre wisps. Creepy-cute, not realistic.
[STYLE]

## 05. 펠트 골렘 (felt golem)
A large bulky humanoid golem built from thick slabs of grey and moss-green felt sewn
together like stone blocks, [POSE]
Its head is a blocky felt cube with two glowing pale-green felt discs for eyes and a deep
stitched seam for a mouth. Its shoulders, chest and thighs are made of separate rounded felt
slabs joined by thick visible cross-stitch seams, like a boulder body quilted from cloth.
Big heavy mitten fists and wide flat feet. Small tufts of green yarn moss sprout from its
shoulders and knees. Heavy and slow-looking but soft, made of cloth not rock.
[STYLE]
