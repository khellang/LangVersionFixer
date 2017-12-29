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
                Console.WriteLine("Usage: LangVersionFixer <folder-with-csprojs> <lang-version-number> [<clean-document>]");
                return -1;
            }

            var directoryPath = args[0];
            var langVersion = args[1];
            var cleanDocument = true;

            if (args.Length > 2)
            {
                if (!bool.TryParse(args[2], out cleanDocument))
                {
                    using (ConsoleColorScope.Start(ConsoleColor.Red))
                    {
                        Console.WriteLine($"Could not convert '{args[2]}' to boolean.");
                        return -1;
                    }
                }
            }

            if (!double.TryParse(langVersion, out var _))
            {
                if (!langVersion.Equals("default", StringComparison.OrdinalIgnoreCase) &&
                    !langVersion.Equals("latest", StringComparison.OrdinalIgnoreCase))
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

            directory.FixLangVersion(langVersion, cleanDocument);

            using (ConsoleColorScope.Start(ConsoleColor.Green))
            {
                Console.WriteLine($"Successfully set LangVersion to {langVersion} in all projects.");
                return 0;
            }
        }

        private static void FixLangVersion(this DirectoryInfo directory, string langVersion, bool cleanDocument = true)
        {
            XNamespace @namespace = "http://schemas.microsoft.com/developer/msbuild/2003";

            var files = directory.EnumerateFiles("*.csproj", SearchOption.AllDirectories);

            Console.WriteLine($"Setting LangVersion to {langVersion} in all projects under {directory.FullName}...");

            foreach (var file in files)
            {
                Console.WriteLine($"Processing {file.Name}.");

                var document = file.ReadXmlDocument();

                var isNetCore = document.Root?.Attribute("Sdk") != null;

                document.AddLangVersionElement(isNetCore ? "" : @namespace, langVersion);

                if (cleanDocument)
                {
                    document.CleanUpEmptyElements();
                }

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = document.Declaration == null
                };

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

            var globalPropertyGroup = emptyPropertyGroup ?? propertyGroups.FirstOrDefault();

            if (globalPropertyGroup == null)
            {
                document.Add(globalPropertyGroup = new XElement(@namespace + "PropertyGroup"));
            }

            var newElement = new XElement(@namespace + "LangVersion")
            {
                Value = langVersion
            };

            globalPropertyGroup.Add(newElement);
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
