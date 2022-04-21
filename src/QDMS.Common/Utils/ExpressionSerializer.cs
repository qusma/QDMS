// -----------------------------------------------------------------------
// <copyright file="ExpressionSerializer.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MetaLinq;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Serialization;

namespace QDMS
{
    /// <summary>
    /// Used internally to serialize LINQ expressions
    /// </summary>
    public static class ExpressionSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string Serialize<T>(this Expression<T> expression)
        {
            using (var stream = new StringWriter())
            {
                EditableExpression mutable = EditableExpression.CreateEditableExpression(expression);
                XmlSerializer xs = new XmlSerializer(typeof(EditableExpression),
                    new[] { typeof(MetaLinq.Expressions.EditableLambdaExpression) });
                xs.Serialize(stream, mutable);
                return stream.ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serialized"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Expression<Func<T, bool>> Deserialize<T>(string serialized)
        {
            if (serialized == null) return null;

            var ms = new StringReader(serialized);

            var xs = new XmlSerializer(typeof(EditableExpression),
                new[] { typeof(MetaLinq.Expressions.EditableLambdaExpression) });

            //Deserialize LINQ expression
            var editableExp = (EditableExpression)xs.Deserialize(ms);
            var expression = (Expression<Func<T, bool>>)editableExp.ToExpression();
            return expression;
        }
    }
}
