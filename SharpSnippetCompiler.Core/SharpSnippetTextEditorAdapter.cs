// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.IO;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Editor;

namespace ICSharpCode.SharpSnippetCompiler.Core
{
    public class SharpSnippetTextEditorAdapter : AvalonEditTextEditorAdapter
    {
        private FileName fileName;

        public SharpSnippetTextEditorAdapter(TextEditor textEditor)
            : base(textEditor)
        {
        }

        public override FileName FileName
        {
            get { return fileName; }
        }

        public void LoadFile(string fileName)
        {
            TextEditor.SyntaxHighlighting =
                HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(fileName));
            TextEditor.Load(fileName);
            this.fileName = new FileName(fileName);
        }
    }
}