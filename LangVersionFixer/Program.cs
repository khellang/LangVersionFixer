using System;
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

            var directoryPath = args[0];
			var langVersion = args[1];

			int parsedLangVersion;
			if (!int.TryParse(langVersion, out parsedLangVersion))
			{
			    if (!langVersion.Equals("default", StringComparison.OrdinalIgnoreCase))
                {
                    using (ConsoleColorScope.Start(ConsoleColor.Red))
                    {
                        Console.WriteLine($"'{langVersion}' is not a valid LangVersion parameter.");
                        return;
                    }
                }
			}

            var directory = new DirectoryInfo(directoryPath);

            if (!directory.Exists)
            {
                using (ConsoleColorScope.Start(ConsoleColor.Red))
                {
                    Console.WriteLine($"Folder '{directoryPath}' does not exist.");
                    return;
                }
            }

            directory.FixLangVersion(langVersion);
        }

        private static void FixLangVersion(this DirectoryInfo directory, string langVersion)
        {
            XNamespace @namespace = "http://schemas.microsoft.com/developer/msbuild/2003";

            var langVersionElement = new XElement(@namespace + "LangVersion") { Value = langVersion };

            var files = directory.EnumerateFiles("*.csproj", SearchOption.AllDirectories);

            var settings = new XmlWriterSettings { Indent = true };

            foreach (var file in files)
            {
                var document = file.ReadXmlDocument();

                document.AddLangVersionElement(@namespace, langVersionElement);

                document.CleanUpEmptyElements();

                using (var writer = XmlWriter.Create(file.FullName, settings))
                {
                    document.Save(writer);
                }
            }
        }

        private static void AddLangVersionElement(this XContainer document, XNamespace @namespace, XElement langVersionElement)
        {
            var propertyGroups = document
                .Descendants(@namespace + "PropertyGroup")
                .ToList();

            var emptyPropertyGroup = propertyGroups.FirstOrDefault(x => !x.HasAttributes);

            var globalPropertyGroup = emptyPropertyGroup ?? propertyGroups.First();

            globalPropertyGroup.Add(langVersionElement);
        }

        private static void CleanUpEmptyElements(this XContainer document)
        {
            document.Descendants().Where(element => element.IsEmpty()).Remove();
        }

        private static bool IsEmpty(this XElement element)
        {
            return element.HasNoValue() && !element.HasAttributes && !element.HasElements;
        }

        private static bool HasNoValue(this XElement element)
        {
            return element.IsEmpty || string.IsNullOrWhiteSpace(element.Value);
        }

        private static XDocument ReadXmlDocument(this FileSystemInfo file)
        {
            using (var readStream = File.OpenRead(file.FullName))
            {
                return XDocument.Load(readStream);
            }
        }

        private class ConsoleColorScope : IDisposable
        {
            private ConsoleColorScope(ConsoleColor color)
            {
                Color = color;
            }

            private ConsoleColor Color { get; }

            public static IDisposable Start(ConsoleColor foregroundColor)
            {
                var scope = new ConsoleColorScope(Console.ForegroundColor);

                Console.ForegroundColor = foregroundColor;

                return scope;
            }

            public void Dispose()
            {
                Console.ForegroundColor = Color;
            }
        }
    }
}
