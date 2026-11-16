// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Extensions;
using System;
using System.Reflection;

namespace Dignus.Godot.DependencyInjection
{
    public class DignusGodotServiceContainer
    {
        private static readonly ServiceContainer _serviceContainer = new ServiceContainer();

        public static ServiceContainer RegisterDependencies(Assembly assembly)
        {
            _serviceContainer.RegisterDependencies(assembly);

            return _serviceContainer;
        }

        public static T GetService<T>()
        {
            return _serviceContainer.GetService<T>();
        }

        public static T GetService<T>(params object[] arguments)
        {
            if (arguments == null)
            {
                return GetService<T>();
            }
            if (arguments.Length == 0)
            {
                return GetService<T>();
            }

            return DignusGodotActivator.Create<T>(_serviceContainer, arguments);
        }
    }
}

