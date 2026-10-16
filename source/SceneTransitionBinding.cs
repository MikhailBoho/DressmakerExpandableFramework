using System;
using System.Reflection;

namespace Dressmaker.DeskMail
{
    internal static class SceneTransitionBinding
    {
        public static MethodInfo Resolve(Type managerType)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var current = managerType.GetProperty("CurrentScene", flags)
                ?? throw new MissingMemberException(managerType.FullName, "CurrentScene");
            // The game uses GameManager+Scene, a nested enum. Derive its actual
            // runtime Type from the property instead of searching for "Scene".
            var sceneType = current.PropertyType;
            if (!sceneType.IsEnum)
                throw new InvalidOperationException(managerType.FullName + ".CurrentScene is not an enum.");
            var method = managerType.GetMethod("SetScene", flags, null,
                new[] { sceneType, typeof(bool) }, null)
                ?? throw new MissingMethodException(managerType.FullName,
                    "SetScene(" + sceneType.FullName + ", Boolean)");
            if (method.ReturnType != typeof(void))
                throw new InvalidOperationException(managerType.FullName + ".SetScene must return void.");
            return method;
        }
    }
}
