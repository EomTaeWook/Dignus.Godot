using Dignus.Framework;
using Dignus.Godot.Coroutine;
using Dignus.Godot.Framework;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
namespace Dignus.Godot
{
    public class DignusGodotSceneManager : Singleton<DignusGodotSceneManager>
    {
        public delegate void SceneLoadStartedHandler(string sceneName);
        public delegate void SceneLoadProgressHandler(string sceneName, float progress);
        public delegate void SceneLoadCompletedHandler(string sceneName);
        public event SceneLoadStartedHandler OnSceneLoadStarted;
        public event SceneLoadProgressHandler OnSceneLoadingProgress;
        public event SceneLoadCompletedHandler OnSceneLoadCompleted;
        public string PreviousSceneName         {
            get;
            private set;
        }
        public string CurrentSceneName         {
            get;
            private set;
        }
        public SceneBase CurrentScene         {
            get;
            private set;
        }
        private SceneBase _loadedScene;
        private bool _loading;
        private readonly Queue<(string SceneName, bool Additive, Action<SceneBase> Callback)> _sceneQueue = new Queue<(string, bool, Action<SceneBase>)>();
        internal void RegisterScene(SceneBase scene)         {
            _loadedScene = scene;
        }
        public void LoadScene(string sceneName, Action<SceneBase> completeCallback = null)         {
            EnqueueScene(sceneName, false, completeCallback);
        }
        public void LoadScene(Enum sceneType, Action<SceneBase> completeCallback = null)
        {
            if (sceneType == null)             {
                throw new ArgumentNullException(nameof(sceneType));
            }
            LoadScene(sceneType.ToString(), completeCallback);
        }
        public void LoadAdditiveScene(string sceneName, Action<SceneBase> completeCallback = null)         {
            EnqueueScene(sceneName, true, completeCallback);
        }
        public void LoadAdditiveScene(Enum sceneType, Action<SceneBase> completeCallback = null)
        {
            if (sceneType == null)             {
                throw new ArgumentNullException(nameof(sceneType));
            }
            LoadAdditiveScene(sceneType.ToString(), completeCallback);
        }
        private void EnqueueScene(string sceneName, bool additive, Action<SceneBase> completeCallback)
        {
            if (string.IsNullOrWhiteSpace(sceneName))             {
                throw new ArgumentException("A res:// scene path is required.", nameof(sceneName));
            }
            _sceneQueue.Enqueue((sceneName, additive, completeCallback));
            if (_loading == true)             {
                return;
            }
            _loading = true;
            DignusGodotCoroutineManager.Start(LoadScenes());
        }
        private IEnumerator LoadScenes()
        {
            try
            {
                while (_sceneQueue.Count > 0)
                {
                    var request = _sceneQueue.Dequeue();
                    OnSceneLoadStarted?.Invoke(request.SceneName);
                    var error = ResourceLoader.LoadThreadedRequest(request.SceneName, "PackedScene");
                    if (error != Error.Ok)                     {
                        throw new InvalidOperationException($"Scene load failed: {request.SceneName} ({error})");
                    }
                    var progress = new global::Godot.Collections.Array();
                    while (ResourceLoader.LoadThreadedGetStatus(request.SceneName, progress) == ResourceLoader.ThreadLoadStatus.InProgress)
                    {
                        if (progress.Count > 0)                         {
                            OnSceneLoadingProgress?.Invoke(request.SceneName, (float)progress[0].AsDouble());
                        }
                        yield return null;
                    }
                    if (ResourceLoader.LoadThreadedGetStatus(request.SceneName) != ResourceLoader.ThreadLoadStatus.Loaded)                     {
                        throw new InvalidOperationException($"Scene load failed: {request.SceneName}");
                    }
                    var packedScene = ResourceLoader.LoadThreadedGet(request.SceneName) as PackedScene;
                    if (packedScene == null)                     {
                        throw new InvalidOperationException($"Not a PackedScene: {request.SceneName}");
                    }
                    var sceneTree = (SceneTree)Engine.GetMainLoop();
                    _loadedScene = null;
                    if (request.Additive == true)
                    {
                        sceneTree.Root.AddChild(packedScene.Instantiate());
                    }
                    else
                    {
                        error = sceneTree.ChangeSceneToPacked(packedScene);
                        if (error != Error.Ok)                         {
                            throw new InvalidOperationException($"Scene change failed: {error}");
                        }
                        yield return null;
                        while (sceneTree.CurrentScene == null)                         {
                            yield return null;
                        }
                        PreviousSceneName = CurrentSceneName;
                        CurrentSceneName = request.SceneName;
                        CurrentScene = _loadedScene;
                    }
                    OnSceneLoadingProgress?.Invoke(request.SceneName, 1f);
                    OnSceneLoadCompleted?.Invoke(request.SceneName);
                    request.Callback?.Invoke(_loadedScene);
                }
            }
            finally             {
                _loading = false;
            }
        }
        public void UnloadAdditiveScene(Enum sceneType)
        {
            if (sceneType == null)             {
                throw new ArgumentNullException(nameof(sceneType));
            }
            UnloadAdditiveScene(sceneType.ToString());
        }
        public void UnloadAdditiveScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))             {
                throw new ArgumentException("A res:// scene path is required.", nameof(sceneName));
            }
            if (sceneName == CurrentSceneName)             {
                throw new InvalidOperationException("Cannot unload the current scene.");
            }
            var sceneTree = (SceneTree)Engine.GetMainLoop();
            foreach (var child in sceneTree.Root.GetChildren())
            {
                if (child.SceneFilePath != sceneName)                 {
                    continue;
                }
                if (child == sceneTree.CurrentScene)                 {
                    throw new InvalidOperationException("Cannot unload the current scene.");
                }
                sceneTree.Root.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}
