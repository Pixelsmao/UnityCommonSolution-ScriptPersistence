using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    public class PersistenceMember : PersistenceCustomAttributeProvider
    {
        public Object owner { get; }
        public string ownerName { get; }
        public MemberInfo member { get; }
        public string tooltip { get; }
        public string memberName { get; }
        public object memberValue { get; }
        private readonly StringComparer comparer = StringComparer.Ordinal;

        public PersistenceMember(Object owner, MemberInfo member)
        {
            this.owner = owner;
            this.ownerName = owner.name;
            this.member = member;
            tooltip = member.GetTooltip();
            memberName = member.Name;
            if (member.TryGetValue(owner, out var value))
            {
                memberValue = value;
            }
        }

        public bool ParseMemberValue(string memberContent)
        {
            //去除成员内容中的空白字符
            memberContent = Regex.Replace(memberContent, @"\s+", "");
            switch (memberContent.GetPersistenceFormat())
            {
                case PersistenceFormat.Unknown:
                    Debug.LogWarning($"【{memberContent}】 不是一个有效的成员内容.");

                    return false;
                case PersistenceFormat.Text:
                    // 剔除注释
                    var keyValuePair = Regex.Replace(memberContent, @"<[^>]*>", "");
                    var key = keyValuePair.Split("=")[0];
                    var unparsedValue = keyValuePair.Split("=")[1];
                    return comparer.Equals(key, memberName) && TryApplyMember(unparsedValue);
                case PersistenceFormat.Json:
                    break;
                case PersistenceFormat.Xml:
                    break;
                case PersistenceFormat.Excel:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return false;
        }

        private bool TryApplyMember(string unparsedValue)
        {
            try
            {
                var type = member.GetValueMemberType();
                if (type.IsEnum) return member.ApplyEnumValue(owner, unparsedValue);
                if (type == typeof(Vector2)) return member.ApplyVector2Member(owner, unparsedValue);
                if (type == typeof(Vector3)) return member.ApplyVector3Member(owner, unparsedValue);
                if (type == typeof(Vector4)) return member.ApplyVector4Member(owner, unparsedValue);
                if (type == typeof(Vector2Int)) return member.ApplyVector2IntMember(owner, unparsedValue);
                if (type == typeof(Vector3Int)) return member.ApplyVector3IntMember(owner, unparsedValue);
                if (type == typeof(Quaternion)) return member.ApplyQuaternionMember(owner, unparsedValue);
                if (type == typeof(Color)) return member.ApplyColorMember(owner, unparsedValue);
                if (type == typeof(string)) return member.ApplyStringValue(owner, unparsedValue);
                if (type.IsValueType) return member.ApplyValueTypeMember(owner, unparsedValue);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"持久化脚本【{ownerName}.{owner.GetType().Name}】成员【{memberName}】赋值失败.{e.Message}");
                return false;
            }
        }


        public override string ToString()
        {
            return ToString(PersistenceFormat.Text);
        }

        public string ToString(PersistenceFormat format)
        {
            switch (format)
            {
                case PersistenceFormat.Text:
                    var tooltipText = tooltip == string.Empty ? string.Empty : $"<{tooltip}>";
                    return $"{tooltipText}{memberName}={memberValue}";
                case PersistenceFormat.Json:
                    throw new NotImplementedException();
                case PersistenceFormat.Xml:
                    throw new NotImplementedException();
                case PersistenceFormat.Excel:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}