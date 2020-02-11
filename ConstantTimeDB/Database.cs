using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConstantTimeDB
{
    public class Database<T>
    {
        public T Tables;

        public Database(string StoragePath)
        {
            Type type = typeof(T);
            Tables = (T)Activator.CreateInstance(type);
            
            const BindingFlags serializationFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            PropertyInfo[] properties = type.GetProperties(serializationFlags);

            foreach(PropertyInfo property in properties)
            {
                property.SetValue(Tables, Activator.CreateInstance(property.PropertyType, StoragePath));
            }
        }
    }
}
