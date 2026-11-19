# Dignus.Godot

English | [한국어](README.ko.md)

Godot integration for dependency injection, scene lifecycles, resources, node pools, coroutines, and binding.

## Requirements

Godot 4.7.2 .NET and a .NET SDK. The library targets net8.0.

## Installation

```xml
<PackageReference Include="Dignus.Godot" Version="1.0.0" />
```

## Quick start

Run the following once during startup before SceneBase<TController>._Ready(). See the guide for node placement and the complete scene/controller/model example.

```csharp
using Dignus.Godot.DependencyInjection;

var container = DignusGodotServiceContainer.RegisterDependencies(typeof(LobbyController).Assembly);
container.Build();
```

## Usage

Use res:// paths with extensions. PackedScene pools require the expected root Node type. SceneBase cleanup follows _ExitTree(); it also runs on detachment. Use BindScene(this) for scene access.

## Documentation

- [API and connected examples / English](docs/Usage.md)
- [API and connected examples / Korean](docs/Usage.ko.md)

## License

MIT License. See [LICENSE](LICENSE).
