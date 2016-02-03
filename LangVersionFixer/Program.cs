using System;
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
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: LangVersionFixer <folder-with-csprojs> <lang-version-number>");
                return;
            }

            var directory = args[0];

			string langVersion = args[1];
			int langVersionInt;
			if (!int.TryParse(langVersion, out langVersionInt) && !langVersion.Equals("default"))
			{
				Console.WriteLine($"'{args[1]}' is not a valid LangVersion parameter.");
				return;
			}

			XNamespace @namespace = "http://schemas.microsoft.com/developer/msbuild/2003";

            var langVersionElement = new XElement(@namespace + "LangVersion") { Value = langVersion };

            var filePaths = GetFilePaths(directory, "*.csproj");

            foreach (var filePath in filePaths)
            {
                var document = ReadDocument(filePath);

                var propertyGroups = document.Descendants(@namespace + "PropertyGroup").ToList();

                var globalPropertyGroup = propertyGroups.FirstOrDefault(x => !x.HasAttributes) ?? propertyGroups.First();

                globalPropertyGroup.Add(langVersionElement);

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
