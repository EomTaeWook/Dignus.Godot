// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Attributes;
using System;
using System.Reflection;

namespace Dignus.Godot.DependencyInjection
{
    public static class DignusGodotActivator
    {
        public static T Create<T>(ServiceContainer serviceContainer, params object[] runtimeArguments)
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }

            return (T)Create(serviceContainer, typeof(T), runtimeArguments);
        }

        public static object Create(ServiceContainer serviceContainer, Type targetType, params object[] runtimeArguments)
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            var arguments = runtimeArguments ?? Array.Empty<object>();

            if (targetType.IsInterface == true)
            {
                if (arguments.Length > 0)                 {
                    throw new InvalidOperationException("Runtime arguments require a concrete type.");
                }
                return serviceContainer.GetService(targetType);
            }
            if (targetType.IsAbstract == true)
            {
                if (arguments.Length > 0)
                {
                    throw new InvalidOperationException($"runtime arguments are not supported for interface or abstract type: {targetType.Name}");
                }

                return serviceContainer.GetService(targetType);
            }

            var constructor = SelectConstructor(targetType);
            var constructorArguments = GetConstructorArguments(serviceContainer, constructor, arguments);
            var instance = constructor.Invoke(constructorArguments);

            foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty))
            {
                var injectAttribute = property.GetCustomAttribute<InjectAttribute>(false);
                if (injectAttribute == null)
                {
                    continue;
                }
                var propertyValue = serviceContainer.GetService(property.PropertyType);
                property.SetValue(instance, propertyValue);
            }

            return instance;
        }

        private static ConstructorInfo SelectConstructor(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"public constructor is required for type {type.Name}");
            }

            ConstructorInfo selectedConstructor = null;
            int injectConstructorCount = 0;

            for (int i = 0; i < constructors.Length; ++i)
            {
                var constructor = constructors[i];

                if (constructor.IsDefined(typeof(InjectConstructorAttribute), false) == true)
                {
                    if (injectConstructorCount > 0)
                    {
                        throw new InvalidOperationException($"multiple constructors have been found. {type.FullName}");
                    }
                    selectedConstructor = constructor;
                    injectConstructorCount++;
                }
            }

            if (injectConstructorCount == 0)
            {
                if (constructors.Length > 1)
                {
                    throw new InvalidOperationException($"multiple constructors have been found. {type.Name}");
                }
                selectedConstructor = constructors[0];
            }

            return selectedConstructor;
        }

        private static object[] GetConstructorArguments(ServiceContainer serviceContainer, ConstructorInfo constructorInfo, object[] runtimeArguments)
        {
            var parameters = constructorInfo.GetParameters();
            var argumentValues = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
            {
                var parameterType = parameters[i].ParameterType;
                var matchingRuntimeArgument = FindMatchingRuntimeArgument(parameterType, runtimeArguments);

                if (matchingRuntimeArgument != null)
                {
                    argumentValues[i] = matchingRuntimeArgument;
                }
                else
                {
                    argumentValues[i] = serviceContainer.GetService(parameterType);
                }
            }

            return argumentValues;
        }

        private static object FindMatchingRuntimeArgument(Type parameterType, object[] runtimeArguments)
        {
            object matchedArgument = null;
            bool found = false;

            foreach (var argument in runtimeArguments)
            {
                if (argument == null)
                {
                    continue;
                }

                if (parameterType.IsAssignableFrom(argument.GetType()) == true)
                {
                    if (found == true)
                    {
                        throw new InvalidOperationException($"multiple scene nodes were provided for constructor parameter type: {parameterType.Name}");
                    }

                    matchedArgument = argument;
                    found = true;
                }
            }

            return matchedArgument;
        }
    }
}

