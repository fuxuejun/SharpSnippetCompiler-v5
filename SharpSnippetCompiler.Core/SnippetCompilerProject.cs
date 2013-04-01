// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
// </file>

using System;
using System.IO;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Project.Converter;

namespace ICSharpCode.SharpSnippetCompiler.Core
{
    public class SnippetCompilerProject : CompilableProject
    {
        public const string DefaultTargetsFile = @"$(MSBuildToolsPath)\Microsoft.CSharp.targets";

        private static readonly string DefaultSnippetSource = "using System;\r\n\r\n" +
                                                              "public class Program\r\n" +
                                                              "{\r\n" +
                                                              "\t[STAThread]\r\n" +
                                                              "\tstatic void Main(string[] args)\r\n" +
                                                              "\t{\r\n" +
                                                              "\t}\r\n" +
                                                              "}";

        public SnippetCompilerProject(ProjectLoadInformation loadInformation)
            : base(loadInformation)
        {
        }

        private SnippetCompilerProject()
            : this(GetProjectCreateInfo())
        {
        }

        private SnippetCompilerProject(ProjectCreateInformation createInfo)
            : base(createInfo)
        {
            AddImport(DefaultTargetsFile, null);

            SetProperty("Debug", null, "CheckForOverflowUnderflow", "True",
                        PropertyStorageLocations.ConfigurationSpecific, true);
            SetProperty("Release", null, "CheckForOverflowUnderflow", "False",
                        PropertyStorageLocations.ConfigurationSpecific, true);

            SetProperty("Debug", null, "DefineConstants", "DEBUG;TRACE", PropertyStorageLocations.ConfigurationSpecific,
                        false);
            SetProperty("Release", null, "DefineConstants", "TRACE", PropertyStorageLocations.ConfigurationSpecific,
                        false);

            SetProperty("Platform", null, "PlatformTarget", "x86", PropertyStorageLocations.ConfigurationSpecific, false);
        }

        public static string SnippetFileName
        {
            get { return GetFullFileName("Snippet.cs"); }
        }

        public static string SnippetProjectFileName
        {
            get { return GetFullFileName("Snippet.csproj"); }
        }

        public static string SnippetSolutionFileName
        {
            get { return GetFullFileName("Snippet.sln"); }
        }

        public override CompilerVersion CurrentCompilerVersion
        {
            get { return CompilerVersion.MSBuild40; }
        }

        public override TargetFramework CurrentTargetFramework
        {
            get { return TargetFramework.Net40; }
        }

        public override string Language
        {
            get { return "C#"; }
        }

        public override ItemType GetDefaultItemType(string fileName)
        {
            if (string.Equals(Path.GetExtension(fileName), ".cs", StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.Compile;
            }
            else
            {
                return base.GetDefaultItemType(fileName);
            }
        }

        public static void Load()
        {
            CreateSnippetProject();
            CreateSnippetFile();
            
            SD.ProjectService.OpenSolutionOrProject(new FileName(SnippetSolutionFileName));
        }

        private static ISolution solution;
        private static ProjectCreateInformation GetProjectCreateInfo()
        {
            solution = new Solution(new FileName(SnippetSolutionFileName),
                                         new ProjectChangeWatcher(SnippetSolutionFileName),
                                         SD.FileService);

            solution.ActiveConfiguration = new ConfigurationAndPlatform("DEBUG", "x86");

            return new ProjectCreateInformation(solution, new FileName(SnippetProjectFileName))
                {
                    TypeGuid = ProjectTypeGuids.CSharp
                };
        }

        /// <summary>
        ///     Loads the snippet project or creates one if it does not already exist.
        /// </summary>
        private static void CreateSnippetProject()
        {
            string fileName = SnippetProjectFileName;
            if (!File.Exists(fileName))
            {
                // Add single snippet file to project.
                var project = new SnippetCompilerProject();
                var item = new FileProjectItem(project, ItemType.Compile, "Snippet.cs");
                ProjectService.AddProjectItem(project, item);

                project.Save();

                solution.Items.Add(project);
                solution.Save();
            }
        }

        /// <summary>
        ///     Loads the snippet file or creates one if it does not already exist.
        /// </summary>
        private static void CreateSnippetFile()
        {
            string fileName = SnippetFileName;
            if (!File.Exists(fileName))
            {
                LoggingService.Info("Creating Snippet.cs file: " + fileName);
                using (StreamWriter snippetFile = File.CreateText(fileName))
                {
                    snippetFile.Write(DefaultSnippetSource);
                }
            }
        }

        /// <summary>
        ///     All snippet compiler files are stored loaded from the config directory so this
        ///     method prefixes the filename with this path.
        /// </summary>
        public static string GetFullFileName(string fileName)
        {
            return Path.Combine(PropertyService.ConfigDirectory, fileName);
        }
    }
}