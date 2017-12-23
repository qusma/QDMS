// -----------------------------------------------------------------------
// <copyright file="EfUtils.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace QDMS.Server
{
    public static class EfUtils
    {
        public static IQueryable<TElement> BuildContainsExpression<TElement, TValue>(
            this IQueryable<TElement> query, IEnumerable<TValue> values, Expression<Func<TElement, TValue>> valueSelector)

        {
            if (null == valueSelector) throw new ArgumentNullException(nameof(valueSelector));
            if (null == values) throw new ArgumentNullException(nameof(values));

            ParameterExpression p = valueSelector.Parameters.Single();

            // p => valueSelector(p) == values[0] || valueSelector(p) == ...

            if (!values.Any())

            {
                return query;
            }

            var equals = values.Select(value =>
                (Expression)Expression.Equal(valueSelector.Body, Expression.Constant(value, typeof(TValue))));

            var body = equals.Aggregate((accumulate, equal) => Expression.Or(accumulate, equal));

            return query.Where(Expression.Lambda<Func<TElement, bool>>(body, p));
        }
    }
}