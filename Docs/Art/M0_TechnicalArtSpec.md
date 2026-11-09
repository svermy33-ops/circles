# 《순환선》 M0 Technical Art Specification

기준일: 2026-11-09 | 코드에서 확인한 현재값과 제작 제안을 구분한다.

## 실제 개발 환경과 소유권

Unity 6000.3.23f1, URP 17.3.0, AI Navigation 2.0.14. `GraphicsSettings.asset` 및 PC 품질 설정은 `PC_RPAsset`을 사용하고 Mobile에는 별도 RP Asset이 있다. 샘플 렌더 시 실제 URP 활성화를 확인했다. 신규 렌더 패키지·외부 셰이더는 설치하지 않았다. Blender MCP 연결 실패, 로컬 Blender 실행 파일도 확인하지 못했다. 이번 실제 모델은 **Unity Editor C# Mesh API로 생성**했다. Blender 작업·FBX/GLB 왕복 검증을 했다고 간주하지 않는다.

개발이 소유하는 항목: 구조 좌표·크기·ID·벽·문·K 점유 영역·Collider·NavMesh·타일 경로·가변 콘텐츠 생성. 아트가 소유하는 항목: 렌더 메시·UV·텍스처·재질·시각적 변형·아트 전용 제작 도구. 아트 프리팹에는 Transform/MeshFilter/MeshRenderer만 둔다. NavMeshModifier, Rigidbody, Collider, NavMeshObstacle, gameplay MonoBehaviour를 넣지 않는다.

## 좌표와 실측 크기

근거: `StationLayoutDefinition.cs`, `StationCoordinateService.cs`, `StationGeometryBuilder.cs`, `M0_Floorplan.json`. 최종 승인을 뜻하지 않으며 개발 코드가 변경되면 다시 맞춘다.

| 항목 | 확인한 현재값/정의 | 상태 |
|---|---|---|
| 월드 단위 | 아트는 1 Unity unit = 1m로 제작 | M0 계약 제안 |
| Cell Size | 1.5 | PDF 시험값 및 JSON 현재값 |
| 도면 틀 | 72열×64행, 108×96m 환산 범위 | 건물 전체가 꽉 찬 직사각형이라는 의미 아님 |
| 원점 | `layout.origin`, 생성된 자산 (0,0,0) | 실제 자산 확인 |
| Up | Unity +Y | 코드 확인 |
| 열/행 증가 | 열 +X, 도면 행 +는 월드 -Z | 코드 확인 |
| 모델 전면 | 실측 벽의 표시 정면은 local -Z | 아트 규약 |
| 변환 | `world = origin + (column*1.5, elevation, -row*1.5)` | 코드 확인 |
| 타일 중심 | `(column+0.5, row+0.5)`를 위 공식에 입력 | 경계 원점과 혼동 금지 |
| 벽 높이/두께 | 3.6m / 0.22m | 생성된 layout 자산 확인, M0 시험값 |
| 문 높이 | 2.6m | 생성된 layout 자산 확인, M0 시험값 |
| 문 폭 | 각 `StationPortal.Width * cellSize` | 일률적 폭을 아트가 고정하지 않음 |
| 바닥 두께/상면 | 0.25m / origin.y | 개발 기본값 및 생성 코드 |
| Agent | radius 0.35m, height 1.8m | 개발 기본값, 아트가 변경 금지 |
| 기둥 | PDF는 2×2셀 6개, 시험 환산 3×3m | 최종 배열·높이 기획 검수 필요 |

Blender 제작 시 Metric, Unit Scale 1.0, 실제 치수로 모델링하고 Rotation/Scale을 Apply한다. Unity 도착 후 기준 축 마커를 검사한다. Blender의 Z-up을 Unity Y-up으로 변환하는 것은 내보내기/임포터가 담당하며 FBX와 glTF에 같은 축 옵션을 무조건 적용하지 않는다. 반사·미러·법선·전면 방향을 기준 큐브로 검사한다.

## 실제 앵커 계약과 두 가지 프리팹

요청의 개념도는 Anchor 아래 Collision/Visual을 두는 구조다. **현재 개발 구현은 실제로 `GeneratedBlockout/CollisionRoot`와 `VisualRoot`에 객체를 두고 `StationGeometryAnchor.collision/visual` 참조로 연결한다.** 따라서 아트 통합 도구가 `anchor.transform.Find("Visual")`을 가정하면 안 된다.

개발 `ApplyVisual()`은 Visual 아래 프리팹을 원점·무회전으로 배치하고 프리팹 localScale에 `anchor.size`를 곱한다. 현행 계약은 **중심 (0,0,0), bounds 1×1×1, root scale 1**이다. 벽·바닥 앵커는 구조물의 부피 중심에 위치한다. 현행 코드는 남북 방향 벽도 회전시키지 않고 X/Z 크기만 바꾼다.

| 프리팹 | 제작 좌표 | 연결 방법 |
|---|---|---|
| `PF_Wall_Basic_150`, `PF_Wall_Weathered_150` | 1.5×3.6×0.22m, 바닥 중심 피벗, scale 1 | 향후 실측 모듈 배치용. 현재 Visual Library에 직접 등록 금지 |
| `PF_Floor_150` | 1.5×0.25×1.5m, 상면 중심 피벗, 아래쪽으로 두께 | 향후 바닥 모듈 배치용. 현재 Visual Library에 직접 등록 금지 |
| `PF_Wall_Basic_UnitCentered`, `PF_Wall_Weathered_UnitCentered` | bounds 1×1×1, 중심 피벗, root scale 1 | 현행 `StationVisualLibrary`용 호환 샘플 |

단위 메시를 구조 크기로 확대하는 것은 현재 개발 계약을 명시적으로 따르는 **호환용 예외**이다. 실측 원본을 엉뚱한 Transform Scale로 보정하는 제작 방식으로 사용하지 않는다. 호환 메시의 UV는 폭 1.5m·높이 3.6m·두께 0.22m일 때만 목표 texel density에 가깝다. 긴 벽/남북 벽/다른 높이에서는 질감이 늘어난다. 현행 전체 역에 무조건 배포할 수 있는 최종 벽 키트가 아니다.

장기 권장: 개발 측에서 Visual 내부에 길이별 실측 모듈을 반복 배치하고 마지막 구간의 길이에 맞는 변형을 선택한다. 또는 크기·방향을 전달하는 UV/머티리얼 계약을 설계한다. 이 변경은 개발 소유 코드에서 수행한다. 아트가 `ApplyVisual()`을 직접 수정하지 않는다.

## Mesh 규격

- 실측 벽: local bounds X ±0.75, Y 0~3.6, Z ±0.11m. Floor: X/Z ±0.75, Y -0.25~0m.
- M0 메시: 모듈당 24 vertices / 12 triangles / 1 submesh / 하드 노멀. 앞·뒤·옆·상하 모두 존재한다.
- 대표 제작 예산은 카탈로그 참조. 메시의 균열은 통과 가능한 구멍을 만들지 않는다.
- 비주얼 돌출은 개발이 지정한 구조 envelope 안에 둔다. 추가 잔해가 통행을 막아야 한다면 별도 구조 변경 요청이다.
- UV0은 재질용. UV1 라이트맵은 M0에 없고 베이크도 하지 않았다. 베이크 단계에서 비중첩 UV1을 생성·검사한다.
- FBX는 필요한 메시만 선택해 명시적으로 triangulate, Geometry/Normals/UV를 포함한다. 카메라·조명·충돌체는 제외. 애니메이션 없는 환경 모듈은 rig/animation을 끈다.

## UV·텍스처 규격

| 항목 | M0 구현 및 M1 방향 |
|---|---|
| 환경 기준 밀도 | 사용자 최종 지시로 약 21.3 px/m. 플레이 거리에서 도트를 확인하되, 격자는 부드럽게 유지 |
| 벽 atlas | 64×128 RGB PNG. 앞/뒤 32×77px, 끝면 5×77px, 상하 32×5px |
| UV island 좌표 | 앞/뒤 (4,4)-(36,81), 끝면 (44,4)-(49,81), 상하 (4,96)-(36,101), 왼쪽 아래 원점 |
| 바닥 atlas | 64×64 RGB PNG, 상면 32×32px. 1.5m당 32px |
| 밀도 오차 | 벽 높이 77/3.6=21.39px/m, 두께 5/.22=22.73px/m. 정수 픽셀 반올림 |
| padding | island 경계 4px edge dilation. 낮은 mip에서 섞임을 막는 보장은 아님; 거리 검사 필요 |
| overlap | 동일 재질의 앞/뒤, 양 끝, 상/하 UV0를 의도적으로 공유. UV1에는 공유 금지 |
| 기본 필터 | Point + Mipmap On + bias 0 + aniso 1. Bilinear 비교 텍스처 별도 제공 |
| Import | Default, sRGB On, Clamp, NPOT None, alpha 없음, Read/Write Off, Uncompressed, max 128 |
| Map | Base Color만. Metallic=0, Smoothness=.05, normal/metallic/AO 텍스처 없음 |

제작 순서는 **실사풍 원본 → area box 축소 → 필요한 texel만 국소 리터칭 → 아틀라스**로 고정한다. 현재 실사풍 원본은 built-in ImageGen 생성 이미지다. 원본·축소본·마스크·최종본과 프롬프트는 `ArtSource/Ringline/M0/`에 보관한다. 기본 벽은 축소본을 그대로 사용하고, 낡은 벽은 10texel(0.406%), 바닥은 4texel(0.391%)만 리터칭했다. 마스크 밖 변경은 0, 채널 변화는 최대 6/255다. 흐린 줄눈 전체를 다시 그리거나 대비를 올리지 않는다. 기존 저해상도 픽셀의 색에 맞춰 조금만 수정한다. 코드로 그리는 반복 픽셀 무늬는 재생성 경로에서 제거했다.

Mip Off는 이동 중 shimmer 비교용 후속 후보이며 아직 테스트하지 않았다. 샘플의 반복 연결·atlas padding은 대형 맵에서 추가 개선이 필요하다. 전체 atlas에 어두운 AO와 방향성 그림자를 강하게 구워 넣지 않는다.

## Material·Shader

현행 URP Lit, 불투명, 한 모듈 한 재질, 그림자 cast/receive. 채색과 조명으로 스타일을 만든다. 외부 PS1 셰이더나 화면 효과는 필요하지 않다. 샘플 재질은 Instancing 플래그를 켰지만 실제 instanced draw 여부는 측정하지 않았다. SRP Batcher 우선 경로와 instancing은 자동으로 누적되는 최적화가 아니다.

향후 풀·덩굴 카드는 URP Lit Alpha Clip을 우선 검토하고 clip threshold·양면·shadow 비용을 실제 카메라에서 결정한다. Vertex Color와 AO, 라이트맵, foliage shader, 풍속 애니메이션은 M0 미구현이다. 낮·밤 가독성은 별도 조명 검수가 필요하다.

## FBX/GLB/Prefab 파이프라인

1. DCC 원본은 향후 `ArtSource/Ringline/`에 보관하고 실측 기준 큐브·축 표시를 포함한다. 현재 `.blend` 파일은 없다.
2. FBX를 Unity 기본 임포트 경로의 우선 후보로 사용. Scale Factor 1, 축 변환 후 실제 bounds 확인, normals import, blend shapes/animation/collider generation off, materials 자동 증식 금지. Tangents는 normal map을 사용할 때만 검토한다.
3. GLB는 현재 manifest에 importer가 없다. interchange 후보로만 기록한다. 승인 없이 glTFast 등 패키지를 추가하지 않는다. FBX/GLB를 제작 완료 산출물로 보고하지 않는다.
4. 가져온 mesh/texture를 아트 소유 Prefab으로 감싼다. 모델 파일을 직접 gameplay anchor로 사용하지 않는다. GUID를 유지하며 재임포트한다.
5. Prefab에 gameplay 컴포넌트가 없는지 검사하고, 현행 단위 중심 호환본과 향후 실측 모듈을 명확히 이름으로 구분한다.
6. 개발 채팅에서 Visual Library 참조만 바꾸어 통합한다. 통합 전후 구조 검증과 NavMesh 경로 비교를 개발 측에서 실행한다.

## 전달과 교체 절차

아트 샘플 생성: `Ringline > Art > Generate M0 LookDev Samples`. 기존 게임 씬을 열거나 저장하지 않고 preview scene을 사용한다. 이전 활성 씬의 root 수와 dirty flag 보존도 검사한다. 생성 경로는 `Assets/RinglineArt/M0`, 증거는 `Docs/Art/Evidence`이다. 저장 API는 아트 경로의 자산에만 적용한다. Preview scene 용도는 [Unity 6000.3 공식 문서](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/scenemanagement/editorscenemanager/newpreviewscene)를 따른다.

개발 통합 시에는 기존 Visual Library의 사본에서 Wall entry의 prefab을 `PF_Wall_Weathered_UnitCentered`로 지정한다. **entry.material도 해당 아트 재질로 바꾸거나 비워야 한다.** 현재 `ApplyVisual()`은 entry.material이 남아 있으면 프리팹 재질을 덮어쓴다. 전체 역 교체 전에 단일 임시 검증 앵커에서 시험한다. 아트가 실제 library asset이나 씬을 수정하지 않았다.

통합 체크: anchor ID/TRS/size 불변, collision 참조·bounds·layer 불변, Collider 추가 0, NavMeshModifier/Obstacle 추가 0, 동일 시작·끝 경로 complete 및 길이 일치, 문 개구부와 진입구 폭 불변, Raycast/시야 정책 불변, Z-fighting 없음, 머티리얼 오류 없음. 문턱·풀·덩굴도 이 원칙을 따른다. sample fixture의 collider 검사만으로 실제 맵의 walkability가 검증되었다고 주장하지 않는다.
