using System;
using System.Linq.Expressions;

namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    public static class TypeExtensions
    {
        public static object GetDefaultValue(this Type type)
        {
            var defaultExpression = Expression.Default(type);
            var lambda = Expression.Lambda<Func<object>>(
                Expression.Convert(defaultExpression, typeof(object))
            );
            return lambda.Compile()();
        }
    }
}