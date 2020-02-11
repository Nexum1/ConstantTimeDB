using C5;
using IONConvert;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;

namespace ConstantTimeDB
{
    public class TableColumnMap<T> : ITableColumnMap
    {
        public TreeDictionary<T, MapperFilePostion> ColumnMapper = new TreeDictionary<T, MapperFilePostion>();
        int MapRowSize = 12;
        int MapRowSizeNoPosition = 0;
        long CurrentColumnMapperSize = 4;
        string ColumnMapFile;
        string ColumnMap;
        MemoryMappedFile MemoryMappedColumnMapFile;
        MemoryMappedViewStream MemoryMappedColumnMapStream;
        MemoryMappedViewAccessor MemoryMappedColumnMapCountAccessor;

        const byte DeleteChar = 0x7f;
        byte[] DeleteBuffer;

        public TableColumnMap(string StoragePath, string TableName, string ColumnName, StringLengthAttribute StringLength, MaxLengthAttribute MaxLength)
        {
            int stringLength = StringLength != null ? StringLength.MaximumLength : 0;
            int stringArrayLength = MaxLength != null ? MaxLength.Length : 0;
            if (stringLength > 0)
            {
                MapRowSizeNoPosition = stringLength;
                if (stringArrayLength > 0)
                {
                    MapRowSizeNoPosition *= stringArrayLength;
                }
                MapRowSizeNoPosition += 4;
            }
            else
            {
                MapRowSizeNoPosition = Marshal.SizeOf(typeof(T));
            }

            MapRowSize = MapRowSizeNoPosition + 8;
            DeleteBuffer = Common.CreateDefaultArray(DeleteChar, MapRowSize);

            ColumnMap = $"{TableName}_{ColumnName}";
            ColumnMapFile = $"{StoragePath}{ColumnMap}.map";
            if (File.Exists(ColumnMapFile))
            {
                FileInfo fi = new FileInfo(ColumnMapFile);
                CurrentColumnMapperSize = fi.Length;
            }
            MemoryMappedColumnMapFile = MemoryMappedFile.CreateFromFile(ColumnMapFile, FileMode.OpenOrCreate, ColumnMap, CurrentColumnMapperSize);

            MemoryMappedColumnMapStream = MemoryMappedColumnMapFile.CreateViewStream(0, CurrentColumnMapperSize);
            MemoryMappedColumnMapCountAccessor = MemoryMappedColumnMapFile.CreateViewAccessor(0, 4, MemoryMappedFileAccess.Write);
            byte[] buffer = new byte[4];
            MemoryMappedColumnMapStream.Read(buffer, 0, 4);
            int ExpectedMappings = BitConverter.ToInt32(buffer, 0);

            if (ExpectedMappings > 0)
            {
                for (long position = 0; position < MemoryMappedColumnMapStream.Length; position += MapRowSize)
                {
                    buffer = new byte[MapRowSizeNoPosition];
                    MemoryMappedColumnMapStream.Read(buffer, 0, MapRowSizeNoPosition);
                    if (!buffer.StartsWith(DeleteChar, MapRowSizeNoPosition))
                    {
                        T key = ION.DeserializeObject<T>(buffer);
                        buffer = new byte[8];
                        MemoryMappedColumnMapStream.Read(buffer, 0, 8);
                        long filePosition = BitConverter.ToInt64(buffer, 0);
                        ColumnMapper.Add(key, new MapperFilePostion(filePosition, position));
                        ExpectedMappings--;

                        if (ExpectedMappings == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        //Skip Position
                        MemoryMappedColumnMapStream.Position += MapRowSize;
                    }
                }

                if (ExpectedMappings > 0)
                {
                    throw new Exception("Mapping File Corrupt!");
                }
            }
        }

        void CheckTableMapSize(long size)
        {
            if (CurrentColumnMapperSize < size)
            {
                int counter = 0;

                while (CurrentColumnMapperSize < size)
                {
                    CurrentColumnMapperSize = 0x1 << counter;
                    counter++;
                }

                long keepPosition = MemoryMappedColumnMapStream.Position;
                MemoryMappedColumnMapStream.Dispose();
                MemoryMappedColumnMapCountAccessor.Dispose();
                MemoryMappedColumnMapFile.Dispose();

                MemoryMappedColumnMapFile = MemoryMappedFile.CreateFromFile(ColumnMapFile, FileMode.OpenOrCreate, ColumnMap, CurrentColumnMapperSize);
                MemoryMappedColumnMapStream = MemoryMappedColumnMapFile.CreateViewStream();
                MemoryMappedColumnMapStream.Position = keepPosition;
                MemoryMappedColumnMapCountAccessor = MemoryMappedColumnMapFile.CreateViewAccessor(0, 4, MemoryMappedFileAccess.Write);
            }
        }

        bool Exists(T Key)
        {
            return ColumnMapper.Contains(Key);
        }

        public MapperFilePostion Equal(object Field)
        {
            return ColumnMapper[(T)Field];
        }

        public List<C5.KeyValuePair<T, MapperFilePostion>> Between(T Bottom, T Top)
        {
            return ColumnMapper.RangeFromTo(Bottom, Top).ToList();
        }

        public List<C5.KeyValuePair<T, MapperFilePostion>> Greater(T Top)
        {
            return ColumnMapper.RangeTo(Top).ToList();
        }

        public List<C5.KeyValuePair<T, MapperFilePostion>> Less(T Bottom)
        {
            return ColumnMapper.RangeFrom(Bottom).ToList();
        }

        public void Insert(object Value, long CurrentTablePosition, byte[] PositionByte)
        {
            T ValueConvert = (T)Value;
            ColumnMapper.Add(ValueConvert, new MapperFilePostion(CurrentTablePosition, MemoryMappedColumnMapStream.Position));
            CheckTableMapSize(MemoryMappedColumnMapStream.Position + MapRowSize);
            byte[] ValueByte = ION.SerializeObject(ValueConvert, PadRight: true, PadAmount: MapRowSizeNoPosition);
            MemoryMappedColumnMapStream.Write(ValueByte, 0, ValueByte.Length);
            MemoryMappedColumnMapStream.Write(PositionByte, 0, PositionByte.Length);
            MemoryMappedColumnMapCountAccessor.Write(0, ColumnMapper.Count);
        }

        public void Update(object OldValue, object NewValue)
        {
            T OldValueConvert = (T)OldValue;
            T NewValueConvert = (T)NewValue;           

            MapperFilePostion mapperFilePostion = ColumnMapper[OldValueConvert];
            ColumnMapper.Remove(OldValueConvert);
            ColumnMapper.Add(NewValueConvert, mapperFilePostion);
            byte[] ValueByte = ION.SerializeObject(NewValueConvert, PadRight: true, PadAmount: MapRowSize);

            using (var accessor = MemoryMappedColumnMapFile.CreateViewAccessor(mapperFilePostion.MapPosition, MapRowSize, MemoryMappedFileAccess.Write))
            {
                accessor.WriteArray(0, ValueByte, 0, ValueByte.Length);
            }
            MemoryMappedColumnMapCountAccessor.Write(0, ColumnMapper.Count);
        }

        public void Delete(object Value)
        {
            T ValueConvert = (T)Value;

            MapperFilePostion mapperPosition = ColumnMapper[ValueConvert];
            ColumnMapper.Remove(ValueConvert);

            //Remove From Mapper
            using (var accessor = MemoryMappedColumnMapFile.CreateViewAccessor(mapperPosition.MapPosition, MapRowSize, MemoryMappedFileAccess.Write))
            {
                accessor.WriteArray(0, DeleteBuffer, 0, DeleteBuffer.Length);
            }
            MemoryMappedColumnMapCountAccessor.Write(0, ColumnMapper.Count);
        }
    }

    public class MapperFilePostion
    {
        public long TablePosition;//Position in the DB File
        public long MapPosition;//Position in the Mapper File

        public MapperFilePostion(long TablePosition, long MapPosition)
        {
            this.TablePosition = TablePosition;
            this.MapPosition = MapPosition;
        }
    }
}
