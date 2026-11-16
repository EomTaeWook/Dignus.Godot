using Dignus.DependencyInjection.Attributes;

namespace Dignus.Godot.Framework
{
    public abstract class SceneControllerBase<TScene, TModel> : ISceneController
        where TScene : SceneBase
        where TModel : ISceneModel
    {
        [Inject]
        public TModel Model
        {
            get;
            set;
        }
        public TScene Scene
        {
            get;
            set;
        }

        public abstract void Dispose();

        public void BindScene(TScene scene)
        {
            Scene = scene;
        }

        void ISceneController.BindScene(SceneBase scene)
        {
            BindScene(scene as TScene);
        }
    }
}
