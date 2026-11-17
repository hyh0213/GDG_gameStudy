# 하윤형 UI 작업 영역

- 담당자: 하윤형 / 지정 브랜치: `HYH`.
- 작업 전에 `git branch --show-current`와 `git status --short`를 확인한다.
  `HYH`가 아니면 수정하지 말고 사용자에게 알린다. 브랜치를 자동으로 전환하지 않는다.
- 런타임 스크립트는 `UI/Runtime/`, UnityEditor 의존 코드는 `UI/Editor/`에 둔다.
- 개인 씬은 `Assets/Scenes/하윤형/`, UI 에셋은 `Assets/UI/하윤형/`을 사용한다.
- 다른 팀원의 폴더, 공용 씬·Prefab·Material은 명시적 허락 없이 수정하지 않는다.
- `ProjectSettings/`, `Packages/`, `.gitignore`, `.gitattributes`, Render Pipeline,
  Input System, Tag/Layer, Physics, Build Settings 변경은 매번 사전 승인을 받는다.
- 기존 `UniversalRP.asset`의 메뉴용 3D 렌더러 참조 1개만 유지하도록 승인받았다.
  이것은 이후 공용 설정 변경이나 렌더러 자동 등록에 대한 포괄적 승인이 아니다.
- 씬·Prefab은 Unity 에디터/CLI로 편집하고, 에셋 이동·이름 변경은
  `AssetDatabase.MoveAsset` / `RenameAsset`을 사용해 `.meta`와 GUID를 보존한다.
- 기존 에셋·Prefab을 재생성하거나 덮어쓰지 않는다. 사용자 미커밋 변경을 폐기하지 않는다.
- 브랜치 생성/전환, Commit, Push, Merge, Rebase는 사용자가 GitHub Desktop에서 수행한다.
- 검증은 개인 StartMenu 씬에서 수행한다. 공용 설정을 몰래 바꾸어 테스트하지 않는다.
- 완료 시 구현, 생성/이동 파일, 수정한 기존 파일, 공용 변경, 컴파일/Play 검증,
  병합 주의사항과 사용자 후속 작업을 구분해 보고한다. 미검증 사항은 명시한다.

수정·연동·검증 방법: `Assets/UI/하윤형/README.md`.
