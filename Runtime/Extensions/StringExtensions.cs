namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    public static class StringExtensions
    {
        public static PersistenceFormat GetPersistenceFormat(this string memberContent)
        {
            if (memberContent.Contains("="))
            {
                return PersistenceFormat.Text;
            }
            else if (memberContent.Contains(":"))
            {
                return PersistenceFormat.Json;
            }
            else if (memberContent.StartsWith("<") && memberContent.EndsWith(">"))
            {
                return PersistenceFormat.Xml;
            }

            return PersistenceFormat.Unknown;
        }
    }
}