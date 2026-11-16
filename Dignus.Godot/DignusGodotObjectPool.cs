using Dignus.Framework;
using Godot;
using System;
using System.Collections.Generic;
namespace Dignus.Godot
{
    public partial class DignusGodotObjectPool : SingletonNode<DignusGodotObjectPool>
    {
        private class PoolItem
        {
            public Node Node             {
                get;
                set;
            }
            public Pool Pool             {
                get;
                set;
            }
        }
        private class Pool : ObjectPoolBase<PoolItem>
        {
            private readonly PackedScene _packedScene;
            public Pool(PackedScene packedScene)             {
                _packedScene = packedScene;
            }
            public override PoolItem CreateItem()
            {
                var node = _packedScene.Instantiate();
                var poolItem = new PoolItem { Node = node, Pool = this };
                Instance._nodeToPoolItemMap.Add(node, poolItem);
                return poolItem;
            }
            public override void Remove(PoolItem item)
            {
                Instance._nodeToPoolItemMap.Remove(item.Node);
                item.Node.QueueFree();
            }
        }
        private readonly Dictionary<Node, PoolItem> _nodeToPoolItemMap = new Dictionary<Node, PoolItem>();
        private readonly Dictionary<PackedScene, Pool> _poolContainer = new Dictionary<PackedScene, Pool>();
        public Node Pop(PackedScene packedScene)
        {
            if (_poolContainer.TryGetValue(packedScene, out var pool) == false)
            {
                pool = new Pool(packedScene);
                _poolContainer.Add(packedScene, pool);
            }
            return pool.Pop().Node;
        }
        public T Pop<T>(PackedScene packedScene) where T : Node
        {
            var node = Pop(packedScene);
            if (node is T typedNode)             {
                return typedNode;
            }
            Push(node);
            throw new InvalidOperationException($"Scene root must be {typeof(T).FullName}.");
        }
        public void Push(Node node)
        {
            if (_nodeToPoolItemMap.TryGetValue(node, out var poolItem) == false)             {
                throw new InvalidOperationException("This node was not created by DignusGodotObjectPool.");
            }
            poolItem.Pool.Push(poolItem);
        }
        public void DestroyPool(PackedScene packedScene)
        {
            if (_poolContainer.TryGetValue(packedScene, out var pool) == true)             {
                pool.Clear();
            }
        }
        public void Clear()         {
            foreach (var pool in _poolContainer.Values)             {
                pool.Clear();
            }
        }
        public void ClearExceptActive()         {
            foreach (var pool in _poolContainer.Values)             {
                pool.ClearExceptActive();
            }
        }
    }
}
