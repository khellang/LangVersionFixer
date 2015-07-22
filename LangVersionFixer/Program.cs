using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace LangVersionFixer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var directory = args[0];

            XNamespace @namespace = "http://schemas.microsoft.com/developer/msbuild/2003";

            var langeVersionElement = new XElement(@namespace + "LangVersion") { Value = "5" };

            var filePaths = GetFilePaths(directory, "*.csproj");

            foreach (var filePath in filePaths)
            {
                var document = ReadDocument(filePath);

                var propertyGroups = document.Descendants(@namespace + "PropertyGroup").ToList();

                var globalPropertyGroup = propertyGroups.FirstOrDefault(x => !x.HasAttributes) ?? propertyGroups.First();

                globalPropertyGroup.Add(langeVersionElement);

                document.Descendants().Where(x => string.IsNullOrWhiteSpace(x.Value) && !x.HasElements).ToList().ForEach(x => x.RemoveNodes());

                document.Descendants().Where(x => (x.IsEmpty || string.IsNullOrWhiteSpace(x.Value)) && !x.HasAttributes && !x.HasElements).Remove();

                var settings = new XmlWriterSettings { Indent = true };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    document.Save(writer);
                }
            }
        }

        private static XDocument ReadDocument(string filePath)
        {
            using (var readStream = File.OpenRead(filePath))
            {
                return XDocument.Load(readStream);
            }
        }

        private static IEnumerable<string> GetFilePaths(string path, string searchPattern)
        {
            return Directory.EnumerateFiles(path, searchPattern, SearchOption.AllDirectories);
        }
    }
}
