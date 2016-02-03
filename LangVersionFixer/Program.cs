using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace LangVersionFixer
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: LangVersionFixer <folder-with-csprojs> <lang-version-number>");
                return -1;
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
                        return -1;
                    }
                }
			}

            var directory = new DirectoryInfo(directoryPath);

            if (!directory.Exists)
            {
                using (ConsoleColorScope.Start(ConsoleColor.Red))
                {
                    Console.WriteLine($"Folder '{directoryPath}' does not exist.");
                    return -1;
                }
            }

            directory.FixLangVersion(langVersion);

            using (ConsoleColorScope.Start(ConsoleColor.Green))
            {
                Console.WriteLine($"Successfully set LangVersion to {langVersion} in all projects.");
                return 0;
            }
        }

        private static void FixLangVersion(this DirectoryInfo directory, string langVersion)
        {
            XNamespace @namespace = "http://schemas.microsoft.com/developer/msbuild/2003";

            var files = directory.EnumerateFiles("*.csproj", SearchOption.AllDirectories);

            var settings = new XmlWriterSettings { Indent = true };

            Console.WriteLine($"Setting LangVersion to {langVersion} in all projects under {directory.FullName}...");

            foreach (var file in files)
            {
                var document = file.ReadXmlDocument();

                Console.WriteLine($"Processed {file.Name}.");

                document.AddLangVersionElement(@namespace, langVersion);

                document.CleanUpEmptyElements();

                using (var writer = XmlWriter.Create(file.FullName, settings))
                {
                    document.Save(writer);
                }
            }
        }

        private static void AddLangVersionElement(this XContainer document, XNamespace @namespace, string langVersion)
        {
            var propertyGroups = document
                .Descendants(@namespace + "PropertyGroup")
                .ToList();

            var langVersionElement = propertyGroups.Descendants(@namespace + "LangVersion").FirstOrDefault();

            if (langVersionElement != null)
            {
                // If we already have an element, just set the value.
                langVersionElement.SetValue(langVersion);
                return;
            }

            var emptyPropertyGroup = propertyGroups.FirstOrDefault(x => !x.HasAttributes);

            var globalPropertyGroup = emptyPropertyGroup ?? propertyGroups.First();

            globalPropertyGroup.Add(new XElement(@namespace + "LangVersion") { Value = langVersion });
        }

        private static void CleanUpEmptyElements(this XContainer document)
        {
            // Collapse start and end tags
            document.Descendants().Where(element => string.IsNullOrWhiteSpace(element.Value) && !element.HasElements).ToList().ForEach(x => x.RemoveNodes());

            // Remove empty tags
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
