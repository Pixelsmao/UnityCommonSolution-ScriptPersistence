using System;
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

            if (memberType == null)return false;
            return memberType.IsValueType || memberType == typeof(string);
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