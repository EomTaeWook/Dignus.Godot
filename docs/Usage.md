# Dignus.Godot

[English overview](../README.md) | [한국어 개요](../README.ko.md)

English | [한국어](../README.ko.md)

Dignus integration for Godot 4.7.2 .NET, providing dependency injection, scene lifecycle management, resource loading, node pooling, coroutines, and value change notifications.

## Project configuration

- Solution: `Dignus.Godot.slnx`
- Library: `Dignus.Godot/Dignus.Godot.csproj`
- SDK: `Godot.NET.Sdk/4.7.2`
- Target framework: `net8.0`
- Dependency: `Dignus` 1.4.3

The Godot .NET editor and .NET SDK are required. Add the following package reference to your Godot C# project. NuGet resolves the Dignus and GodotSharp dependencies automatically.

```xml
<ItemGroup>
  <PackageReference Include="Dignus.Godot" Version="1.0.0" />
</ItemGroup>
```

## NuGet packaging

Package ID: `Dignus.Godot`. Version: `1.0.0`.

```powershell
dotnet pack Dignus.Godot/Dignus.Godot.csproj -c ExportRelease
```

The package is written to `publish/nuget/Dignus.Godot.1.0.0.nupkg`. ExportRelease builds also copy the library to `publish/dll/net8.0/`. The package includes the icon, both READMEs, and MIT license. Dependency declarations are generated from package references. Publishing to NuGet is a separate operation.

The solution exposes `Debug`, `ExportDebug`, and `ExportRelease` configurations.

## Dependency injection

Register the target assembly with the static container in `Dignus.Godot.DependencyInjection`. Registration and lifetimes follow Dignus registration rules.

```csharp
DignusGodotServiceContainer.RegisterDependencies(typeof(LobbyController).Assembly);
var controller = DignusGodotServiceContainer.GetService<LobbyController>();
```

Controllers and services are resolved through the Dignus container. Scene nodes are connected through `BindScene(this)`.

## Scenes and controllers

`Dignus.Godot.Framework.SceneBase` derives from `Node`.

| Entry point | Call order |
| --- | --- |
| `_Ready()` | Register with scene manager → `OnAwakeScene()` → `OnShow()` |
| `_ExitTree()` | `OnHide()` → `OnDestroyScene()` |

`SceneBase<TController>` resolves its controller from the container and calls `BindScene(this)` before the base `_Ready()`.

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

Controllers implement `ISceneController` or inherit from `SceneControllerBase<TScene>` or `SceneControllerBase<TScene, TModel>`. The model variant exposes a `Model` property marked with `[Inject]`. Scene base classes do not automatically call the controller's `Dispose()`; perform required cleanup in the scene's `OnDestroyScene()`.

`OnShow` runs from `_Ready`, and `OnHide` runs from `_ExitTree`. These are not visibility change events. Detaching a node from the tree also calls `OnDestroyScene`; do not treat it as a callback restricted to memory deallocation.

## Scene transitions

Pass scene paths to `DignusGodotSceneManager.Instance`.

```csharp
var sceneManager = DignusGodotSceneManager.Instance;
sceneManager.LoadScene("res://Scenes/Lobby.tscn");
sceneManager.LoadAdditiveScene("res://Scenes/Overlay.tscn");
sceneManager.UnloadAdditiveScene("res://Scenes/Overlay.tscn");
```

- Load requests are processed sequentially through one queue. Resources are retrieved using the threaded loading API of `ResourceLoader`.
- Main scenes are replaced with `ChangeSceneToPacked`. Additive scenes are attached as children of `SceneTree.Root`.
- Events include `OnSceneLoadStarted`, `OnSceneLoadingProgress`, and `OnSceneLoadCompleted`. The request's completion callback runs after the completion event.
- The completion callback receives the last `SceneBase` registered during loading, or null if none was registered.
- `CurrentSceneName`, `PreviousSceneName`, and `CurrentScene` are updated during main scene transitions handled by the manager. Registering the initial scene in `_Ready` alone does not initialize these properties.
- Additive unloading detaches root children whose `SceneFilePath` matches the requested path, then calls `QueueFree`. The current main scene cannot be unloaded this way. Missing targets produce no action.
- Enum overloads use `ToString()` directly; they do not construct scene paths.

## Resources

`DignusGodotResourceManager` caches loaded `Resource` instances by path.

```csharp
var texture = DignusGodotResourceManager.Instance.LoadAsset<Godot.Texture2D>("res://Textures/Icon.png");
var packedScene = DignusGodotResourceManager.Instance.LoadAsset<Godot.PackedScene>("res://Scenes/Bullet.tscn");
```

`LoadScene<T>()` reads `ResourcePathAttribute` from a Node type and returns a `PackedScene`. The default file name is `TypeName.tscn`. An explicit file name is used unchanged, so include its extension.

```csharp
using Dignus.Godot.Attributes;
using Godot;

[ResourcePath("res://Scenes", "Bullet.tscn")]
public partial class Bullet : Node2D
{
}
```

For `LoadAsset<T>()`, T must be a Resource type. It uses the same attribute to construct the path, but its default file name is the type name without an automatically added extension. A missing attribute causes an exception. `LoadAsset<T>(string path)` returns null without caching when loading returns null.

`UnloadAsset(string path)` and `UnloadAsset(Resource resource)` only remove references from the manager's cache. They do not forcibly release resources held by other consumers or the engine.

## Node pooling

`DignusGodotObjectPool` maintains a pool for each `PackedScene`. T in `Pop<T>` must match the instantiated root Node type. A type mismatch returns the node to its pool and throws an exception.

```csharp
using Dignus.Godot.Extensions;

var bullet = this.InstantiateWithPool<Bullet>("res://Scenes/Bullet.tscn");
if (bullet != null)
{
    bullet.Recycle();
}
```

`InstantiateWithPool<T>` provides overloads for an attribute-derived path, an explicit path, or a `PackedScene`. It attaches the retrieved node to the caller. `Recycle()` detaches the node from its parent and returns it to the pool.

When calling `Pop` or `Push` directly, the caller manages tree attachment and detachment. Pushing a node that was not created by this pool throws an exception.

- `DestroyPool(packedScene)`: removes cached and active nodes in that pool, retaining the pool registration.
- `Clear()`: removes cached and active nodes in all pools.
- `ClearExceptActive()`: removes cached nodes only.

Removal uses `QueueFree`. Reusing nodes does not automatically reset their state. `_Ready` does not run on every reattachment. Pooling a `SceneBase` also means that detachment invokes its `_ExitTree` cleanup methods.

## Singletons and coroutines

`SingletonNode<T>.Instance` searches the running `SceneTree` root for a node named after the type and creates one if necessary. Because it is not a child of the main scene, it is not included in main scene replacement. Access requires a running SceneTree and must occur on the main thread.

`Dignus.Godot.Coroutine.DignusGodotCoroutineManager` accepts `IEnumerator` and Dignus `CoroutineHandle` instances. It supports a delay and completion callback, updating the Dignus coroutine handler from `_Process(double delta)`.

```csharp
DignusGodotCoroutineManager.Start(enumerator);
DignusGodotCoroutineManager.Start(0.5f, enumerator, OnCompleted);
```

Yielded values are interpreted by the Dignus coroutine handler.

## Binding and object validity

`Dignus.Godot.Binding.BindableProperty<T>` exposes a `ValueChanged` event. Notifications are skipped when the current value is non-null and `Equals` the new value. `NotifyChanged()` forces notification of the current value. `IReadOnlyBindableProperty<T>` exposes a read-only Value and the event. Consumers manage unsubscription.

`Dignus.Godot.Extensions.ObjectExtensions.IsNull()` returns the inverse of `GodotObject.IsInstanceValid`. It does not separately detect whether an object is queued for deletion.

## Connected usage examples

The following snippets are separate type files in the consuming Godot project. Attach `Bootstrap` to a persistent ancestor of `LobbyScene`, so its `_EnterTree()` registers the assembly before the scene's `_Ready()`. Register each required assembly once at startup; do not register it again on every scene transition. Call Build() after registration and before resolving services. ISceneModel provides transient registration through its interface attribute; the concrete model needs no additional Injectable attribute. DI creates the controller and model, while the scene tree owns the scene Node.

### Bootstrap, model, controller, and cleanup

```csharp
// Bootstrap.cs
using Dignus.Godot.DependencyInjection;
using Godot;

public partial class Bootstrap : Node
{
    public override void _EnterTree()
    {
        var container = DignusGodotServiceContainer.RegisterDependencies(typeof(LobbyController).Assembly);
        container.Build();
    }
}
```

```csharp
// LobbyModel.cs
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Attributes;
using Dignus.Godot.Framework;

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
The scene's `_Ready()` resolves the transient controller, injects its model, binds the scene, and then calls `OnAwakeScene()`. Add controller-owned subscription and resource cleanup to `Dispose()`; the example clears the scene reference. If overriding `_Ready()` or `_ExitTree()` in a scene, call the corresponding base implementation to retain this lifecycle.

### Coroutine body and completion

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
The delay is passed to the Dignus handler; the body runs on subsequent coroutine updates. A `yield return null` yields control back to that handler. Completion callbacks run after the enumerator finishes. This example starts a finite sequence that does not retain the Node in its iterator body.

### Binding subscription and removal

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
Subscribing does not immediately emit the current value, so the example displays it explicitly. Matching subscriptions and unsubscriptions prevents an exited view from retaining its event handler.

### Loading progress and completion callback

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
Keep `SceneNavigation` outside the main scene being replaced if it must observe the entire transition. Its progress subscription ends when it leaves the tree; the completion callback belongs to the load request and is invoked separately. A callback's scene argument can be null when no `SceneBase` registered during loading.

### Attribute-based scene loading and pooling

Use the earlier `[ResourcePath("res://Scenes", "Bullet.tscn")]` declaration on `Bullet`. The root of `Bullet.tscn` must use that Node type. Import `Dignus.Godot` and `Dignus.Godot.Extensions` in the caller.
```csharp
// Within a Node-derived caller, using the Bullet type shown above.
var packedScene = DignusGodotResourceManager.Instance.LoadScene<Bullet>();
var bullet = this.InstantiateWithPool<Bullet>();
if (bullet != null)
{
    bullet.Recycle();
}
```
`LoadScene<Bullet>()` only loads the PackedScene; it does not instantiate or attach a Node. The no-argument pool extension uses the same attribute to obtain the PackedScene, then attaches the pooled root to the caller. These are alternative entry points; the explicit load is shown to explain the relationship.

## License

MIT License. See [LICENSE](../LICENSE).
