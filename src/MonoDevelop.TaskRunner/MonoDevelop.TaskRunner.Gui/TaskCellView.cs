//
// TaskCellView.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.TaskRunner.Gui
{
	class TaskCellView : CanvasCellView
	{
		public IDataField<string> NameField { get; set; }
		public IDataField<Image> ImageField { get; set; }

		WidgetSpacing namePadding = new WidgetSpacing (5, 0, 0, 0);

		protected override void OnDraw (Context ctx, Rectangle cellArea)
		{
			FillCellBackground (ctx);
			UpdateTextColor (ctx);

			Image image = GetValue (ImageField);

			DrawName (ctx, cellArea, image?.Width);

			if (image != null) {
				DrawImage (ctx, cellArea, image);
			}
		}

		void FillCellBackground (Context ctx)
		{
			if (Selected) {
				FillCellBackground (ctx, Styles.BaseSelectionBackgroundColor);
			//} else if (IsBackgroundColorFieldSet ()) {
			//	FillCellBackground (ctx, Styles.BackgroundColor);
			}
		}

		void FillCellBackground (Context ctx, Color color)
		{
			ctx.Rectangle (BackgroundBounds);
			ctx.SetColor (color);
			ctx.Fill ();
		}

		void UpdateTextColor (Context ctx)
		{
			if (Selected) {
				ctx.SetColor (Styles.BaseSelectionTextColor);
			} else {
				ctx.SetColor (Styles.BaseForegroundColor);
			}
		}

		void DrawImage (Context ctx, Rectangle cellArea, Image image)
		{
			Point imageLocation = GetImageLocation (image.Size, cellArea);
			ctx.DrawImage (
				image,
				cellArea.Left + imageLocation.X,
				Math.Round (cellArea.Top + imageLocation.Y));
		}

		Point GetImageLocation (Size imageSize, Rectangle cellArea)
		{
			double x = 0;
			double y = (cellArea.Height - imageSize.Height) / 2;
			return new Point (x, y);
		}

		void DrawName (Context ctx, Rectangle cellArea, double? imageWidth)
		{
			if (!imageWidth.HasValue) {
				imageWidth = 0;
			}

			var textLayout = new TextLayout ();
			textLayout.Markup = GetValue (NameField);

			Size size = textLayout.GetSize ();

			cellArea.Width = imageWidth.Value + namePadding.Left + namePadding.Right + size.Width;
			cellArea.Height = size.Height;

			ctx.DrawTextLayout (
				textLayout,
				cellArea.Left + imageWidth.Value + namePadding.Left,
				cellArea.Top);
		}

		[Obsolete]
		protected override Size OnGetRequiredSize ()
		{
			var layout = new TextLayout ();
			layout.Text = "W";
			Size size = layout.GetSize ();
			return new Size (0, size.Height + namePadding.VerticalSpacing);
		}
	}
}
