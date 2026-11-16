# Dignus.Godot

English | [한국어](README.ko.md)

Dignus integration for Godot 4.7.2 .NET, providing dependency injection, scene lifecycle management, resource loading, node pooling, coroutines, and value change notifications.

## Project configuration

- Solution: `Dignus.Godot.slnx`
- Library: `Dignus.Godot/Dignus.Godot.csproj`
- SDK: `Godot.NET.Sdk/4.7.2`
- Target framework: `net8.0`
- Dependency: `Dignus` 1.4.3

The Godot .NET editor and .NET SDK are required. This repository does not contain a runnable `project.godot` or sample scenes. Reference the library from your Godot C# project, adjusting the relative path to your project location.

```xml
<ItemGroup>
  <ProjectReference Include="../Dignus.Godot/Dignus.Godot/Dignus.Godot.csproj" />
</ItemGroup>
```

The project currently has no NuGet packaging or automated distribution configuration. The `publish` directory contains icons only. Publication of a usable Godot package has not been confirmed.

## Dependency injection

Register the target assembly with the static container in `Dignus.Godot.DependencyInjection`. Registration and lifetimes follow Dignus registration rules.

```csharp
DignusGodotServiceContainer.RegisterDependencies(typeof(LobbyController).Assembly);
var controller = DignusGodotServiceContainer.GetService<LobbyController>();
```

When arguments are provided to `GetService<T>(params object[] arguments)`, `DignusGodotActivator` matches runtime arguments to constructor parameter types and resolves remaining parameters through the container. It also injects properties marked with `[Inject]`. Calls without arguments use regular container resolution.

If a type has multiple public constructors, select one with `[InjectConstructor]`. Multiple runtime arguments assignable to the same parameter cause an exception. Runtime arguments cannot be supplied when resolving interface or abstract types.

## Scenes and controllers

`Dignus.Godot.Framework.SceneBase` derives from `Node`.

| Entry point | Call order |
| --- | --- |
| `_Ready()` | Register with scene manager → `OnAwakeScene()` → `OnShow()` |
| `_ExitTree()` | `OnHide()` → `OnDestroyScene()` |

`SceneBase<TController>` creates its controller and calls `BindScene(this)` before the base `_Ready()`. `InjectedNodes` is a `Node[]` passed to constructor injection. Assign it in code before `_Ready()`; it is not an exported field.

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

## License

MIT License. See [LICENSE](LICENSE).
