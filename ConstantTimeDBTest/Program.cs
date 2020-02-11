using C5;
using ConstantTimeDB;
using IONConvert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstantTimeDBTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Database<TestDatabase> db = new Database<TestDatabase>(@"DB\");           

            int testLength = 99999;

            DateTime start = DateTime.Now;
            for (int i = 0; i < testLength; i++)
            {
                string value = String.Join("", "Test", i.ToString());
                db.Tables.Table1.Insert(new TestTable(i, value));
            }
            TimeSpan InsertTime = DateTime.Now - start;
            Console.WriteLine("Insert Time: " + InsertTime.TotalMilliseconds + "ms");
            Console.WriteLine("Insert Time Per Record: " + (InsertTime.TotalMilliseconds / (double)testLength) + "ms");

            List<TestTable> rows = new List<TestTable>();
            start = DateTime.Now;
            for (int i = 0; i < testLength; i++)
            {
                string value = String.Join("", "Tests", i.ToString());
                TestTable test = new TestTable(i, value);
                rows.Add(test);
                db.Tables.Table1.Update(test);
            }
            TimeSpan UpdateTime = DateTime.Now - start;
            Console.WriteLine("Update Time: " + UpdateTime.TotalMilliseconds + "ms");
            Console.WriteLine("Update Time Per Record: " + (UpdateTime.TotalMilliseconds / (double)testLength) + "ms");
            
            start = DateTime.Now;
            for (int i = 0; i < testLength; i++)
            {
                db.Tables.Table1.Fetch(i);
            }
            TimeSpan FetchTime = DateTime.Now - start;
            Console.WriteLine("Fetch Time: " + FetchTime.TotalMilliseconds + "ms");
            Console.WriteLine("Fetch Time Per Record: " + (FetchTime.TotalMilliseconds / (double)testLength) + "ms");

            start = DateTime.Now;
            for (int i = 0; i < testLength; i++)
            {
                db.Tables.Table1.Delete(i);
            }
            TimeSpan DeleteTime = DateTime.Now - start;
            Console.WriteLine("Delete Time: " + DeleteTime.TotalMilliseconds + "ms");
            Console.WriteLine("Delete Time Per Record: " + (DeleteTime.TotalMilliseconds / (double)testLength) + "ms");
            Console.Read();
        }
    }
}
