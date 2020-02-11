using IONConvert;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq.Expressions;
using System.Reflection;

namespace ConstantTimeDB.Storage
{
    public class TableStorage<T> where T : TableModel
    {
        string TableFile;

        MemoryMappedFile MemoryMappedTableFile;

        int TableRowSize = 0;
        long CurrentTablePosition = 0;
        long CurrentTableSize = 1;
        const byte DeleteChar = 0x7f;
        byte[] DeleteBuffer;

        //TO DO:
        //Start on right place after reboot        
        //Cleanup Database Function (Clear gaps between deletes - maybe keep deleted items in a seperate list
        //Memory usage investigation
        //Keep table structure to remap if changes
        //Index (map) per column
        //Reserve 0 for deletes

        ITableColumnMap[] Columns;
        PropertyInfo KeyColumn;
        const int KeyColumnIndex = 0;
        internal Dictionary<string, int> ColumnPosition = new Dictionary<string, int>();
        Type MyType = typeof(T);
        PropertyInfo[] Properties;

        public TableStorage(string StoragePath)
        {
            TableFile = $"{StoragePath}{MyType.Name}.dat";
            TableRowSize = Common.IONSize<T>();
            DeleteBuffer = Common.CreateDefaultArray(DeleteChar, TableRowSize);

            const BindingFlags serializationFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            Properties = MyType.GetProperties(serializationFlags);

            Columns = new ITableColumnMap[Properties.Length];
            int currentPos = 0;

            foreach (PropertyInfo property in Properties)
            {
                var hasKey = Attribute.IsDefined(property, typeof(KeyAttribute));
                InstantiateColumn(ref currentPos, StoragePath, MyType.Name, property, property.PropertyType, out int pos);
                if (pos == 0)
                {
                    KeyColumn = property;
                }
                ColumnPosition.Add(property.Name, pos);
            }

            if (File.Exists(TableFile))
            {
                FileInfo fi = new FileInfo(TableFile);
                CurrentTableSize = fi.Length;
            }
            MemoryMappedTableFile = MemoryMappedFile.CreateFromFile(TableFile, FileMode.OpenOrCreate, "Table", CurrentTableSize);
        }

        void InstantiateColumn(ref int currentPos, string StoragePath, string TableName, MemberInfo member, Type type, out int pos)
        {
            KeyAttribute key = (KeyAttribute)member.GetCustomAttribute(typeof(KeyAttribute));
            StringLengthAttribute stringLength = (StringLengthAttribute)member.GetCustomAttribute(typeof(StringLengthAttribute));
            MaxLengthAttribute maxLength = (MaxLengthAttribute)member.GetCustomAttribute(typeof(MaxLengthAttribute));
            pos = key == null ? ++currentPos : 0;
            Common.GenericCallHelper<TableStorage<T>>("InstantiateColumnGeneric", type, this, pos, StoragePath, TableName, member.Name, stringLength, maxLength);
        }

        public void InstantiateColumnGeneric<A>(int pos, string StoragePath, string TableName, string MemberName, StringLengthAttribute StringLength, MaxLengthAttribute MaxLength)
        {
            Columns[pos] = new TableColumnMap<A>(StoragePath, TableName, MemberName, StringLength, MaxLength);
        }

        public void CheckTableSize(long size)
        {
            if (CurrentTableSize < size)
            {
                int counter = 0;

                while (CurrentTableSize < size)
                {
                    CurrentTableSize = 0x1 << counter;
                    counter++;
                }

                MemoryMappedTableFile.Dispose();
                MemoryMappedTableFile = MemoryMappedFile.CreateFromFile(TableFile, FileMode.OpenOrCreate, "Table", CurrentTableSize);
            }
        }

        public T Get(long Position)
        {
            using (var accessor = MemoryMappedTableFile.CreateViewAccessor(Position, TableRowSize, MemoryMappedFileAccess.Read))
            {
                byte[] Buffer = new byte[TableRowSize];
                accessor.ReadArray(0, Buffer, 0, TableRowSize);
                return ION.DeserializeObject<T>(Buffer);
            }
        }

        public void Insert(T Row)
        {
            long position = CurrentTablePosition;
            byte[] PositionByte = BitConverter.GetBytes(CurrentTablePosition);
            foreach (PropertyInfo property in Properties)
            {
                object propertyValue = property.GetValue(Row);
                if (propertyValue != null)
                {
                    int column = ColumnPosition[property.Name];
                    Columns[column].Insert(propertyValue, CurrentTablePosition, PositionByte);
                }
            }

            long size = position + TableRowSize;
            CheckTableSize(size);
            using (var accessor = MemoryMappedTableFile.CreateViewAccessor(position, TableRowSize, MemoryMappedFileAccess.Write))
            {
                byte[] Buffer = ION.SerializeObject(Row, PadRight: true, PadAmount: TableRowSize);
                accessor.WriteArray(0, Buffer, 0, Buffer.Length);
            }

            CurrentTablePosition += TableRowSize;
        }

        public void Update(T Row)
        {
            DateTime start = DateTime.Now;
            object propertyValue = KeyColumn.GetValue(Row);
            long Position = Columns[KeyColumnIndex].Equal(propertyValue).TablePosition;
            T Original = Get(Position);
            foreach (PropertyInfo property in Properties)
            {
                object newValue = property.GetValue(Row);
                object oldValue = property.GetValue(Original);
                if (newValue != null && !newValue.Equals(oldValue))
                {
                    int column = ColumnPosition[property.Name];
                    Columns[column].Update(oldValue, newValue);
                }
            }
            using (var accessor = MemoryMappedTableFile.CreateViewAccessor(Position, TableRowSize, MemoryMappedFileAccess.Write))
            {
                byte[] Buffer = ION.SerializeObject(Row, PadRight: true, PadAmount: TableRowSize);
                accessor.WriteArray(0, Buffer, 0, Buffer.Length);
            }
        }

        public void Delete(object Key, T Row = null)
        {
            long Position = Columns[KeyColumnIndex].Equal(Key).TablePosition;
            if(Row == null)
            {
                Row = Get(Position);
            }

            foreach (PropertyInfo property in Properties)
            {
                object value = property.GetValue(Row);
                int column = ColumnPosition[property.Name];
                Columns[column].Delete(value);
            }
            //Remove From DB
            using (var accessor = MemoryMappedTableFile.CreateViewAccessor(Position, TableRowSize, MemoryMappedFileAccess.Write))
            {
                accessor.WriteArray(0, DeleteBuffer, 0, DeleteBuffer.Length);
            }
        }

        public void Delete(T Row)
        {
            DateTime start = DateTime.Now;
            object propertyValue = KeyColumn.GetValue(Row);
            Delete(propertyValue, Row);
        }

        public T Fetch(object Key)
        {
            long Position = Columns[KeyColumnIndex].Equal(Key).TablePosition;            
            return Get(Position);
        }
    }

}
