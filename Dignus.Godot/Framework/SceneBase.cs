using Dignus.Godot.DependencyInjection;
using Godot;
using System;
namespace Dignus.Godot.Framework
{
    public abstract partial class SceneBase : Node
    {
        public override void _Ready()
        {
            DignusGodotSceneManager.Instance.RegisterScene(this);
            OnAwakeScene();
            OnShow();
        }
        public override void _ExitTree()
        {
            OnHide();
            OnDestroyScene();
        }
        protected abstract void OnAwakeScene();
        public abstract void OnDestroyScene();
        protected virtual void OnHide()
        {
        }
        protected virtual void OnShow()
        {
        }
    }
}
