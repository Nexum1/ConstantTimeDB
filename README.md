# ConstantTimeDB

### What is ConstantTimeDB? ###

ConstantTimeDB is a fast in-memory on-disk hybrid database that does not lose speed with volumes and ensures fast operations. ConstantTimeDB indexes on all columns using a very evenly distributed hashing algoritm called MurmurHash to ensure minimal chance of collisions. The hash indexing ensures liniar space usage as well as liniar CRUD operations. Saving to the disk is instant and bit serialized using one of my other projects - ION.

### How do I get set up? ###

* Download Repo
* Build ContantTimeDB project
* Reference ConstantTimeDB.dll, C5.dll, MurmurHash.dll
* Add using ConstantTimeDB;
* Create a Database class:
```
public class TestDatabase
{
  public Table<TestTable> Table1 { get; set; }
}
```
* Create a Table Class:
```
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
```
* To Create/Load the database simply use: 
```
Database<TestDatabase> db = new Database<TestDatabase>(@"YourDatabaseFolder\");  
```
* You can now insert, update, delete and fetch like this:
```
 db.Tables.Table1.Insert(new TestTable(0, ""));
 db.Tables.Table1.Update(new TestTable(0, "a"));
 db.Tables.Table1.Fetch(0);
 db.Tables.Table1.Delete(0);
```

### To-Do ###

* Cleanup DB after delete
* Lambda Fetch/Delete
* Fetch on non-key fields using already indexed columns

### Contact ###

* If you have any ideas or issues, the issue tracker is your friend. Alternatively please email me (Corne Vermeulen) on Nexum1@live.com
