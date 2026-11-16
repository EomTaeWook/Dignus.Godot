using Godot;
using System;
namespace Dignus.Godot
{
    public partial class SingletonNode<T> : Node where T : Node, new()
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (GodotObject.IsInstanceValid(_instance) == true)
                {
                    return _instance;
                }
                var sceneTree = Engine.GetMainLoop() as SceneTree;
                if (sceneTree == null)
                {
                    throw new InvalidOperationException("A running SceneTree is required.");
                }
                _instance = sceneTree.Root.GetNodeOrNull<T>(typeof(T).Name);
                if (_instance == null)
                {
                    _instance = new T { Name = typeof(T).Name };
                    sceneTree.Root.AddChild(_instance);
                }
                return _instance;
            }
        }
        public override void _EnterTree()
        {
            OnAwake();
        }
        protected virtual void OnAwake()
        {
        }
    }
}
