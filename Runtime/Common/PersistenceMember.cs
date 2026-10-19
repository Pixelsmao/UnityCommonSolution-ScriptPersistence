using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
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
            ownerName = owner.name;
            this.member = member;
            tooltip = member.GetTooltip();
            memberName = member.Name;
            if (member.TryGetValue(owner, out var value)) memberValue = value;
        }

        internal static bool TryGetTextMemberName(string memberContent, out string memberName)
        {
            memberName = string.Empty;
            if (string.IsNullOrEmpty(memberContent)) return false;

            var keyValuePair = Regex.Replace(Regex.Replace(memberContent, @"\s+", string.Empty), @"<[^>]*>", string.Empty);
            var separatorIndex = keyValuePair.IndexOf('=');
            if (separatorIndex <= 0) return false;

            memberName = keyValuePair.Substring(0, separatorIndex);
            return memberName.Length > 0;
        }

        public bool ParseMemberValue(string memberContent)
        {
            memberContent = Regex.Replace(memberContent, @"\s+", string.Empty);
            switch (memberContent.GetPersistenceFormat())
            {
                case PersistenceFormat.Unknown:
                    Debug.LogWarning($"【{memberContent}】 不是一个有效的成员内容.");
                    return false;
                case PersistenceFormat.Text:
                    var keyValuePair = Regex.Replace(memberContent, @"<[^>]*>", string.Empty);
                    var separatorIndex = keyValuePair.IndexOf('=');
                    if (separatorIndex <= 0) return false;
                    var key = keyValuePair.Substring(0, separatorIndex);
                    var unparsedValue = keyValuePair.Substring(separatorIndex + 1);
                    return comparer.Equals(key, memberName) && TryApplyMember(unparsedValue);
                case PersistenceFormat.Json:
                case PersistenceFormat.Xml:
                case PersistenceFormat.Excel:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool TryApplyMember(string unparsedValue)
        {
            try
            {
                var type = member.GetFieldOrPropertyType();
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
                case PersistenceFormat.Xml:
                case PersistenceFormat.Excel:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}
