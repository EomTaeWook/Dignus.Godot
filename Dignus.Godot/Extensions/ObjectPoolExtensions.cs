using Godot;
namespace Dignus.Godot.Extensions
{
    public static class DignusObjectPoolExtensions
    {
        public static void Recycle(this Node node)
        {
            var parent = node.GetParent();
            if (parent != null)             {
                parent.RemoveChild(node);
            }
            DignusGodotObjectPool.Instance.Push(node);
        }
        public static T InstantiateWithPool<T>(this Node caller) where T : Node
        {
            return caller.InstantiateWithPool<T>(DignusGodotResourceManager.Instance.LoadScene<T>());
        }
        public static T InstantiateWithPool<T>(this Node caller, string path) where T : Node
        {
            return caller.InstantiateWithPool<T>(DignusGodotResourceManager.Instance.LoadAsset<PackedScene>(path));
        }
        public static T InstantiateWithPool<T>(this Node caller, PackedScene packedScene) where T : Node
        {
            if (packedScene == null)             {
                return null;
            }
            var node = DignusGodotObjectPool.Instance.Pop<T>(packedScene);
            var parent = node.GetParent();
            if (parent != null)             {
                parent.RemoveChild(node);
            }
            caller.AddChild(node);
            return node;
        }
    }
}
