using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace ConstantTimeDB
{
    public static class ModelFiller
    {
        public static T Fill<T>()
        {
            Type type = typeof(T);
            T obj;
            obj = (T)Activator.CreateInstance(type);

            const BindingFlags serializationFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            FieldInfo[] fields = type.GetFields(serializationFlags);
            PropertyInfo[] properties = type.GetProperties(serializationFlags);

            foreach (FieldInfo field in fields)
            {   
                field.SetValue(obj, FillHelper(field.FieldType, field));
            }
            foreach (PropertyInfo property in properties)
            {
                property.SetValue(obj, FillHelper(property.PropertyType, property));
            }

            return obj;
        }        

        public static object FillHelper(Type type, MemberInfo memberInfo)
        {
            if (IsStringArray(type))
            {
                StringLengthAttribute stringLength = (StringLengthAttribute)memberInfo.GetCustomAttribute(typeof(StringLengthAttribute));
                MaxLengthAttribute maxLength = (MaxLengthAttribute)memberInfo.GetCustomAttribute(typeof(MaxLengthAttribute));
                if (stringLength == null || maxLength == null)
                {
                    throw new Exception("Double length attribute required for type " + type.Name);
                }
                return StringArray(maxLength.Length, stringLength.MaximumLength);
            }
            else if (IsString(type))
            {
                StringLengthAttribute stringLength = (StringLengthAttribute)memberInfo.GetCustomAttribute(typeof(StringLengthAttribute));
                if (stringLength == null)
                {
                    throw new Exception("Length attribute required for type " + type.Name);
                }
                return GetString(stringLength.MaximumLength);
            }
            else if (IsArray(type))
            {
                MaxLengthAttribute maxLength = (MaxLengthAttribute)memberInfo.GetCustomAttribute(typeof(MaxLengthAttribute));
                if (maxLength == null)
                {
                    throw new Exception("Length attribute required for type " + type.Name);
                }
                return GenericCallHelper("Array", type.GetElementType(), maxLength.Length);
            }
            else if (CustomIsClass(type))
            {
                return GenericCallHelper("Fill", type);
            }
            else
            {
                return GenericCallHelper("GetDefaultValue", type);
            }
        }

        public static object StringArray(int arrayLength, int stringSize)
        {
            string[] arr = new string[arrayLength];
            string str = GetString(stringSize);

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = str;
            }

            return arr;
        }

        static string GetString(int length)
        {
            return new string('-', length);
        }

        public static object Array<T>(int arrayLength)
        {
            T[] arr = new T[arrayLength];
            Type type = typeof(T);
            if (CustomIsClass(type))
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = (T)GenericCallHelper("Fill", type);
                }
            }
            return arr;
        }

        static object GenericCallHelper(string Method, Type Type, params object[] Parameter)
        {
            MethodInfo genericFunction = typeof(ModelFiller).GetMethod(Method);
            MethodInfo func = genericFunction.MakeGenericMethod(Type);
            return func.Invoke(null, Parameter);
        }

        public static object GetDefaultValue<T>()
        {
            return default(T);
        }        

        static bool IsString(Type t)
        {
            return t == typeof(string);
        }

        static bool IsStringArray(Type t)
        {
            if (t.IsArray && t.Name == "String[]")
            {
                return true;
            }
            return false;
        }

        static bool IsArray(Type t)
        {
            if (t.IsArray || t == typeof(string) || CustomIsDictionary(t) || CustomIsList(t))
            {
                return true;
            }
            return false;
        }

        internal static bool CustomIsDictionary(Type type)
        {
            return type.Name == "Dictionary`2" && type.IsGenericType;
        }

        internal static bool CustomIsList(Type type)
        {
            return type.Name == "List`1" && type.IsGenericType;
        }

        internal static bool CustomIsClass(Type type)
        {
            return type.IsClass && !type.IsArray && !type.IsPrimitive && !CustomIsDictionary(type) && !CustomIsList(type);
        }
    }
}
