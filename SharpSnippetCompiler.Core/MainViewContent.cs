// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
// </file>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.Core;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.Bookmarks;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Workbench;
using ICSharpCode.SharpSnippetCompiler.Core.Completion;

namespace ICSharpCode.SharpSnippetCompiler.Core
{
    public class MainViewContent : IViewContent, ITextEditorProvider, IPositionable
    {
        public delegate string GetTextHelper();

        CompletionWindow completionWindow;
        OverloadInsightWindow insightWindow;

        private readonly SharpSnippetTextEditorAdapter adapter;
        private readonly IconBarManager iconBarManager;
        private readonly TextEditor textEditor;
        private SnippetFile file;
        private CodeEditor codeEditor;
        public MainViewContent(string fileName, IWorkbenchWindow workbenchWindow)
        {
            codeEditor = new CodeEditor();

            textEditor = new TextEditor();
            textEditor.FontFamily = new FontFamily("Consolas");

            textEditor.TextArea.TextEntering += TextAreaOnTextEntering;
            textEditor.TextArea.TextEntered += TextAreaOnTextEntered;
            textEditor.ShowLineNumbers = true;


            var ctrlSpace = new RoutedCommand();
            ctrlSpace.InputGestures.Add(new KeyGesture(Key.Space, ModifierKeys.Control));
            var cb = new CommandBinding(ctrlSpace, OnCtrlSpaceCommand);
            // this.CommandBindings.Add(cb);

            adapter = new SharpSnippetTextEditorAdapter(textEditor);
            this.WorkbenchWindow = workbenchWindow;
            textEditor.TextArea.TextView.Services.AddService(typeof (ITextEditor), adapter);
            LoadFile(fileName);

            iconBarManager = new IconBarManager();
            textEditor.TextArea.LeftMargins.Insert(0, new IconBarMargin(iconBarManager));

            var textMarkerService = new TextMarkerService(textEditor.Document);
            textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
            textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
            textEditor.TextArea.TextView.Services.AddService(typeof (ITextMarkerService), textMarkerService);
            textEditor.TextArea.TextView.Services.AddService(typeof (IBookmarkMargin), iconBarManager);
        }

        public Control Control
        {
            get { return textEditor; }
        }

        #region Code Completion
        private void TextAreaOnTextEntered(object sender, TextCompositionEventArgs textCompositionEventArgs)
        {
            ShowCompletion(textCompositionEventArgs.Text, false);
        }

        private void OnCtrlSpaceCommand(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            ShowCompletion(null, true);
        }

        private void ShowCompletion(string enteredText, bool controlSpace)
        {
            if (!controlSpace)
                Debug.WriteLine("Code Completion: TextEntered: " + enteredText);
            else
                Debug.WriteLine("Code Completion: Ctrl+Space");

            //only process csharp files
            if (String.IsNullOrEmpty(textEditor.Document.FileName))
                return;
            var fileExtension = Path.GetExtension(textEditor.Document.FileName);
            fileExtension = fileExtension != null ? fileExtension.ToLower() : null;
            //check file extension to be a c# file (.cs, .csx, etc.)
            if (fileExtension == null || (!fileExtension.StartsWith(".cs")))
                return;

            if (completionWindow == null)
            {
                CodeCompletionResult results = null;
                try
                {
                    results = CSharpCompletion.GetCompletions(textEditor.Document, textEditor.CaretOffset, controlSpace);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("Error in getting completion: " + exception);
                }
                if (results == null)
                    return;

                if (insightWindow == null && results.OverloadProvider != null)
                {
                    insightWindow = new OverloadInsightWindow(textEditor.TextArea);
                    insightWindow.Provider = results.OverloadProvider;
                    insightWindow.Show();
                    insightWindow.Closed += (o, args) => insightWindow = null;
                    return;
                }

                if (completionWindow == null && results != null && results.CompletionData.Any())
                {
                    // Open code completion after the user has pressed dot:
                    completionWindow = new CompletionWindow(textEditor.TextArea);
                    completionWindow.CloseWhenCaretAtBeginning = controlSpace;
                    completionWindow.StartOffset -= results.TriggerWordLength;
                    //completionWindow.EndOffset -= results.TriggerWordLength;

                    IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                    foreach (var completion in results.CompletionData.OrderBy(item => item.Text))
                    {
                        data.Add(completion);
                    }
                    if (results.TriggerWordLength > 0)
                    {
                        //completionWindow.CompletionList.IsFiltering = false;
                        completionWindow.CompletionList.SelectItem(results.TriggerWord);
                    }
                    completionWindow.Show();
                    completionWindow.Closed += (o, args) => completionWindow = null;
                }
            }//end if


            //update the insight window
            if (!string.IsNullOrEmpty(enteredText) && insightWindow != null)
            {
                //whenver text is entered update the provider
                var provider = insightWindow.Provider as CSharpOverloadProvider;
                if (provider != null)
                {
                    //since the text has not been added yet we need to tread it as if the char has already been inserted
                    provider.Update(textEditor.Document, textEditor.CaretOffset);
                    //if the windows is requested to be closed we do it here
                    if (provider.RequestClose)
                    {
                        insightWindow.Close();
                        insightWindow = null;
                    }
                }
            }
        }//end method

        private void TextAreaOnTextEntering(object sender, TextCompositionEventArgs textCompositionEventArgs)
        {
            Debug.WriteLine("TextEntering: " + textCompositionEventArgs.Text);
            if (textCompositionEventArgs.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(textCompositionEventArgs.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(textCompositionEventArgs);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }
        #endregion

        public string Text
        {
            get
            {
                if (SD.MainThread.InvokeRequired)
                {
                    return SD.MainThread.InvokeIfRequired(() => GetText());
                }
                else
                {
                    return GetText();
                }
            }
            set
            {
                if (SD.MainThread.InvokeRequired)
                {
                    SD.MainThread.InvokeIfRequired(() => SetText(value));
                }
                else
                {
                    SetText(value);
                }
            }
        }

        public void JumpTo(int line, int column)
        {
            adapter.JumpTo(line, column);
        }

        public int Line
        {
            get { return textEditor.TextArea.Caret.Line; }
        }

        public int Column
        {
            get { return textEditor.TextArea.Caret.Column; }
        }

        public ITextEditor TextEditor
        {
            get { return adapter; }
        }

        public event EventHandler TabPageTextChanged;
        public event EventHandler TitleNameChanged;
        public event EventHandler Disposed;
        public event EventHandler IsDirtyChanged;

        public IWorkbenchWindow WorkbenchWindow { get; set; }

        public string TabPageText
        {
            get { return Path.GetFileName(file.FileName); }
        }

        public string TitleName
        {
            get { throw new NotImplementedException(); }
        }

        public IList<OpenedFile> Files
        {
            get { throw new NotImplementedException(); }
        }

        public OpenedFile PrimaryFile
        {
            get { return file; }
        }

        public FileName PrimaryFileName
        {
            get { return file.FileName; }
        }

        public bool IsDisposed
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsViewOnly
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection<IViewContent> SecondaryViewContents
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsDirty
        {
            get { throw new NotImplementedException(); }
        }

        public void Save(OpenedFile file, Stream stream)
        {
            throw new NotImplementedException();
        }

        public void Load(OpenedFile file, Stream stream)
        {
            throw new NotImplementedException();
        }

        public INavigationPoint BuildNavPoint()
        {
            throw new NotImplementedException();
        }

        public bool SupportsSwitchFromThisWithoutSaveLoad(OpenedFile file, IViewContent newView)
        {
            throw new NotImplementedException();
        }

        public bool SupportsSwitchToThisWithoutSaveLoad(OpenedFile file, IViewContent oldView)
        {
            throw new NotImplementedException();
        }

        public void SwitchFromThisWithoutSaveLoad(OpenedFile file, IViewContent newView)
        {
            throw new NotImplementedException();
        }

        public void SwitchToThisWithoutSaveLoad(OpenedFile file, IViewContent oldView)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public event EventHandler InfoTipChanged;

        object IViewContent.Control
        {
            get { return TextEditor; }
        }

        public object InitiallyFocusedControl
        {
            get { return TextEditor; }
        }

        public string InfoTip
        {
            get { return String.Empty; }
        }

        public bool CloseWithSolution
        {
            get { return false; }
        }

        public object GetService(Type serviceType)
        {
            return null;
        }

        public IDocument GetDocumentForFile(OpenedFile file)
        {
            return null;
        }
        
        private string GetText()
        {
            return textEditor.Document.Text;
        }

        private void SetText(string value)
        {
            textEditor.Document.Replace(0, textEditor.Document.TextLength, value);
        }
        
        public void LoadFile(string fileName)
        {
            file = new SnippetFile(fileName);

            if (completionWindow != null)
                completionWindow.Close();
            if (insightWindow != null)
                insightWindow.Close();

            textEditor.Load(fileName);
            textEditor.Document.FileName = fileName;

            adapter.LoadFile(fileName);
        }

        public void Save()
        {
            textEditor.Save(file.FileName);
        }
    }
}