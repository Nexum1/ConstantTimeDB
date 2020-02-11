using ConstantTimeDB.Storage;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using System.Linq.Expressions;

namespace ConstantTimeDB
{
    public class Table<T> where T : TableModel
    {
        internal TableStorage<T> storage;

        public Table(string StoragePath)
        {
            storage = new TableStorage<T>(StoragePath);
        }

        public void Insert(T row)
        {
            storage.Insert(row);
        }

        public void Update(T row)
        {
            storage.Update(row);
        }

        public void Delete(object key)
        {
            storage.Delete(key);
        }

        public void Delete(T row)
        {
            storage.Delete(row);
        }

        public T Fetch(object key)
        {
            return storage.Fetch(key);
        }
    }   
}
