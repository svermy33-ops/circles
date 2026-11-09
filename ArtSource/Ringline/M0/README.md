# M0 실사 기반 텍스처 제작 기록

기준일: 2026-11-09. 사용자 최종 기준은 **실사로 제작 → 해상도를 낮춰 도트가 보이게 축소 → 기존 픽셀에 맞춰 조금씩만 리터칭**이다. 격자나 줄눈 전체를 또렷하게 다시 그리지 않는다.

## 출처와 최종 해상도

원본은 built-in ImageGen으로 생성한 실사풍 이미지이며 현장 촬영 사진은 아니다. 별도 외부 사진을 다운로드하지 않았다. 원본과 최종 후보를 이 프로젝트에 복사했고 Codex 생성 원본도 남겼다.

| 대상 | 실제 사용 면 | 저장 아틀라스 | 기준 밀도 |
|---|---|---|---|
| 벽 | 32×77px | 64×128px | 약 21.3px/m |
| 바닥 | 32×32px | 64×64px | 약 21.3px/m |

초기 96×230/96², 중간 72×173/72²를 검토한 뒤 사용자 추가 지시로 위 해상도까지 낮췄다. 최종 프리팹과 아틀라스는 위 표의 값을 사용한다. 원본 프롬프트에 있던 초기 목표 해상도는 생성 당시 기록이며 현재 적용값이 아니다.

## 보관 구조

- `PhotoSources/`: 고해상도 기본 벽·낡은 벽·바닥 원본.
- `Downsampled/`: 면적 가중 box 필터로 축소한 실제 32×77/32² 이미지. 샤프닝이나 가짜 입자는 추가하지 않는다.
- `Review/`: 축소본의 최근접 12배 확대본. 확대 파일 크기를 실제 해상도로 오인하지 않는다.
- `RetouchCandidates/`: 격자를 강조하지 않도록 요청한 최종 ImageGen 국소 보정 후보.
- `RetouchMasks/`: 실제 반영 영역. 이미지 왼쪽 위 기준의 작은 줄눈 교차점 주변만 선택한다.
- `Retouched/`: 기본 벽은 수정 0, 낡은 벽은 10/2464texel(0.406%), 바닥은 4/1024texel(0.391%)만 수정한 최종 면.
- `FinalAtlases/`: 프리팹용 아틀라스. 면 텍스처와 그 면에서 가져온 끝면/상하면을 배치하고 4px edge dilation을 둔다.

리터칭의 RGB 채널 변화는 최대 6/255이며 마스크 밖 픽셀은 byte 단위로 동일하다. 후보 전체를 덮어쓰지 않고 1~2도트 영역에서 주변 색에 맞춰 약하게 합성한다. 선을 새로 그리거나, 전체 대비를 올리거나, 원래 사진의 흐린 줄눈을 검은 선으로 바꾸지 않는다. 자동 검사 기록은 `Docs/Art/Evidence/M0_PhotoTextureValidation.txt`에 있다.

## 재생성 및 프리팹 연결

Unity 메뉴 `Ringline/Art/Photo Pipeline/1 Prepare Downsamples` → 축소본 검토 → 필요 부위만 ImageGen 후보 제작 → `2 Apply Local Retouch And Pack` → `Ringline/Art/Generate M0 LookDev Samples` 순서다.

마지막 단계는 최종 아틀라스를 기존 Texture 경로에 복사하므로 Material·Prefab 참조를 유지한다. UV는 새 아틀라스에 맞추고 메시 크기·피벗·Collider는 유지한다. GPU 버퍼도 Mesh API로 갱신해 편집 직후 렌더에 이전 UV가 남지 않게 한다.

Point + Mipmap On이 기본이다. `M0_Close.png`와 `M0_PlayerView.png`는 기본 벽/낡은 벽 모두 Point이다. Bilinear는 별도 `M0_FilterComparison.png`에만 비교한다. 아직 이동 중 shimmer, 실제 역의 긴 벽/남북 벽 스케일, 타일의 seamless 반복을 검증하지 않았다.

## 최종 사용 프롬프트 집합

모든 생성·편집은 built-in `image_gen.imagegen`을 사용했다. CLI/API fallback은 사용하지 않았다.

### 기본 벽 원본

```text
Use case: photorealistic-natural. Asset type: physically plausible diffuse base-color texture SOURCE for a low-poly Unity abandoned metro station wall, later downsampled to only 96x230 pixels. Create one photorealistic front-on close photograph of an aged ceramic wall surface, neutral diffuse overcast illumination, approximately 1.5 meters wide by 3.6 meters tall. Portrait 5:12 aspect, texture fills the entire image edge to edge, strictly flat and orthographic, no perspective, no border. Three columns of square 50cm ceramic tiles, about seven rows vertically. Main upper tiles are warm chalky off-white / faded limestone beige. Lower third is a faded muted desaturated green ceramic dado band, continuous horizontally. Fine worn grout, subtle real ceramic pores, small irregular chips and believable patina. This is the basic maintained-but-old variant: only sparse hairline cracks and light damp dirt, no moss, no large broken holes. Real photographic material, NOT pixel art, NOT hand painting, NOT a rendered 3D wall prop. No floor, no ceiling, no objects, no text, no symbols, no logos, no cast shadows, no strong specular highlights, no vignette, no directional baked lighting. Keep outer left/right edges compatible for horizontal repeating. Retain realistic broad surface variation and restrained chroma.
```

### 바닥 원본

```text
Use case: photorealistic-natural. Asset type: square diffuse base-color texture SOURCE for a Unity low-poly abandoned station floor, later reduced to 96x96 pixels. One photorealistic straight-down material photograph, exactly flat orthographic, surface fills entire square edge to edge. A 1.5 by 1.5 meter patch with three by three large square 50cm stone/concrete floor tiles. Worn muted gray-green stone with warm mineral specks and subtle genuine porous concrete detail, weathered dark narrow grout. A few fine cracks and muted damp patches. Very sparse moss trapped only in some grout intersections, no continuous green stripes, no grass blades or weeds, no large debris. Neutral diffuse albedo-like illumination, no shadows from other objects, no directional lighting gradient, no highlights, no perspective, no borders, no text, no labels. Real photographic material texture, NOT pixel art, NOT illustration, NOT a 3D preview. All four edges should be visually tileable, with restrained contrast to keep small game characters readable.
```

### 낡은 벽 원본 변형

```text
Use case: precise-object-edit. Input image is the edit target: a photorealistic ceramic station wall diffuse material source. Make only a weathered variant of this exact wall. Preserve its exact portrait aspect ratio, flat front view, tile grid, grout alignment, beige upper tiles, muted green bottom two rows, and all overall material identity. Add restrained irregular damp staining near the base, a few tiny chips and one or two natural fine cracks, and small low-contrast moss colonies localized in lower grout corners. Photorealistic organic irregular patina, not painted blocks. Do not add a solid moss stripe, diagonal scratch repeated across the wall, holes, objects, text, borders, lighting gradients, or cast shadows. Keep tile edges legible and reasonably horizontally repeatable. Remain a HIGH RESOLUTION photograph-like source; do not pixelate yet. This source will later be downsampled to 96x230 pixels and only awkward details retouched.
```

### 낡은 벽의 약한 국소 보정

```text
Use case: precise-object-edit. This input is the EDIT TARGET, a 12x nearest-neighbor view of a 32x77 low-resolution photographic texture. The user specifically wants the existing SOFT, FAINT grout and photographic blur retained. Do NOT make the tile grid clearer, darker, sharper, straighter, more regular or more prominent. Do not redraw grout lines. Preserve essentially the entire input unchanged, including its merged coarse pixel colors and subtle irregularity. Retouch ONLY one or two tiny distracting isolated texels right at an existing grout intersection near the transition to the green lower wall band: match each to the surrounding existing pixel colors. Very small local value adjustments only. Do not add details, noise, cracks, outlines, high-contrast borders, or restore fine photographic detail. Keep the exact composition, tile positions, palette and source texel scale. This should be almost indistinguishable from the input, with no stronger visible grid.
```

### 바닥의 약한 국소 보정

```text
Use case: precise-object-edit. This input is the EDIT TARGET, a 12x nearest-neighbor view of a 32x32 low-resolution photographic texture. The user specifically wants the existing SOFT, FAINT grout and photographic blur retained. Do NOT make the tile grid clearer, darker, sharper, straighter, more regular or more prominent. Do not redraw grout lines. Preserve essentially the entire input unchanged, including its merged coarse pixel colors and subtle irregularity. Retouch ONLY one or two tiny distracting isolated texels right at an existing grout intersection near one-third and two-thirds across the floor: match each to the surrounding existing pixel colors. Very small local value adjustments only. Do not add details, noise, cracks, outlines, high-contrast borders, or restore fine photographic detail. Keep the exact composition, tile positions, palette and source texel scale. This should be almost indistinguishable from the input, with no stronger visible grid.
```
