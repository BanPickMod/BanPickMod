# CONTRIBUTING — 코드 컨벤션 및 기여 가이드

> 이 문서는 BanPickMod 프로젝트에 기여하는 **사람과 AI 에이전트 모두**를 대상으로 합니다.  
> 코드를 작성하거나 수정하기 전에 반드시 숙지하세요.

---

## 0. 시작 전 필독 순서

1. [`docs/README.md`](./docs/README.md) — 문서 인덱스 확인
2. [`docs/Plan_launcher_revised.md`](./docs/Plan_launcher_revised.md) — 핵심 기획서 (전체 구조 파악)
3. [`docs/Design.md`](./docs/Design.md) — UI/UX 설계
4. 이 파일 (`CONTRIBUTING.md`) — 코드 컨벤션 및 작업 규칙

---

## 1. 프로젝트 구성 요소별 작업 범위

### 1.1 외부 런처 (`launcher/`)

- **언어**: C# (WPF 또는 WinForms) 또는 Python (tkinter/pywebview) — 기술 스택 결정 후 업데이트
- **역할**: 설치/업데이트/실행 도구. 게임 룰 판정·밴픽 결과 전달 로직을 여기에 넣지 않는다.
- **허용되는 SC2 통신**: 로컬 실행 맵 경로, UI 언어/설정, 비권위 메타 정보(외부 매치 ID 등)만.
- **금지**: Bank 기반 밴픽 결과 전달, 플레이어별 금지 유닛 외부 주입.

### 1.2 통합 확장 모드 (`sc2-mod/`)

SC2 에디터의 Galaxy Script / 트리거 기반 코드입니다.

**트리거 폴더 구조 준수 (§8.2, `Plan_launcher_revised.md`)**:

```
Triggers/
├─ Init/          # 전역 변수 초기화, DB 로딩, 기본값 세팅
├─ BanPick/       # 밴픽 UI/라운드/후보 풀/HUD
├─ Balance/       # 유닛 풀, 캠페인·스타1, 밸런스 조정, 역할 그룹
├─ Lobby/         # 대기실/옵션/옵저버/단계 흐름
└─ Convenience/   # QoL 기능 (자원, 카메라, 핫키 등)
```

**데이터 폴더 구조 준수**:

```
Data/
├─ DB/            # UnitMeta, UpgradeMeta, ButtonMeta, MapPool 레코드
├─ Units/         # 유닛 데이터 (커스텀/캠페인/스타1)
├─ Upgrades/      # Ban Lock 업그레이드, 밸런스 업그레이드
└─ UI/            # 레이아웃, 아이콘, 프레임 정의
```

### 1.3 BPM 전용 섬멸전 맵 (`sc2-maps/`)

- 각 맵은 `BPM_Core.SC2Mod`만을 의존성으로 가진다.
- 맵에 넣을 것: 지형, 시작 위치, 자원 배치, 맵별 오브젝트, 최소 초기화 트리거.
- **맵에 넣지 않을 것**: 공통 밴픽 UI/로직, 확장 유닛 데이터, 공통 Requirement/업그레이드 토큰.

---

## 2. 네이밍 규칙

### 2.1 파일 / ID 프리픽스

| 대상 | 프리픽스 규칙 | 예시 |
|---|---|---|
| BPM 전용 업그레이드 | `BPM_` | `BPM_Ban_Marine`, `BPM_Ban_ReaverBW` |
| BPM 전용 유닛 | `BP_` 또는 `BPM_` | `BPM_TestUnit` |
| 트리거 라이브러리 | 폴더명 약어 + `_` | `BP_Core`, `BP_Rounds`, `Conv_Camera` |
| SC2 맵 파일 | `BPM_1v1_` | `BPM_1v1_TestMap.SC2Map`, `BPM_1v1_Gresvan.SC2Map` |
| 캠페인/스타1 유닛 | 유닛명 + `BW` | `ReaverBW`, `DragoonBW`, `VultureBW` |

### 2.2 변수/함수명 (Galaxy Script)

- **전역 변수**: PascalCase, 의미 있는 접두어 포함
  - 예: `CfgUnitPoolMode`, `CfgBanCount`, `BPMStage`, `FinalDisabledUnits`
- **함수**: PascalCase
  - 예: `InitBanRounds()`, `ApplyBanUpgrades()`, `CreateStartUnits()`
- **로컬 변수**: camelCase
  - 예: `currentRound`, `targetPlayer`, `unitId`
- **상수/열거**: 대문자 + 언더스코어
  - 예: `BPMSTAGE_LOCKED`, `ROLE_TANK`, `POOL_VANILLA`

### 2.3 DB 레코드 필드명

- PascalCase 사용: `UnitId`, `DisplayNameKey`, `IconPath`, `IsBanEligible`, `DefaultPool_A`

---

## 3. 핵심 설계 원칙 (변경 금지)

아래는 기획서(`Plan_launcher_revised.md §2.1`)에서 확정된 원칙입니다.  
**이 원칙에 반하는 코드는 작성하지 않습니다.**

| 원칙 | 설명 |
|---|---|
| **Bank 미사용 (핵심 경로)** | 밴픽 결과, 금지 유닛, 승패는 Bank로 전달하지 않는다. 전역 변수/구조체 사용. |
| **Next Map 미사용** | 밴픽 후 다른 맵으로 전환하지 않는다. 같은 `.SC2Map` 세션 안에서 처리. |
| **런처는 게임 룰 판정 ✗** | 런처는 설치/업데이트/실행만 담당. 밴픽 결과를 외부에서 SC2에 주입하지 않는다. |
| **DB 기반 참조** | 유닛/업그레이드/버튼 ID를 하드코딩하지 않는다. `UnitMeta`, `UpgradeMeta`, `ButtonMeta` DB를 참조한다. |
| **모드 단일화** | 공통 기능은 `BPM_Core.SC2Mod` 하나에 집중. 맵마다 개별 로직을 복사하지 않는다. |

---

## 4. 코드 작성 규칙

### 4.1 공통

- 코드보다 **설계 문서를 먼저 확인**한다. 특히 새 기능을 추가할 때는 `Plan_launcher_revised.md`의 해당 섹션을 먼저 읽는다.
- 기존 코드를 수정할 때는 변경 이유를 주석 또는 커밋 메시지에 명시한다.
- "일단 동작하면 됨" 코드를 남기지 않는다. TODO 주석을 달고 이슈를 남긴다.

### 4.2 Galaxy Script (SC2 트리거)

- 트리거 하나가 하나의 명확한 역할을 갖도록 분리한다.
- 플레이어 루프는 `1 ~ MAX_PLAYERS` 범위를 하드코딩하지 않고 변수로 관리한다.
- 유닛/업그레이드 ID는 `UnitMeta`/`UpgradeMeta` DB 참조를 원칙으로 한다. 불가피한 경우 상단에 상수로 선언하고 이유를 주석으로 남긴다.
- `Bank`는 **사용자 UI 설정, 최근 선택 옵션, 디버그 로그**에만 사용한다.

### 4.3 런처 코드

- SC2 실행은 우선순위 순서를 따른다: `Support64\SC2Switcher_x64.exe` → `Support\SC2Switcher.exe` → `StarCraft II.exe`
- 파일 경로 관련 모든 작업에서 Path Traversal / ZIP Slip 방지 검사를 포함한다.
- 런처 상태(`state.txt`, `manifest.txt`)는 `%LOCALAPPDATA%\SC2BanPickModeLauncher\`에 저장한다.

---

## 5. AI 에이전트 작업 가이드

AI가 이 프로젝트에서 코드를 작성하거나 문서를 수정할 때 따라야 할 추가 규칙입니다.

### 5.1 작업 전 반드시 확인

1. `docs/Plan_launcher_revised.md`의 관련 섹션 — 기능/구조가 이미 설계되어 있는지 확인.
2. `docs/Design.md` — UI 관련 작업 전 레이아웃/색상/폰트 가이드 확인.
3. 이 파일(`CONTRIBUTING.md`) §2 네이밍 규칙 — ID/변수명이 규칙을 따르는지 확인.

### 5.2 금지 행동

- **핵심 설계 원칙(§3)을 우회하는 코드** 작성 금지.
  - 예: 런처에서 Bank 파일을 SC2 맵으로 직접 주입하는 코드, `Next Map` 사용.
- **문서와 일치하지 않는 구조** 임의 도입 금지.
  - 예: 트리거 폴더를 계획에 없는 이름으로 생성.
- **하드코딩된 유닛/업그레이드 ID**를 새로 추가하지 않는다. DB 레코드 추가를 먼저 제안한다.
- 문서 수정 시 **기존 설계 의도를 삭제하거나 덮어쓰지 않는다**. 변경이 필요하면 별도 섹션/문서로 제안한다.

### 5.3 권장 행동

- 새 기능 구현 전: "이 기능이 §12 로드맵의 몇 단계에 해당하는가?" 확인 후 작업.
- 불명확한 설계가 있으면 임의로 결정하지 않고, 코드/문서에 `TODO: [설계 확인 필요]` 주석을 남기고 사용자에게 질문한다.
- DB 레코드(UnitMeta 등)를 추가할 때는 `Plan_launcher_revised.md §8.4`의 필드 정의를 따른다.
- 테스트/검증이 가능한 변경은 `BPM_1v1_TestMap.SC2Map` 기준으로 먼저 확인한다.

### 5.4 문서 수정 시 규칙

- `docs/Plan_launcher_revised.md`는 **기획 확정 문서**다. 구현 중 발견한 수정 사항은 문서 하단에 `## [수정 제안]` 섹션을 추가하고 사용자 승인을 받는다.
- `docs/Design.md`는 UI 구현 진행에 따라 업데이트 가능하다. 단, 레이아웃 구조 변경은 `BanPick Style Guide.md`와 충돌하지 않는지 확인한다.
- `docs/README.md`의 문서 목록은 새 문서 추가 시 반드시 업데이트한다.

---

## 6. 커밋 메시지 규칙

```
<타입>(<범위>): <요약>

[선택] 본문: 변경 이유 및 맥락 설명
[선택] 참조: Plan §섹션번호, Design §섹션번호
```

**타입**:
- `feat` — 새 기능
- `fix` — 버그 수정
- `docs` — 문서만 변경
- `refactor` — 동작 변경 없는 구조 개선
- `data` — DB/데이터 레코드 추가·수정
- `chore` — 빌드/스크립트/설정 변경

**범위 예시**: `launcher`, `sc2-mod`, `sc2-maps`, `docs`, `ban-pick`, `hud`, `balance`

**예시**:
```
feat(ban-pick): Add shadow ban icon rendering in unit grid

Shadow ban slots now display silhouette icons during ban phase.
Actual unit reveals on match start as per Plan §3.5.

참조: Plan §5.4, Design §2.3
```

---

## 7. 이슈/PR 규칙

- **이슈 제목**: `[컴포넌트] 문제 또는 기능 요약`
  - 예: `[sc2-mod] 밴픽 타이머 만료 시 랜덤 밴 미작동`
- **PR 제목**: 커밋 메시지 타입 규칙과 동일하게 작성.
- PR에는 어떤 기획서 섹션을 구현했는지 링크 또는 참조를 포함한다.
