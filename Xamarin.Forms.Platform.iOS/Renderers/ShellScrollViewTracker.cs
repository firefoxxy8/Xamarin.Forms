﻿using System;
using UIKit;
using PointF = CoreGraphics.CGPoint;

namespace Xamarin.Forms.Platform.iOS
{
	public class ShellScrollViewTracker : IDisposable, IShellContentInsetObserver
	{
		#region IShellContentInsetObserver

		void IShellContentInsetObserver.OnInsetChanged(Thickness inset, double tabThickness)
		{
			UpdateContentInset(inset, tabThickness);
		}

		#endregion IShellContentInsetObserver

		private bool _disposed;
		private bool _isInShell;
		private bool _isInItems;
		private IVisualElementRenderer _renderer;
		private UIScrollView _scrollView;
		private ShellSection _shellSection;

		public ShellScrollViewTracker(IVisualElementRenderer renderer)
		{
			_renderer = renderer;
			_scrollView = (UIScrollView)_renderer.NativeView;

			var parent = _renderer.Element.Parent;

			while (!Application.IsApplicationOrNull(parent))
			{
				if (parent is ScrollView || parent is ListView || parent is TableView)
					break;
				parent = parent.Parent;

				// Currently ShellContents are not pushable onto the stack so we know we are not being pushed
				// on the stack if we are in a ShellContent
				if (parent is ShellContent)
					_isInItems = true;

				if (parent is ShellSection shellSection)
				{
					_isInShell = true;
					_shellSection = shellSection;
					((IShellSectionController)_shellSection).AddContentInsetObserver(this);

					break;
				}
			}

			if (_isInShell)
				UpdateVerticalBounce();
		}

		public void OnLayoutSubviews()
		{
			if (!_isInShell)
				return;

			if (Forms.IsiOS11OrNewer)
			{
				var newBounds = _scrollView.AdjustedContentInset.InsetRect(_scrollView.Bounds).ToRectangle();
				newBounds.X = 0;
				newBounds.Y = 0;
				((ScrollView)_renderer.Element).LayoutAreaOverride = newBounds;
			}
		}

		private void UpdateContentInset(Thickness inset, double tabThickness)
		{
			if (Forms.IsiOS11OrNewer)
			{
				if (_shellSection.Items.Count > 1 && _isInItems)
				{
					_scrollView.ContentInset = new UIEdgeInsets((float)tabThickness, 0, 0, 0);
					_scrollView.ContentOffset = new PointF(0, -tabThickness);
				}

				_scrollView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Always;
			}
			else
			{
				var top = (float)(inset.Top);

				if (_shellSection.Items.Count > 1 && _isInItems)
					top += (float)tabThickness;

				_scrollView.ContentInset = new UIEdgeInsets(top, (float)inset.Left, (float)inset.Bottom, (float)inset.Right);
				_scrollView.ContentOffset = new PointF(0, -top);
			}
		}

		private void UpdateVerticalBounce()
		{
			// Normally we dont want to do this unless this scrollview is vertical and its
			// element is the child of a Page with a SearchHandler that is collapsable.
			// If we can't bounce in that case you may not be able to expose the handler.
			// Also the hiding behavior only depends on scroll on iOS 11. In 10 and below
			// the search goes in the TitleView so there is nothing to collapse/expand.
			if (!Forms.IsiOS11OrNewer || ((ScrollView)_renderer.Element).Orientation == ScrollOrientation.Horizontal)
				return;

			var parent = _renderer.Element.Parent;
			while (!Application.IsApplicationOrNull(parent))
			{
				if (parent is Page)
				{
					var searchHandler = Shell.GetSearchHandler(parent);
					if (searchHandler?.SearchBoxVisibility == SearchBoxVisiblity.Collapsable)
						_scrollView.AlwaysBounceVertical = true;
					return;
				}
				parent = parent.Parent;
			}
		}

		#region IDisposable Support

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					((IShellSectionController)_shellSection).RemoveContentInsetObserver(this);
				}

				_renderer = null;
				_scrollView = null;
				_shellSection = null;

				_disposed = true;
			}
		}

		#endregion IDisposable Support
	}
}