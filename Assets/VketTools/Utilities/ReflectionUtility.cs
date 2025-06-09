using System;
using System.Reflection;

namespace VketTools.Utilities
{
    public static class ReflectionUtility
    {
        public static object GetField(Type type, string field, object instance)
        {
            FieldInfo fieldInfo = type.GetField(field, BindingFlags.GetField | BindingFlags.NonPublic | (instance == null ? BindingFlags.Static : BindingFlags.Instance));
            return fieldInfo.GetValue(instance);
        }

        public static void SetField(Type type, string field, object instance, object value)
        {
            FieldInfo fieldInfo = type.GetField(field, BindingFlags.GetField | BindingFlags.NonPublic | (instance == null ? BindingFlags.Static : BindingFlags.Instance));
            fieldInfo.SetValue(instance, value);
        }

        public static object InvokeMethod(Type type, string method, object instance, object[] param)
        {
            MethodInfo methodInfo = type.GetMethod(method, BindingFlags.InvokeMethod | BindingFlags.NonPublic | (instance == null ? BindingFlags.Static : BindingFlags.Instance));
            return methodInfo.Invoke(instance, param);
        }
    }
}
