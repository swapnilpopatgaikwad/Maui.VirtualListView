﻿using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using System.Diagnostics.CodeAnalysis;
using UIKit;

namespace Microsoft.Maui;

internal class CvCell : UICollectionViewCell
{
	public VirtualListViewHandler Handler { get; set; }

	public WeakReference<NSIndexPath> IndexPath { get; set; }

	public PositionInfo PositionInfo { get; set; }

	public WeakReference<Action<IView>> ReuseCallback { get; set; }

	[Export("initWithFrame:")]
	public CvCell(CGRect frame) : base(frame)
	{
		this.ContentView.AddGestureRecognizer(new UITapGestureRecognizer(() => InvokeTap()));
	}

	public TapHandlerProxy TapHandler { get; set; }

	WeakReference<UIKeyCommand[]> keyCommands;

	public override UIKeyCommand[] KeyCommands
	{
		get
		{
			if (keyCommands?.TryGetTarget(out var commands) ?? false)
                return commands;

			var v = new[]
			{
				UIKeyCommand.Create(new NSString("\r"), 0, new ObjCRuntime.Selector("keyCommandSelect")),
				UIKeyCommand.Create(new NSString(" "), 0, new ObjCRuntime.Selector("keyCommandSelect")),
			};

            keyCommands = new WeakReference<UIKeyCommand[]>(v);

			return v;
		}

	}

	[Export("keyCommandSelect")]
	public void KeyCommandSelect()
	{
		InvokeTap();
	}

	void InvokeTap()
	{
		if (PositionInfo.Kind == PositionKind.Item)
			TapHandler?.Invoke(this);
	}

	public void UpdateSelected(bool selected)
	{
		PositionInfo.IsSelected = selected;

		if (VirtualView?.TryGetTarget(out var virtualView) ?? false)
		{
			if (virtualView is IPositionInfo positionInfo)
			{
				positionInfo.IsSelected = selected;
				virtualView.Handler?.UpdateValue(nameof(PositionInfo.IsSelected));
			}
		}
	}

	public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(UICollectionViewLayoutAttributes layoutAttributes)
	{
		if ((NativeView is not null && NativeView.TryGetTarget(out var _))
			&& (VirtualView is not null && VirtualView.TryGetTarget(out var virtualView)))
		{
			var measure = virtualView.Measure(layoutAttributes.Size.Width, double.PositiveInfinity);

			layoutAttributes.Frame = new CGRect(0, layoutAttributes.Frame.Y, layoutAttributes.Frame.Width, measure.Height);

			return layoutAttributes;
		}

		return layoutAttributes;
	}

	public bool NeedsView
		=> NativeView == null || !NativeView.TryGetTarget(out var _);

	public WeakReference<IView> VirtualView { get; set; }

	public WeakReference<UIView> NativeView { get; set; }

	public override void PrepareForReuse()
	{
		base.PrepareForReuse();

		// TODO: Recycle
		if ((VirtualView?.TryGetTarget(out var virtualView) ?? false)
			&& (ReuseCallback?.TryGetTarget(out var reuseCallback) ?? false))
		{
			reuseCallback?.Invoke(virtualView);
		}
	}

	public void SwapView(IView newView)
	{
        // Create a new platform native view if we don't have one yet
        if (!(NativeView?.TryGetTarget(out var nativeView) ?? false))
        {
            nativeView = newView.ToPlatform(this.Handler.MauiContext);
            nativeView.Frame = this.ContentView.Frame;
            nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
            this.ContentView.AddSubview(nativeView);
            NativeView = new WeakReference<UIView>(nativeView);
        }

		// Create a new virtual view if we don't have one yet
		if (!(VirtualView?.TryGetTarget(out var virtualView) ?? false) || (virtualView?.Handler is null))
		{
			virtualView = newView;
			VirtualView = new WeakReference<IView>(virtualView);
		}
		else
		{
			var handler = virtualView.Handler;
			virtualView.Handler = null;
			newView.Handler = handler;
			handler.SetVirtualView(newView);
			VirtualView.SetTarget(newView);
		}
    }

	public class TapHandlerProxy
	{
        public TapHandlerProxy(Action<CvCell> tapHandler)
		{
            TapHandler = tapHandler;
        }

        public Action<CvCell> TapHandler { get; set; }

		public void Invoke(CvCell cell)	
			=> TapHandler?.Invoke(cell);

    }
}