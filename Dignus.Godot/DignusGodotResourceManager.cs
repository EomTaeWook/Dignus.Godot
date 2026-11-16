using Dignus.Framework;
using Dignus.Godot.Attributes;
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
namespace Dignus.Godot
{
    public class DignusGodotResourceManager : Singleton<DignusGodotResourceManager>
    {
        private readonly Dictionary<string, Resource> _pathToResourceMap = new Dictionary<string, Resource>();
        private readonly Dictionary<Type, string> _typeToPathMap = new Dictionary<Type, string>();
        public PackedScene LoadScene<T>() where T : Node
        {
            var type = typeof(T);
            if (_typeToPathMap.TryGetValue(type, out var path) == false)
            {
                var attribute = type.GetCustomAttribute<ResourcePathAttribute>();
                if (attribute == null)                 {
                    throw new InvalidOperationException($"failed to find {nameof(ResourcePathAttribute)}");
                }
                path = attribute.Path.TrimEnd('/') + "/" + (attribute.FileName ?? type.Name + ".tscn");
                _typeToPathMap.Add(type, path);
            }
            return LoadAsset<PackedScene>(path);
        }
        public T LoadAsset<T>() where T : Resource
        {
            var attribute = typeof(T).GetCustomAttribute<ResourcePathAttribute>();
            if (attribute == null)             {
                throw new InvalidOperationException($"failed to find {nameof(ResourcePathAttribute)}");
            }
            return LoadAsset<T>(attribute.Path.TrimEnd('/') + "/" + (attribute.FileName ?? typeof(T).Name));
        }
        public T LoadAsset<T>(string path) where T : Resource
        {
            if (_pathToResourceMap.TryGetValue(path, out var resource) == false)
            {
                resource = ResourceLoader.Load<T>(path);
                if (resource == null)                 {
                    return null;
                }
                _pathToResourceMap.Add(path, resource);
            }
            return resource as T;
        }
        public void UnloadAsset(string path)         {
            _pathToResourceMap.Remove(path);
        }
        public void UnloadAsset(Resource resource)
        {
            var paths = new List<string>();
            foreach (var entry in _pathToResourceMap)
            {
                if (entry.Value == resource)                 {
                    paths.Add(entry.Key);
                }
            }
            foreach (var path in paths)             {
                UnloadAsset(path);
            }
        }
    }
}
