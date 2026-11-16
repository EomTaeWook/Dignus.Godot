using Dignus.Godot.DependencyInjection;

namespace Dignus.Godot.Framework
{
    public abstract partial class SceneBase<TController> : SceneBase where TController : ISceneController
    {
        public TController SceneController
        {
            get;
            private set;
        }
        public override void _Ready()
        {
            SceneController = DignusGodotServiceContainer.GetService<TController>();
            SceneController.BindScene(this);
            base._Ready();
        }
    }
}
