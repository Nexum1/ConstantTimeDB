using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstantTimeDB
{
    public interface ITableColumnMap
    {
        void Delete(object Field);
        void Insert(object Field, long CurrentTablePosition, byte[] PositionByte);
        void Update(object OldField, object NewField);
        MapperFilePostion Equal(object Field);
    }
}
