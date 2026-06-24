# BanPickMod (BPM)

> StarCraft II 밴픽 기반 1v1 섬멸전 모드  
> **전용 외부 런처 + BPM 전용 섬멸전 맵 내부 밴픽** 구조

---

## 프로젝트 구조

```
BanPickMod/
│
├─ docs/                      # 기획 및 설계 문서
│
├─ launcher/                  # BPM Launcher (Windows EXE)
│   ├─ src/
│   └─ ...
│
├─ sc2-mod/                   # BPM_Core.SC2Mod 소스 관리
│   ├─ Triggers/
│   │   ├─ Init/
│   │   ├─ BanPick/
│   │   ├─ Balance/
│   │   ├─ Lobby/
│   │   └─ Convenience/
│   ├─ Data/
│   │   ├─ DB/
│   │   ├─ Units/
│   │   ├─ Upgrades/
│   │   └─ UI/
│   └─ ...
│
├─ sc2-maps/                  # BPM 전용 섬멸전 맵 파일
│   ├─ BPM_1v1_TestMap.SC2Map
│   └─ ...
│
├─ release/                   # 배포 패키지 빌드 결과물
│   └─ BPM_Release.zip
│
├─ scripts/                   # 빌드/배포/검증 자동화 스크립트
│
├─ CONTRIBUTING.md            # 코드 컨벤션 및 기여 가이드 (AI 포함)
└─ README.md                  # 이 파일
```

## 핵심 구성 요소

| 구성 요소 | 경로 | 역할 |
|---|---|---|
| 외부 런처 | `launcher/` | 설치/업데이트/맵 실행 (게임 룰 판정 ✗) |
| 통합 확장 모드 | `sc2-mod/` | 밴픽 UI/로직, 밸런스, 편의 기능, DB |
| BPM 전용 섬멸전 맵 | `sc2-maps/` | 실제 섬멸전 경기 + 인게임 밴픽 |
| 배포 패키지 | `release/` | GitHub Release용 ZIP 패키지 |
| 자동화 스크립트 | `scripts/` | 빌드/패키징/검증 |

## 빠른 시작

1. `launcher/` 빌드 후 `BPM Launcher.exe` 실행
2. SC2 설치 경로 확인 → 필요 시 수동 설정
3. "설치" 버튼으로 `BPM_Core.SC2Mod` 및 `BPM_1v1_TestMap.SC2Map` 설치
4. "Play" 버튼으로 테스트 맵 직접 실행

자세한 내용은 [`docs/`](./docs/README.md) 참조.
