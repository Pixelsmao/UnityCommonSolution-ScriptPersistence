using System.Reflection;
using UnityEngine;

namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    public static class ICustomAttributeProviderExtensions
    {
        public static string GetTooltip(this ICustomAttributeProvider fieldInfo)
        {
            var attributes = fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false);
            var tooltip = attributes.Length > 0 ? ((TooltipAttribute)attributes[0]).tooltip : string.Empty;
            return string.IsNullOrEmpty(tooltip) ? string.Empty : tooltip;
        }

        public static bool HasTooltip(this ICustomAttributeProvider fieldInfo)
        {
            var attributes = fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false);
            return attributes.Length > 0;
        }
    }
}