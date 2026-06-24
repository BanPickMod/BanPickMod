# 밴픽 + 섬멸전 Mass Recall식 런처 구조 기획서 (초안)

> 이 문서는 밴픽 기반 섬멸전 모드를 **Mass Recall식 런처 구조**로 구현하기 위한 설계 문서이다.  
> 외부 런처, 런처 맵, 섬멸전 맵, 통합 확장 모드, 유닛/맵 데이터베이스, 옵저버 지원을 포함한다.

---

## 1. 개요

### 1.1 프로젝트 목표

- 밴픽 시스템을 포함한 1v1 섬멸전 모드를  
  **Mass Recall 스타일 런처 → 인게임** 구조로 구현한다.
- 스타2 공식 래더 맵 및 커스텀 섬멸전 맵들을 **맵 풀(Map Pool)**로 관리하고,  
  런처에서 선택/투표할 수 있도록 한다.
- 기존 편의성 확장 모드(`ConvenienceBase`)의 기능을  
  **현재 프로젝트의 통합 확장 모드 내부로 흡수**하여,  
  모드 간 의존성/충돌 가능성을 제거한다.
- 편의 기능 및 밴픽/밸런스에 사용되는 **유닛 데이터(아이콘 경로, 버튼, 업그레이드, 메타 정보)**를  
  에디터 상의 “데이터베이스(메타 데이터 테이블)” 형태로 정의하고,  
  트리거/코드는 이 DB를 참조해 동작하도록 만든다.
- 여러 확장 모드(`ConvenienceBase`, `BanPickSys`, `CampaignBalance`)를  
  **하나의 최종 확장 모드로 통합**하되,  
  내부적으로는 기능별 폴더/트리거 라이브러리/데이터 카테고리로  
  명확히 분리해 유지보수성을 확보한다.
- 1v1 플레이 + 옵저버(관전자) 0~N명을 지원한다.

### 1.2 전체 사용자 플로우

1. **외부 런처 실행**
   - 사용자가 프로젝트 전용 런처(EXE)를 실행한다.
   - 런처는 SC2 설치 경로와 모드 파일들을 확인한 뒤,
     SC2를 런처 맵(`BanpickLauncher.SC2Map`)과 함께 실행한다.

2. **SC2 + 런처 맵 로드**
   - SC2가 런처 맵을 로드한다.
   - 기본 UI는 숨겨지고, 프로젝트 전용 **대기실/메뉴 UI**가 전체 화면에 표시된다.

3. **대기실 / 맵 선택 / 옵저버**
   - Player 1, Player 2(참가자)가 입장하고 Ready 상태를 설정한다.
   - 추가 플레이어들은 **옵저버**로 입장한다.
   - 상단/중앙 UI에서 **맵 풀에 등록된 래더/커스텀 맵 목록** 중 하나를 선택한다.

4. **밴픽 설정 및 밴픽 진행**
   - 옵션 선택:
     - 유닛 풀 버전(UnitPoolMode: 공허 바닐라 / 캠페인+스타1),
     - 밴 개수(BanCount: 0 / 1 / 3 / 5),
     - 기타 룰 옵션(필요시).
   - 롤(Lol) 스타일 풀스크린 밴픽 UI에서:
     - 공개/쉐도우 밴,
     - 본인/상대 밴,
     - (캠페인 버전일 때) 역할 그룹 스왑 규칙을 적용해 밴을 진행한다.

5. **설정/밴 결과 저장 및 섬멸전 맵 로드**
   - 선택된 맵 ID, 옵션(UnitPoolMode, BanCount 등),  
     각 플레이어의 밴 결과를 **Bank + 내부 구조체**에 저장한다.
   - 런처 맵 트리거가 **선택된 섬멸전 맵(BP_Melee_XXX.SC2Map)**을  
     `Next Map`으로 설정하고 로드한다.

6. **섬멸전 인게임**
   - 섬멸전 맵 초기화 시:
     - Bank에서 설정/밴 결과를 읽고,
     - 통합 확장 모드의 BanPick/Balance 초기화 함수를 호출한다.
   - `FinalDisabledUnits`에 따라 유닛 생산/변태/패널/용병/업그레이드를 잠그고,
     상단 HUD(ME/ENEMY 토글)를 띄운 상태에서 섬멸전 경기를 시작한다.
   - 옵저버는 기본 관전자 시점으로 전체 경기를 관전한다.

### 1.3 대상 환경

- 게임 모드:
  - 1v1 섬멸전(플레이어 2명),
  - 옵저버(관전자) 0~N명.
- 실행 환경:
  - Battle.net 커스텀 게임(Arcade/Custom),
  - 로컬 에디터 `Test Document` 실행.
- 맵:
  - 공허의 유산 래더 맵(로컬 SC2Map 파일 형태로 확보),
  - 프로젝트 전용 섬멸전 맵(추가 제작).

---

## 2. 시스템 구조

### 2.1 구성 요소 및 역할

#### 2.1.1 외부 런처 클라이언트

- 역할
  - SC2 설치 경로 자동 검출 및 설정 저장.
  - 프로젝트에 필요한 `Mods/` 및 `Maps/` 폴더 구조, 파일 존재 여부 검사.
  - `SC2Switcher.exe` 또는 `StarCraft II.exe`를 호출해  
    `BanpickLauncher.SC2Map`을 즉시 로드하도록 실행.
- 선택적 역할
  - 맵 풀/모드 파일 다운로드 및 버전 업데이트 관리.
  - (추후) 랭킹/계정/매칭 시스템과의 연동.

#### 2.1.2 런처 맵 – `BanpickLauncher.SC2Map`

- 역할
  - Mass Recall의 메인 메뉴 역할을 하는 런처 맵.
  - 시작 시 SC2 기본 UI를 숨기고,  
    **대기실 + 맵 선택 + 옵션 설정 + 밴픽 UI**를 제공.
  - 맵 풀(`MapPool`)에서 섬멸전 맵을 선택하고,
    밴픽 시스템을 통해 밴 결과를 생성한다.
  - 최종 설정/밴 결과를 내부 구조체 + Bank에 저장하고,  
    섬멸전 맵을 `Next Map`으로 로드한다.

#### 2.1.3 섬멸전 맵들 – `BP_Melee_*.SC2Map`

- 역할
  - 실제 섬멸전 인게임이 진행되는 맵.
  - 공허의 유산 래더 맵/커스텀 맵들을 **맵 풀의 엔트리로 등록**해 사용.
  - `Map Initialization`에서 Bank를 읽고:
    - UnitPoolMode, BanCount, Ban 결과 등으로  
      통합 확장 모드의 상태를 초기화.
  - 기본 승리 조건은 섬멸전과 동일,  
    다만 유닛/업그레이드/패널에 밴 규칙이 적용된다.

#### 2.1.4 통합 확장 모드 – `BP_Combined.SC2Mod` (가칭)

- 기존 모드 통합:
  - `ConvenienceBase`의 편의 기능,
  - `BanPickSys`의 밴픽 UI/로직,
  - `CampaignBalance`의 유닛 풀/캠페인/스타1/밸런스 조정.
- 기능
  - 편의 기능:
    - 자원, 카메라, 핫키, 인터페이스 편의 기능 등.
  - 밴픽 시스템:
    - 밴 라운드/타이머/역할 그룹 스왑,
    - 인게임 HUD(ME/ENEMY).
  - 밸런스/유닛 풀:
    - Version A/B 유닛 풀 구성,
    - 캠페인/스타1 유닛 활성화,
    - Ban 업그레이드/Requirement, 밸런스 조정(리버 등).
  - 데이터베이스:
    - 유닛/아이콘/버튼/업그레이드/맵 메타 정보를 가지고 있는  
      “메타 테이블(Record/Catalog)”을 제공.

---

### 2.2 맵 풀 구조 (섬멸전 맵 관리)

#### 2.2.1 MapPool 메타 데이터

- `MapPool`은 런처/섬멸전 맵에서 공통으로 조회하는 **맵 메타 데이터 테이블**이다.
- 각 엔트리 필드 예시:
  - `MapId` : 문자열 키 (예: `"Ladder_Gresvan"`)
  - `FileName` : 실제 SC2Map 파일명 (예: `"Gresvan_LE.SC2Map"`)
  - `DisplayNameKey` : UI에 표시할 이름용 로컬라이즈 키
  - `MiniMapImage` : 썸네일 이미지 경로(선택 사항)
  - `MapSize` : S/M/L
  - `DescriptionKey` : 간단 설명(로컬라이즈 키)
  - `IsOfficialLadder` : 공식 래더 맵 여부(bool)
- 구현:
  - 통합 모드의 `Data/DB` 영역에 Record/Behavior/Upgrade 등을 이용해  
    메타 데이터를 저장.
  - 런처 맵/섬멸전 맵의 트리거에서 `Catalog Field Value Get`으로 읽는다.

#### 2.2.2 실제 래더 맵 포함 방식

- 프로젝트에 포함하고 싶은 래더/커스텀 맵을  
  `Maps/` 폴더에 `.SC2Map` 파일 형태로 배치한다.
- `MapPool`의 `FileName` 필드를 해당 파일명과 맞춘다.
- 런처 맵에서 플레이어가 `MapId`를 선택하면,
  - 트리거가 해당 `FileName`을 사용해 `Next Map`으로 설정한다.
- 필요시:
  - 맵 풀에서 꺼낸 맵 정보(예: 맵 크기)에 따라  
    대기실에서 간단한 추천/경고 메시지를 표시할 수 있다.

---

### 2.3 유닛/아이콘/버튼/업그레이드 데이터베이스화

#### 2.3.1 목표

- 유닛/업그레이드/버튼/아이콘 정보를  
  **하드코딩된 ID가 아니라 메타 데이터 테이블로 관리**하여,
  - 신규 캠페인 유닛/스타1 유닛 추가 시
  - 밴픽 UI/편의 기능/밸런스 로직이 DB만 수정해도 자동 반영되도록 한다.
- 기존 `ConvenienceBase` 코드에서 유닛/업그레이드 참조를  
  이 DB 구조로 옮겨 중복·충돌을 줄인다.

#### 2.3.2 UnitMeta 테이블

- 필드 예시:
  - `UnitId` : SC2 유닛 ID
  - `DisplayNameKey` : 유닛 이름용 텍스트 키
  - `IconPath` : 아이콘 파일 경로
  - `Race` : Terran / Zerg / Protoss
  - `TechTier` : 1 / 2 / 3 (또는 Early/Mid/Late)
  - `Role` : Tank / DPS / Siege / Spell 등
  - `IsWorker` : 밴 불가 여부 플래그
  - `IsBanEligible` : 밴 대상 포함 여부
- 사용처:
  - 밴픽 UI에서 유닛 그리드 생성/정렬/툴팁 표시.
  - 밴 후보 필터링(일꾼 제외, 특정 Role 제외 등).
  - HUD 아이콘 렌더링.

#### 2.3.3 UpgradeMeta / ButtonMeta

- `UpgradeMeta`:
  - `UpgradeId`
  - `RelatedUnitId`
  - `BanLockFlagId` : 해당 업그레이드를 Ban 잠금에 사용할 플래그
  - `IconPath`
- `ButtonMeta`:
  - `ButtonId` (Command Card 버튼 ID)
  - `UnitId` / `AbilityId`
  - `BanRequirementId` : Ban 업그레이드와 연결되는 Requirement ID
- 사용처:
  - 밴된 유닛의 생산 버튼/업그레이드/패널/용병 호출을  
    일관적으로 잠그는 데 사용한다.

---

### 2.4 통합 모드 구조 (기능별 코드/데이터 분리)

#### 2.4.1 트리거 폴더 구조

- `Triggers/Convenience/`
  - 기존 편의 모드 기능들 (자원, 생산, 카메라, UI QoL 등).
- `Triggers/BanPick/`
  - 밴픽 라운드/타이머/역할 그룹 스왑 로직.
  - 런처 맵 밴픽 UI 컨트롤.
  - 인게임에서 밴 결과 적용/HUD 제어.
- `Triggers/Balance/`
  - 유닛 풀 Version A/B 구성.
  - 캠페인·스타1 유닛 활성화/비활성.
  - Ban 업그레이드/Requirement 설정.
- `Triggers/Lobby/`
  - 런처 맵 대기실/맵 선택/옵저버 처리.
  - Bank 저장/로드.

각 폴더는 기능 단위의 라이브러리로 세분화:
- 예) `BP_BanRounds`, `BP_UnitPool`, `BP_HUD`, `BP_RoleGroups` 등.

#### 2.4.2 데이터 모듈 구조

- `Data/Units/`
  - 프로젝트에서 추가/수정하는 유닛(캠페인/스타1/밸런스 수정).
- `Data/Upgrades/`
  - Ban 업그레이드, 캠페인 전용 업그레이드 수정 등.
- `Data/UI/`
  - 밴픽 UI용 아이콘/레이아웃/프레임 정의.
- `Data/DB/`
  - `UnitMeta`, `UpgradeMeta`, `ButtonMeta`, `MapPool` 등 메타 데이터 레코드.

#### 2.4.3 기존 ConvenienceBase 흡수

- 기존 `ConvenienceBase`의 트리거/데이터 중:
  - 프로젝트에 필요한 기능들을 선별해
  - 통합 모드의 `Triggers/Convenience` 및 관련 Data 카테고리로 포팅한다.
- 외부 `ConvenienceBase` 모드에 대한 의존성은 제거하고,
  - 모든 편의 기능이 `BP_Combined.SC2Mod` 내부에서 해결되도록 한다.

---

### 2.5 플레이어/옵저버 구조

- Player 1:
  - Role = PlayerA (참가자 1).
- Player 2:
  - Role = PlayerB (참가자 2).
- Player 3~N:
  - Role = Observer (관전자).

처리 규칙:

- 런처 맵:
  - 대기실 UI에서 플레이어 슬롯(1,2)와 옵저버 슬롯(3~N)을 분리 표기.
  - 입력 권한:
    - Player 1,2만 Ready/맵 선택/밴픽 조작 가능.
    - Observer는 읽기 전용.
- 섬멸전 맵:
  - Ban/유닛 풀/밸런스 적용은 Player 1,2만 대상으로 수행.
  - 옵저버는 전체 시야/관전자 UI 사용.
  - HUD:
    - 기본적으로 Player 1 기준,
    - (선택) 단축키로 Player 1/2 기준 전환 가능.

---
## 3. 사용자 플로우 상세

> 이 장에서는 “외부 런처 → 런처 맵 → 밴픽 → 섬멸전 인게임”까지의  
> 전체 흐름을 단계별로 정리한다. 플레이어(참가자)와 옵저버의 시점을 모두 포함한다.

---

### 3.1 전체 단계 개요

1. 외부 런처 실행
2. SC2 + 런처 맵 로드
3. 대기실(플레이어/옵저버 입장, Ready)
4. 맵 선택 (맵 풀에서 선택)
5. 밴픽 옵션 설정 (유닛 풀 버전, 밴 개수 등)
6. 밴픽 진행 (풀스크린 UI)
7. 설정/밴 결과 저장
8. 섬멸전 맵 로드
9. 섬멸전 인게임 초기화
10. 경기 진행 + 관전

---

### 3.2 외부 런처 단계

**목표:** 사용자가 “게임 실행”만 누르면 SC2가 런처 맵부터 자동으로 시작되도록 한다.

- **사용자 행동**
  - 외부 런처(EXE)를 실행하고 “Play” 버튼 클릭.

- **런처 동작**
  - SC2 설치 경로 확인.
  - `BP_Combined.SC2Mod`, 런처 맵(`BanpickLauncher.SC2Map`), 섬멸전 맵들(`BP_Melee_*.SC2Map`)이
    올바른 폴더에 존재하는지 확인.
  - `SC2Switcher.exe` 또는 `StarCraft II.exe`를 호출해,
    `BanpickLauncher.SC2Map`이 직접 로드되도록 실행.

- **에러 처리**
  - SC2 미설치 / 경로 오류: 경고 메시지 + 경로 재설정 요청.
  - 필수 모드/맵 누락: 어떤 파일이 필요한지 안내 후 실행 중단.

---

### 3.3 런처 맵 로드 (BanpickLauncher)

**목표:** SC2 기본 UI를 숨기고, 프로젝트 전용 대기실/메뉴 UI를 표시한다.

- **맵 로드 직후**
  - `BP_Combined.SC2Mod`가 의존성으로 로드된다.
  - `Map Initialization` 트리거에서:
    - 기본 게임 UI 숨김(자원/미니맵/명령 패널).
    - 전체 화면 `RootDialog`/레이아웃 생성.
    - 대기실 화면(플레이어 리스트, 옵저버 리스트, 중앙 패널 등) 표시.

- **플레이어 역할 판정**
  - Player 1,2 → 참가자(Player).
  - Player 3~N → 옵저버(Observer).
  - 역할에 따라 UI 입력 가능 여부를 나중 단계에서 사용.

---

### 3.4 대기실 단계

**목표:** 두 참가자를 확정하고, 옵저버를 포함한 세션 구성을 안정화한다.

- **화면 구성**
  - 좌측 상단: Player 1, Player 2 슬롯
    - 이름, 종족, 색상, Ready 상태.
  - 좌측 하단 또는 우측: Observer 슬롯 목록
    - 이름, 색상 표시.
  - 중앙: 맵/옵션/밴픽 설정 패널(초기에는 기본 상태).

- **사용자 행동**
  - Player 1,2:
    - 본인 준비가 되면 Ready 토글 버튼 클릭.
  - 옵저버:
    - 별도 입력 없이 대기(Ready 버튼은 선택 사항).

- **로직**
  - 두 참가자(Player 1,2)가 모두 Ready일 때만
    “다음 단계(맵 선택/옵션 설정)를 진행 가능” 상태가 된다.
  - Ready 되기 전에는 맵/밴픽 설정 패널은 읽기 전용 또는 비활성화 상태.

- **예외**
  - 플레이어가 퇴장하면:
    - 해당 슬롯 Ready 해제, 세션을 다시 대기 상태로 되돌림.
  - 옵저버 퇴장은 진행에 영향 없음(단, UI 목록만 갱신).

---

### 3.5 맵 선택 단계 (맵 풀)

**목표:** 맵 풀에 정의된 섬멸전 맵들 중에서 이번 경기에 사용할 맵을 선택한다.

- **화면 구성 (중앙 패널)**
  - 맵 이름/타입/설명 표시.
  - 좌우 화살표 또는 목록/그리드 형태의 맵 선택 UI.
  - (선택) 미니맵 썸네일.

- **사용자 행동**
  - 기본적으로 Player 1(혹은 방장으로 지정된 플레이어)만 맵을 변경할 수 있다.
  - Player 2는 선택 상태를 보기만 한다.
  - 옵저버는 완전 읽기 전용.

- **로직**
  - 맵 선택 UI에서 `MapId`를 변경하면:
    - 해당 `MapId`에 대응하는 `MapPool` 엔트리를 조회.
    - FileName, DisplayName, MiniMap 등 UI 갱신.
  - 현재 선택된 MapId는 전역 변수에 저장.
  - 나중에 Bank 저장 시 `MapId`/`FileName`를 같이 기록한다.

- **제한**
  - 맵 풀에 없는 맵은 선택 불가.
  - 맵 풀이 비어 있을 경우, 기동 자체를 막거나 기본 맵 1개만 허용.

---

### 3.6 밴픽 옵션 설정 단계

**목표:** 밴픽 룰을 사전에 설정한다.

- **화면 구성 (중앙 패널 하단 등)**
  - 유닛 풀 버전 선택:
    - 토글 버튼/드롭다운:
      - “공허 바닐라” / “캠페인 + 스타1”.
  - 밴 개수 선택:
    - 0 / 1 / 3 / 5 버튼 또는 슬라이더.
  - (선택) 기타 옵션:
    - 선공/후공, 게임 속도, 특수 룰 토글 등.

- **사용자 행동**
  - Player 1(방장)이 옵션을 설정.
  - Player 2는 읽기 전용(또는 동의 버튼만).
  - 옵저버는 읽기 전용.

- **로직**
  - 선택된 옵션은 즉시 전역 변수에 반영:
    - `CfgUnitPoolMode`, `CfgBanCount` 등.
  - 옵션 변경 시 간단한 설명/툴팁 업데이트.
  - Ready 상태 + 맵 선택 + 옵션 설정이 완료되면  
    “밴픽 시작” 버튼이 활성화된다.

---

### 3.7 밴픽 진행 단계 (풀스크린 UI)

**목표:** 설정된 룰에 따라 플레이어 간 밴픽을 수행한다.

- **화면 전환**
  - “밴픽 시작” 버튼 클릭 시:
    - 대기실 UI 숨기기.
    - 밴픽 전용 풀스크린 UI 표시:
      - 상단: 라운드, 타이머, 현재 차례/밴 타입.
      - 좌측: 내 밴 리스트(플레이어 시점별).
      - 우측: 상대 밴 리스트.
      - 중앙: 유닛 아이콘 그리드.
      - 하단: 선택 미리보기 + 확인 버튼.

- **라운드 흐름**
  - `BanRounds[]`에 정의된 순서대로 진행:
    - 각 라운드: Owner, Target(Self/Enemy), Visibility(Public/Shadow).
  - 타이머(예: 10초) 시작.
  - 해당 라운드의 Owner인 플레이어만:
    - 유닛 그리드 클릭 + 확인 버튼 활성.

- **사용자 행동**
  - 차례인 플레이어:
    - 유닛 아이콘 클릭 → 미리보기 확인 → “확인(BAN)” 버튼.
  - 상대 플레이어/옵저버:
    - 밴 픽 과정을 관전만, 클릭은 무시.
  - 타이머가 끝나도 선택 없으면 해당 라운드는 “미선택”으로 패스.

- **플레이어 색/역할**
  - 내 관련 UI(내 밴 리스트, 내 차례 표시, 선택 테두리)는 내 색.
  - 상대 관련 UI는 상대 색.
  - 옵저버는 어떤 플레이어 시점인지 상단 라벨로 표시(예: “관전자 – Player1 vs Player2”).

- **밴 로직**
  - 유닛 멤버십/DB를 이용해:
    - 일꾼/밴 불가 유닛 필터.
    - 이미 밴된 유닛 필터.
    - Version B면 역할 그룹 스왑(그룹 BanFlag)을 적용.

- **종료**
  - 예정된 모든 라운드 수행 후:
    - 밴 결과 구조(`PublicBansAgainst`, `ShadowBansAgainst`, `SelfBans`) 확정.
    - FinalDisabledUnits 계산 준비 상태로 저장.

---

### 3.8 설정/밴 결과 저장 단계

**목표:** 런처 맵에서 결정된 모든 정보를 섬멸전 맵으로 전달할 수 있게 저장한다.

- **저장 대상**
  - 맵 정보:
    - `MapId`, `FileName`.
  - 룰/옵션:
    - `UnitPoolMode`, `BanCount`, 기타 룰.
  - 밴 결과:
    - 각 플레이어별 `PublicBansAgainst`, `ShadowBansAgainst`, `SelfBans`.
  - 랜덤 시드(필요시).

- **저장 방식**
  - 내부 트리거 전역 구조체:
    - 섬멸전 맵에서도 동일 구조를 사용할 경우를 대비.
  - Bank 파일 `BanpickConfig.SC2Bank`:
    - `[Config]`, `[PlayerA]`, `[PlayerB]` 섹션 등.

- **시점**
  - 밴픽 마지막 라운드가 끝난 직후.
  - 다음 맵 로딩을 걸기 전에 반드시 완료.

---

### 3.9 섬멸전 맵 로드 단계

**목표:** 선택된 섬멸전 맵으로 게임을 전환한다.

- **런처 맵 트리거**
  - `Game - Set Next Map(FileName)`:
    - `FileName`은 선택된 `MapId`에 대응하는 `MapPool.FileName`.
  - `Game - End Game` 또는 Next Map 로딩 API 호출.

- **엔진 동작**
  - SC2가 런처 맵을 종료하고,
  - 지정된 섬멸전 맵(`BP_Melee_XXX.SC2Map`)을 로드한다.
  - 같은 통합 모드(`BP_Combined.SC2Mod`)를 의존성으로 재사용.

---

### 3.10 섬멸전 인게임 초기화 단계

**목표:** 섬멸전 맵에서 런처에서 내려준 설정/밴 결과를 적용한 상태로 게임을 시작한다.

- **맵 초기화 트리거**
  - `Map Initialization`에서:
    - Bank `BanpickConfig` 열기.
    - `MapId`/`FileName` 검증 (혹시 잘못된 맵이면 에러 처리 또는 기본 값).
    - UnitPoolMode, BanCount, 밴 리스트 읽기.
  - 통합 모드 초기화:
    - `CfgUnitPoolMode`, `CfgBanCount` 설정.
    - BanPickSys의 구조체에 밴 리스트 복원.
    - `FinalDisabledUnits` 계산.
    - Ban 업그레이드/Requirement 적용(생산/업그레이드/패널/용병 잠금).
  - HUD:
    - 상단 중앙 ME/ENEMY HUD 생성 및 초기 뷰 설정.

- **플레이어/옵저버 초기화**
  - Player 1,2:
    - 종족/시작 위치/팀 등 섬멸전 기본 세팅.
  - 옵저버:
    - 관전자 설정, 전체 시야, 관전자 UI.

---

### 3.11 경기 진행 + 관전 단계

**목표:** 밴 결과가 적용된 섬멸전 경기를 자연스럽게 진행하고, 관전 경험도 제공한다.

- **플레이어 경험**
  - 시작 시:
    - 생산/연구/패널에서 밴된 유닛/능력이 잠겨 있는 것을 확인.
  - 게임 중:
    - 상단 HUD를 통해:
      - `Alt+B`로 ME/ENEMY 밴 뷰 토글,
      - 공개/쉐도우 밴 상태를 간단히 확인.
  - 승리 조건:
    - 기본 섬멸전 승리 조건과 동일.

- **옵저버 경험**
  - 전체 맵 시야.
  - 밴 HUD는:
    - 기본적으로 Player 1 기준,
    - (선택) 단축키로 Player 1/2 기준 전환 가능.
  - 리플레이 저장/공유는 기존 SC2 기능을 따름.

- **종료**
  - 게임이 끝나면:
    - SC2 기본 결과 화면.
    - (선택) 외부 런처가 리플리/결과를 추가로 처리할 수 있도록  
      파일/Bank에 최소 정보 기록.

---
## 4. 외부 런처 설계

> 이 장에서는 Mass Recall 스타일의 **외부 런처(EXE)**가  
> SC2 및 런처 맵과 어떻게 연동되는지, 역할 범위와 데이터 흐름을 정의한다.

---

### 4.1 런처의 역할 범위

#### 4.1.1 필수 역할

- **SC2 실행 및 런처 맵 호출**
  - 사용자가 런처에서 “Play”를 누르면:
    - SC2 설치 경로를 이용해 `SC2Switcher.exe` 또는 `StarCraft II.exe`를 실행.
    - 시작 맵으로 `BanpickLauncher.SC2Map`이 로드되도록 설정.

- **파일/버전 확인**
  - 실행 전 다음 항목들을 검사:
    - 통합 모드 `BP_Combined.SC2Mod` 존재 여부 및 버전.
    - 런처 맵 `BanpickLauncher.SC2Map`.
    - 섬멸전 맵 풀에 등록된 `BP_Melee_*.SC2Map`들.
  - 누락/버전 불일치 시 사용자에게 알리고 실행 중단 또는 업데이트 유도.

#### 4.1.2 선택적 역할 (확장 여지)

- **다운로드/업데이트 관리**
  - GitHub/클라우드에서 최신 모드/맵을 내려받아 Mods/Maps 폴더에 배치.
- **매치메이킹/계정 시스템 연동**
  - 외부 계정/랭킹/매치메이킹.
  - 매치 정보(맵, 룰, 선수명)를 Bank 또는 설정 파일에 기록해  
    런처 맵에서 읽게 할 수 있음.
- **경기 결과 수집**
  - 인게임 종료 후(별도 프로세스로) 리플레이/결과 파일을 찾아  
    서버에 업로드하거나 통계 저장.

---

### 4.2 SC2 실행 방식

#### 4.2.1 SC2 실행 파일 선택

- 기본 대상:
  - Windows: `StarCraft II.exe` 또는 `SC2Switcher.exe`.
- 런처 설정에서 사용자 환경에 따라 경로를 지정 가능하게 한다:
  - 예: `C:\Program Files (x86)\StarCraft II\StarCraft II.exe`.

#### 4.2.2 런처 맵 지정 방식

Mass Recall/Custom Campaign Launcher에서 사용하는 방식과 동일한 패턴을 따른다:

- **옵션 A – SC2Switcher 인자 활용**
  - 런처가 내부적으로:
    - `"<SC2Path>\Support\SC2Switcher.exe" -run "<MapsPath>\BanpickLauncher.SC2Map"`  
      와 유사한 명령을 호출(정확한 인자는 구현 시 확인 필요).
- **옵션 B – 저장된 프로필/캠페인 설정 활용**
  - 별도 설정 파일/프로필에서 “시작 맵”을 런처 맵으로 지정하고,  
    SC2를 실행하면 자동으로 그 맵을 불러오도록 구성.

실제 구현은 사용 중인 Campaign Launcher/SC2CCM 예제 코드를 참고해  
가장 호환성 좋은 방식을 선택한다.

#### 4.2.3 실행 환경 옵션

- 전체화면/창 모드, 해상도 등 그래픽 옵션은  
  SC2 내 설정에 맡기고, 런처는 관여하지 않는다(기본).
- 필요 시:
  - 첫 실행 시 최소 요구 해상도 체크(예: 16:9 이상) 후  
    경고 메시지만 제공.

---

### 4.3 파일/경로 관리

#### 4.3.1 경로 구조

런처는 다음 경로를 기준으로 동작한다:

- SC2 설치 루트:  
  - `SC2Root = C:\Program Files (x86)\StarCraft II\`
- Mods 경로:
  - `SC2Root\Mods\BP_Combined.SC2Mod`
- 런처 맵 경로:
  - `SC2Root\Maps\BanpickLauncher.SC2Map`
- 섬멸전 맵 경로:
  - `SC2Root\Maps\Banpick\BP_Melee_*.SC2Map`  
  (하위 폴더를 하나 두어 정리하는 것도 가능)

#### 4.3.2 파일 검증

런처 실행 시:

1. SC2 루트 경로 존재 여부 확인.
2. 필수 모드/맵 리스트를 하드코딩 또는 메타 데이터 파일로 정의:
   - `BP_Combined.SC2Mod`
   - `BanpickLauncher.SC2Map`
   - `BP_Melee_Arena.SC2Map`, `BP_Melee_XY.SC2Map` 등.
3. 각 파일의 존재를 체크:
   - 없으면:
     - “필수 파일 누락: BP_Combined.SC2Mod”처럼 메시지 출력.
     - (선택) “지금 다운로드/설치” 버튼 제공.

---

### 4.4 런처 ↔ SC2 데이터 교환 범위

#### 4.4.1 기본 방침

- **게임 룰/밴픽/설정의 대부분은 SC2 내부 런처 맵에서 처리**한다.
- 외부 런처는 기본적으로:
  - SC2 실행과 파일 준비까지만 책임진다.
- 이유:
  - BanPick/Balance 모드는 SC2 내부 트리거/데이터에 강하게 의존하며,
  - 외부에서 모든 밴픽을 처리하려면 코드/Bank 프로토콜이 복잡해진다.

#### 4.4.2 옵션 – 외부에서 일부 설정 미리 넘기기

확장 여지를 위해, 외부 런처가 다음 정보를 **Bank 또는 설정 파일**에 써둘 수 있다:

- 플레이어 이름/닉네임(표시용).
- 외부 매치메이킹에서 결정된 맵/유닛 풀 모드/밴 개수.
- 외부 플랫폼 기준 매치 ID.

SC2 런처 맵은:

- 초기화 시 해당 Bank/설정 파일을 읽고,
- 기본 값으로 세팅한 뒤, 플레이어가 런처 맵에서 최종 확인/수정할 수 있게 한다.

---

### 4.5 에러/예외 처리

#### 4.5.1 실행 전 오류

- SC2 설치 경로 없음:
  - “SC2를 찾을 수 없습니다. 설치 경로를 선택해 주세요.”
  - 경로 선택 UI 제공 후 재시도.
- 필수 모드/맵 누락:
  - 누락된 파일 목록을 표시.
  - (선택) 자동 다운로드/설치 옵션,
  - 또는 설치 가이드 링크 제공.

#### 4.5.2 실행 중 오류 (SC2 쪽)

- SC2가 비정상 종료되었을 때:
  - 런처는 로그를 남기고,
  - “게임이 예상치 못하게 종료되었습니다.” 메시지 표시.
- 런처에서 SC2 프로세스 실행 실패:
  - 권한/보안 소프트웨어 이슈 가능성 안내.

---

### 4.6 UX 고려사항

- **원클릭 플레이**:
  - 사용자는 런처에서 “Play”만 누르면
    → SC2가 켜지고
    → 런처 맵 대기실이 뜨도록 설계.
- **상태 표시**
  - “파일 확인 중…”, “SC2 실행 중…”, “오류: SC2 경로를 찾을 수 없음” 등  
    상태를 런처 UI에 명확히 표시.
- **고급 설정(선택 사항)**
  - SC2 실행 파일 경로 수동 지정.
  - Mods/Maps 설치 경로 오버라이드.
  - (향후) 외부 계정 로그인/매치메이킹 설정.

---
## 5. 런처 맵 설계 (`BanpickLauncher.SC2Map`)

> 이 장에서는 런처 맵이 제공하는 **대기실/맵 선택/옵션 설정/밴픽/전환** 기능을  
> UI 레이아웃 + 트리거 플로우 관점에서 정의한다.

---

### 5.1 런처 맵 개요

- 맵 이름: `BanpickLauncher.SC2Map`
- 플레이어 슬롯:
  - Player 1, 2: 참가자(PlayerA, PlayerB)
  - Player 3~N: 옵저버(Observer)
- 의존성:
  - `BP_Combined.SC2Mod` (통합 모드: 편의 + 밴픽 + 밸런스 + DB)
- 기본 동작:
  1. 맵 로드 → 기본 UI 숨김.
  2. 런처 전용 UI(대기실/맵 선택/옵션/밴픽) 표시.
  3. 설정/밴픽 완료 후 Bank 저장 + 섬멸전 맵 로드.

---

### 5.2 초기화: 기본 UI 숨김 및 루트 다이얼로그

#### 5.2.1 Map Initialization 트리거

- SC2 기본 UI 숨김:
  - 자원 패널, 미니맵, 명령 패널, 알림 등 비활성화.
- `RootDialog` 생성:
  - 화면 전체를 덮는 Dialog/레이아웃.
  - 반투명 어두운 배경(`BGPanel`) 설정.
- 주요 패널 생성:
  - `TopPanel`   : 프로젝트 로고/제목, 플레이어 이름, 안내 텍스트.
  - `LeftPanel`  : 플레이어 슬롯 및 Ready 버튼.
  - `RightPanel` : 옵저버 리스트.
  - `CenterPanel`: 맵 선택/옵션/밴픽 내용이 단계에 따라 표시됨.
  - `BottomPanel`: 상태 메시지, “밴픽 시작/게임 시작” 버튼.

#### 5.2.2 단계 상태 변수

- `LauncherStage` : 정수/Enum
  - 0 = 대기실
  - 1 = 맵/옵션 설정
  - 2 = 밴픽 진행
  - 3 = 완료/다음 맵 로딩 준비
- 각 단계 전환 시 UI를 재구성하거나, Panel 내 콘텐츠만 교체.

---

### 5.3 대기실 UI

#### 5.3.1 플레이어 슬롯 (LeftPanel)

- Player Slot 구조:
  - `PlayerSlot[1]`, `PlayerSlot[2]`
    - 이름 라벨 (Battle.net 닉네임 또는 Bank 기반 이름)
    - 종족 아이콘
    - 색상 표시(경계/아이콘)
    - Ready 체크박스/버튼
- 동작:
  - 해당 플레이어만 자신의 Ready 버튼을 클릭 가능.
  - Ready 상태가 변하면:
    - 라벨 색/아이콘으로 표시.
    - 상단 상태 메시지 업데이트.

#### 5.3.2 옵저버 리스트 (RightPanel)

- Observer Slot 목록:
  - Player 3~N에 대해:
    - 이름 라벨
    - 색상 표시
  - 스크롤 가능 리스트로 구성(옵저버 수가 많을 경우 대비).
- 옵저버에겐 Ready 버튼 없음(기본).

#### 5.3.3 상태/진행 조건

- `ReadyCountPlayers` = Ready인 참가자 수.
- 다음 조건 충족 시만 **다음 단계(맵/옵션 설정)**로 진입 가능:
  - Player 1, 2 모두 접속 + 두 슬롯 모두 Ready.
- BottomPanel에 상태 표시:
  - “플레이어 두 명이 Ready를 눌러야 합니다”
  - “Ready 완료 – 맵/옵션을 설정하세요” 등.

---

### 5.4 맵 선택 UI (CenterPanel – Stage 1)

Ready 조건이 충족되면 `LauncherStage = 1`로 바꾸고, CenterPanel 내용을 맵/옵션 UI로 교체한다.

#### 5.4.1 맵 풀 리스트 표시

- 중앙 상단:
  - 현재 선택된 맵 이름 (`MapPool[CurrentIndex].DisplayName`).
  - 맵 타입/크기/간단 설명.
- 중앙:
  - 미니맵 썸네일 이미지(선택사항).
- 중앙 하단:
  - 좌/우 화살표 버튼: 이전/다음 맵.
  - 혹은 드롭다운 목록으로 맵 직접 선택.

#### 5.4.2 조작 권한

- 기본:
  - Player 1 (방장)만 맵 변경 가능.
- Player 2 및 옵저버:
  - 선택 상태만 확인, 버튼 비활성(커서/색상으로 표시).

#### 5.4.3 내부 로직

- `CurrentMapIndex` 변경 시:
  - `MapId = MapPool[CurrentMapIndex].MapId`.
  - FileName, DisplayName, 설명, 썸네일 정보 업데이트.
- 최종적으로:
  - Bank 저장용 변수 `SelectedMapId`/`SelectedMapFileName`에 반영.

---

### 5.5 밴픽 옵션 설정 UI (CenterPanel – Stage 1)

맵 선택 UI 바로 아래/옆에 옵션 영역을 배치.

#### 5.5.1 유닛 풀 버전 선택

- 토글/버튼:
  - [공허 바닐라] [캠페인 + 스타1]
- 선택 시:
  - `CfgUnitPoolMode = 0 or 1`.
  - 간단 설명 텍스트 갱신:
    - “공허 바닐라: LotV 섬멸전 유닛만 사용”
    - “캠페인 + 스타1: 캠페인/스타1 유닛 포함, 그룹 스왑 활성화” 등.

#### 5.5.2 밴 개수 선택

- 버튼 or 슬라이더:
  - 0 / 1 / 3 / 5
- 선택 시:
  - `CfgBanCount` 업데이트.
  - 예상 밴 라운드 수를 상단/하단에 표시:
    - 예: “총 6 라운드, 플레이어당 3밴” 등.

#### 5.5.3 추가 옵션 (선택 사항)

- 선공/후공 설정:
  - “Player 1 선공 / 랜덤 / Player 2 선공” 토글.
- 특수 룰:
  - 예: “초반 3분 공격 불가”, “광물/가스 기본량 증가” 등 (향후 확장).

#### 5.5.4 적용/확정

- CenterPanel 하단 또는 BottomPanel에 버튼:
  - “밴픽 시작”
- 버튼 활성 조건:
  - Player 1,2 Ready 상태.
  - 최소 하나의 맵이 선택되어 있음.
  - UnitPoolMode, BanCount가 유효하게 설정됨.

---

### 5.6 밴픽 UI/로직 (CenterPanel – Stage 2)

“밴픽 시작” 버튼을 누르면 `LauncherStage = 2`로 전환하고,  
밴픽 전용 풀스크린 UI를 띄운다.

#### 5.6.1 레이아웃 개요

- 상단:
  - 타이틀: “밴할 유닛을 선택하세요”
  - 타이머 숫자 (10 → 0)
  - 라운드 정보: “라운드 X / Y”
  - 현재 차례/밴 타입:
    - 예: “Player 1 – 상대 유닛 공개 밴”
- 좌측 중앙:
  - `LeftBanList`: Player 1(내 시점) 기준 내가 밴한 것 목록.
- 우측 중앙:
  - `RightBanList`: 상대가 밴한 것 목록.
- 중앙:
  - 유닛 아이콘 그리드 (`UnitGrid`).
- 하단:
  - 선택 미리보기(아이콘 + 이름 + 설명).
  - “확인(BAN)” 버튼.
  - 안내 문구.

#### 5.6.2 권한/입력 제어

- 각 라운드에서 Owner인 플레이어만:
  - `UnitGrid` 클릭,
  - Confirm 버튼 활성.
- 상대/옵저버:
  - 마우스 오버 툴팁은 가능,
  - 클릭/확인은 비활성.

#### 5.6.3 라운드 진행 트리거

- `BanRounds[]` 초기화:
  - `CfgBanCount`와 룰에 따라 Owner/Target/Visibility 시퀀스 생성.
- `CurrentBanRoundIndex` 변수를 증가시키며 진행:
  - 라운드 시작:
    - 타이머 10초 세팅 및 UI 업데이트.
    - Target에 따른 UnitGrid 구성:
      - 상대 유닛 vs 자기 유닛,
      - UnitPoolMode에 따른 유닛 풀 사용,
      - 이미 밴된 유닛/일꾼/역할 그룹 잠금 유닛 필터링.
  - 라운드 종료:
    - 선택 확정 or 타임아웃.
    - Ban 리스트/Left/RightBanList UI 갱신.
    - 다음 라운드로 전환 or 밴픽 종료.

#### 5.6.4 밴 결과 반영

- 각 선택 시:
  - Visibility/Public이면 `PublicBansAgainst[target]`에 추가.
  - Visibility/Shadow이면 `ShadowBansAgainst[target]`에 추가.
  - Owner == Target이면 `SelfBans[owner]`에도 기록.
  - Version B + RoleGroup이 있을 경우: `GroupBanFlag[target, groupId] = true`.

---

### 5.7 런처 맵 종료 / 섬멸전 맵 전환

밴픽이 완료되면 `LauncherStage = 3`로 전환한다.

#### 5.7.1 Bank 저장

- `BanpickConfig.SC2Bank`에 저장:
  - `[Config]` 섹션:
    - `MapId`, `FileName`, `UnitPoolMode`, `BanCount`, `RandomSeed`.
  - `[PlayerA]`, `[PlayerB]`:
    - `Id` (실제 Player 번호),
    - `PublicBans`, `ShadowBans`, `SelfBans` (유닛 ID 리스트).

#### 5.7.2 Next Map 지정

- `SelectedMapFileName`을 기반으로:
  - `Game - Set Next Map(SelectedMapFileName)`.
- BottomPanel에 “섬멸전 맵 로딩 중…” 상태 표시.

#### 5.7.3 런처 맵 종료

- 짧은 딜레이 후:
  - `Game - End Game` 또는 Next Map 로딩 API 호출.
- SC2 엔진이 런처 맵을 종료하고,
  - 지정된 섬멸전 맵을 로드.

---

### 5.8 옵저버 처리 (런처 맵 관점)

- 대기실:
  - 옵저버는 별도 리스트에 이름/색상만 표시.
  - Ready/설정/밴픽 조작 권한 없음.
- 밴픽 화면:
  - 좌/우 밴 리스트, 그리드, 타이머 모두 동일하게 보인다.
  - 클릭 이벤트는 무시(버튼 비활성).
- 맵/옵션 설정:
  - 값 변화는 실시간으로 옵저버 화면에도 반영.

---
## 6. 섬멸전 맵 설계 (`BP_Melee_*.SC2Map`)

> 이 장에서는 런처 이후에 로드되는 **섬멸전 인게임 맵**의 요구사항과  
> 초기화/밴 적용/HUD/옵저버 처리 방식을 정의한다.

---

### 6.1 맵 목록 및 역할

#### 6.1.1 맵 종류

- 파일명 예시:
  - `BP_Melee_Arena.SC2Map`
  - `BP_Melee_Gresvan.SC2Map` (공식 래더 맵 포트)
  - `BP_Melee_XXXX.SC2Map` (추가 맵)
- 역할:
  - 각 맵은 1v1 섬멸전 경기를 위한 **실제 전장**.
  - BanPick/Balance 룰은 모드에서 통일 관리,  
    맵은 지형/시작 위치/자원 배치만 달라진다.

#### 6.1.2 공통 요구 사항

- 플레이어 수:
  - Player 1, 2: 참가자
  - Player 3~N: 옵저버(선택)
- 지형/시작 위치:
  - 양쪽 시작 지점이 공정한 1v1 레이아웃.
- 자원:
  - 표준 섬멸전 멀티 자원 배치(본진, 앞마당 등).
- 승리 조건:
  - 기본 섬멸전 규칙(모든 건물 파괴 등)을 사용.

---

### 6.2 의존성 및 모드 구조

#### 6.2.1 필수 의존성

각 섬멸전 맵은 다음 모드를 의존성으로 가진다:

- `BP_Combined.SC2Mod` (통합 확장 모드)
  - 내부에:
    - 편의 기능(Convenience),
    - 밴픽 시스템(BanPick),
    - 밸런스/유닛 풀(CampaignBalance),
    - 데이터베이스(DB)가 포함되어 있음.

#### 6.2.2 런처와의 연계

- 런처 맵에서 `Game - Set Next Map(SelectedMapFileName)`로  
  지정된 맵이 로드되므로,  
  모든 섬멸전 맵은 동일한 통합 모드에 의존해야 한다.
- 모드 버전 불일치 방지를 위해:
  - 맵 업로드 시 항상 최신 `BP_Combined.SC2Mod`를 참조하도록 관리.

---

### 6.3 플레이어/옵저버 설정

#### 6.3.1 Player Properties

- Player 1:
  - Controller: User
  - Race: Any (섬멸전 기본 설정)
  - Team: Team 1
- Player 2:
  - Controller: User
  - Race: Any
  - Team: Team 2
- Player 3~N (옵저버):
  - Controller: User
  - Team: 중립 또는 별도 관전자 팀
  - Alliance:
    - 모든 플레이어와 동맹,
    - Shared Vision 활성화 (관전용).

#### 6.3.2 관전자 옵션

- 맵 설정에서:
  - 옵저버 UI 사용 옵션 활성화.
  - 필요 시 관전자용 카메라/시야 설정.

---

### 6.4 초기화 로직 (Map Initialization)

**목표:** 런처에서 내려준 설정/밴 결과를 읽고,  
BanPick/Balance 모드를 초기화한 뒤 게임을 시작한다.

#### 6.4.1 Bank 읽기

- 초기화 트리거 `InitGameFromBank`:

1. `BankOpen("BanpickConfig", Player 1)` (또는 공용 Bank).
2. `[Config]` 섹션에서:
   - `MapId`
   - `FileName`
   - `UnitPoolMode`
   - `BanCount`
   - `RandomSeed` (있다면)
   을 읽어 변수에 저장.
3. `[PlayerA]`, `[PlayerB]` 섹션에서:
   - `Id` (실제 Player 번호: 1 또는 2)
   - `PublicBans`, `ShadowBans`, `SelfBans` (유닛 ID 리스트)
   를 읽어 구조체/배열에 복원.

- 검증:
  - `FileName`이 현재 맵 파일명과 다르면:
    - 경고/로그 기록 후, 기본 설정으로 진행하거나 게임 시작을 중단.

#### 6.4.2 통합 모드 초기화 호출

- `CfgUnitPoolMode = UnitPoolMode` 설정.
- `CfgBanCount = BanCount` 설정.
- BanPickSys 구조체에:
  - 각 플레이어별 `PublicBansAgainst`, `ShadowBansAgainst`, `SelfBans` 할당.
- Version B일 경우:
  - RoleGroups/GroupBanFlag 상태를 밴 결과에 맞게 초기화  
    (예: 이미 그룹 내 유닛이 밴된 그룹에 GroupBanFlag 설정).

---

### 6.5 밴 결과 적용 (생산/사용 제한)

**목표:** `FinalDisabledUnits`에 포함된 유닛이  
해당 플레이어에게 완전히 사용 불가가 되도록 한다.

#### 6.5.1 FinalDisabledUnits 계산

- 각 플레이어에 대해:
  - `FinalDisabledUnits[player] = Union(PublicBansAgainst[player], ShadowBansAgainst[player], SelfBans[player])`
  - 일꾼 등 밴 불가능 대상은 필터로 제거.

#### 6.5.2 Ban 업그레이드/Requirement 적용

- Data 모듈에서:
  - 각 유닛/능력/변태/패널/용병 버튼에
    - `Not(HasUpgrade(BP_Ban_UnitX))` 같은 Requirement를 부여.
- Trigger `ApplyBanLocks`:
  - `FinalDisabledUnits[player]`를 순회하며:
    - 해당 유닛에 대응하는 Ban 업그레이드 `BP_Ban_UnitX`를  
      `SetUpgradeLevel(player, BP_Ban_UnitX, 1)`로 설정.
- 효과:
  - 생산 건물의 해당 유닛 버튼 비활성화/숨김.
  - 그 유닛을 unlock하는 업그레이드/패널/용병 호출도,  
    동일 Ban 업그레이드를 Requirement에 연결해 잠김.

#### 6.5.3 특수 경로 처리

- 변태/진화:
  - 변태 결과 유닛이 `FinalDisabledUnits`에 포함되어 있으면,  
    해당 변태 버튼에도 같은 Ban Requirement를 연결.
- 캠페인/스타1 유닛:
  - 캠페인 패널/용병 호출/특수 소환 등  
    모든 경로에 동일 Ban 플래그 사용.

---

### 6.6 인게임 HUD (ME / ENEMY 토글)

#### 6.6.1 HUD 생성

- `CreateBanHud` 트리거:
  - 게임 시작 직후 호출.
  - 상단 중앙에 작은 HUD 다이얼로그 생성:
    - 라벨: “ME BANNED” 또는 “ENEMY BANNED”.
    - 아이콘 슬롯 N개(예: 6~8개) 배치.

#### 6.6.2 HUD 내용

- 플레이어 기준:
  - `BanHudMode[player] = 0` (ME) / `1` (ENEMY).
- ME 뷰:
  - `SelfBans[me] + PublicBansAgainst[me] + ShadowBansAgainst[me]`.
- ENEMY 뷰:
  - `SelfBans[enemy] + PublicBansAgainst[enemy] + ShadowBansAgainst[enemy]`.
- 표시 규칙:
  - 공개 밴: 실제 유닛 아이콘.
  - 쉐도우 밴: 잠금/마스크 아이콘.

#### 6.6.3 토글 핫키

- 트리거 `OnBanHudToggle`:
  - (예: Alt+B) 입력 시:
    - `BanHudMode[player] = 1 - BanHudMode[player]`.
    - HUD 라벨/아이콘 리스트 재렌더링.

#### 6.6.4 옵저버 HUD

- 옵저버는:
  - 기본적으로 Player 1 기준 HUD를 본다.
  - (선택) 다른 핫키로 Player 1/2 기준 전환:
    - 예: Alt+1 → Player 1 기준, Alt+2 → Player 2 기준.

---

### 6.7 편의 기능/기본 룰 적용

통합 모드의 편의 기능(원래 ConvenienceBase 내용)은  
섬멸전 맵에서도 함께 사용된다.

- 예:
  - 시작 자원/작업자 수 조정.
  - 카메라/생산/정보 표시 개선.
  - 멀티킬/자원/효율 관련 UI.

섬멸전 맵은:

- 특별히 덮어쓰지 않는 한,  
  통합 모드의 편의 기능을 모두 그대로 사용한다.

---

### 6.8 멀티/동기화 고려사항

- Bank 읽기:
  - 모든 플레이어(1,2 및 옵저버)는 같은 Bank 내용을 읽어야 한다.
  - 일반적으로 공용 Bank 또는 Player 1 기준 Bank를 사용.
- 연결 끊김:
  - 게임 시작 후 플레이어가 퇴장하면  
    SC2 기본 섬멸전 규칙에 따라 처리.
  - 옵저버 퇴장은 규칙/밴에 영향을 미치지 않음.
- 버전 불일치:
  - 통합 모드와 맵 버전이 다른 경우,
    - 개발/테스트 단계에서는 경고 로그를 남기고 중단,
    - 배포 단계에서는 런처가 설치/업데이트를 보장하는 것을 전제로 함.

---
## 7. 데이터 교환 설계 (Bank / 코드)

> 이 장에서는 **런처 맵 ↔ 섬멸전 맵** 사이의 데이터 교환 방식을 정의한다.  
> 주된 수단은 SC2 Bank 파일과, 필요 시 코드(압축 문자열)이다.

---

### 7.1 기본 방침

- 런처 맵에서 결정된 정보:
  - 맵 선택 결과,
  - 밴픽 옵션(유닛 풀, 밴 수, 기타 룰),
  - 밴 결과(유닛 리스트),
  - 랜덤 시드 등
- 은 **Bank 파일 + 내부 구조체**를 사용해 섬멸전 맵으로 전달한다.
- 외부 런처는 기본적으로 Bank에 직접 손대지 않고,
  - 필요한 경우에만 별도 설정 Bank 또는 파일을 통해 “초기 값” 정도만 제공한다.

---

### 7.2 Bank 파일 개요

- 파일명: `BanpickConfig.SC2Bank`
- 저장 위치: SC2 기본 Bank 폴더 (플레이어 계정 폴더 아래)
- 사용 범위:
  - 런처 맵:
    - “게임 시작” 직전에 쓰기(Save).
  - 섬멸전 맵:
    - `Map Initialization`에서 읽기(Load).

Bank 구조는 크게 세 섹션으로 나눈다:

1. `[Config]` : 경기 전반 설정.
2. `[PlayerA]` : Player A(실제 참가자 1)의 밴 정보.
3. `[PlayerB]` : Player B(실제 참가자 2)의 밴 정보.

옵저버는 Ban/룰의 대상이 아니므로 Bank에 별도 정보를 저장하지 않는다  
(필요하면 나중에 경기 메타 정보용 섹션을 추가할 수 있음).

---

### 7.3 [Config] 섹션 설계

#### 7.3.1 키 목록

- `MapId`
  - 타입: 문자열
  - 설명: 선택된 맵의 논리적 ID (예: `"Ladder_Gresvan"`).
- `FileName`
  - 타입: 문자열
  - 설명: 실제 로드할 맵 파일명 (예: `"Gresvan_LE.SC2Map"`).
- `UnitPoolMode`
  - 타입: 정수
  - 예: 0 = 공허 바닐라, 1 = 캠페인+스타1.
- `BanCount`
  - 타입: 정수
  - 예: 0 / 1 / 3 / 5.
- `FirstMover`
  - 타입: 정수
  - 예: 1 = Player 1 선공, 2 = Player 2 선공, 0 = 랜덤.
- `RandomSeed`
  - 타입: 정수(또는 문자열)
  - 설명: 밴픽/기타 룰에서 사용하는 RNG 시드 (optional).
- `ConfigVersion`
  - 타입: 정수
  - 설명: Bank 포맷 버전 관리용 (예: 1).

#### 7.3.2 런처 → 섬멸전 흐름

- 런처 맵:
  - 밴픽 완료/확정 시 위 키들을 모두 기록.
- 섬멸전 맵:
  - `Map Initialization`에서 `ConfigVersion`과 `MapId`/`FileName`을 검증.
  - 이상이 없으면 나머지 설정을 통합 모드 초기화에 사용.

---

### 7.4 [PlayerA] / [PlayerB] 섹션 설계

두 섹션은 동일 구조를 갖고, 각각 실제 참가자 한 명의 정보를 표현한다.

#### 7.4.1 기본 필드

- `Id`
  - 타입: 정수
  - 설명: SC2 Player 번호 (1 또는 2).
- `Name`
  - 타입: 문자열
  - 설명: 플레이어 이름(표시용, optional).
- `Race`
  - 타입: 문자열
  - 설명: T/Z/P/Random 등 (optional, 주로 UI용).

#### 7.4.2 밴 리스트 필드

밴 리스트는 **콤마 구분 문자열** 또는 **고정 구분자 문법**으로 저장한다.

- `PublicBans`
  - 타입: 문자열
  - 예: `"Marine,Marauder,Reaver"`
  - 설명: 이 플레이어가 상대에게 한 공개 밴(또는 자신에게 적용되는 공개 밴 포함).
- `ShadowBans`
  - 타입: 문자열
  - 예: `"VoidRay,Corsair"`
  - 설명: 이 플레이어에게 숨겨진(쉐도우) 밴 리스트.
- `SelfBans`
  - 타입: 문자열
  - 예: `"Baneling,Defiler"`
  - 설명: 이 플레이어가 **자기 유닛**을 밴한 목록.

※ 내부적으로는:
- 유닛 ID(예: `Marine`, `ReaverBW`, `CampReaver`)가  
  `UnitMeta` DB와 일치하도록 관리한다.

#### 7.4.3 선택 필드

- `RoleGroupBans`
  - 타입: 문자열 (optional)
  - 예: `"Zerg_Spellcasters,Protoss_Carriers"`
  - 설명: Version B에서 역할 그룹 단위 밴이 발생했을 때,  
    그룹 이름 ID를 저장해 두는 용도.
- `ExtraFlags`
  - 타입: 문자열 (optional)
  - 설명: 향후 필요해질 수 있는 플래그/옵션용 확장 필드.

---

### 7.5 문자열 인코딩 규칙

#### 7.5.1 리스트 포맷

- 기본 포맷:
  - `"UnitId1,UnitId2,UnitId3"`
- 빈 리스트:
  - 빈 문자열 `""` 또는 `"None"` (파서에서 둘 다 처리).
- 공백:
  - 가급적 공백 없이 저장(파서에서 Trim을 보조적으로 사용).

예시:

- `PublicBans = "Marine,SiegeTank,Mutalisk"`
- `ShadowBans = ""`
- `SelfBans = "ReaverBW"`

#### 7.5.2 파서 구현 방침

- 런처/섬멸전 트리거 공통 유틸:
  - `ParseUnitList(string) -> string array`
    - 콤마 기준 Split, 공백 Trim, 빈 항목 제거.
  - `SerializeUnitList(string array) -> string`
    - 배열을 콤마로 Join.
- 잘못된 값/알 수 없는 유닛 ID는:
  - 로그만 남기고 무시(또는 기본 값으로 치환).

---

### 7.6 런처 맵에서의 저장 플로우

1. 밴픽 끝 + 맵/옵션 확정 시:
   - 내부 구조체:
     - `SelectedMapId`, `SelectedMapFileName`,
     - `CfgUnitPoolMode`, `CfgBanCount`, `FirstMover`, `RandomSeed`,
     - Player A/B의 Ban 리스트 구조.
2. Bank 열기:
   - `BankOpen("BanpickConfig", Player 1)` (공용으로 사용).
3. `[Config]` 섹션에 키 쓰기:
   - `BankSet("Config", "MapId", SelectedMapId)` 등.
4. `[PlayerA]`, `[PlayerB]` 섹션에 각 Ban 리스트를  
   `SerializeUnitList`로 문자열화해 저장.
5. `BankSave()` 호출 후 닫기.

---

### 7.7 섬멸전 맵에서의 로드 플로우

1. `Map Initialization` 시:
   - `BankOpen("BanpickConfig", Player 1)`.
2. `[Config]` 읽기:
   - `ConfigVersion` 확인:
     - 예상 값과 다르면 → 기본 설정 사용 or 에러 처리.
   - `MapId`/`FileName` 읽어서 현재 맵과 대조:
     - 다르면 로그/경고, 필요시 기본 값 사용.
   - `UnitPoolMode`/`BanCount`/`FirstMover`/`RandomSeed` 읽기.
3. `[PlayerA]`, `[PlayerB]` 읽기:
   - `Id` → 실제 Player 번호 매핑.
   - `PublicBans`/`ShadowBans`/`SelfBans` 문자열 파싱 → 배열/구조체.
4. 통합 모드에 전달:
   - `CfgUnitPoolMode`, `CfgBanCount`, `FirstMover` 세팅.
   - 각 플레이어 구조체에 Ban 리스트 할당.
   - `FinalDisabledUnits` 계산 + Ban 업그레이드 적용.

---

### 7.8 외부 코드(시드) 기반 모드와의 호환성 (선택 사항)

향후 외부 클라이언트에서 밴픽을 수행하고  
“코드 하나”로 결과를 넘기고 싶을 경우를 위한 확장 설계.

#### 7.8.1 코드 포맷

- 예시 포맷:
  - `"MAPID|UPM|BC|P1PUB|P1SH|P1SELF|P2PUB|P2SH|P2SELF"`
- UPM(UnitPoolMode), BC(BanCount)는 숫자,
- 나머지는 UnitId 리스트를 Base64 또는 단축 코드로 인코딩 가능.

#### 7.8.2 런처 맵에서의 활용

- 런처 맵에 “코드 입력” 필드 추가(선택).
- 코드 입력 시:
  - 코드 파싱 → 내부 구조체에 반영.
  - Bank 저장 단계는 동일.
- 현재 설계에서는 **필수 아님**:
  - 내부 밴픽이 완성된 후, 필요하다면 추가.

---

### 7.9 버전 관리 및 호환성

- `ConfigVersion` 필드를 이용해 Bank 포맷을 관리한다.
  - 예: v1 → v2로 필드가 바뀔 때,
    - 섬멸전 맵에서 구버전 Bank를 읽어도 최소한의 동작만 보장하거나,
    - 에러 메시지 후 새 밴픽/설정을 요구하도록 처리.
- 중요한 변경 시나리오:
  - 새로운 옵션(예: 추가 룰 플래그)이 들어갈 때.
  - Ban 그룹/역할 시스템이 크게 바뀔 때.

---

### 7.10 예외/에러 처리 방침

- Bank 열기 실패:
  - 기본 섬멸전 설정(밴 없음, 기본 유닛 풀)으로 진행하거나,
  - 개발/테스트 환경에서는 에러 메시지 후 게임 중단.
- MapId/FileName 불일치:
  - 런처와 섬멸전 맵이 엇갈린 경우로 판단.
  - 기본 설정으로 진행하되 로그/경고 남김.
- 잘못된 유닛 ID:
  - 해당 유닛만 무시, 나머지는 정상 처리.
- PlayerA/B Id 누락/오류:
  - Player 1,2를 기본 A/B로 가정하거나,  
    양쪽에 동일한 밴을 적용하는 보수적 처리.

---
## 8. 모드/데이터 정의 (통합 확장 모드 `BP_Combined.SC2Mod`)

> 이 장에서는 통합 확장 모드의 **기능 구조(트리거)**와  
> **데이터 구조(유닛/업그레이드/UI/DB)**를 정의한다.  
> 기존 `ConvenienceBase`, `BanPickSys`, `CampaignBalance`를 하나로 합친 형태를 기준으로 한다.

---

### 8.1 모드 개요

- 모드 이름(가칭): `BP_Combined.SC2Mod`
- 목적:
  - 편의 기능(QoL),
  - 밴픽 시스템,
  - 유닛 풀/캠페인+스타1/밸런스 조정,
  - 메타 데이터(DB) 관리
  를 **단일 모드**에서 제공한다.
- 의존성:
  - 기본 라이브러리: `Liberty`, `Swarm`, `Void`
  - 멀티/캠페인: `Liberty Campaign`, `Swarm Campaign`, `Void Campaign`, `Void Multiplayer` (필요 범위에 맞게)
- 사용 대상:
  - `BanpickLauncher.SC2Map` (런처 맵),
  - `BP_Melee_*.SC2Map` (섬멸전 인게임 맵).

---

### 8.2 트리거 구조

#### 8.2.1 폴더 구조 개요

- `Triggers/Convenience/` – 편의 기능
- `Triggers/BanPick/` – 밴픽 로직/UI/데이터
- `Triggers/Balance/` – 유닛 풀/캠페인·스타1/밸런스
- `Triggers/Lobby/` – 런처 맵 대기실/맵 선택/옵저버
- `Triggers/Init/` – 공통 초기화/유틸리티

#### 8.2.2 Triggers/Convenience

기존 `ConvenienceBase` 기능에서 필요한 것만 선별해 이 안에 포팅한다.

- 예시 서브 라이브러리:
  - `Conv_Resources` : 시작 자원/작업자 수, 수동/자동 자원 관련 QoL.
  - `Conv_Camera` : 카메라 초기 위치, 관전 시점 보정.
  - `Conv_UI` : 핫키, 정보 표시, 간단 카운터/타이머 등.
  - `Conv_Misc` : 기타 게임 품질개선 기능.

원칙:

- **유닛/업그레이드 ID를 직접 하드코딩하지 않고**,  
  필요 시 `UnitMeta`/`UpgradeMeta` DB를 참조하도록 단계적으로 개선한다.
- 섬멸전/캠페인 공통으로 사용 가능한 기능만 남기고,  
  프로젝트와 관련 없는 기능은 제거 or 비활성화.

#### 8.2.3 Triggers/BanPick

밴픽 관련 로직과 UI를 분리/모듈화한다.

- 서브 라이브러리 예시:
  - `BP_Core`  
    - 공통 타입/상수/전역 변수 정의.
    - `CfgUnitPoolMode`, `CfgBanCount`, `FinalDisabledUnits[]` 등.
  - `BP_Rounds`  
    - Ban 라운드 시퀀스 생성/진행.
    - Owner/Target/Visibility 정의.
  - `BP_UnitPool`  
    - `UnitPoolMode`에 따른 유닛 후보 리스트 생성.
    - 일꾼/밴 불가 유닛 필터링.
  - `BP_LauncherUI`  
    - 런처 맵용 밴픽 UI (풀스크린 레이아웃 제어).
  - `BP_IngameApply`  
    - 섬멸전 맵에서 Ban 결과를 Ban 업그레이드/Requirement로 적용.
  - `BP_HUD`  
    - 인게임 HUD(ME/ENEMY 토글), 아이콘 리스트 갱신.

#### 8.2.4 Triggers/Balance

유닛 풀/캠페인+스타1/밸런스 조정 관련 로직.

- 서브 라이브러리 예시:
  - `BP_UnitPoolMeta`
    - Version A/B에서 어떤 유닛이 포함/제외되는지 메타 정보 관리.
  - `BP_CampaignUnits`
    - 캠페인/스타1 유닛 활성화/비활성 트리거(필요 시).
  - `BP_RoleGroups`
    - 역할 그룹 정의(예: Zerg Spellcasters, Protoss Spellcasters).
    - Version B에서 그룹 스왑/GroupBanFlag 반영 로직.
  - `BP_BalanceTweaks`
    - 리버 등 특정 유닛의 능력/사거리/쿨다운 조정(트리거 필요 시).

#### 8.2.5 Triggers/Lobby

런처 맵에서 사용하는 대기실/맵 선택/옵저버용 트리거.

- 서브 라이브러리 예시:
  - `Lobby_Init`
    - 런처 맵 초기화, 기본 UI 숨김, 루트 다이얼로그 생성.
  - `Lobby_PlayerSlots`
    - 플레이어/옵저버 슬롯 표기/Ready 로직.
  - `Lobby_MapSelect`
    - 맵 풀 표시/선택, CurrentMapIndex 관리.
  - `Lobby_Options`
    - UnitPoolMode, BanCount, 기타 룰 옵션 설정.
  - `Lobby_StageFlow`
    - Stage(대기실→옵션→밴픽→완료) 전환 관리.
  - `Lobby_BankSave`
    - Bank(`BanpickConfig`) 저장/검증.

#### 8.2.6 Triggers/Init & Util

- `Init_Global`
  - 모드 전역 변수 초기화, DB 로딩, 기본 값 세팅.
- `Util_Bank`
  - Bank 읽기/쓰기 공통 함수.
- `Util_Strings`
  - 리스트 문자열 파싱/직렬화(`ParseUnitList`, `SerializeUnitList` 등).
- `Util_Player`
  - PlayerA/PlayerB ↔ Player 번호 매핑.
- `Util_Color`
  - 플레이어 색상 정보 조회 및 UI에 적용.

---

### 8.3 데이터 모듈 구조

#### 8.3.1 Units (Data/Units)

- 기본 원칙:
  - 기존 멀티/캠페인 유닛을 직접 수정하기보다는,  
    **필요한 경우에 한해 복제/부분 수정**으로 처리.
- 카테고리 예시:
  - `BP_Units_Core`
    - 프로젝트 전용 유닛(예: 테스트 유닛, 특수 토템 등).
  - `BP_Units_Campaign`
    - 캠페인 유닛 추가/수정(히페리온, 히어로 유닛 등).
  - `BP_Units_SC1`
    - 스타1 유닛(리버, 드라군, 벌쳐 등)의 SC2 버전.
  - `BP_Units_Balance`
    - 밸런스 조정된 유닛 데이터(리버 사거리/쿨타임 변경 등).

- 캠페인/스타1 유닛은:
  - `UnitMeta` DB와 연결될 수 있도록  
    일관된 UnitId 네이밍을 사용:
    - 예: `ReaverBW`, `DragoonBW`, `VultureBW`.

#### 8.3.2 Upgrades (Data/Upgrades)

- `BP_Upgrades_BanLocks`
  - 각 유닛에 대응하는 Ban 업그레이드:
    - 예: `BP_Ban_Marine`, `BP_Ban_ReaverBW`.
  - Player별로 업그레이드 레벨을 1로 올려 Ban 적용.
- `BP_Upgrades_Balance`
  - 밸런스용 업그레이드(특정 유닛 능력 강화/너프).
- `BP_Upgrades_Campaign`
  - 캠페인/스타1 전용 업그레이드(연구, 패널 등).

#### 8.3.3 UI (Data/UI)

- Layout/Frame 정의:
  - 런처 맵용 UI 프레임 (대기실/맵 선택/밴픽 레이아웃).
  - 인게임 HUD 프레임(ME/ENEMY 표시).
- 아이콘:
  - 밴 HUD용 잠금/쉐도우 아이콘.
  - 특수 옵션/룰 아이콘.
- 버튼/Requirement 연결:
  - 커맨드 카드/패널/용병 버튼에 Ban Requirement 연결 시  
    필요한 UI 요소 정의.

---

### 8.4 DB 영역 (Data/DB)

**핵심 요구:**  
“유닛/업그레이드/버튼/맵” 정보가 하드코딩 대신  
**메타 데이터 레코드**로 관리되도록 한다.

#### 8.4.1 UnitMeta

- 구현 방식:
  - Record 또는 Dummy Unit/Behavior/Upgrade에  
    커스텀 필드를 사용해 메타 정보 저장.
- 필드 예시:
  - `UnitId` : SC2 유닛 ID (Record key 자체로 활용 가능)
  - `DisplayNameKey`
  - `IconPath`
  - `Race` (T/Z/P)
  - `TechTier` (1/2/3)
  - `Role` (DPS/Tank/Siege/Spell 등)
  - `IsWorker`
  - `IsBanEligible`
  - `DefaultPool_A` (Version A에서 사용 여부)
  - `DefaultPool_B` (Version B에서 사용 여부)

이 DB는:

- 런처 밴픽 UI의 유닛 그리드 구성,
- 유닛 풀 필터링,
- HUD/툴팁 표시의 근거가 된다.

#### 8.4.2 UpgradeMeta

- 필드 예시:
  - `UpgradeId`
  - `RelatedUnitId`
  - `BanLockUpgradeId` (Ban 업그레이드와 1:1, 필요 시)
  - `IconPath`
- 사용처:
  - 특정 유닛의 생산/업그레이드 잠금 제어를 일관되게 할 수 있도록 연결.

#### 8.4.3 ButtonMeta

- 필드 예시:
  - `ButtonId`
  - `UnitId` / `AbilityId`
  - `BanRequirementId`
- 사용처:
  - Ban 업그레이드와 Requirement를 연결할 때,  
    “어떤 버튼이 어떤 유닛/능력을 나타내는지”를 매핑하는 데이터.

#### 8.4.4 MapPool

- 필드 예시는 2.2에서 정의한 내용과 동일:
  - `MapId`, `FileName`, `DisplayNameKey`, `MiniMapImage`, `MapSize`, `DescriptionKey`, `IsOfficialLadder` 등.
- 런처 맵의 맵 선택 UI 및  
  섬멸전 맵 검증(이 맵이 어떤 ID로 선택된 것인지 확인)에 사용.

---

### 8.5 기존 ConvenienceBase 코드 흡수 전략

#### 8.5.1 단계적 포팅

1. ConvenienceBase 트리거/데이터에서  
   프로젝트와 관련된 기능만 선별.
2. 기능별로 `Triggers/Convenience` 아래에 복사:
   - `Conv_Resources`, `Conv_Camera`, `Conv_UI` 등.
3. 외부 `ConvenienceBase` 모드 의존성을  
   모든 맵에서 제거하고,
   - `BP_Combined.SC2Mod`만 사용하도록 변경.

#### 8.5.2 충돌 방지

- 동일 이름/ID를 가진 유닛/업그레이드/버튼이  
  다른 모드나 캠페인과 충돌하지 않게:
  - ID에 `BP_` prefix를 붙이는 등 네이밍 규칙을 정한다.
- 기존 편의 모드에서 쓰던 ID가  
  밴픽/밸런스와 겹치지 않도록 정리.

---

### 8.6 밴픽/밸런스와 DB의 연결

#### 8.6.1 밴픽 → DB 사용

- UnitGrid 구성 시:
  - `UnitMeta`에서 `IsBanEligible = true`인 유닛만 후보로 사용.
  - `DefaultPool_A/B`와 `UnitPoolMode`에 따라 필터링.
- 라운드 대상(본인/상대)에 따라:
  - Target Race/UnitSet을 결정할 때 `UnitMeta.Race` 등을 사용.

#### 8.6.2 밸런스 → DB 사용

- Version A/B에서 유닛 풀을 구성할 때:
  - `UnitMeta.DefaultPool_A/B` 값을 기반으로 유닛 목록을 생성.
- 캠페인/스타1 유닛 토글:
  - `UnitMeta`에 `IsCampaignOnly`/`IsSC1` 플래그를 두고,
    룰/옵션에 따라 활성/비활성 제어.

#### 8.6.3 편의 기능 → DB 사용 (장기 목표)

- 편의 기능이 특정 유닛/업그레이드를 참조할 때도:
  - 직접 ID를 쓰는 대신 `UnitMeta`/`UpgradeMeta`를 조회.
- 예:
  - 특정 유닛의 생산 상황/자원 표시 기능을  
    DB 기반으로 확장하기 용이해진다.

---

### 8.7 버전 관리 / 모드 업데이트

- `BP_Combined.SC2Mod` 버전 문자열:
  - 예: `BP_Combined v0.1.0`.
- 주요 변경 시:
  - Bank `ConfigVersion`,
  - DB Record 버전,
  - 맵/런처의 요구 버전 등을 변경 기록.
- 외부 런처에서:
  - 모드 버전 확인 후,
  - 구버전/신버전 간 호환성 안내 or 자동 업데이트 지원.

---
## 9. 옵저버 지원

> 이 장에서는 **옵저버(관전자)**가 보는 UI/정보 구성을 정의한다.  
> 기본적으로 **GameHeart 스타일 관전자 UI**를 기반으로 하되,  
> 양측의 밴픽 정보를 추가로 보여주는 형태로 확장한다.

---

### 9.1 목표 및 전제

- 옵저버는 **GameHeart 기반 관전자 UI**를 사용한다.
  - 생산/인구/자원/APM/인게임 타이밍 등 기존 정보 유지.
- 여기에 **양측 밴픽 결과(공개 + 쉐도우 상태)**를  
  추가 HUD로 표시한다.
- 런처 맵(밴픽 단계)와 섬멸전 인게임 모두에서:
  - 플레이어와 거의 동일한 정보를 보되,
  - 입력(클릭/확정)은 불가.
- 플레이어 수:
  - Player 1, Player 2: 실제 플레이어.
  - Player 3~N: 옵저버(관전자).

---

### 9.2 런처 맵에서의 옵저버 지원

#### 9.2.1 대기실 화면

- 옵저버 리스트:
  - RightPanel에 Player 3~N의 이름/색상 표시.
- 권한:
  - Ready/맵 변경/옵션 변경/밴픽 조작 불가.
  - UI 요소는 “비활성화된 버튼” 또는 커서 변경으로 표현.
- 정보:
  - 맵 선택 및 옵션 변경 상황은 실시간으로 옵저버 화면에도 동기화.

#### 9.2.2 밴픽 화면

- 옵저버는 플레이어와 동일한 밴픽 풀스크린 UI를 본다:
  - 좌측: Player 1 밴 리스트.
  - 우측: Player 2 밴 리스트.
  - 중앙: 유닛 그리드, 상단 타이머/라운드 표시.
- 입력:
  - 유닛 그리드 클릭/확인 버튼은 비활성화.
  - 마우스 오버 툴팁(유닛 이름/설명)은 허용.
- 공개/쉐도우 표시:
  - 플레이어 화면 기준과 동일 규칙:
    - 공개 밴: 실제 유닛 아이콘 + 이름.
    - 쉐도우 밴: 잠금/마스크 아이콘, 이름 미표시.

---

### 9.3 인게임 관전자 UI – GameHeart 기반

> 이 섹션은 GameHeart 스타일의 관전자 UI를 **전제로 한 확장**이다.  
> 실제 구현에서는 GameHeart 레이아웃/트리거를 참고하거나,  
> 유사한 구조를 모방해 제작한다.

#### 9.3.1 기본 정보 표시

- 상단:
  - 양 플레이어 이름, 종족, 색상, 점수/시리즈 스코어(선택).
- 좌측/우측:
  - 인구, 자원, APM, 업그레이드, 생산 큐 등 기본 GH HUD.
- 하단:
  - 타이머, 시청자/옵저버 리스트 등.

밴픽 확장 기능은 **기본 GameHeart HUD 위에 추가 레이어**로 구현한다.

---

### 9.4 인게임 밴픽 HUD – 옵저버 확장

#### 9.4.1 HUD 위치 및 형태

- 위치:
  - 화면 상단 중단, GameHeart 팀 이름/스코어 아래/사이에 배치.
- 구조:
  - 좌측 밴 바(Team 1, Player 1)
  - 우측 밴 바(Team 2, Player 2)
- 각 밴 바:
  - 최대 N개의 슬롯(예: 5개).
  - 슬롯별:
    - 공개 밴: 유닛 아이콘.
    - 쉐도우 밴: 잠금/마스크 아이콘.
  - 밴 타입(상대/자기/쉐도우)은 아이콘 테두리/오버레이 색으로 구분.

예시:

- 왼쪽 밴 바: Player 1 관점
  - 파란 테두리: Player 1이 밴한 상대 유닛.
  - 회색 잠금 아이콘: Player 1에게 숨겨진 쉐도우 밴(실제 유닛 미공개).
- 오른쪽 밴 바: Player 2 관점(빨간 테두리).

#### 9.4.2 옵저버용 정보 범위

옵저버에게 밴 정보를 어디까지 보여줄지 두 가지 모드가 있다:

- **토너먼트 모드(완전 공개)** – 기본안
  - 옵저버는 양쪽의 **실제 밴 유닛**을 모두 볼 수 있다.
  - 즉:
    - 플레이어 화면에서는 쉐도우 밴이 잠금 아이콘으로만 보이지만,
    - 옵저버(방송)는 실제 유닛 아이콘/이름을 본다.
- **플레이어 시점 모드(부분 공개)**
  - 옵저버도 특정 플레이어 시점 그대로 본다.
  - 이 경우 쉐도우 밴은 아이콘으로 숨겨진 상태 그대로.

기획 기본값은 **토너먼트/관전 편의상 “완전 공개 모드”**로 한다.  
(설정으로 두 모드 중 선택 가능하게 확장 여지 유지)

#### 9.4.3 HUD 동작 로직

- 게임 시작 시:
  - 각 플레이어의 `FinalDisabledUnits`,  
    `PublicBans`, `ShadowBans`, `SelfBans`를 읽어 HUD 데이터 생성.
- HUD 갱신:
  - Ban은 게임 시작 전에 확정이므로,  
    인게임 중에는 HUD 내용이 변하지 않는다.
- 툴팁:
  - 아이콘 위에 마우스를 올리면:
    - 유닛 이름 + 짧은 역할 설명 표시.

---

### 9.5 옵저버 HUD 토글 및 시점 전환

#### 9.5.1 기본 시점

- 기본적으로 옵저버 HUD는 “양쪽 모두 보여주는 모드”를 사용한다:
  - 상단 좌우 밴 바에 양 팀의 밴 정보를 동시에 표시.
- GameHeart 시점 토글(예: 탭 키로 P1/P2/양쪽 전환)을 사용할 경우,
  - 밴 바 테두리 강조색을 그 시점 팀에 맞게 조정.

#### 9.5.2 세부 토글 (선택 사항)

추가로 다음과 같은 토글을 제공할 수 있다:

- `Ctrl+B` : 밴 HUD 전체 On/Off.
- `Ctrl+1` / `Ctrl+2` : Player 1/2 관점으로 밴 HUD 축소 표시  
  (예: 좌측/우측만 확대).

이는 추후 실제 관전/방송 환경 테스트 후 필요 시 구현하는 옵션으로 둔다.

---

### 9.6 옵저버용 데이터 동기화

#### 9.6.1 런처 맵 → 인게임

- 옵저버는 런처 맵과 섬멸전 맵 모두 **동일 Bank**를 조회한다.
- 섬멸전 맵 초기화 시:
  - 플레이어용 Ban 구조체를 생성.
  - 옵저버 HUD용 데이터 구조도 함께 생성:
    - `ObserverBans_Player1`, `ObserverBans_Player2` 등.
- “완전 공개 모드”에서는:
  - 옵저버용 구조체에 **쉐도우 밴의 실제 유닛 ID**를 넣는다.
- “플레이어 시점 모드”에서는:
  - 플레이어 구조체를 그대로 참조해 보여준다.

#### 9.6.2 게임 내 지연/재접속

- 옵저버가 중간 합류/재접속해도:
  - `Map Initialization`이 끝난 후이므로,
  - HUD 생성 트리거는 `Player - Player joins game` 이벤트에서도  
    한 번 더 실행해 최신 상태를 그려준다.

---

### 9.7 예외/에러 처리 (옵저버 관련)

- Bank/밴 정보가 없는 경우:
  - HUD를 숨기고 기본 GameHeart UI만 사용.
- PlayerA/B ID가 잘못된 경우:
  - Ban HUD를 비활성화하고, 로그에만 기록.
- 옵저버가 많을 때:
  - HUD는 동일하게 유지,  
    옵저버 수가 많아도 표시 정보는 변하지 않음.

---

### 9.8 요약

- 런처/밴픽 단계:
  - 옵저버는 **플레이어와 동일한 화면**에서 밴픽 과정을 관전하되,
    조작은 불가.
- 인게임:
  - GameHeart 스타일 관전자 UI를 기본으로 사용하면서,
  - 화면 상단에 **양측 밴픽 결과 HUD**를 추가해  
    관전자/시청자가 밴 전략을 한눈에 이해할 수 있게 한다.
- 쉐도우 밴 공개 범위:
  - 기본값은 옵저버에게만 완전 공개하는 “토너먼트 모드”,
  - 필요하면 플레이어 시점과 동일하게 숨기는 모드도 옵션으로 지원.

---

## 10. 멀티/동기화 고려 사항

> 이 장에서는 1v1 + 옵저버 + **심판 슬롯**까지 포함한  
> 멀티플레이 동작 규칙과 동기화/판정 로직을 정의한다.

---

### 10.1 플레이어 역할/슬롯 구조

- Player 1: 참가자 A (`Role = PlayerA`)
- Player 2: 참가자 B (`Role = PlayerB`)
- Player 3: 심판 (`Role = Referee`) – **있어도 되고 없어도 되는 선택 슬롯**
- Player 4~N: 옵저버 (`Role = Observer`)

역할에 따른 권한:

- PlayerA/B:
  - Ready 토글 가능.
  - 맵/옵션을 **보기만** 가능 (기본적으로 방장/심판만 변경).
- 심판(Referee):
  - 맵/옵션 최종 확정 권한 (플레이어 대신 설정 가능 여부는 기획에 따라).
  - “게임 시작” 버튼 권한 (심판이 있을 때는 심판만).
  - 경기 중/후 판정(리매치/재게임/기권 처리 등)을 내릴 수 있는 주체.
- 옵저버:
  - 모든 설정/밴픽/인게임 관전만 가능, 입력 불가.

---

### 10.2 Ready 및 시작 조건

#### 10.2.1 공통 Ready 조건

- `ReadyA` = Player 1 Ready 여부
- `ReadyB` = Player 2 Ready 여부
- 두 값이 모두 `true`일 때만 **게임 시작** 버튼이 활성화될 수 있다.

#### 10.2.2 심판이 없는 경우

- 심판(Player 3)이 **없거나 유효하지 않은 경우**:

  - “방장(Host)” 개념:
    - 런처 맵에서 `HostPlayer`를 정한다.
    - 기본: 첫 입장 플레이어(일반적으로 Player 1).
  - 시작 조건:
    - `ReadyA == true` AND `ReadyB == true`.
    - `HostPlayer`에게만 “게임 시작” 버튼 활성화.
  - 동작:
    - Host가 “게임 시작” 클릭 → 밴픽 단계로 진입(또는 이미 끝났다면 섬멸전으로 이동).
    - Host가 나가면:
      - 남은 플레이어 중 하나를 새 Host로 지정 (예: Player 2 → Host).

#### 10.2.3 심판이 있는 경우

- Player 3이 **접속해 있고 Referee 역할로 설정**된 상태:

  - 시작 조건:
    - `ReadyA == true` AND `ReadyB == true`.
  - 버튼 권한:
    - Player 1,2는 Ready만 가능, “게임 시작” 버튼 **비활성화**.
    - Player 3(심판)에게만 “게임 시작” 버튼 활성화.
  - 동작:
    - 심판이 모든 설정/Ready 상태를 확인한 뒤,
    - “게임 시작” 클릭 → 밴픽/섬멸전 플로우 진행.

---

### 10.3 런처 맵 단계의 동기화

#### 10.3.1 대기실 동기화

- 각 플레이어/옵저버/심판이 입장/퇴장할 때:
  - `Lobby_PlayerSlots` 트리거가 전체 슬롯 UI를 갱신.
- Ready 상태 변경:
  - Ready 버튼 클릭 → 전역 변수 업데이트 → 모든 클라이언트에 UI 동기화.
- Host/Referee 표시:
  - `HostPlayer`와 `RefereePlayer`를 상단에 태그로 표시:
    - 예: “Host”, “Referee” 라벨.

#### 10.3.2 맵/옵션 변경 동기화

- 맵/옵션 변경 권한은:
  - 심판이 있으면 → 심판,
  - 없으면 → HostPlayer.
- 변경 시:
  - 전역 변수 값 업데이트,
  - CenterPanel 내용을 모든 클라이언트에게 재렌더링.

---

### 10.4 밴픽 단계의 동기화

- 차례(Owner), Target 정보는 **서버/호스트 권위 트리거**를 기준으로 결정.
- 각 밴 라운드마다:
  - Owner인 플레이어에게만 클릭/확정 버튼 활성화,
  - 나머지는 읽기 전용.
- 심판/옵저버:
  - 밴 진행 상황을 실시간 관전.
- 타이머:
  - 서버 기준으로 감소,  
    각 클라이언트는 동일한 남은 시간 표시.
- 따라서:
  - 네트워크 지연/입력 동기화는 밴픽 로직에서 일관되게 관리되어야 한다.

---

### 10.5 연결 끊김/재접속 처리

#### 10.5.1 런처/밴픽 단계에서의 튕김

- 참가자(PlayerA/B)가 런처 또는 밴픽 단계에서 튕길 경우:
  - 즉시 Ready 해제 + 슬롯 비어 있음 표시.
  - 진행 중인 밴픽이 있다면:
    - 그 즉시 밴픽을 중단하고 대기실 상태로 롤백하거나,
    - 심판이 있다면 심판에게 대기실로 리턴/세션 종료 버튼 제공.
- 심판이 있는 경우:
  - 심판은 이 상황에서 “해당 플레이어 패배” 또는 “재매치” 같은  
    외부 판정을 내릴 수 있게 해야 하지만,  
    **규칙상 기본 안전 규칙은 ‘튕긴 플레이어 패배’**로 한다.
  - 즉:
    - 심판이 추가 판정 로직을 쓰지 않을 경우,
    - 시스템 기본값은 튕긴 쪽 패배로 처리.

#### 10.5.2 인게임(섬멸전 맵)에서의 튕김

- 기본 안전 규칙:
  - Player 1 또는 Player 2가 게임 중 튕기면:
    - SC2 자체 규칙으로 남은 플레이어가 승리 처리.
    - 이는 “튕긴 플레이어 패배”라는 안전 규칙과 일치.
- 심판이 있는 경우:
  - 심판은 이 경기 결과를 외부적으로(토너먼트 규정 등)  
    번복하거나 재경기를 명령할 수 있다.
  - 다만 인게임 시스템 관점에서는:
    - 튕긴 플레이어가 패배로 기록되는 기본 동작을 유지한다.

---

### 10.6 심판 기능 상세

#### 10.6.1 심판 UI 요소

- 런처 맵 상단/하단에 심판 전용 UI:
  - “게임 시작” 버튼 (Ready 조건 충족 시 활성화).
  - (선택) “리매치 요청”, “설정 리셋” 버튼 등.
- 인게임에서:
  - 별도의 심판 전용 UI는 필수는 아니지만,
  - (향후) 게임 도중 특정 이벤트(리메이크/재시작)를 트리거할 수 있는  
    심판용 단축키/버튼 추가도 가능.

#### 10.6.2 심판 부재 시 대체 동작

- Player 3가 없으면:
  - HostPlayer가 실질적 “심판/방장” 역할을 수행:
    - 맵/옵션 확정,
    - “게임 시작” 버튼 클릭.

---

### 10.7 Bank 및 상태 동기화

- Bank(`BanpickConfig`)는:
  - 런처 맵에서 **한 번만** 쓰고,
  - 섬멸전 맵에서 **한 번만** 읽는다.
- 심판/옵저버는 Bank에 별도 상태를 기록하지 않는다.
- 런처 맵에서 Bank 쓰기 완료 전:
  - 어떤 플레이어도 “게임 시작” 상태로 넘어갈 수 없도록  
    단계/플래그 관리.

---

### 10.8 에러 및 안전 장치

- **게임 시작 버튼 활성 조건**
  - Player 1,2 모두 Ready.
  - 맵/옵션 설정 유효.
  - 밴픽(필요 시) 완료 또는 진행 여부에 따라 적절한 Stage.
  - Bank 쓰기 성공 플래그 확인(섬멸전 로딩 직전).
- **심판이 있다가 도중에 나간 경우**
  - 나가는 즉시:
    - HostPlayer를 새로 지정하고,
    - Host에게 “게임 시작” 버튼 권한이 넘어가도록 한다.
- **동기화 오류(예: 한 클라이언트에서 맵/옵션 값 불일치)**
  - 실질적 권위는 서버/Host의 전역 변수에 두고,
  - 클라이언트 UI는 이 값을 주기적으로 재반영.
  - 심판/Host 화면의 값이 기준이 되도록 한다.

---

### 10.9 요약

- 1v1 + 옵저버 + 심판 구조에서:
  - **심판이 없을 때**:  
    - 두 플레이어 Ready → Host가 “시작” → 진행.
  - **심판이 있을 때**:  
    - 두 플레이어 Ready → 심판만 “시작” 가능 → 진행.
- 튕김/연결 끊김에 대한 **기본 안전 규칙**은:
  - “튕긴 플레이어가 패배”로 처리한다.
  - 심판이 존재할 경우, 이 결과를 외부적으로 수정/재경기하는 권한은  
    심판/운영 측에 있지만, 인게임 시스템은 이 규칙을 기준으로 동작한다.

---

## 11. 에러/예외 시나리오

> 이 장에서는 런처/밴픽/섬멸전 단계에서 발생할 수 있는  
> 주요 오류 및 예외 상황과, 그에 대한 처리 방식을 정의한다.

---

### 11.1 외부 런처 단계 오류

#### 11.1.1 SC2 설치 경로 없음 / 잘못됨

- **상황**
  - 런처가 SC2 실행 파일을 찾지 못함.
- **처리**
  - 팝업: “StarCraft II 설치 경로를 찾을 수 없습니다.”
  - 경로 선택 UI 제공 → 사용자가 직접 `StarCraft II.exe`를 지정.
  - 지정 경로를 설정 파일에 저장, 이후부터 자동 사용.
- **재시도**
  - 경로 설정 완료 후 다시 “Play” 버튼 활성화.

#### 11.1.2 필수 모드/맵 파일 누락

- **상황**
  - `BP_Combined.SC2Mod` 또는 `BanpickLauncher.SC2Map`/`BP_Melee_*.SC2Map` 누락.
- **처리**
  - 누락 파일 리스트 표시:
    - 예: “필수 파일 누락: BP_Combined.SC2Mod, BP_Melee_Arena.SC2Map”
  - (선택) “다운로드/설치” 버튼 제공,
    - 또는 다운로드 페이지/가이드 링크 표시.
- **게임 실행**
  - 필수 파일이 모두 준비되기 전에는 “Play” 비활성화.

---

### 11.2 런처 맵 단계 오류

#### 11.2.1 플레이어 부족

- **상황**
  - Player 1 또는 Player 2가 없는 상태에서 시작을 시도.
- **처리**
  - “플레이어가 2명 필요합니다” 메시지 표시.
  - `ReadyA`/`ReadyB` 조건을 만족하지 않으면  
    “밴픽 시작/게임 시작” 버튼은 항상 비활성화.

#### 11.2.2 Ready 상태 불일치

- **상황**
  - 한쪽만 Ready, 또는 Ready 상태가 UI와 내부 상태가 일시적으로 어긋남.
- **처리**
  - Ready 변경 시마다 중앙 전역 상태를 기준으로 UI 재동기화.
  - “시작” 버튼 활성 조건:
    - `ReadyA == true` AND `ReadyB == true`  
    (심판/Host 유무와 무관하게 동일).

#### 11.2.3 심판/Host 부재/퇴장

- **상황**
  - 심판/HostPlayer가 방을 나감.
- **처리**
  - HostPlayer 재선정:
    - 우선순위: 심판(Player 3) → Player 1 → Player 2.
  - UI에 새 Host/Referee 표시 갱신.
  - 시작 버튼 권한을 새 Host/Referee에게 이전.

---

### 11.3 맵/옵션 설정 단계 오류

#### 11.3.1 맵 풀 비어 있음

- **상황**
  - 맵 풀 데이터가 로드되지 않았거나, 맵 엔트리가 0개.
- **처리**
  - CenterPanel에 메시지:
    - “맵 풀에 사용할 수 있는 맵이 없습니다.”
  - “밴픽 시작/게임 시작” 버튼 비활성화.
  - 개발/테스트 중에는 로그를 출력해 원인 파악.

#### 11.3.2 잘못된 MapId / FileName

- **상황**
  - `MapPool`에서 선택된 MapId에 대응하는 FileName이 비어 있음.
- **처리**
  - 기본 섬멸전 맵(예: `BP_Melee_Arena`)을 Fallback으로 지정하거나,
  - “맵 정보를 읽는 데 실패했습니다” 메시지 후 선택을 요구.
- **Next Map**
  - 런처에서 Next Map 설정 시 유효한 FileName이 없으면  
    섬멸전 로딩 시도를 하지 않도록 한다.

---

### 11.4 밴픽 단계 오류

#### 11.4.1 유닛 풀/메타 데이터 누락

- **상황**
  - `UnitMeta`에 없는 유닛이 밴 대상 후보에 들어간 경우.
- **처리**
  - 해당 유닛을 후보 리스트에서 제외.
  - 개발 중에는 로그에  
    “UnitMeta 누락: <UnitId>” 기록.
- **사용자 영향**
  - 유닛이 그리드에 안 보이는 것뿐, 밴픽 진행 자체는 가능해야 함.

#### 11.4.2 잘못된 밴 입력

- **상황**
  - 네트워크 지연/동기화 이슈로  
    이미 밴된 유닛을 다시 선택하려 할 때.
- **처리**
  - 서버/Host 기준:
    - Ban 후보 리스트에서 해당 유닛을 사전에 비활성화.
  - 클릭 시:
    - 아무 반응이 없게 처리,
    - 툴팁으로 “이미 밴된 유닛입니다” 정도 안내(선택).

#### 11.4.3 타이머 시간 초과

- **상황**
  - 제한 시간 내에 밴 확정이 이루어지지 않음.
- **처리**
  - 해당 라운드를 “미선택”으로 넘기고 다음 라운드로 진행.
  - 미선택 라운드는 Ban 리스트에 아무 것도 추가하지 않음.
- **예외**
  - 규칙상 반드시 N개의 밴이 필요하면:
    - 무작위 밴 자동 선택(설정 옵션)도 고려할 수 있음.

---

### 11.5 Bank/데이터 교환 단계 오류

#### 11.5.1 Bank 파일 열기 실패

- **상황**
  - `BanpickConfig` Bank를 열 수 없음(저장 권한/경로 문제).
- **런처 맵에서**
  - 밴픽 완료 후 Bank 저장 실패:
    - “설정 저장에 실패했습니다. 다시 시도해 주세요.” 메시지.
    - Bank 저장 재시도 후에만 “게임 시작” 버튼 활성화.
- **섬멸전 맵에서**
  - Bank 읽기 실패:
    - 기본 섬멸전 상태(밴 없음, 기본 유닛 풀)로 시작하거나,
    - 개발/테스트 중에는 게임 시작을 중단하고 경고 표시.

#### 11.5.2 ConfigVersion 불일치

- **상황**
  - 섬멸전 맵에서 읽은 `ConfigVersion`이 기대 값과 다른 경우.
- **처리**
  - 구버전 Bank → 호환용 로직이 없다면:
    - “구버전 설정 파일입니다. 새 게임을 생성해 주세요.” 로그/메시지.
    - 밴/옵션 적용은 스킵하고 기본 상태로 시작.
- **운영 시나리오**
  - 실제 배포에서는 **외부 런처가 모드/맵 버전을 관리**해  
    이 상황이 발생하지 않도록 하는 것이 원칙.

#### 11.5.3 잘못된 유닛 ID/리스트 파싱 실패

- **상황**
  - Ban 리스트 문자열 파싱 중 알 수 없는 유닛 ID 발견.
- **처리**
  - 해당 항목만 무시, 나머지는 정상 적용.
  - 개발 모드에서 로그:
    - “Unknown banned unit ID: <UnitId>”.

---

### 11.6 섬멸전 인게임 단계 오류

#### 11.6.1 Ban 적용 실패

- **상황**
  - 어떤 유닛에 대한 Ban 업그레이드를 찾지 못해서  
    생산/버튼 잠금이 일부 적용되지 않음.
- **처리**
  - 최소한:
    - Ban HUD에는 표시되지만, 생산이 가능한 상태가 될 수 있음.
  - 개발 모드:
    - 로그에 “Ban upgrade not found for unit <UnitId>”.
- **장기**
  - `UnitMeta` ↔ `UpgradeMeta` ↔ `ButtonMeta` 연결을 검증하는  
    개발용 체크 툴을 만드는 것도 고려.

#### 11.6.2 관전자 HUD 오류

- **상황**
  - 옵저버 HUD 생성 시 PlayerA/B 매핑에 실패하거나,
  - Ban 데이터가 비어 있어서 HUD가 표시할 것이 없음.
- **처리**
  - HUD를 숨기고 GameHeart 기본 UI만 유지.
  - 개발 로그: “Observer HUD disabled due to missing data”.

---

### 11.7 튕김/연결 끊김 시나리오

#### 11.7.1 런처/밴픽 중 튕김

- **플레이어 튕김**
  - 즉시 Ready 해제 + 슬롯 비어 있음.
  - 심판이 있는 경우:
    - 대기실로 강제 복귀 or 세션 종료 버튼 제공.
  - 시스템 기본 규칙:
    - 아직 인게임 전이므로 즉시 승패를 판단하지 않고,
    - 심판 또는 플레이어 간 합의에 따라 세션을 종료/재시작.

#### 11.7.2 인게임(섬멸전) 중 튕김

- **기본 규칙**
  - SC2 기본 동작대로:
    - 남은 플레이어 승리, 튕긴 플레이어 패배.
  - 이는 안전 규칙:
    - 심판이 개입하지 않아도 최소한의 결과가 자동으로 정해진다.
- **심판 개입**
  - 심판이 있을 경우:
    - 외부적으로 “재경기”를 명령하거나,
    - 해당 경기 결과를 무효로 처리할 수 있음.
  - 인게임 시스템은 기본 규칙(튕긴 쪽 패배)을 기준으로만 동작.

---

### 11.8 UI/피드백 정책

- 치명적 에러(게임 진행 불가):
  - 명시적인 팝업/메시지와 함께,
  - 가능한 한 “어떻게 복구할 수 있는지” 가이드를 짧게 제공.
- 경미한 에러(일부 데이터 누락/밴 적용 누락 등):
  - 사용자에게는 표시 최소화,
  - 개발/테스트 환경에서만 디버그 로그 활성화.

---

### 11.9 개발/테스트용 진단 도구 (선택 사항)

- **Debug HUD**
  - 런처/인게임에서 현재:
    - MapId, UnitPoolMode, BanCount, Ban 리스트, ConfigVersion 등을  
      실시간으로 확인할 수 있는 개발자용 HUD.
- **Consistency Check 트리거**
  - 맵 로드 시:
    - `UnitMeta` ↔ `UpgradeMeta` ↔ `ButtonMeta` 일관성 검사.
  - 오류를 발견하면 내부 로그/디버그 메시지만 출력.

---

## 12. 개발 단계 및 우선순위

> 이 장에서는 전체 시스템을 **실제 작업 순서**로 쪼개고,  
> 각 단계에서의 목표/산출물/의존 관계를 정리한다.  
> “최소 기능 버전(MVP) → 확장” 흐름을 기준으로 한다.

---

### 12.1 전체 로드맵 개요

1. **1단계 – 통합 모드 최소 골격 구축**
2. **2단계 – 런처 맵 기본(대기실 + 맵 선택 + Ready/심판)**
3. **3단계 – 밴픽 UI/로직 (런처 맵)**
4. **4단계 – Bank 저장/섬멸전 맵 로딩/초기화 연동**
5. **5단계 – 밴 적용(유닛 풀/업그레이드/Requirement) + 인게임 HUD**
6. **6단계 – 편의 기능(Convenience) 흡수/정리**
7. **7단계 – 유닛/맵 메타 데이터(DB) 정리 및 연동**
8. **8단계 – 옵저버/GameHeart 스타일 확장 + 심판 UI**
9. **9단계 – 맵 풀 확장(실제 래더 맵 적용)**
10. **10단계 – 외부 런처(클라이언트) 구현**
11. **11단계 – 테스트/튜닝/배포 준비**

각 단계는 가능한 한 “단독으로도 동작하는 작은 완성 상태”를 목표로 한다.

---

### 12.2 1단계 – 통합 모드 최소 골격 구축

**목표**  
`BP_Combined.SC2Mod`의 기본 구조와 공통 초기화/유틸을 만든다.  
아직 밴픽/편의 기능은 간단한 더미만 두고, 프로젝트의 **코어 틀**을 잡는 단계.

**작업 항목**

1. **새 모드 생성**
   - `BP_Combined.SC2Mod` 생성.
   - 의존성:
     - Liberty/Swarm/Void 기본 라이브러리,
     - 필요 최소 캠페인/멀티 모드 (LotV Multiplayer 등).

2. **트리거 폴더 scaffold**
   - `Triggers/Init/Init_Global`
   - `Triggers/Util/Util_Bank`, `Util_Strings`, `Util_Player`, `Util_Color`.
   - 모든 맵에서 공통으로 사용할 전역 변수 구조만 선언:
     - `CfgUnitPoolMode`, `CfgBanCount`,
     - PlayerA/B ID/역할,
     - `LauncherStage` 등.

3. **데이터 폴더 scaffold**
   - `Data/Units/BP_Units_Core`
   - `Data/Upgrades/BP_Upgrades_BanLocks` (아직 내용은 비워두거나 예제 1~2개)
   - `Data/UI/BP_UI_Core`
   - `Data/DB/BP_DB_Core` (UnitMeta/MapPool의 빈 틀만)

4. **간단 테스트**
   - 빈 테스트 맵 하나에 `BP_Combined` 의존성만 붙여서:
     - 게임이 정상 실행되는지,
     - 기본 UI 숨기지 않은 상태에서 Init 트리거가 문제 없이 도는지 확인.

**산출물**

- 기본 구조가 잡힌 `BP_Combined.SC2Mod`.
- 이후 모든 기능이 이 안으로 들어갈 “컨테이너” 확보.

---

### 12.3 2단계 – 런처 맵 기본 (대기실 + 맵 선택 + Ready/심판)

**목표**  
`BanpickLauncher.SC2Map`에서 **대기실/맵 선택/Ready/심판 로직**까지 만든다.  
밴픽/Bank 연동은 아직 넣지 않는다.

**작업 항목**

1. **런처 맵 생성**
   - `BanpickLauncher.SC2Map` 만들고,  
     의존성에 `BP_Combined` 추가.

2. **기본 UI 숨김 + RootDialog**
   - `Lobby_Init` 트리거에서:
     - 기본 UI 숨김,
     - `RootDialog`와 `TopPanel`, `LeftPanel`, `RightPanel`,
       `CenterPanel`, `BottomPanel` 생성.
   - 상단에 제목/버전 정도만 표시.

3. **플레이어/심판/옵저버 역할 판정**
   - Player 1,2 → PlayerA/B.
   - Player 3 → Referee (접속 여부로 판단).
   - Player 4~N → Observer.
   - 상단에 “Player A/B/Referee/Observer” 라벨 표시.

4. **대기실 UI & Ready 로직**
   - LeftPanel에 Player 1,2 슬롯:
     - 이름, 색상, Ready 버튼.
   - RightPanel에 Observer 목록.
   - Ready 버튼 트리거:
     - 본인만 클릭 가능,
     - Ready 상태 변경 시 전체 UI 갱신.

5. **맵 선택 UI (기본 1~2개 더미)**
   - CenterPanel에 간단한 맵 선택:
     - “TestMap_A”, “TestMap_B” 같은 더미 엔트리.
   - Player 1(Host) 또는 Referee만 변경 가능.

6. **심판/Host에 따른 “시작” 버튼 로직**
   - 심판이 없으면:
     - HostPlayer(보통 Player 1)만 “밴픽 시작” 버튼 활성.
   - 심판이 있으면:
     - Player 3만 버튼 활성.
   - 두 플레이어 Ready가 아니면 버튼 비활성.

7. **검증**
   - Battle.net/로컬에서 여러 계정/테스트로 접속:
     - Ready/맵 선택/시작 버튼 권한이 의도대로 동작하는지 확인.

**산출물**

- 대기실 + 맵 선택 + Ready/심판 로직이 동작하는 런처 맵.
- 아직 밴픽/Bank/섬멸전 연동은 없음.

---

### 12.4 3단계 – 밴픽 UI/로직 (런처 맵)

**목표**  
앞에서 설계한 LoL 스타일 **풀스크린 밴픽 UI + 라운드 로직**을 런처 맵에 구현한다.  
아직 섬멸전 인게임 연결은 안 해도 된다.

**작업 항목**

1. **밴픽 Stage 전환**
   - “밴픽 시작” 버튼 클릭 시:
     - `LauncherStage = 2`,
     - 대기실 UI 숨기고 밴픽 UI 표시.

2. **밴픽 UI 레이아웃 구현**
   - `BP_LauncherUI` 트리거에서:
     - 상단: 제목, 타이머, 라운드, 현재 차례/밴 타입.
     - 좌측: 내 밴 리스트(세로).
     - 우측: 상대 밴 리스트.
     - 중앙: 유닛 그리드(테스트용 10~20개 더미 유닛).
     - 하단: 선택 미리보기 + 확인 버튼 + 안내 문구.

3. **라운드 시퀀스/타이머**
   - `BP_Rounds`에서:
     - `CfgBanCount` 기반 라운드 배열 생성.
     - 각 라운드 Owner/Target/Visibility 정의.
   - 타이머:
     - 라운드 시작 시 10초 카운트다운.
     - 0초 도달 또는 Confirm 시 라운드 종료.

4. **유닛 후보 리스트(임시/더미)**
   - 초기에는 SC2 기본 유닛 몇 개를 하드코딩으로 넣고,  
     그리드/선택/확인만 잘 돌아가는지 확인한다.

5. **밴 데이터 구조**
   - 전역 구조:
     - `PublicBansAgainst[player]`, `ShadowBansAgainst[player]`, `SelfBans[player]`.
   - 매 라운드에서 선택된 유닛을 구조에 반영,
     좌/우 리스트 UI 갱신.

6. **테스트**
   - 2인 플레이로:
     - 차례, 타이머, 밴 리스트, UI 업데이트가 정상인지 확인.
   - 옵저버로:
     - 모든 진행이 잘 보이지만 클릭이 막혀 있는지 확인.

**산출물**

- 런처 맵에서 **밴픽 화면과 로직만 완전히 작동하는 상태**.
- 인게임 맵/Bank는 아직 필요하지 않지만,  
  밴 구조체는 완성된다.

---

### 12.5 4단계 – Bank 저장/섬멸전 맵 로딩/초기화 연동

**목표**  
밴픽 결과와 설정을 Bank로 저장하고,  
선택된 섬멸전 맵을 로드해 인게임 초기화까지 이어지는  
**Mass Recall식 플로우**를 완성한다.

**작업 항목**

1. **Bank 쓰기 구현 (런처 맵)**
   - `Lobby_BankSave` 또는 `BP_BankSave` 트리거:
     - `[Config]` 섹션:
       - MapId/FileName/UnitPoolMode/BanCount/RandomSeed/ConfigVersion.
     - `[PlayerA]`, `[PlayerB]` 섹션:
       - Id/Name/Race,
       - `PublicBans`/`ShadowBans`/`SelfBans` 문자열.
   - 밴픽 끝 + “게임 시작” 직전에 Save.

2. **섬멸전 맵 준비**
   - `BP_Melee_Arena.SC2Map` 같은 기본 맵 하나 선정.
   - 의존성에 `BP_Combined` 추가.
   - `Map Initialization`에서:
     - `Init_FromBank` 트리거 작성:
       - `BankOpen("BanpickConfig", Player 1)`,
       - Config/PlayerA/B 정보 읽기,
       - 간단히 Debug 메시지 출력.

3. **Next Map 로딩**
   - 런처 맵에서:
     - Bank 저장 후 `Game - Set Next Map(SelectedMapFileName)`.
     - `Game - End Game` 호출.
   - 섬멸전 맵이 실제로 로드되는지 확인.

4. **초기화 동작 확인**
   - 섬멸전 맵에서:
     - Bank 내용이 제대로 읽히는지,
     - ConfigVersion/MapId/FileName 검증 로직이 정상인지 확인.

**산출물**

- 런처 맵 → Bank 저장 → 섬멸전 맵 로드 → Bank 로드까지의  
  **엔드투엔드 흐름이 완성**된 상태.
- 아직 Ban 적용/편의 기능/밸런스는 최소화 또는 더미.

---

### 12.6 5단계 – 밴 적용 + 인게임 HUD

**목표**  
섬멸전 인게임에서 실제로 **밴된 유닛을 생산/업그레이드/용병 등에서 막고**,  
이 정보를 상단 HUD로 표시한다.

**작업 항목**

1. **Ban 업그레이드/Requirement 세트 정의**
   - `BP_Upgrades_BanLocks`:
     - 예: `BP_Ban_Marine`, `BP_Ban_ReaverBW` 등.
   - Requirement:
     - “해당 Ban 업그레이드가 없는 경우에만 사용 가능” 조건.
   - 관련 버튼/업그레이드/패널에 Requirement 연결.

2. **FinalDisabledUnits 계산 및 적용 트리거**
   - `BP_IngameApply`:
     - Bank에서 읽은 Ban 리스트 → `FinalDisabledUnits[player]` 계산.
     - 각 유닛 ID에 대응하는 Ban 업그레이드 레벨 설정.
   - 인게임 시작 직후 호출.

3. **인게임 HUD (ME/ENEMY)**
   - `BP_HUD`:
     - 상단 중앙에 ME/ENEMY HUD 생성.
     - Ban 리스트에 따라 아이콘/툴팁 표시.
     - 토글 키(예: Alt+B) 처리.

4. **테스트**
   - Ban 결과에 따라 실제로 생산/업그레이드/용병이 막히는지 확인.
   - HUD에 올바른 밴 정보가 표시되는지 확인.

**산출물**

- “밴픽한 내용이 실제 게임 룰에 반영되는”  
  기본 기능이 완성된다.
- 이 시점에서 이미 **MVP(최소 기능 섬멸전 밴픽 모드)**로 플레이 가능.

---

### 12.7 6단계 – 편의 기능(Convenience) 흡수/정리

**목표**  
기존 `ConvenienceBase` 모드에서 사용하던  
편의 기능을 `BP_Combined` 내부로 이식하고 충돌을 정리한다.

**작업 항목**

1. **ConvenienceBase 분석**
   - 어떤 트리거/데이터가 실제 플레이에 필요한지 선별:
     - 자원 처리,
     - 카메라/UI 편의,
     - 기타 QoL.

2. **포팅**
   - 필요한 트리거/데이터를
     - `Triggers/Convenience/*`,
     - `Data/Units/`, `Data/UI/` 등으로 복사/수정.
   - ID/네이밍에 `BP_` prefix를 붙여 충돌 방지.

3. **의존성 제거**
   - 모든 맵에서 외부 `ConvenienceBase` 의존성 제거,
   - `BP_Combined`만 사용하도록 변경.

4. **회귀 테스트**
   - 기존에 사용하던 편의 기능이 정상 동작하는지,
   - 밴픽/밸런스와 충돌이 없는지 확인.

**산출물**

- 외부 편의 모드에 의존하지 않는  
  “자가 포함형” 통합 모드.

---

### 12.8 7단계 – 유닛/맵 메타 데이터(DB) 정리 및 연동

**목표**  
UnitMeta/UpgradeMeta/ButtonMeta/MapPool DB를 실제로 채우고,  
트리거/밴픽/밸런싱/맵 선택이 이 DB를 참조하도록 한다.

**작업 항목**

1. **UnitMeta 채우기**
   - 멀티/캠페인/스타1 유닛 목록 정리.
   - 각 유닛에 대해:
     - UnitId, NameKey, IconPath, Race, TechTier, Role, IsWorker, IsBanEligible, DefaultPool_A/B 등 입력.

2. **UpgradeMeta/ButtonMeta 채우기**
   - 각 유닛의 생산/업그레이드/패널/용병 버튼과 연결되는 ID 매핑.
   - BanLock, Requirement와의 연결 정보 추가.

3. **MapPool 정리**
   - 프로젝트에서 사용할 섬멸전 맵 리스트 작성.
   - MapId/FileName/DisplayNameKey/MapSize/IsOfficialLadder 등을 DB에 등록.

4. **트리거 연동**
   - `BP_UnitPool`이 UnitMeta에서 후보 리스트를 읽도록 수정.
   - 런처 맵의 맵 선택 UI가 MapPool을 직접 조회하도록 변경.
   - Ban/Balance 로직이 DB 기반으로 동작하는지 확인.

**산출물**

- DB 기반의 유연한 밴픽/맵 선택 시스템.
- 향후 유닛/맵 추가 시 DB만 수정하면 코드 수정 없이 반영 가능.

---

### 12.9 8단계 – 옵저버/GameHeart 스타일 확장 + 심판 UI

**목표**  
관전자 경험을 GameHeart 스타일로 개선하고,  
양측 밴픽 정보를 상단 HUD에 통합 표시한다.  
또한, 심판 전용 UI/권한을 정리한다.

**작업 항목**

1. **GameHeart 레이아웃 참고/포트**
   - 기존 GameHeart UI 연구:
     - 상단 플레이어 정보/자원/생산/미니맵 구성.
   - 필요한 부분만 흉내내거나 참고하여  
     `BP_UI_Observer` 레이아웃 정의.

2. **관전자 HUD + 밴픽 정보 결합**
   - 상단 양쪽에 밴 바 추가:
     - Player 1 밴 리스트(왼쪽), Player 2 밴 리스트(오른쪽).
   - 옵저버 모드:
     - 기본적으로 모든 밴(공개+쉐도우)을 실제 유닛으로 표시.

3. **심판 UI**
   - 런처 맵에서:
     - Referee 전용 “게임 시작” 버튼.
     - (선택) “리매치”/“재설정” 버튼.
   - 인게임에서:
     - 추가 기능은 후순위로 두고,  
       우선 런처 쪽 심판 기능만 완성.

4. **테스트**
   - 여러 옵저버/심판이 들어왔을 때 HUD/권한이 의도대로 동작하는지 확인.

**산출물**

- 방송/관전자 친화적인 UI와 심판 운영 흐름.

---

### 12.10 9단계 – 맵 풀 확장 (실제 래더 맵 적용)

**목표**  
실제 공허의 유산 래더 맵들을 MapPool에 넣고,  
선택/로딩/검증까지 안정적으로 동작시키는 단계.

**작업 항목**

1. **맵 파일 준비**
   - 원하는 래더 맵들을 `.SC2Map` 형태로 확보.
   - `Maps\Banpick\` 폴더 등에 배치.

2. **MapPool 업데이트**
   - 각 래더 맵에 대한 MapId/FileName/DisplayNameKey/MapSize 등을 DB에 등록.

3. **런처 맵 UI 개선**
   - 맵 리스트/그리드 UI를 래더 맵 수에 맞게 개선.
   - 미니맵/간단 설명 표시.

4. **섬멸전 맵 검증**
   - 각 래더 맵이 `BP_Combined` 의존성으로 잘 실행되는지 테스트.
   - 시작 위치/자원/관전 카메라 등 확인.

---

### 12.11 10단계 – 외부 런처(클라이언트) 구현

**목표**  
SC2 실행과 파일 설치/검증을 담당하는  
간단한 외부 런처(EXE)를 완성한다.

**작업 항목**

1. **실행/경로 관리**
   - SC2 설치 경로 설정/저장.
   - “Play” 버튼 → SC2 + 런처 맵 실행.

2. **파일/버전 검사**
   - `BP_Combined`, 런처 맵, 섬멸전 맵 존재 확인.
   - 버전 문자열 비교.

3. **(선택) 자동 업데이트**
   - GitHub/서버에서 최신 파일 다운로드.
   - Mods/Maps 폴더에 덮어쓰기.

4. **UI**
   - 간단한 상태 표시(파일 확인 중/SC2 실행 중/오류 등).
   - 향후 계정/매치메이킹 패널을 추가할 수 있도록 확장성 고려.

---

### 12.12 11단계 – 테스트/튜닝/배포 준비

**목표**  
실전 환경에서 안정적으로 사용할 수 있도록  
디버깅/튜닝/문서화를 진행한다.

**작업 항목**

1. **기능 테스트**
   - 1v1 + 옵저버/심판 조합별로:
     - 런처 → 밴픽 → 섬멸전 → 결과까지 전체 흐름 테스트.
   - 다양한 맵/UnitPoolMode/BanCount 조합 테스트.

2. **밸런스/유닛 풀 튜닝**
   - 캠페인/스타1 유닛 포함 시 실제 밸런스 확인.
   - 필요에 따라 `BP_BalanceTweaks` 조정.

3. **에러/예외 시나리오 테스트**
   - Bank 삭제/손상, 튕김, 심판 유/무, 맵 누락 등.

4. **성능/UX 튜닝**
   - 밴픽 타이머, UI 반응성, HUD 가독성 최적화.
   - 불필요 애니메이션/효과 최소화.

5. **문서화**
   - 설치/실행 가이드.
   - 플레이어용 사용 설명서.
   - 심판/옵저버용 가이드.
   - 개발자용 구조/코드 개요.

---

### 12.13 우선순위 정리 (요약)

1. **필수 코어**
   - 1단계: 통합 모드 골격
   - 2단계: 런처 대기실/Ready/맵 선택
   - 3단계: 런처 밴픽 UI/로직
   - 4단계: Bank/NextMap/초기화 연동
   - 5단계: 인게임 Ban 적용 + HUD

2. **중요 확장**
   - 6단계: 편의 기능 흡수
   - 7단계: 유닛/맵 DB 정리 및 연동
   - 8단계: 옵저버/GameHeart + 심판 UI

3. **후순위/플랫폼**
   - 9단계: 실제 래더 맵 풀 확장
   - 10단계: 외부 런처 구현
   - 11단계: 대규모 테스트/튜닝/배포

---
