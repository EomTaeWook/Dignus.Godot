// Copyright (c) 2026 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Godot.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ResourcePathAttribute : System.Attribute
    {
        public string Path
        {
            get;
            private set;
        }
        public string FileName
        {
            get;
            private set;
        }
        public ResourcePathAttribute(string path, string fileName = null)
        {
            Path = path;
            FileName = fileName;
        }
    }
}
