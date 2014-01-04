// -----------------------------------------------------------------------
// <copyright file="LambdaEqualityComparer.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    public class LambdaEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _equalityFunc;
        private readonly Func<T, int> _hashFunc;

        public LambdaEqualityComparer(Func<T, T, bool> equalityFunc) 
            : this(equalityFunc, (obj) => 0)
        {
        }

        public LambdaEqualityComparer(Func<T, int> hashFunc)
            : this((x, y) => true, hashFunc)
        {
        }

        public LambdaEqualityComparer(Func<T, T, bool> equalityFunc, Func<T, int> hashFunc)
        {
            if (equalityFunc == null) throw new ArgumentNullException("equalityFunc");
            if (hashFunc == null) throw new ArgumentNullException("hashFunc");

            _equalityFunc = equalityFunc;
            _hashFunc = hashFunc;
        }

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        /// <param name="x">The first object to compare.</param><param name="y">The second object to compare.</param><exception cref="T:System.ArgumentException"><paramref name="x"/> and <paramref name="y"/> are of different types and neither one can handle comparisons with the other.</exception>
        public bool Equals(T x, T y)
        {
            return _equalityFunc(x, y);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <returns>
        /// A hash code for the specified object.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param><exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
        public int GetHashCode(T obj)
        {
            return _hashFunc(obj);
        }
    }
}
