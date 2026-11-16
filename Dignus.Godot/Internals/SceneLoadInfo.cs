using Dignus.Godot.Framework;
using System;

namespace Dignus.Godot.Internals
{
    internal struct SceneLoadInfo
    {
        public string SceneName
        {
            get;
            set;
        }

        public bool Additive
        {
            get;
            set;
        }

        public Action<SceneBase> Callback
        {
            get;
            set;
        }
    }
}
