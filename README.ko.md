# Dignus.Godot

[English](README.md) | 한국어

Godot 4.7.2 .NET용 Dignus 통합 라이브러리다. 의존성 주입, 씬 수명 주기, 리소스 로드, 노드 풀, 코루틴, 값 변경 알림을 제공한다.

## 프로젝트 구성

- 솔루션: `Dignus.Godot.slnx`
- 라이브러리: `Dignus.Godot/Dignus.Godot.csproj`
- SDK: `Godot.NET.Sdk/4.7.2`
- 대상 프레임워크: `net8.0`
- 의존 패키지: `Dignus` 1.4.3

Godot .NET 에디터와 .NET SDK가 필요하다. 사용하는 Godot C# 프로젝트에 다음 패키지 참조를 추가한다. Dignus와 GodotSharp 의존성은 NuGet이 자동으로 해결한다.

```xml
<ItemGroup>
  <PackageReference Include="Dignus.Godot" Version="1.0.0" />
</ItemGroup>
```

## NuGet 패키징

패키지 ID는 `Dignus.Godot`, 버전은 `1.0.0`이다.

```powershell
dotnet pack Dignus.Godot/Dignus.Godot.csproj -c ExportRelease
```

패키지는 `publish/nuget/Dignus.Godot.1.0.0.nupkg`에 생성한다. ExportRelease 빌드는 라이브러리를 `publish/dll/net8.0/`에도 복사한다. 패키지에 아이콘, 한국어·영어 README와 MIT 라이선스를 포함한다. 의존성 선언은 패키지 참조에서 생성한다. NuGet 업로드는 별도로 수행한다.

솔루션 구성은 `Debug`, `ExportDebug`, `ExportRelease`를 제공한다.

## 의존성 주입

`Dignus.Godot.DependencyInjection`의 정적 컨테이너에 대상 어셈블리를 등록한다. 등록 대상과 수명은 Dignus의 등록 규칙을 따른다.

```csharp
DignusGodotServiceContainer.RegisterDependencies(typeof(LobbyController).Assembly);
var controller = DignusGodotServiceContainer.GetService<LobbyController>();
```

컨트롤러와 서비스는 Dignus 컨테이너에서 조회한다. 씬 노드는 `BindScene(this)`를 통해 연결한다.

## 씬과 컨트롤러

`Dignus.Godot.Framework.SceneBase`는 `Node` 기반이다.

| 진입점 | 호출 순서 |
| --- | --- |
| `_Ready()` | 씬 관리자 등록 → `OnAwakeScene()` → `OnShow()` |
| `_ExitTree()` | `OnHide()` → `OnDestroyScene()` |

`SceneBase<TController>`는 기본 `_Ready()`보다 먼저 컨테이너에서 컨트롤러를 조회하고 `BindScene(this)`를 호출한다.

```csharp
using Dignus.Godot.Framework;

public partial class LobbyScene : SceneBase
{
    protected override void OnAwakeScene()
    {
    }

    public override void OnDestroyScene()
    {
    }
}
```

컨트롤러는 `ISceneController`를 구현하거나 `SceneControllerBase<TScene>`, `SceneControllerBase<TScene, TModel>`을 상속한다. 모델형 기반 클래스는 `[Inject]`가 붙은 `Model` 프로퍼티를 제공한다. 씬 기반 클래스가 컨트롤러의 `Dispose()`를 자동 호출하지 않으므로 필요한 정리는 씬의 `OnDestroyScene()`에서 수행한다.

`OnShow`는 `_Ready`에, `OnHide`는 `_ExitTree`에 연결되어 있다. 가시성 변경 이벤트가 아니다. 노드가 트리에서 분리되어도 `OnDestroyScene`이 호출되므로, 실제 메모리 해제 때만 호출되는 메서드로 취급하지 않는다.

## 씬 전환

`DignusGodotSceneManager.Instance`를 통해 씬 경로를 전달한다.

```csharp
var sceneManager = DignusGodotSceneManager.Instance;
sceneManager.LoadScene("res://Scenes/Lobby.tscn");
sceneManager.LoadAdditiveScene("res://Scenes/Overlay.tscn");
sceneManager.UnloadAdditiveScene("res://Scenes/Overlay.tscn");
```

- 로드 요청은 하나의 큐에서 순차 처리한다. 리소스는 `ResourceLoader`의 스레드 로드 API로 가져온다.
- 기본 씬은 `ChangeSceneToPacked`로 교체한다. 추가 씬은 `SceneTree.Root`에 자식으로 붙인다.
- `OnSceneLoadStarted`, `OnSceneLoadingProgress`, `OnSceneLoadCompleted` 이벤트를 제공한다. 완료 이벤트 뒤에 요청의 완료 콜백을 호출한다.
- 완료 콜백에는 로드 과정에서 마지막으로 등록된 `SceneBase`가 전달된다. 등록된 씬이 없으면 null이다.
- `CurrentSceneName`, `PreviousSceneName`, `CurrentScene`은 관리자의 기본 씬 전환 과정에서 갱신한다. 시작 씬의 `_Ready` 등록만으로 이 프로퍼티들이 초기화되지는 않는다.
- 추가 씬 해제는 루트 자식의 `SceneFilePath`가 요청 경로와 같은 노드들을 분리한 뒤 `QueueFree`한다. 현재 기본 씬은 해제할 수 없다. 대상이 없으면 아무 작업도 하지 않는다.
- Enum 오버로드는 `ToString()` 결과를 그대로 사용한다. 씬 경로를 조합하는 기능은 없다.

## 리소스

`DignusGodotResourceManager`는 경로별로 로드한 `Resource`를 보관한다.

```csharp
var texture = DignusGodotResourceManager.Instance.LoadAsset<Godot.Texture2D>("res://Textures/Icon.png");
var packedScene = DignusGodotResourceManager.Instance.LoadAsset<Godot.PackedScene>("res://Scenes/Bullet.tscn");
```

`LoadScene<T>()`는 Node 타입의 `ResourcePathAttribute`를 읽어 `PackedScene`을 반환한다. 기본 파일명은 `타입명.tscn`이다. 파일명을 지정하면 해당 문자열을 그대로 사용하므로 확장자까지 적는다.

```csharp
using Dignus.Godot.Attributes;
using Godot;

[ResourcePath("res://Scenes", "Bullet.tscn")]
public partial class Bullet : Node2D
{
}
```

`LoadAsset<T>()`의 T는 Resource 타입이어야 하며, 같은 특성으로 경로를 구성한다. 이 오버로드의 기본 파일명은 타입명이며 확장자를 자동 추가하지 않는다. 특성이 없으면 예외가 발생한다. 경로를 받는 `LoadAsset<T>(string path)`는 로드 결과가 null이면 null을 반환하고 캐시에 추가하지 않는다.

`UnloadAsset(string path)`와 `UnloadAsset(Resource resource)`는 관리자 캐시의 참조만 제거한다. 다른 소비자나 엔진이 보유한 리소스를 강제 해제하지 않는다.

## 노드 풀

`DignusGodotObjectPool`은 `PackedScene`별 풀을 관리한다. `Pop<T>`의 T는 인스턴스의 루트 Node 타입이어야 한다. 타입이 다르면 노드를 풀에 반환하고 예외를 발생시킨다.

```csharp
using Dignus.Godot.Extensions;

var bullet = this.InstantiateWithPool<Bullet>("res://Scenes/Bullet.tscn");
if (bullet != null)
{
    bullet.Recycle();
}
```

`InstantiateWithPool<T>`는 특성 기반 경로, 명시적 경로, `PackedScene`을 받는 오버로드를 제공한다. 가져온 노드를 호출자의 자식으로 붙인다. `Recycle()`은 부모에서 분리하고 풀에 반환한다.

직접 `Pop`/`Push`를 호출하면 트리 부착·분리를 호출자가 처리한다. 이 풀에서 생성하지 않은 노드를 `Push`하면 예외가 발생한다.

- `DestroyPool(packedScene)`: 해당 풀의 보관 노드와 사용 중 노드를 제거한다. 풀 등록 자체는 유지한다.
- `Clear()`: 모든 풀의 보관 노드와 사용 중 노드를 제거한다.
- `ClearExceptActive()`: 보관 중인 노드만 제거한다.

제거는 `QueueFree`를 사용한다. 풀 재사용 시 상태를 자동 초기화하지 않는다. `_Ready`는 재부착마다 호출되지 않으며, `SceneBase`를 풀링하면 분리 때 `_ExitTree`의 정리 메서드가 실행된다는 점을 고려해야 한다.

## 싱글턴과 코루틴

`SingletonNode<T>.Instance`는 실행 중인 `SceneTree`의 루트에서 타입 이름의 노드를 찾고, 없으면 생성한다. 기본 씬의 자식이 아니므로 기본 씬 교체 대상에 포함되지 않는다. SceneTree가 없으면 예외가 발생한다. 메인 스레드에서 접근한다.

`Dignus.Godot.Coroutine.DignusGodotCoroutineManager`는 `IEnumerator`와 Dignus `CoroutineHandle`을 받는다. 지연 시간과 완료 콜백을 지정할 수 있으며, `_Process(double delta)`에서 Dignus 코루틴 처리기를 갱신한다.

```csharp
DignusGodotCoroutineManager.Start(enumerator);
DignusGodotCoroutineManager.Start(0.5f, enumerator, OnCompleted);
```

yield 값의 해석은 Dignus 코루틴 처리기를 따른다.

## 바인딩과 객체 유효성

`Dignus.Godot.Binding.BindableProperty<T>`는 `ValueChanged` 이벤트를 제공한다. 현재 값이 null이 아니고 새 값과 `Equals`가 같으면 알림을 생략한다. `NotifyChanged()`는 현재 값을 강제로 알린다. `IReadOnlyBindableProperty<T>`는 읽기 전용 Value와 이벤트를 노출한다. 구독 해제는 소비자가 처리한다.

`Dignus.Godot.Extensions.ObjectExtensions.IsNull()`은 `GodotObject.IsInstanceValid`의 반대 결과를 반환한다. 큐에 해제를 요청한 상태를 별도로 판별하는 메서드는 아니다.

## 연결된 사용 예제

다음 코드는 소비 Godot 프로젝트의 타입별 개별 파일 예제다. `Bootstrap`을 `LobbyScene`보다 상위에 있는 유지되는 노드에 붙이면 `_EnterTree()`에서 어셈블리를 등록한 뒤 씬의 `_Ready()`가 실행된다. 필요한 어셈블리는 시작 시 한 번씩 등록하고 씬 전환마다 다시 등록하지 않는다. 컨트롤러와 모델은 DI가 생성하고, 씬 Node는 씬 트리가 소유한다.

### 초기화·모델·컨트롤러·종료 정리
```csharp
// Bootstrap.cs
using Dignus.Godot.DependencyInjection;
using Godot;

public partial class Bootstrap : Node
{
    public override void _EnterTree()
    {
        DignusGodotServiceContainer.RegisterDependencies(typeof(LobbyController).Assembly);
    }
}
```

```csharp
// LobbyModel.cs
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Attributes;
using Dignus.Godot.Framework;

[Injectable(LifeScope.Transient)]
public class LobbyModel : ISceneModel
{
}
```

```csharp
// LobbyController.cs
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Attributes;
using Dignus.Godot.Framework;

[Injectable(LifeScope.Transient)]
public class LobbyController : SceneControllerBase<LobbyScene, LobbyModel>
{
    public override void Dispose()
    {
        Scene = null;
    }
}
```

```csharp
// LobbyScene.cs
using Dignus.Godot.Framework;

public partial class LobbyScene : SceneBase<LobbyController>
{
    protected override void OnAwakeScene()
    {
        // SceneController.Scene and SceneController.Model are available here.
    }

    public override void OnDestroyScene()
    {
        if (SceneController != null)
        {
            SceneController.Dispose();
        }
    }
}
```
씬의 `_Ready()`는 Transient 컨트롤러 조회, 모델 주입, 씬 연결을 거쳐 `OnAwakeScene()`을 호출한다. 컨트롤러 소유 구독과 자원 정리는 `Dispose()`에 작성한다. 예제에서는 씬 참조를 비운다. 씬에서 `_Ready()`나 `_ExitTree()`를 재정의하면 해당 기본 메서드도 호출해 수명 주기 처리를 유지한다.

### 코루틴 본문과 완료 처리
```csharp
// CoroutineExample.cs
using Dignus.Godot.Coroutine;
using Godot;
using System.Collections;

public partial class CoroutineExample : Node
{
    public override void _Ready()
    {
        DignusGodotCoroutineManager.Start(0.5f, RunSequence(), OnCompleted);
    }

    private IEnumerator RunSequence()
    {
        GD.Print("Started");
        yield return null;
        GD.Print("Continued");
    }

    private void OnCompleted()
    {
        GD.Print("Completed");
    }
}
```
지연 시간은 Dignus 처리기에 전달하며, 본문은 이후 코루틴 갱신에서 실행된다. `yield return null`은 처리기에 제어를 돌려준다. 열거가 끝나면 완료 콜백을 호출한다. 예제는 반복 본문에서 Node를 참조하지 않는 유한 시퀀스를 시작한다.

### 바인딩 구독과 해제
```csharp
// ScoreView.cs
using Dignus.Godot.Binding;
using Godot;

public partial class ScoreView : Node
{
    private readonly BindableProperty<int> _score = new BindableProperty<int>(0);

    public override void _EnterTree()
    {
        _score.ValueChanged += OnScoreChanged;
        OnScoreChanged(_score.Value);
    }

    public void AddScore(int amount)
    {
        _score.Value += amount;
    }

    public override void _ExitTree()
    {
        _score.ValueChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int score)
    {
        GD.Print(score);
    }
}
```
구독 시 현재 값을 자동 통지하지 않으므로 예제에서는 직접 표시한다. 구독과 해제를 짝지어 트리를 떠난 뷰의 이벤트 핸들러가 남지 않도록 한다.

### 로드 진행률과 완료 콜백
```csharp
// SceneNavigation.cs
using Dignus.Godot;
using Dignus.Godot.Framework;
using Godot;

public partial class SceneNavigation : Node
{
    public override void _EnterTree()
    {
        DignusGodotSceneManager.Instance.OnSceneLoadingProgress += OnLoadingProgress;
    }

    public void OpenLobby()
    {
        DignusGodotSceneManager.Instance.LoadScene("res://Scenes/Lobby.tscn", OnLobbyLoaded);
    }

    public override void _ExitTree()
    {
        DignusGodotSceneManager.Instance.OnSceneLoadingProgress -= OnLoadingProgress;
    }

    private void OnLoadingProgress(string sceneName, float progress)
    {
        GD.Print($"{sceneName}: {progress}");
    }

    private void OnLobbyLoaded(SceneBase scene)
    {
        if (scene is LobbyScene lobbyScene)
        {
            GD.Print(lobbyScene.Name);
        }
    }
}
```
전환 전체를 관찰하려면 `SceneNavigation`을 교체되는 기본 씬 밖에 둔다. 진행률 구독은 해당 노드가 트리를 떠날 때 해제하며, 완료 콜백은 로드 요청에 별도로 연결된다. 로드 중 등록된 `SceneBase`가 없으면 완료 콜백 인자는 null일 수 있다.

### 특성 기반 씬 로드와 풀 연결

앞에서 설명한 `[ResourcePath("res://Scenes", "Bullet.tscn")]`를 `Bullet`에 지정한다. `Bullet.tscn`의 루트는 해당 Node 타입을 사용한다. 호출부에는 `Dignus.Godot`, `Dignus.Godot.Extensions`를 가져온다.
```csharp
// Within a Node-derived caller, using the Bullet type shown above.
var packedScene = DignusGodotResourceManager.Instance.LoadScene<Bullet>();
var bullet = this.InstantiateWithPool<Bullet>();
if (bullet != null)
{
    bullet.Recycle();
}
```
`LoadScene<Bullet>()`는 PackedScene만 로드하며 Node를 생성하거나 부착하지 않는다. 인자 없는 풀 확장은 같은 특성으로 PackedScene을 가져온 뒤 풀의 루트 노드를 호출자에 붙인다. 두 호출은 대체 진입점이며, 명시적 로드는 연결 관계를 설명하기 위해 함께 표시했다.

## 라이선스

MIT License. [LICENSE](LICENSE)를 참조한다.
