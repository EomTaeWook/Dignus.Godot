# Dignus.Godot

Godot 4.7.2 .NET용 Dignus 통합 라이브러리. `Dignus.Godot.slnx`를 사용한다.

- DI 및 바인딩의 기존 역할을 유지한다.
- `SingletonNode`는 SceneTree 루트에 생성되어 기본 씬 교체 후에도 유지된다. 메인 스레드에서 사용한다.
- 코루틴은 `_Process`의 delta로 갱신한다.
- 리소스는 `res://` 전체 경로와 확장자로 로드한다. `LoadScene<T>`는 ResourcePathAttribute에 따라 PackedScene을 로드하며 기본 파일명은 타입명.tscn이다.
- 풀은 PackedScene별로 루트 Node를 보관한다. Recycle은 트리에서 노드를 분리하며, 재사용 시 AddChild로 다시 붙인다. Godot의 _Ready는 재부착마다 호출되지 않는다.
- SceneBase는 _Ready에서 초기화 및 OnShow, _ExitTree에서 OnHide 및 OnDestroyScene을 호출한다. InjectedNodes는 코드에서 지정하는 Node 배열이다.
- 씬 로드는 전체 res:// 경로를 사용하며 순차 처리한다. Enum 오버로드는 ToString 결과를 그대로 경로로 사용한다. 추가 씬은 SceneTree 루트의 자식으로 붙이고 SceneFilePath로 찾아 제거한다.
- UnloadAsset은 관리자의 강한 참조만 제거한다. 다른 소비자가 보유한 Godot Resource를 강제로 해제하지 않는다.


빌드, 패키지 복원 및 Godot 실행은 수행하지 않았다. SDK 호환성과 씬 전환, 풀 재사용의 런타임 동작은 미검증이다.

공식 API 참고: https://docs.godotengine.org/en/stable/classes/class_resourceloader.html 및 https://docs.godotengine.org/en/stable/classes/class_scenetree.html
