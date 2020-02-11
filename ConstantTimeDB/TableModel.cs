using Murmur;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace ConstantTimeDB
{
    public abstract class TableModel
    {
        static readonly HashAlgorithm Hasher = MurmurHash.Create32(managed: true);

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(Hasher.ComputeHash(Encoding.ASCII.GetBytes(ToString())), 0);
        }

        public void Validate()
        {
            var results = new List<ValidationResult>();
            bool IsValid = Validator.TryValidateObject(this, new ValidationContext(this, null, null), results, true);
            if (!IsValid)
                results.ForEach(r => { throw new ArgumentOutOfRangeException(r.ErrorMessage); });
        }
    }
}
