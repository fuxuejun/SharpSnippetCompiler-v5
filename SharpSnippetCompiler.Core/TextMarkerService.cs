// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.SharpDevelop.Editor;

namespace ICSharpCode.SharpSnippetCompiler.Core
{
    /// <summary>
    ///     Handles the text markers for a code editor.
    /// </summary>
    public sealed class TextMarkerService : DocumentColorizingTransformer, IBackgroundRenderer, ITextMarkerService,
                                            ITextViewConnect
    {
        private readonly TextDocument document;
        private readonly TextSegmentCollection<TextMarker> markers;

        public TextMarkerService(TextDocument document)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            this.document = document;
            markers = new TextSegmentCollection<TextMarker>(document);
        }

        #region ITextMarkerService

        public ITextMarker Create(int startOffset, int length)
        {
            if (markers == null)
                throw new InvalidOperationException("Cannot create a marker when not attached to a document");

            int textLength = document.TextLength;
            if (startOffset < 0 || startOffset > textLength)
                throw new ArgumentOutOfRangeException("startOffset", startOffset,
                                                      "Value must be between 0 and " + textLength);
            if (length < 0 || startOffset + length > textLength)
                throw new ArgumentOutOfRangeException("length", length,
                                                      "length must not be negative and startOffset+length must not be after the end of the document");

            var m = new TextMarker(this, startOffset, length);
            markers.Add(m);
            // no need to mark segment for redraw: the text marker is invisible until a property is set
            return m;
        }

        public IEnumerable<ITextMarker> GetMarkersAtOffset(int offset)
        {
            if (markers == null)
                return Enumerable.Empty<ITextMarker>();
            else
                return markers.FindSegmentsContaining(offset);
        }

        public IEnumerable<ITextMarker> TextMarkers
        {
            get { return markers ?? Enumerable.Empty<ITextMarker>(); }
        }

        public void RemoveAll(Predicate<ITextMarker> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            if (markers != null)
            {
                foreach (TextMarker m in markers.ToArray())
                {
                    if (predicate(m))
                        Remove(m);
                }
            }
        }

        public void Remove(ITextMarker marker)
        {
            if (marker == null)
                throw new ArgumentNullException("marker");
            var m = marker as TextMarker;
            if (markers != null && markers.Remove(m))
            {
                Redraw(m);
                m.OnDeleted();
            }
        }

        /// <summary>
        ///     Redraws the specified text segment.
        /// </summary>
        internal void Redraw(ISegment segment)
        {
            foreach (TextView view in textViews)
            {
                view.Redraw(segment, DispatcherPriority.Normal);
            }
        }

        #endregion

        #region DocumentColorizingTransformer

        protected override void ColorizeLine(DocumentLine line)
        {
            if (markers == null)
                return;
            int lineStart = line.Offset;
            int lineEnd = lineStart + line.Length;
            foreach (TextMarker marker in markers.FindOverlappingSegments(lineStart, line.Length))
            {
                Brush foregroundBrush = null;
                if (marker.ForegroundColor != null)
                {
                    foregroundBrush = new SolidColorBrush(marker.ForegroundColor.Value);
                    foregroundBrush.Freeze();
                }
                ChangeLinePart(
                    Math.Max(marker.StartOffset, lineStart),
                    Math.Min(marker.EndOffset, lineEnd),
                    element =>
                        {
                            if (foregroundBrush != null)
                            {
                                element.TextRunProperties.SetForegroundBrush(foregroundBrush);
                            }
                        }
                    );
            }
        }

        #endregion

        #region IBackgroundRenderer

        public KnownLayer Layer
        {
            get
            {
                // draw behind selection
                return KnownLayer.Selection;
            }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException("textView");
            if (drawingContext == null)
                throw new ArgumentNullException("drawingContext");
            if (markers == null || !textView.VisualLinesValid)
                return;
            ReadOnlyCollection<VisualLine> visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;
            int viewStart = visualLines.First().FirstDocumentLine.Offset;
            int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;
            foreach (TextMarker marker in markers.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            {
                if (marker.BackgroundColor != null)
                {
                    var geoBuilder = new BackgroundGeometryBuilder();
                    geoBuilder.AlignToWholePixels = true;
                    geoBuilder.CornerRadius = 3;
                    geoBuilder.AddSegment(textView, marker);
                    Geometry geometry = geoBuilder.CreateGeometry();
                    if (geometry != null)
                    {
                        Color color = marker.BackgroundColor.Value;
                        var brush = new SolidColorBrush(color);
                        brush.Freeze();
                        drawingContext.DrawGeometry(brush, null, geometry);
                    }
                }
            }
        }

        private IEnumerable<Point> CreatePoints(Point start, Point end, double offset, int count)
        {
            for (int i = 0; i < count; i++)
                yield return new Point(start.X + i*offset, start.Y - ((i + 1)%2 == 0 ? offset : 0));
        }

        #endregion

        #region ITextViewConnect

        private readonly List<TextView> textViews = new List<TextView>();

        void ITextViewConnect.AddToTextView(TextView textView)
        {
            if (textView != null && !textViews.Contains(textView))
            {
                Debug.Assert(textView.Document == document);
                textViews.Add(textView);
            }
        }

        void ITextViewConnect.RemoveFromTextView(TextView textView)
        {
            if (textView != null)
            {
                Debug.Assert(textView.Document == document);
                textViews.Remove(textView);
            }
        }

        #endregion
    }

    public sealed class TextMarker : TextSegment, ITextMarker
    {
        private readonly TextMarkerService service;
        private Color? backgroundColor;
        private Color? foregroundColor;
        private Color markerColor;

        public TextMarker(TextMarkerService service, int startOffset, int length)
        {
            if (service == null)
                throw new ArgumentNullException("service");
            this.service = service;
            StartOffset = startOffset;
            Length = length;
        }

        public event EventHandler Deleted;

        public bool IsDeleted
        {
            get { return !IsConnectedToCollection; }
        }

        public void Delete()
        {
            service.Remove(this);
        }

        public Color? BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                if (backgroundColor != value)
                {
                    backgroundColor = value;
                    Redraw();
                }
            }
        }

        public Color? ForegroundColor
        {
            get { return foregroundColor; }
            set
            {
                if (foregroundColor != value)
                {
                    foregroundColor = value;
                    Redraw();
                }
            }
        }

        public FontWeight? FontWeight { get; set; }
        public FontStyle? FontStyle { get; set; }
        public TextMarkerTypes MarkerTypes { get; set; }

        public object Tag { get; set; }

        public Color MarkerColor
        {
            get { return markerColor; }
            set
            {
                if (markerColor != value)
                {
                    markerColor = value;
                    Redraw();
                }
            }
        }

        public object ToolTip { get; set; }

        internal void OnDeleted()
        {
            if (Deleted != null)
                Deleted(this, EventArgs.Empty);
        }

        private void Redraw()
        {
            service.Redraw(this);
        }
    }
}