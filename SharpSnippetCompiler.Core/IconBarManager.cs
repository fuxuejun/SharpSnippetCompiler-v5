// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ICSharpCode.SharpDevelop.Editor.Bookmarks;

namespace ICSharpCode.SharpSnippetCompiler.Core
{
    /// <summary>
    ///     Stores the entries in the icon bar margin. Multiple icon bar margins
    ///     can use the same manager if split view is used.
    /// </summary>
    public class IconBarManager : IBookmarkMargin
    {
        private readonly ObservableCollection<IBookmark> bookmarks = new ObservableCollection<IBookmark>();

        public IconBarManager()
        {
            bookmarks.CollectionChanged += bookmarks_CollectionChanged;
        }

        public IList<IBookmark> Bookmarks
        {
            get { return bookmarks; }
        }

        public void Redraw()
        {
            if (RedrawRequested != null)
                RedrawRequested(this, EventArgs.Empty);
        }

        public event EventHandler RedrawRequested;

        private void bookmarks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Redraw();
        }
    }
}