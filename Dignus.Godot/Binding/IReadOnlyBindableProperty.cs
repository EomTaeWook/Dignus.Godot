// Copyright (c) 2026 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Godot.Binding
{
    public interface IReadOnlyBindableProperty<T>
    {
        T Value         {
            get;
        }

        event BindableProperty<T>.ValueChangedHandler ValueChanged;
    }
}
