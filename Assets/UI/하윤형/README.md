# 암흑 미로 — UI 및 씬 구성

## 열기

1. GitHub Desktop에서 현재 브랜치가 **HYH**인지 확인한다. 다른 브랜치에서는 작업하지 않는다.
2. 시작 화면 확인: `Assets/Scenes/하윤형/StartMenu.unity`를 열고 **Game** 탭에서 ▶ Play를 누른다.
3. 인게임 HUD·장비·암전·일시정지·결과 화면 확인: `Assets/Scenes/하윤형/GameplayUITest.unity`를 열고 **Game** 탭에서 ▶ Play를 누른다.

개인 테스트 씬이므로 **Build Settings에는 자동 등록하지 않는다.** 기존 SampleScene과 2D 렌더러는 유지한다.

### GameplayUITest.unity 테스트 단축키

| 키 | 테스트 동작 |
| --- | --- |
| `1` | 체력 20 감소 + 피격 화면 플래시 + 체력 0 시 게임 오버(UI-05) |
| `2` | 체력 20 회복 + 회복 플래시 (UI-06 슬롯 활성 시 회복 아이템 차감) |
| `3` | 중앙 하단 상호작용 안내(`[E] 고대 장치 작동하기`) 켜기/끄기 |
| `4` | 탐사 장비(UI-04) 상태 순환(`준비됨` → `작동 중` → `재충전 중`) |
| `5` | 화면 암전(UI-04/DV-02) 3단계 순환 |
| `6` | UI-04 윤곽선 범례 및 UI-06 회복 아이템/추가 탐사 장비 슬롯 표시 전환 |
| `7` / `8` | 게임 오버 / 게임 클리어 화면(UI-05) 테스트 |
| `9` | 상단 목표(`탈출 단서 0~3 / 3`) 진행 |
| `Esc` | 일시 정지 메뉴(UI-02) 열기/닫기 (`환경 설정` 연동 포함) |

## 담당자별 파일 구조

```text
Assets/Scripts/하윤형/
  AGENTS.md
  UI/Runtime/             메뉴·인게임 HUD·장비/암전·결과 화면·씬 전환·커서 동작
  UI/Editor/              개인 씬/Prefab 생성 도구·점검 코드 (빌드에 포함되지 않음)
Assets/Scenes/하윤형/
  StartMenu.unity          시작 화면 개인 테스트 씬 (UI-01)
  GameplayUITest.unity     인게임 UI 통합 테스트 씬 (UI-02~UI-06)
Assets/UI/하윤형/
  Prefabs/
    StartMenu.prefab       시작 화면 전체 Prefab (UI-01)
    GameplayHUD.prefab     플레이 씬 드롭인용 인게임 HUD·일시정지·결과·전환 Prefab (UI-02~UI-06)
  Art/                    메시·텍스처·커서·메뉴용 렌더러·색감 프로파일
  Fonts/                  폰트와 원본 라이선스
  Materials/              개인 배경 재질
  README.md               수정·연동·검증 안내
```

이동은 Unity AssetDatabase로 수행하며 기존 `.meta`와 GUID를 유지한다.
`StartMenu.prefab`에는 Canvas, Controller, EventSystem, 배경 카메라와 조명을 함께 넣어 내부 참조를 유지한다.
반면 `GameplayHUD.prefab`은 플레이 팀의 씬에 바로 올릴 수 있도록 **카메라·AudioListener·EventSystem을 포함하지 않는다.**
(`EventSystem`이 없는 테스트 환경에서만 실행 시 `GameplayHudController`가 임시 EventSystem을 보충한다.)
다른 씬에 재사용할 때는 해당 씬 담당자의 허락을 받고, 씬의 안개·환경광 설정은 Prefab에 포함되지 않으므로 시작 화면 외형 확인은 `StartMenu.unity`를 기준으로 한다.

## UI 담당자가 수정할 곳

- **문구·배치·색상**: `Start Menu Canvas > Menu Layout` 아래의 `Text`, `Image`, `RectTransform`.
- **타이틀**: `Game title`. 고운바탕 폰트를 사용한다.
- **버튼**: `Start Button`, `Help Button`, `Settings Button`, `Quit Button`. 자식의 Label / English caption / Focus glow를 수정한다.
- **안내창 디자인**: `Start Menu Canvas > Dialog overlay > Dialog card`.
  편집할 때만 Dialog overlay를 켜서 확인한다. 실행 시에는 자동으로 닫힌다.
- **안내창 내용**: `Assets/Scripts/하윤형/UI/Runtime/MainMenuController.cs`의 `ShowHelp`, `ShowQuitConfirmation`.
- **임시 3D 배경**: `3D Menu Backdrop (cosmetic only)` 아래의 아치·문·조명.
  배경은 시작 화면 연출용이며 실제 게임의 미로/충돌/몬스터 구현이 아니다.
- **배경 색감**: `Art/MenuAtmosphere.asset`과 각 조명.
- **움직임**: `Main Menu Controller > MenuAtmosphere`. 컴포넌트를 끄면 카메라와 조명 움직임이 멈춘다.
- **설정창 디자인**: Prefab의 `Start Menu Canvas > Settings overlay > Settings card`.
  설정창을 편집할 때만 Settings overlay를 활성화한다. Play에서는 자동으로 닫힌다.

Canvas는 1920×1080 기준 `Scale With Screen Size / Expand`로 구성되어 다른 화면비에서도 버튼이 잘리지 않도록 한다.
마우스 호버와 키보드 포커스 모두 강조 표시를 지원한다.

## 플레이 씬 연결

현재 실제 플레이 씬이 없으므로 시작 버튼은 연결 안내창을 표시한다. 빈 SampleScene을 게임처럼 실행하지 않는다.

1. 팀과 통합 시점을 합의하고 **Build Settings 변경 승인을 받은 뒤**, 사용자가 **File > Build Profiles > Scene List**에 메뉴 및 플레이 씬을 추가한다.
2. `Main Menu Controller` 오브젝트를 선택한다.
3. `MainMenuController > Gameplay Scene Name`에 씬 이름(예: `Maze`) 또는 전체 경로를 입력한다.
4. 씬을 저장하고 Play에서 시작 버튼을 확인한다.

연결된 씬은 페이드아웃 후 비동기로 로드한다. 게임 플레이 코드에 대한 의존성은 없다.
조작 안내의 WASD / 마우스 / F / E는 **제안안**이므로 실제 플레이 담당자와 합의 후 변경한다.

## 환경 설정

| 항목 | 적용 범위 |
| --- | --- |
| 해상도 | 모니터가 제공하는 해상도와 현재 선택값. 데스크톱 빌드에서 적용 |
| 창 모드 | 창 모드 / 테두리 없는 전체 화면. 데스크톱 빌드에서 적용 |
| 전체 음량 | `AudioListener.volume`으로 실행 중 전체 소리 크기 조절 |
| 마우스 감도 | 0.2~3.0배 저장. **플레이 카메라에는 아직 미연결** |
| Y축 반전 | 설정값 저장. **플레이 카메라에는 아직 미연결** |
| 메뉴 배경 움직임 줄이기 | 메뉴 카메라·지도 움직임과 랜턴 깜빡임을 멈춤. 게임 플레이에는 영향 없음 |

- 슬라이더·체크박스 변경은 임시 선택이며 **적용하고 닫기**를 눌러야 저장된다.
- **취소 / × / Esc**는 변경을 버리고 메뉴로 돌아간다.
- **기본값**은 음량·조작·움직임의 임시 선택만 초기화한다. 해상도는 건드리지 않으며 적용 전에는 저장하지 않는다.
- 화면 변경 후 **15초 이내 유지하기**를 누르지 않으면 복구한다. 확인 중 Esc나 창 포커스 상실도 복구한다.
  이 복구 동작은 **실제 데스크톱 빌드에서 최종 검증해야 한다.**
- Editor에서는 Game 뷰/프로젝트 설정을 건드리지 않도록 해상도와 창 모드는 선택값만 저장한다.
- 설정은 사용자 PC의 `PlayerPrefs` 중 `DarkMaze.HYH.Settings.*` 키에 저장한다. Git에는 포함되지 않는다.
  `ProjectSettings/` 및 공용 Input System 에셋을 수정하지 않는다.
- 배경음/효과음 개별 음량, 화면 밝기, 키 재설정, FOV와 FPS 제한은 추후 플레이·사운드 담당자와 합의 후 추가한다.

### 플레이 담당자 연결점

설정 저장/읽기: `Assets/Scripts/하윤형/UI/Runtime/MenuPreferences.cs`.
설정창 동작: `Assets/Scripts/하윤형/UI/Runtime/MenuSettingsController.cs`.

```csharp
// 플레이 카메라에서 실제로 사용하는 감도와 수직 입력에 반영한다.
float sensitivityMultiplier = DarkMaze.UI.MenuPreferences.MouseSensitivity;
float verticalDirection = DarkMaze.UI.MenuPreferences.InvertY ? -1f : 1f;
```

현재 메뉴 작업은 다른 팀원의 카메라나 입력 코드를 수정하지 않는다.
키 재설정은 실제 Input Actions가 확정된 뒤 구현한다. `ShowHelp()`의 안내 문구는 별도이며 사용자 수정 내용을 유지한다.

## 마우스 포인터

- 기본 상태: 아이보리·골드 화살표, 선택 가능한 UI 위: 손 모양 선택 포인터.
- 버튼·슬라이더·체크박스는 Unity `Selectable`을 기준으로 자동 감지한다.
- 비활성 버튼, `CanvasGroup`이 막은 UI, 안내창 뒤의 메뉴와 장식용 텍스트에는 선택 포인터가 뜨지 않는다.
- 최상단 Raycast 결과만 검사하므로, 안내창 배경을 통과해 뒤쪽 버튼을 가리키지 않는다.
- 메뉴가 꺼지거나 다른 씬으로 넘어가면 시스템 기본 커서로 복구한다.
  커서 잠금·숨김은 건드리지 않으며, 게임 플레이 씬에 커서 컴포넌트를 자동 추가하지 않는다.

**이미지**: `Assets/UI/하윤형/Art/Cursors/MenuArrow.png`, `MenuSelect.png`.
32×32 투명 PNG를 직접 제작했다. `Cursor` 타입·Read/Write 활성·무압축·Mipmap 비활성 설정을 유지한다.

**연결 및 클릭 기준점**: 개인 Prefab의 `Main Menu Controller > MenuCursorController`.
`Arrow Hotspot`은 (4, 2), `Select Hotspot`은 (12, 3)이며, 좌상단 기준 클릭 지점이다.
화면 위에 그린 별도 Image가 아니라 `Cursor.SetCursor`를 사용하므로 실제 OS 포인터와 클릭 지점이 일치한다.
하드웨어 커서는 Game 뷰 스크린샷에 포함되지 않을 수 있다. 모양 전환은 실제 Game 뷰에서 마우스를 움직여 확인한다.

### 다른 클릭 기능에도 선택 포인터 표시하기

기본 Button/Slider/Toggle에는 추가 컴포넌트가 필요 없다.
직접 만든 클릭 가능한 **UI**에는 해당 담당자의 허락을 받은 뒤 다음과 같이 연결한다.

1. 해당 UI 오브젝트에 `MenuCursorTarget`을 추가한다.
2. UI의 Image 등 Graphic에서 `Raycast Target`이 켜져 있는지 확인한다.
3. 실제 클릭 기능은 기존 코드에서 처리한다. 이 컴포넌트가 클릭 동작을 만들어 주지는 않는다.
4. 기능을 사용할 수 없을 때는 `InteractionEnabled = false`로 설정하거나 컴포넌트를 끈다.

3D 월드 오브젝트·게임 플레이용 조준점은 이번 구현 범위에 포함하지 않는다.
메뉴 밖에서 재사용할 때는 다른 커서 관리자와 중복되지 않도록 통합 담당자가 연결한다.

## 메뉴 조작

| 입력 | 동작 |
| --- | --- |
| 마우스 | 버튼 선택 / 클릭 |
| 방향키 | 버튼 포커스 이동 |
| Tab / Shift+Tab | 다음 / 이전 버튼 |
| Enter | 선택 |
| Esc | 안내창 닫기 / 종료 확인 |

게임 종료는 확인창을 거친다. Editor에서는 Play 모드만 종료하며, 빌드에서는 앱을 종료한다.
배경음악이나 효과음은 넣지 않았으므로 사운드 담당자가 별도 AudioSource를 추가하면 된다.

## 인게임 HUD · 장비 · 결과 화면 연동 (UI-02 ~ UI-06)

플레이 팀은 `Assets/UI/하윤형/Prefabs/GameplayHUD.prefab`을 플레이 씬에 배치한 뒤 아래 C# API 또는 Inspector 이벤트로 UI 상태를 구동한다. (게임플레이 스크립트에 대한 의존성 없음)

### 1) HUD 및 일시정지 (`GameplayHudController`)
```csharp
var hud = FindAnyObjectByType<DarkMaze.UI.GameplayHudController>();
hud.SetHealth(currentHp, maxHp);                        // 체력 바·트레일·저체력 경고·피격/회복 플래시
hud.SetObjective("탈출 단서 1 / 3 — 장치를 해제하세요"); // 상단 목표 표시
hud.ShowInteractionPrompt("장치 작동하기", "E");         // 상호작용 프롬프트
hud.HideInteractionPrompt();
hud.Pause(); / hud.Resume(); / hud.TogglePause();        // Esc 자동 지원, timeScale·커서 잠금 복원
```

### 2) 암전 · 탐사 장비 · 선택 슬롯 (`DarknessScannerUI`, UI-04 / UI-06)
```csharp
var scanner = FindAnyObjectByType<DarkMaze.UI.DarknessScannerUI>();
scanner.SetDarknessLevel(0.75f);                         // 0~1 화면 암전 비네트(DV-02 연동)
scanner.SetScannerState(DarkMaze.UI.ScannerState.Active, remaining / duration, remaining);
scanner.TriggerScannerPulse();                           // 탐사 장비 발동 시 화면 테두리 펄스
scanner.SetLegendVisible(true);                          // 필요 시 윤곽선 색상 범례(지형·장치·위험) 표시
scanner.SetHealItem(count, visible: true);               // UI-06 회복 아이템 슬롯(기본 숨김)
scanner.SetSubGear("광역 탐지기", DarkMaze.UI.ScannerState.Ready, 1f, 0f, visible: true); // UI-06 추가 장비
```

### 3) 게임 오버 / 클리어 (`GameResultController`, UI-05)
```csharp
var result = FindAnyObjectByType<DarkMaze.UI.GameResultController>();
result.ShowGameOver("어둠 속 추적자에게 붙잡혔습니다.", elapsedSeconds, solved, total);
result.ShowGameClear(elapsedSeconds, remainingHp / maxHp, solved, total);
```
`다시 시도`는 `SceneTransitionManager.ReloadCurrentScene`을, `메인 화면으로`는 `SceneTransitionManager.LoadScene(mainMenuSceneName)`을 호출한다.
`StartMenu` 또는 플레이 씬이 Build Profiles에 미등록 상태이면 씬 이동 대신 카드 하단에 안내 문구를 띄운다.

### 4) 씬 구조 및 Git 브랜치 운영 규칙 (UI-03)
- **권장 씬 구성**:
  1. `StartMenu.unity` (시작 화면 — `StartMenu.prefab`)
  2. `MazeGameplay.unity` 등 실제 플레이 씬 (플레이 팀 제작 — `GameplayHUD.prefab` 드롭인)
  3. 통합 테스트 완료 후 Build Profiles에 `StartMenu` → 플레이 씬 순서로 사용자 승인 하에 등록한다.
- **Git 브랜치 분리 규칙**:
  - 공용 씬·`ProjectSettings/`·`UniversalRP.asset`은 통합 담당자 합의 없이 개인 브랜치에서 수정하지 않는다.
  - UI 작업은 개인 폴더(`Assets/Scripts/하윤형/`, `Assets/Scenes/하윤형/`, `Assets/UI/하윤형/`)와 `HYH` 브랜치에만 반영한다.
  - 다른 팀원 씬에 `GameplayHUD.prefab`을 연결할 때는 별도 통합 PR/머지 단계에서 진행한다.

## 협업 및 렌더링

- 작업 브랜치는 **HYH**, 담당자는 **하윤형**이다. 다른 팀원의 파일·공용 씬은 수정하지 않는다.
- 스크립트·씬·UI 에셋은 위 담당자 폴더에서만 수정한다. 기존 SampleScene은 수정하지 않는다.
- `Assets/Settings/UniversalRP.asset`의 **Menu3DRenderer** 참조 한 개는 사용자가 명시적으로 승인하여 유지했다.
  기본 2D 렌더러(index 0)는 그대로이며, 메뉴 카메라만 별도 3D 렌더러를 선택한다.
- 이전에 자동 추가했던 시작 씬의 Build Settings 등록은 제거했다. 다른 씬의 등록·순서·활성 상태는 유지한다.
- 이후 `ProjectSettings/`, `Packages/`, Render Pipeline, Input System, Build Settings 변경은 **매번 사전 승인**을 받는다.
- Git 브랜치 전환·Commit·Push·Merge는 사용자가 GitHub Desktop에서 수행한다. 에이전트는 조회만 한다.
- 에셋은 `.meta`와 함께 병합한다. 폴더 변경은 Unity 에디터에서 하고, 공용 YAML은 직접 수정하지 않는다.
- 메뉴 배경의 벽과 바닥은 재질별 메시로 합쳐 드로우 호출을 줄였다.
- 메뉴가 활성 씬인 상태에서 저장한다. 플레이 씬은 별도 파일/브랜치로 작업한다.
- 스크립트는 `DarkMaze.UI` 네임스페이스로 분리했다.

## 생성 도구 및 점검

`Assets/Scripts/하윤형/UI/Editor/StartMenuSceneBuilder.cs`는 최초 구성을 재현하는 도구다.
**이미 존재하는 StartMenu 씬은 덮어쓰지 않는다.** 일반적인 UI 수정에는 재실행할 필요가 없다.
기존 개인 리소스와 Prefab도 덮어쓰지 않으며, 다른 씬을 닫거나 저장하지 않는다.
공용 렌더러 등록과 Build Settings 자동 수정 코드는 제거했다. 승인된 기존 렌더러가 없으면 중단한다.
Prefab을 수정할 때는 `Assets/UI/하윤형/Prefabs/StartMenu.prefab`의 Prefab Mode를 사용한다.
현재 씬에서만 임시 수정한 경우에는 Inspector의 Overrides를 확인하고, 다른 Prefab에 일괄 적용하지 않는다.
`Editor/MenuSettingsBuilder.cs`는 기존 개인 Prefab에 설정창을 최초로 추가하는 도구다.
이미 설정창이 있으면 중단하며, 일반적인 문구·배치 수정 시 다시 실행하지 않는다.
`Editor/MenuCursorBuilder.cs`도 커서 최초 추가용이다. 기존 이미지·컴포넌트는 덮어쓰지 않는다.

개인 `StartMenu` 씬을 열고 CLI에서 Play 모드에 진입한 뒤 다음 검사 메서드를 실행할 수 있다.
검사 코드는 해당 개인 씬에서만 실행되며, 다른 씬의 동일 이름 오브젝트를 조작하지 않는다.

```powershell
unity command script eval --code 'return DarkMaze.Editor.StartMenuChecks.VerifyButtons();' --format json
unity command script eval --code 'return DarkMaze.Editor.StartMenuChecks.VerifyLayout();' --format json
unity command script eval --code 'return DarkMaze.Editor.StartMenuChecks.VerifySettings();' --format json
unity command script eval --code 'return DarkMaze.Editor.StartMenuChecks.VerifySettingsPersistence();' --format json
```

Prefab의 영구 연결 검사는 **Play 종료 후** 같은 개인 씬에서 실행한다.
Unity는 Play 중 편집용 Prefab 인스턴스 정보를 유지하지 않으므로 두 검사를 구분한다.

```powershell
unity command script eval --code 'return DarkMaze.Editor.StartMenuChecks.VerifySceneReferences();' --format json
```

버튼 리스너, 안내창, 미연결 씬 처리, 종료 취소, 포커스, 버튼을 가리는 그래픽,
누락된 직렬화 참조와 현재 해상도의 주요 UI 잘림을 검사한다. 검사에서 실제 종료는 누르지 않는다.
Prefab 연결, Missing Script, 손상된 오브젝트 참조와 메뉴 카메라의 렌더러 연결도 검사한다.
설정 검사는 열기·취소·임시 기본값·포커스·잘림·저장·재조회·음량 적용을 확인한다.
저장 검사는 자체 설정 키를 백업하고 `finally`에서 원래 존재 여부와 값, 실행 중 음량을 복구한다.
Editor 검사로 실제 OS 해상도 전환이나 15초 복구가 검증되었다고 간주하지 않는다.
설정창을 열고 Game 뷰에서 한 프레임 이상 렌더링한 뒤
`DarkMaze.Editor.StartMenuChecks.VerifyOpenSettingsRaycasts()`로 버튼 클릭 영역을 별도 검사한다.
열기와 같은 프레임에서 호출하면 아직 렌더링되지 않은 그래픽의 클릭 영역을 검사할 수 없다.

커서 검사는 `DarkMaze.Editor.MenuCursorChecks`를 사용한다.
Edit 모드의 `VerifyAssets()`로 텍스처와 Hotspot을 확인한다.
Play에서 화면 렌더링 후 `VerifyMainMenuHover()`와 `VerifyCustomTarget()`을 실행하고,
설정창을 열어 한 프레임 이상 렌더링한 뒤 `VerifyOpenSettingsHover()`로 슬라이더·체크박스를 확인한다.
조작 안내/종료 확인창도 열고 렌더링한 뒤 `VerifyOpenDialogHover()`로 버튼과 뒤쪽 메뉴 차단을 확인한다.
검사는 OS 마우스를 강제로 이동하지 않고 UI의 화면 좌표에서 실제 Raycast 결과를 검사한다.

초기 구현은 Unity 6000.4.5f1의 실제 Play 모드에서 1920×1080, 1366×768,
16:10(1124×703) 화면으로 점검했다. Tab/Enter/Esc 입력과 임시 SampleScene 연결을 통한
실제 비동기 씬 전환도 확인했다. 씬 전환 테스트의 임시 연결 값은 저장하지 않았다.

폴더 정리 및 Prefab 전환 후에는 컴파일, 1920×1080 배치, 버튼/안내창 동작,
편집 모드의 Prefab·렌더러 연결과 손상된 참조 검사를 다시 수행했다.
이번 자동 Play 검사에서는 에디터 프레임 진행이 멈춰 **연속 애니메이션은 재검증하지 못했다.**
공용 백그라운드 실행 설정은 변경하지 않았다. Unity 창을 활성화한 상태에서 Play로 확인한다.
Unity가 자동 생성한 `ProjectSettings/SceneTemplateSettings.json`은 임의 삭제하지 않았으므로,
Commit 전에 GitHub Desktop에서 공용 설정 담당자와 검토한다.

설정 메뉴 추가 후에는 1920×1080과 1366×768에서 배치·취소·기본값·저장/재조회·실제 음량 적용,
설정창의 버튼 클릭 영역을 재검사했다. 에디터 비활성 상태의 자동 재생 정지 때문에
디버그 단일 프레임 진행을 사용했으며, **일반 키보드 조작·연속 재생·실제 빌드의 화면 전환과 복구는 최종 수동 검증이 필요**하다.

커서 추가 후에는 텍스처·클릭 기준점·Prefab 참조 검사와 Play의 메인 버튼·설정 슬라이더/체크박스·
안내/종료 확인창 버튼·비활성 버튼·뒤쪽 메뉴 차단·사용자 정의 대상 검사를 통과했다.
두 종류의 `Cursor.SetCursor` 호출과 컴포넌트 비활성화 시 복구도 오류 없이 완료했다.
프레임 증가를 한 차례 확인했으나 이후 비활성 상태에서 다시 멈춰 일부 검사는 단일 프레임 진행을 사용했다.
**OS에 표시되는 실제 커서의 모양과 전환은 Unity Game 뷰 및 최종 빌드에서 수동 확인해야 한다.**

인게임 HUD(`GameplayHUD.prefab` / `GameplayUITest.unity`, UI-02~UI-06)는 `GameplayHudChecks`로 점검한다.

```powershell
# Play 모드 진입 후
unity command script eval --code 'return DarkMaze.Editor.GameplayHudChecks.VerifyHudStates();' --format json
unity command script eval --code 'return DarkMaze.Editor.GameplayHudChecks.VerifyHudLayout();' --format json
# Play 종료 후 (Edit 모드)
unity command script eval --code 'return DarkMaze.Editor.GameplayHudChecks.VerifyPrefabAndSceneReferences();' --format json
```

현재 Edit 모드 참조/자체 포함 검사(StartMenu 340 GameObjects / 1011 Components, GameplayUITest 232 GameObjects / 713 Components, 0 누락/손상), Play 모드 1920×1080 상태·배치·일시정지·설정·게임 오버(선행 암전 후 카드 페이드 인)·클리어(미로 출구 시점 배경 + 카드 배치)·커서 레이캐스트·시각 캡처 점검을 완료했다.
실제 플레이 씬과의 Build Profiles 통합 전환, 데스크톱 빌드에서 `SceneTransitionManager`의 실제 비동기 씬 왕복, Game 뷰 활성 상태의 연속 키보드 조작은 플레이 씬 통합 후 수동 확인이 필요하다.

## 미스터리 호러 텍스처 및 결과 연출 (`Art/UI/`)

- `DossierCard.png` (9-slice): 찢어진 탄 가장자리와 먹물 번짐이 들어간 짙은 수사 기록지(dossier) 카드 배경(소형 HUD·안내/설정 모달).
- `InkBrushWash.png`: 포커스된 버튼에 적용되는 붉은 먹물 붓 터치 하이라이트.
- `InkBloodSplatter.png`: 시작 타이틀, 조작 안내, 일시정지 화면에 배치한 먹물·핏자국 장식.
- `LeftInkVeil.png`: 일시정지 화면을 중앙 박스 없이 좌측 풀스크린 먹물 그라데이션 위에 배치하기 위한 배경 비네트.
- `GameOverArtwork.png`: 게임 오버 시 화면 전체를 채우는 칠흑 배경 + 좌상단 먹 번짐 + 우측·우하단 마른 핏자국 풀스크린 일러스트.
- `MazeExitClear.png`: 게임 클리어 시 좌측 38% 어두운 석조 미로 내벽(텍스트·하단 버튼 영역)과 우측 62% 열린 출구(하늘·안개 낀 나무숲·흙길)로 화면 전체를 채우는 1인칭 일러스트.
- 게임 오버 시 `GameResultController`가 먼저 화면 전체를 완전히 암전(`Result blackout`)시킨 뒤 풀스크린 게임 오버 아트워크와 문구·버튼을 서서히 드러낸다.
- 재생성/일괄 적용 도구: `Assets/Scripts/하윤형/UI/Editor/MysteryHorrorUiBuilder.cs` (`Tools/하윤형/UI/Apply Mystery Horror Ink & Exit Theme`).

## 폰트 라이선스

- Gothic A1 Regular — HanYang I&C / SIL Open Font License 1.1 (본문·서사 문구·위치 안내·결과 설명·HUD/설정 수치)
- Gowun Batang Regular — Yanghee Ryu / SIL Open Font License 1.1
- Song Myung Regular — Sungchul Lee, Hangeul Design Lab / SIL Open Font License 1.1 (메뉴 버튼·오버레이 제목)
- TillVictoryComes Regular — moldu (정성영) / 무료 배포(개인·상업용 사용 가능), **수정 및 재배포 금지** (`StartMenu.prefab`의 `Game title` 및 `GameplayHUD.prefab`의 `Result title`에 적용, 약 72.3MB 대용량 및 GitHub 공개 저장소 업로드 시 재배포 조항 주의 필요)

Google Fonts의 공개 저장소에서 가져온 폰트는 원본 라이선스를 `Fonts/*-OFL.txt`에 함께 포함했다.
