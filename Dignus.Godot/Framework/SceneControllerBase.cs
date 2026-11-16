// Copyright (c) 2026 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library



namespace Dignus.Godot.Framework
{
    public abstract class SceneControllerBase<TScene> : ISceneController where TScene : SceneBase
    {
        public TScene Scene
        {
            get;
            set;
        }
        public void BindScene(TScene scene)
        {
            Scene = scene;
        }
        public abstract void Dispose();

        void ISceneController.BindScene(SceneBase scene)
        {
            BindScene(scene as TScene);
        }
    }
}
