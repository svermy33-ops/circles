# 《순환선》 M0 Modular Asset Catalog

기준일: 2026-11-09. C=현재 셀 1.5m, H=벽 3.6m, T=벽 0.22m. 예산·치수는 제작 계획이며 실제 문 폭/구조 envelope는 개발 데이터가 우선한다. 모든 아트 모듈은 Collider를 포함하지 않는다.

| 분류/ID | 기준 크기와 피벗 | 제안 triangle 상한 | 텍스처/재질 | M0 상태 |
|---|---|---:|---|---|
| Floor FL-01 | C×C, 상면 중심, 두께 아래 0.25m | 12~48 | 128², 타일 공용 | 실제 12tri 샘플 |
| Floor FL-02 | 손상 변형, 같은 외곽/상면 | 96 | FL-01 재사용 | 계획 |
| Wall WL-01 | C×H×T, 바닥 중심, 정면 -Z | 12~96 | 256², 타일+콘크리트 | 기본/낡은형 실제 샘플 |
| Wall WL-02 | 균열·박리, WL-01과 동일 envelope | 160 | 같은 atlas | 텍스처 손상만 M0 구현 |
| Wall Corner WC-I/O | H, 내/외 모서리 교점 바닥 피벗 | 96~192 | 벽 atlas | 계획, 이중 벽 겹침 금지 |
| Doorway DW-01 | 실제 portal width×2.6m 개구부, 문턱 중심 | 120~240 | 벽/금속 atlas | 계획, 문 폭 임의 축소 금지 |
| Pillar PL-01 | PDF 시험 2C×2C×H, 바닥 중심 | 24~160 | 콘크리트 공용 | 계획, 임의로 날씬하게 하지 않음 |
| Solid Block SB-01 | K의 회색 밀폐 footprint별 치수, 바닥 중심 | 48~256/조각 | 설비 atlas 256² 후보 | 계획, v0.4 개방부를 채우지 않음 |
| Platform PF-01 | C 길이, 상면 모서리 기준 | 24~96 | 바닥/경계 atlas | 계획, 실제 낙차 미확정 |
| Ceiling CE-01 | C×C, 아래면 중심 | 12~96 | 콘크리트 공용 | 계획, Visual 숨김 단위 포함 |
| Ceiling Light CL-01 | 0.6~1.2m 제안, 설치면 피벗 | 48~160 | 설비 atlas | 계획, 조명 수·shadow 예산 미확정 |
| Vegetation VG-01 | 풀 0.15~0.6m 제안, 뿌리 피벗 | 8~40 | 128² Alpha Clip 후보 | 계획 |
| Vegetation VG-02 | 벽 덩굴, 접착면 피벗 | 16~96 | 128²/256² 후보 | 계획, 통로 돌출 제한 |
| Vegetation VG-03 | 낮은 관목 0.5~1.2m 제안, 뿌리 피벗 | 80~240 | 식생 atlas | 계획 |
| Detail DT-01 | 배관·케이블, 끝 접합점 피벗 | 24~192 | 녹슨 금속 공용 | 계획, 고정 구조 안에 수용 |
| Detail DT-02 | 표지판, 설치면 피벗 | 12~64 | 128² 제안 | 계획, 가상 산업 명칭 |
| Detail DT-03 | 작은 잔해, 바닥 접점 | 12~96 | 공용 atlas | 계획, 이동로에 장애물처럼 보이지 않게 |

상한은 모델링 검토 기준이며 프레임 성능을 보증하는 수치가 아니다. 실루엣이 실제로 개선되지 않으면 상한까지 채우지 않는다. 식생은 triangle 수보다 화면 내 alpha overdraw와 그림자 비용도 함께 검수한다.

## 실제 납품 목록

`Assets/RinglineArt/M0/` 아래에 Unity가 생성한 다음 파일을 둔다.

- Mesh 3개: `SM_Wall_150x360x022.asset`, `SM_Wall_UnitCentered.asset`, `SM_Floor_150x025x150.asset`.
- Texture 4개: `T_Wall_Basic.png`, `T_Wall_Weathered.png`, `T_Wall_Weathered_Bilinear.png`, `T_Floor.png`. Point/Bilinear의 소스 그림은 동일한 규칙으로 생성한다.
- Material 4개: 기본 벽 Point, 낡은 벽 Point/Bilinear, 바닥 Point. 모두 URP Lit.
- Prefab 5개: 실측 벽 기본/낡은형, 실측 바닥, 현행 library용 중심 피벗 벽 기본/낡은형.

실측형과 단위 중심 호환형은 같은 이름의 단순 변형으로 섞지 않는다. 정확한 접합·피벗·UV 좌표는 `M0_TechnicalArtSpec.md`를 따른다. 기둥·문틀·식생 모델, FBX/GLB/Blender 원본은 이번에 만들지 않았다.

## 결합과 변형 규칙

실측 벽은 X축을 따라 C 간격으로 접합하고 양 끝면을 구조 경계에 맞춘다. 남북 벽은 실측형에서 90도 회전시키지만, 현재 unit-centered adapter는 개발 코드가 X/Z를 스케일하므로 회전을 아트 root에 넣지 않는다. 모서리 trim·문틀은 기존 구조 두께 안쪽에 수용하고 문 개구부를 침범하지 않는다.

바닥의 상면은 origin.y로 맞추며 서로 겹치는 동일 높이 면을 두지 않는다. 벽/바닥 이끼는 텍스처 변화부터 적용한다. 구조가 막힌 곳의 파손 표현은 어두운 금/박리로 제한해 통과 가능한 구멍처럼 읽히지 않게 한다. 긴 K구조물은 작은 조각의 반복을 사용해도 원본 점유 영역을 유지한다.

가변 상자·가구·상점·생존자·몬스터는 이 고정 건축 카탈로그와 별도 콘텐츠 묶음으로 제작한다. 프리팹 안에 방 ID나 원정별 보상·점유 상태를 저장하지 않는다.
