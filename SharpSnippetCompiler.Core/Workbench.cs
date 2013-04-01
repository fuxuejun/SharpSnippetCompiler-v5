// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Workbench;

namespace ICSharpCode.SharpSnippetCompiler.Core
{
    public class Workbench : Window,IWorkbench, IWin32Window
    {
        private static readonly string viewContentPath = "/SharpDevelop/Workbench/Pads";

        private readonly Window mainWindow;
        private readonly List<PadDescriptor> padDescriptors = new List<PadDescriptor>();
        private readonly StatusBarService statusBarService = new StatusBarService();
        private readonly List<IViewContent> views = new List<IViewContent>();
        
        public Workbench(Window mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public IWorkbenchLayout WorkbenchLayout { get; set; }
        public ISynchronizeInvoke SynchronizingObject { get; private set; }

        public IStatusBarService StatusBar
        {
            get { return statusBarService; }
        }

        public event EventHandler ActiveWorkbenchWindowChanged;
        public event EventHandler ActiveViewContentChanged;
        public event EventHandler ActiveContentChanged;

        event EventHandler<ViewContentEventArgs> IWorkbench.ViewOpened
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventHandler<ViewContentEventArgs> IWorkbench.ViewClosed
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public string Title
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ICollection<IViewContent> ViewContentCollection
        {
            get { return views; }
        }

        public ICollection<IViewContent> PrimaryViewContents
        {
            get { return views.AsReadOnly(); }
        }

        public IList<IWorkbenchWindow> WorkbenchWindowCollection
        {
            get { throw new NotImplementedException(); }
        }

        public IList<PadDescriptor> PadContentCollection
        {
            get { return padDescriptors; }
        }

        public IWorkbenchWindow ActiveWorkbenchWindow
        {
            get { throw new NotImplementedException(); }
        }

        public IViewContent ActiveViewContent { get; set; }

        public IServiceProvider ActiveContent { get; set; }

        public bool IsActiveWindow
        {
            get { return true; }
        }

        public string CurrentLayoutConfiguration { get; set; }

        public void ShowView(IViewContent content)
        {
            views.Add(content);
            OnViewOpened(new ViewContentEventArgs(content));
        }

        public void ShowView(IViewContent content, bool switchToOpenedView)
        {
            return;
        }

        public void ActivatePad(PadDescriptor content)
        {
            return;
        }

        public PadDescriptor GetPad(Type type)
        {
            foreach (PadDescriptor pad in padDescriptors)
            {
                if (pad.Class == type.FullName)
                {
                    return pad;
                }
            }
            return null;
        }

        public void CloseAllViews()
        {
            throw new NotImplementedException();
        }

        public bool CloseAllSolutionViews(bool force)
        {
            throw new NotImplementedException();
        }

        public Properties CreateMemento()
        {
            throw new NotImplementedException();
        }

        public void SetMemento(Properties memento)
        {
            Console.WriteLine("Workbench.SetMemento not implemented");
        }

        public void Initialize()
        {
            SynchronizingObject = new WpfSynchronizeInvoke(mainWindow.Dispatcher);
            try
            {
                List<PadDescriptor> contents =
                    AddInTree.GetTreeNode(viewContentPath).BuildChildItems<PadDescriptor>(this);
                foreach (PadDescriptor content in contents)
                {
                    if (content != null)
                    {
                        padDescriptors.Add(content);
                    }
                }
            }
            catch (TreePathNotFoundException)
            {
            }
        }

        public IWin32Window MainWin32Window
        {
            get { throw new NotImplementedException(); }
        }

        public Window MainWindow
        {
            get { return mainWindow; }
        }

        public bool FullScreen { get; set; }
        public event EventHandler ViewOpened;
        public event EventHandler ViewClosed;

        public void ShowPad(PadDescriptor content)
        {
            throw new NotImplementedException();
        }

        public void UnloadPad(PadDescriptor content)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnActiveWorkbenchWindowChanged(EventArgs e)
        {
            if (ActiveWorkbenchWindowChanged != null)
            {
                ActiveWorkbenchWindowChanged(this, e);
            }
        }

        protected virtual void OnActiveViewContentChanged(EventArgs e)
        {
            if (ActiveViewContentChanged != null)
            {
                ActiveViewContentChanged(this, e);
            }
        }

        protected virtual void OnActiveContentChanged(EventArgs e)
        {
            if (ActiveContentChanged != null)
            {
                ActiveContentChanged(this, e);
            }
        }

        protected virtual void OnViewOpened(ViewContentEventArgs e)
        {
            if (ViewOpened != null)
            {
                ViewOpened(this, e);
            }
        }

        protected virtual void OnViewClosed(ViewContentEventArgs e)
        {
            if (ViewClosed != null)
            {
                ViewClosed(this, e);
            }
        }

        public bool CloseAllSolutionViews()
        {
            return true;
        }

        public IntPtr Handle
        {
            get
            {
                var wnd = PresentationSource.FromVisual(this) as System.Windows.Interop.IWin32Window;
                if (wnd != null)
                    return wnd.Handle;
                else
                    return IntPtr.Zero;
            }
        }
    }
}