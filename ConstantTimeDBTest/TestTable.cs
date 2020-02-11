using ConstantTimeDB;
using System.ComponentModel.DataAnnotations;

namespace ConstantTimeDBTest
{  
    public class TestTable : TableModel
    {
        [Key]
        public int Id { get; set; }

        [StringLength(10)]
        public string Value { get; set; }

        public TestTable()
        {
        }

        public TestTable(int Id)
        {
            this.Id = Id;
        }

        public TestTable(int Id, string Value)
        {
            this.Id = Id;
            this.Value = Value;
        }
    }
}
