using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    public static class MemberInfoExtensions
    {
        public static bool IsSupportedSerializableMember(this MemberInfo member)
        {
            var memberType = member switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                _ => null
            };

            if (memberType == null) return false;
            return memberType.IsValueType || memberType == typeof(string) ||
                   (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(List<>));
        }

        internal static Type GetListElementType(this Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
                ? type.GetGenericArguments()[0]
                : null;
        }

        internal static bool IsSupportedListElementType(this Type type)
        {
            if (type == null) return false;
            if (type == typeof(string) || type.IsEnum) return true;
            if (type == typeof(bool) || type == typeof(char) ||
                type == typeof(byte) || type == typeof(sbyte) ||
                type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int) || type == typeof(uint) ||
                type == typeof(long) || type == typeof(ulong) ||
                type == typeof(float) || type == typeof(double) ||
                type == typeof(decimal)) return true;

            return type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                   type == typeof(Vector2Int) || type == typeof(Vector3Int) ||
                   type == typeof(Quaternion) || type == typeof(Color);
        }

        internal static bool ApplyListMember(this MemberInfo member, Object owner, string value)
        {
            var listType = member.GetFieldOrPropertyType();
            var elementType = listType.GetListElementType();
            if (!elementType.IsSupportedListElementType() || value.Length < 2 ||
                value[0] != '[' || value[value.Length - 1] != ']') return false;

            var list = (IList)Activator.CreateInstance(listType);
            var listContent = value.Substring(1, value.Length - 2);
            if (listContent.Length > 0)
            {
                var elements = listContent.Split(new[] { '|' }, StringSplitOptions.None);
                foreach (var element in elements)
                {
                    if (!TryParseListElement(elementType, element, out var parsedElement)) return false;
                    list.Add(parsedElement);
                }
            }

            return TryApplyMember(member, owner, list);
        }

        private static bool TryParseListElement(Type type, string value, out object parsedValue)
        {
            parsedValue = null;
            if (type == typeof(string))
            {
                parsedValue = value;
                return true;
            }

            if (type.IsEnum) return Enum.TryParse(type, value, out parsedValue);

            try
            {
                if (type == typeof(Vector2))
                {
                    var values = value.Trim('(', ')').Split(',');
                    float.TryParse(values[0].Trim(), out var x);
                    float.TryParse(values[1].Trim(), out var y);
                    parsedValue = new Vector2(x, y);
                }
                else if (type == typeof(Vector2Int))
                {
                    var values = value.Trim('(', ')').Split(',');
                    int.TryParse(values[0].Trim(), out var x);
                    int.TryParse(values[1].Trim(), out var y);
                    parsedValue = new Vector2Int(x, y);
                }
                else if (type == typeof(Vector3))
                {
                    var values = value.Trim('(', ')').Split(',');
                    float.TryParse(values[0].Trim(), out var x);
                    float.TryParse(values[1].Trim(), out var y);
                    float.TryParse(values[2].Trim(), out var z);
                    parsedValue = new Vector3(x, y, z);
                }
                else if (type == typeof(Vector3Int))
                {
                    var values = value.Trim('(', ')').Split(',');
                    int.TryParse(values[0].Trim(), out var x);
                    int.TryParse(values[1].Trim(), out var y);
                    int.TryParse(values[2].Trim(), out var z);
                    parsedValue = new Vector3Int(x, y, z);
                }
                else if (type == typeof(Vector4))
                {
                    var values = value.Trim('(', ')').Split(',');
                    float.TryParse(values[0].Trim(), out var x);
                    float.TryParse(values[1].Trim(), out var y);
                    float.TryParse(values[2].Trim(), out var z);
                    float.TryParse(values[3].Trim(), out var w);
                    parsedValue = new Vector4(x, y, z, w);
                }
                else if (type == typeof(Quaternion))
                {
                    var values = value.Trim('(', ')').Split(',');
                    float.TryParse(values[0].Trim(), out var x);
                    float.TryParse(values[1].Trim(), out var y);
                    float.TryParse(values[2].Trim(), out var z);
                    float.TryParse(values[3].Trim(), out var w);
                    parsedValue = new Quaternion(x, y, z, w);
                }
                else if (type == typeof(Color))
                {
                    var values = value.Split('(', ')')[1].Split(',');
                    float.TryParse(values[0].Trim(), out var r);
                    float.TryParse(values[1].Trim(), out var g);
                    float.TryParse(values[2].Trim(), out var b);
                    float.TryParse(values[3].Trim(), out var a);
                    parsedValue = new Color(r, g, b, a);
                }
                else if (IsSupportedListElementType(type))
                {
                    parsedValue = Convert.ChangeType(value, type, CultureInfo.CurrentCulture);
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch
            {
                parsedValue = null;
                return false;
            }
        }


        public static bool TryGetValue(this MemberInfo member, Object script, out object value)
        {
            var defaultValue = member.DeclaringType.GetDefaultValue();
            try
            {
                object rawValue;
                switch (member)
                {
                    case FieldInfo fieldInfo:
                        rawValue = fieldInfo.GetValue(script);
                        break;
                    case PropertyInfo propertyInfo:
                        rawValue = propertyInfo.GetValue(script);
                        break;
                    default:
                        value = defaultValue;
                        return false;
                }

                // 检查是否为枚举类型，如果是则转换为int
                if (rawValue != null && rawValue.GetType().IsEnum)
                {
                    value = Convert.ToInt32(rawValue);
                }
                else
                {
                    value = rawValue;
                }

                return true;
            }
            catch (Exception)
            {
                value = defaultValue;
                return false;
            }
        }


        public static Type GetFieldOrPropertyType(this MemberInfo member)
        {
            return member switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                _ => null
            };
        }

        public static bool ApplyValueTypeMember(this MemberInfo member, Object owner, string value)
        {
            var convertedValue = Convert.ChangeType(value, member.GetFieldOrPropertyType());
            return TryApplyMember(member, owner, convertedValue);
        }

        public static bool ApplyEnumValue(this MemberInfo member, Object owner, string value)
        {
            return Enum.TryParse(member.GetFieldOrPropertyType(), value, out var enumValue) &&
                   TryApplyMember(member, owner, enumValue);
        }

        public static bool ApplyStringValue(this MemberInfo member, Object owner, string value)
        {
            return TryApplyMember(member, owner, value);
        }

        public static bool ApplyVector2Member(this MemberInfo member, Object owner, string value)
        {
            var trimValue = value.Trim('(', ')');
            var values = trimValue.Split(',');
            float.TryParse(values[0].Trim(), out var x);
            float.TryParse(values[1].Trim(), out var y);
            return TryApplyMember(member, owner, new Vector2(x, y));
        }

        public static bool ApplyVector2IntMember(this MemberInfo member, Object owner, string value)
        {
            var trimValue = value.Trim('(', ')');
            var values = trimValue.Split(',');
            int.TryParse(values[0].Trim(), out var x);
            int.TryParse(values[1].Trim(), out var y);
            return TryApplyMember(member, owner, new Vector2Int(x, y));
        }

        public static bool ApplyVector3Member(this MemberInfo member, Object owner, string value)
        {
            var trimValue = value.Trim('(', ')');
            var values = trimValue.Split(',');
            float.TryParse(values[0].Trim(), out var x);
            float.TryParse(values[1].Trim(), out var y);
            float.TryParse(values[2].Trim(), out var z);
            return TryApplyMember(member, owner, new Vector3(x, y, z));
        }

        public static bool ApplyVector3IntMember(this MemberInfo member, Object owner, string value)
        {
            var trimValue = value.Trim('(', ')');
            var values = trimValue.Split(',');
            int.TryParse(values[0].Trim(), out var x);
            int.TryParse(values[1].Trim(), out var y);
            int.TryParse(values[2].Trim(), out var z);
            return TryApplyMember(member, owner, new Vector3Int(x, y, z));
        }

        public static bool ApplyVector4Member(this MemberInfo member, Object owner, string value)
        {
            var trimValue = value.Trim('(', ')');
            var values = trimValue.Split(',');
            float.TryParse(values[0].Trim(), out var x);
            float.TryParse(values[1].Trim(), out var y);
            float.TryParse(values[2].Trim(), out var z);
            float.TryParse(values[3].Trim(), out var w);
            return TryApplyMember(member, owner, new Vector4(x, y, z, w));
        }

        public static bool ApplyQuaternionMember(this MemberInfo member, Object owner, string value)
        {
            var trimValue = value.Trim('(', ')');
            var values = trimValue.Split(',');
            float.TryParse(values[0].Trim(), out var x);
            float.TryParse(values[1].Trim(), out var y);
            float.TryParse(values[2].Trim(), out var z);
            float.TryParse(values[3].Trim(), out var w);
            return TryApplyMember(member, owner, new Quaternion(x, y, z, w));
        }

        public static bool ApplyColorMember(this MemberInfo member, Object owner, string value)
        {
            var trimValue = value.Split('(', ')')[1];
            var values = trimValue.Split(',');
            float.TryParse(values[0].Trim(), out var r);
            float.TryParse(values[1].Trim(), out var g);
            float.TryParse(values[2].Trim(), out var b);
            float.TryParse(values[3].Trim(), out var a);
            return TryApplyMember(member, owner, new Color(r, g, b, a));
        }

        private static bool TryApplyMember(this MemberInfo member, Object owner, object value)
        {
            try
            {
                switch (member)
                {
                    case FieldInfo field:
                        field.SetValue(owner, value);
                        return true;
                    case PropertyInfo property:
                        property.SetValue(owner, value);
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"应用成员值失败：{e.Message}");
                return false;
            }
        }
    }
}
