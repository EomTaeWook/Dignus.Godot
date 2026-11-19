# Dignus.Godot

[English](README.md) | 한국어

DI, 씬 수명 주기, 리소스, 노드 풀, 코루틴과 바인딩을 제공하는 Godot 통합 라이브러리.

## 요구 사항

Godot 4.7.2 .NET과 .NET SDK가 필요하다. 라이브러리는 net8.0을 대상으로 한다.

## 설치

```xml
<PackageReference Include="Dignus.Godot" Version="1.0.0" />
```

## 빠른 시작

SceneBase<TController>._Ready()보다 먼저 시작 시 한 번 실행한다. 노드 배치와 전체 씬·컨트롤러·모델 예제는 가이드를 따른다.

```csharp
using Dignus.Godot.DependencyInjection;

var container = DignusGodotServiceContainer.RegisterDependencies(typeof(LobbyController).Assembly);
container.Build();
```

## 사용법

확장자를 포함한 res:// 경로를 사용한다. PackedScene 풀은 기대하는 루트 Node 타입을 요구한다. SceneBase 정리는 _ExitTree()에서 수행하므로 트리 분리 때도 실행된다. 씬 연결은 BindScene(this)를 사용한다.

## 문서

- [API와 연결 예제 · 영어](docs/Usage.md)
- [API와 연결 예제 · 한국어](docs/Usage.ko.md)

## 라이선스

MIT 라이선스. [LICENSE](LICENSE)를 참조한다.
