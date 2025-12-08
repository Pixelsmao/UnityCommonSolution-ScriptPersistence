using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
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

        /// <summary>
        /// 获取MonoBehaviour派生类的所有成员（仅包含派生类，不包含MonoBehaviour自身）
        /// </summary>
        /// <param name="targetType">目标类型（必须是MonoBehaviour的子类）</param>
        /// <param name="bindingFlags">成员筛选标记</param>
        /// <returns>去重后的成员数组</returns>
        public static MemberInfo[] GetMonoBehaviourDerivedMembers(this Type targetType, BindingFlags bindingFlags)
        {
            // 1. 参数校验：确保目标类型是MonoBehaviour的子类
            if (targetType == null)
                return Array.Empty<MemberInfo>();
            if (!typeof(MonoBehaviour).IsAssignableFrom(targetType))
                return Array.Empty<MemberInfo>();
            if (targetType == typeof(MonoBehaviour))
                return Array.Empty<MemberInfo>();

            // 2. 存储去重后的成员
            HashSet<MemberInfo> collectedMembers = new HashSet<MemberInfo>();
            Type currentType = targetType;

            // 3. 遍历继承链：仅处理MonoBehaviour的派生类（遇到MonoBehaviour则停止）
            while (currentType != null && currentType != typeof(MonoBehaviour))
            {
                // 额外校验：确保当前类型仍是MonoBehaviour的派生（防止遍历到非Mono父类）
                if (!typeof(MonoBehaviour).IsAssignableFrom(currentType))
                    break;

                // 获取当前派生类的成员并去重
                MemberInfo[] currentLayerMembers = currentType.GetMembers(bindingFlags);
                foreach (var member in currentLayerMembers)
                {
                    collectedMembers.Add(member);
                }

                // 切换到父类型（如：MyScript → MyBaseScript → MonoBehaviour）
                currentType = currentType.BaseType;
            }

            // 4. 转换为数组返回
            return collectedMembers.ToArray();
        }
    }
}