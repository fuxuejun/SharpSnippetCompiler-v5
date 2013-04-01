// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Input;
using ICSharpCode.Core;
using ICSharpCode.Core.WinForms;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Commands;
using ICSharpCode.SharpDevelop.Logging;
using ICSharpCode.SharpDevelop.WinForms;

namespace ICSharpCode.SharpSnippetCompiler.Core
{
    public sealed class SharpSnippetCompilerManager
    {
        private SharpSnippetCompilerManager()
        {
        }

        public static void Init()
        {
            var manager = new SharpSnippetCompilerManager();
            Assembly exe = manager.GetType().Assembly;

            string rootPath = Path.GetDirectoryName(exe.Location);
            string configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                  "SharpSnippetCompiler");
            string dataDirectory = Path.Combine(rootPath, "data");

            var container = new SharpDevelopServiceContainer(ServiceSingleton.FallbackServiceProvider);
            ServiceSingleton.ServiceProvider = container;

            var startup = new CoreStartup("SharpSnippetCompiler");
            startup.ConfigDirectory = configDirectory;
            startup.DataDirectory = dataDirectory;
            startup.PropertiesName = "SharpSnippetCompiler";
            FileUtility.ApplicationRootPath = "SharpSnippetCompiler";
            startup.StartCoreServices();

            SD.ResourceService.RegisterNeutralStrings(new ResourceManager("ICSharpCode.SharpSnippetCompiler.Core.Resources.StringResources", exe));
            SD.ResourceService.RegisterNeutralImages(new ResourceManager("ICSharpCode.SharpSnippetCompiler.Core.Resources.BitmapResources", exe));

            CommandWrapper.LinkCommandCreator = (link => new LinkCommand(link));
            CommandWrapper.RegisterConditionRequerySuggestedHandler = (eh => CommandManager.RequerySuggested += eh);
            CommandWrapper.UnregisterConditionRequerySuggestedHandler = (eh => CommandManager.RequerySuggested -= eh);
            StringParser.RegisterStringTagProvider(new SharpDevelopStringTagProvider());

            string addInFolder = Path.Combine(rootPath, "AddIns");
            startup.AddAddInsFromDirectory(addInFolder);
            startup.RunInitialization();
        }
    }
}