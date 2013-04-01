// SharpDevelop samples
// Copyright (c) 2013, AlphaSierraPapa
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this list
//   of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice, this list
//   of conditions and the following disclaimer in the documentation and/or other materials
//   provided with the distribution.
//
// - Neither the name of the SharpDevelop team nor the names of its contributors may be used to
//   endorse or promote products derived from this software without specific prior written
//   permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS &AS IS& AND ANY EXPRESS
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor.Bookmarks;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Parser;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.WinForms;
using ICSharpCode.SharpDevelop.Workbench;
using ICSharpCode.SharpSnippetCompiler.Core;
using FileService = ICSharpCode.SharpDevelop.Workbench.FileService;
using StatusBarService = ICSharpCode.SharpSnippetCompiler.Core.StatusBarService;

namespace ICSharpCode.SharpSnippetCompiler
{
    public sealed class Program
    {
        App app;
        MainWindow mainWindow;
        
        [STAThread]
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Run(args);
        }
        
        void Run(string[] args)
        {
            SharpSnippetCompilerManager.Init();

            app = new App();
            SD.Services.AddService(typeof(IMessageLoop), new DispatcherMessageLoop(app.Dispatcher, SynchronizationContext.Current));

            mainWindow = new MainWindow();
            var workbench = new Workbench(mainWindow);
            InitializeWorkbench(workbench, new WorkbenchLayout());
            ViewModels.MainViewModel.AddInitialPads();
            
            SnippetCompilerProject.Load();
            IProject project = GetCurrentProject();
            ProjectService.CurrentProject = project;
            LoadFiles(project);
            
            try {
                app.Run(SD.Workbench.MainWindow);
            } finally {
                try {
                    // Save properties
                    //PropertyService.Save();
                } catch (Exception ex) {
                    MessageService.ShowException(ex, "Properties could not be saved.");
                }
            }
        }

        const string workbenchMemento = "WorkbenchMemento";
        const string activeContentState = "Workbench.ActiveContent";
        static void InitializeWorkbench(Workbench workbench, IWorkbenchLayout layout)
        {
            SD.Services.AddService(typeof(IWorkbench), workbench);

            SD.Services.AddService(typeof(IWinFormsService), new WinFormsService());
            SD.Services.AddService(typeof(IWinFormsToolbarService), new WinFormsToolbarService());
            SD.Services.AddService(typeof(IWinFormsMenuService), new WinFormsMenuService());
            SD.Services.AddService(typeof(IProjectService), new SDProjectService());
            SD.Services.AddService(typeof(IBuildService), new BuildService());
            SD.Services.AddService(typeof(IParserService), new ParserService());
            SD.Services.AddService(typeof(IFileService), new FileService());
            SD.Services.AddService(typeof(IStatusBarService), new StatusBarService());
            SD.Services.AddService(typeof(IMSBuildEngine), new MSBuildEngine());
            SD.Services.AddService(typeof(IDisplayBindingService), new DisplayBindingService());
            SD.Services.AddService(typeof(IBookmarkManager), new BookmarkManager());
            
            LanguageService.ValidateLanguage();

            TaskService.Initialize();
            CustomToolsService.Initialize();

            workbench.Initialize();
            workbench.SetMemento(PropertyService.NestedProperties(workbenchMemento));
            workbench.WorkbenchLayout = layout;

            var applicationStateInfoService = SD.GetService<ApplicationStateInfoService>();
            if (applicationStateInfoService != null)
            {
                applicationStateInfoService.RegisterStateGetter(activeContentState, delegate { return SD.Workbench.ActiveContent; });
            }

            WorkbenchSingleton.OnWorkbenchCreated();

            // initialize workbench-dependent services:
            NavigationService.InitializeService();

            workbench.ActiveContentChanged += delegate
            {
                //Debug.WriteLine("ActiveContentChanged to " + workbench.ActiveContent);
                LoggingService.Debug("ActiveContentChanged to " + workbench.ActiveContent);
            };
            workbench.ActiveViewContentChanged += delegate
            {
                //Debug.WriteLine("ActiveViewContentChanged to " + workbench.ActiveViewContent);
                LoggingService.Debug("ActiveViewContentChanged to " + workbench.ActiveViewContent);
            };
            workbench.ActiveWorkbenchWindowChanged += delegate
            {
                //Debug.WriteLine("ActiveWorkbenchWindowChanged to " + workbench.ActiveWorkbenchWindow);
                LoggingService.Debug("ActiveWorkbenchWindowChanged to " + workbench.ActiveWorkbenchWindow);
            };
        }
        

        void LoadFiles(IProject project)
        {
            foreach (ProjectItem item in project.Items) {
                var fileItem = item as FileProjectItem;
                if (fileItem != null && File.Exists(item.FileName)) {
                    ViewModels.MainViewModel.LoadFile(item.FileName);
                }
            }
        }
        
        IProject GetCurrentProject()
        {
            return ProjectService.OpenSolution.Projects.FirstOrDefault();
        }
    }
}
