using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConstantTimeDB
{
    public static class Common
    {
        public static T[] CreateDefaultArray<T>(T value, long size)
        {
            T[] arr = new T[size];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
            return arr;
        }

        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

        public static bool StartsWith<T>(this T[] arr, T Value, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (!arr[i].Equals(Value))
                {
                    return false;
                }
            }
            return true;
        }

        public static int IONSize<T>()
        {
            T obj = ModelFiller.Fill<T>();
            byte[] ionBytes = IONConvert.ION.SerializeObject(obj);
            return ionBytes.Length;
        }

        public static object GenericCallHelper<T>(string Method, Type Type, object Target, params object[] Parameter)
        {
            MethodInfo genericFunction = typeof(T).GetMethod(Method);
            MethodInfo func = genericFunction.MakeGenericMethod(Type);
            return func.Invoke(Target, Parameter);
        }
    }
}
