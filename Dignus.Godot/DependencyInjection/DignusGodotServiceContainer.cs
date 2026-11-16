// Copyright (c) 2026 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Extensions;
using System.Reflection;

namespace Dignus.Godot.DependencyInjection
{
    public class DignusGodotServiceContainer
    {
        private static readonly ServiceContainer _serviceContainer = new();

        public static ServiceContainer RegisterDependencies(Assembly assembly)
        {
            _serviceContainer.RegisterDependencies(assembly);

            return _serviceContainer;
        }

        public static T GetService<T>()
        {
            return _serviceContainer.GetService<T>();
        }
    }
}
