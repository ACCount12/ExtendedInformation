using System;
using System.Reflection;


namespace ReflectionHelper
{
    public static class Helper
    {
        public static object InvokePrivateMethode(object instance, string methodname, object[] parameters)
        {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance);
            return methodInfo.Invoke(instance, parameters);
        }

        public static object InvokePrivateMethode(object instance, string methodname, object[] parameters, Type[] types)
        {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
            return methodInfo.Invoke(instance, parameters);
        }

        public static void SetPrivateProperty(object instance, string propertyname, object value)
        {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(instance, value, null);
        }

        public static object GetPrivateProperty(object instance, string propertyname)
        {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance);
            return property.GetValue(instance, new object[] { });
        }

        public static void SetPrivateProperty(Type type, string propertyname, object value)
        {
            PropertyInfo property = type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(type, value, null);
        }

        public static object GetPrivateProperty(Type type, string propertyname)
        {
            PropertyInfo property = type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance);
            return property.GetValue(type, new object[] { });
        }

        public static void SetPrivateField(object instance, string fieldname, object value)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(instance, value);
        }

        public static object GetPrivateField(object instance, string fieldname)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            return field.GetValue(instance);
        }

        public static void SetPrivateField(Type type, string fieldname, object value)
        {
            FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(type, value);
        }

        public static object GetPrivateField(Type type, string fieldname)
        {
            FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            return field.GetValue(type);
        }
    }
}
