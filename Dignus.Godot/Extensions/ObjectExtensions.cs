using Godot;
namespace Dignus.Godot.Extensions
{
    public static class ObjectExtensions
    {
        public static bool IsNull(this GodotObject instance)         {
            return !GodotObject.IsInstanceValid(instance);
        }
    }
}
