using System;
using System.Collections;
using System.Collections.Generic;
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
                var listElementType = type.GetListElementType();
                if (listElementType != null)
                {
                    if (!listElementType.IsSupportedListElementType()) return false;
                    return member.ApplyListMember(owner, unparsedValue);
                }

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
            if (!TryFormat(format, out var content)) return string.Empty;
            return content;
        }

        internal bool TryFormat(PersistenceFormat format, out string content)
        {
            content = string.Empty;
            switch (format)
            {
                case PersistenceFormat.Text:
                    var tooltipText = tooltip == string.Empty ? string.Empty : $"<{tooltip}>";
                    var elementType = member.GetFieldOrPropertyType().GetListElementType();
                    if (elementType == null)
                    {
                        content = $"{tooltipText}{memberName}={memberValue}";
                        return true;
                    }

                    if (!elementType.IsSupportedListElementType())
                    {
                        Debug.LogWarning($"持久化成员【{ownerName}.{memberName}】的列表元素类型【{elementType.FullName}】不受支持，已跳过该字段。");
                        return false;
                    }

                    if (!TryFormatListValue(elementType, out var listValue)) return false;
                    content = $"{tooltipText}{memberName}={listValue}";
                    return true;
                case PersistenceFormat.Json:
                case PersistenceFormat.Xml:
                case PersistenceFormat.Excel:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private bool TryFormatListValue(Type elementType, out string listValue)
        {
            listValue = string.Empty;
            if (memberValue == null)
            {
                listValue = "[]";
                return true;
            }

            if (!(memberValue is IList values)) return false;
            if (elementType == typeof(string) && values.Count == 1 && string.IsNullOrWhiteSpace(values[0] as string))
            {
                Debug.LogWarning($"持久化成员【{ownerName}.{memberName}】仅包含一个空字符串，无法与空列表区分，已跳过该字段。");
                return false;
            }

            var serializedValues = new List<string>(values.Count);
            foreach (var value in values)
            {
                if (value == null && elementType != typeof(string)) return false;

                var serializedValue = value == null ? string.Empty : value.ToString();

                if (serializedValue.Contains("|"))
                {
                    Debug.LogWarning($"持久化成员【{ownerName}.{memberName}】的列表元素包含分隔符【|】，无法无损保存，已跳过该字段。");
                    return false;
                }

                serializedValues.Add(serializedValue);
            }

            listValue = $"[{string.Join("|", serializedValues)}]";
            return true;
        }
    }
}
