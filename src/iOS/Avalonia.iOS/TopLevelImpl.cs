using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia.iOS;
using UIKit;
using Avalonia.iOS.Specific;
using ObjCRuntime;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;

namespace Avalonia.iOS
{
    [Adopts("UIKeyInput")]
    class TopLevelImpl : UIView, ITopLevelImpl, IFramebufferPlatformSurface
    {
        private IInputRoot _inputRoot;
        private readonly KeyboardEventsHelper<TopLevelImpl> _keyboardHelper;
        private Point _position;

        public TopLevelImpl()
        {
            _keyboardHelper = new KeyboardEventsHelper<TopLevelImpl>(this);
            AutoresizingMask = UIViewAutoresizing.All;
        }

        [Export("hasText")]
        public bool HasText => _keyboardHelper.HasText();

        [Export("insertText:")]
        public void InsertText(string text) => _keyboardHelper.InsertText(text);

        [Export("deleteBackward")]
        public void DeleteBackward() => _keyboardHelper.DeleteBackward();

        public override bool CanBecomeFirstResponder => _keyboardHelper.CanBecomeFirstResponder();

        public Action Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }
        public Action<Point> PositionChanged { get; set; }

        public IPlatformHandle Handle => null;

        public double Scaling => UIScreen.MainScreen.Scale;

        public WindowState WindowState
        {
            get { return WindowState.Normal; }
            set { }
        }

        public override void LayoutSubviews() => Resized?.Invoke(ClientSize);

        public Size ClientSize
        {
            get { return Bounds.Size.ToAvalonia(); }
            set { InvokeOnMainThread(() => Resized?.Invoke(ClientSize)); }
        }

        public void Activate()
        {
        }

        public override void Draw(CGRect rect)
        {
            Paint?.Invoke(new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        }

        public void Invalidate(Rect rect) => SetNeedsDisplay();

        public void SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;

        public Point PointToClient(Point point) => point;

        public Point PointToScreen(Point point) => point;

        public void SetCursor(IPlatformHandle cursor)
        {
            //Not supported
        }

        public void Show()
        {
            _keyboardHelper.ActivateAutoShowKeybord();
        }

        public void BeginMoveDrag()
        {
            //Not supported
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            //Not supported
        }

        public Point Position
        {
            get { return _position; }
            set
            {
                _position = value;
                PositionChanged?.Invoke(_position);
            }
        }

        public Size MaxClientSize => Bounds.Size.ToAvalonia();

        public IEnumerable<object> Surfaces => new object[] { this };


        public void Hide()
        {
            //Not supported
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            var touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var location = touch.LocationInView(this).ToAvalonia();

                Input?.Invoke(new RawMouseEventArgs(
                    iOSPlatform.MouseDevice,
                    (uint)touch.Timestamp,
                    _inputRoot,
                    RawMouseEventType.LeftButtonUp,
                    location,
                    InputModifiers.None));
            }
        }

        Point _touchLastPoint;
        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            var touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var location = touch.LocationInView(this).ToAvalonia();
                _touchLastPoint = location;
                Input?.Invoke(new RawMouseEventArgs(iOSPlatform.MouseDevice, (uint)touch.Timestamp, _inputRoot,
                    RawMouseEventType.Move, location, InputModifiers.None));

                Input?.Invoke(new RawMouseEventArgs(iOSPlatform.MouseDevice, (uint)touch.Timestamp, _inputRoot,
                    RawMouseEventType.LeftButtonDown, location, InputModifiers.None));
            }
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            var touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var location = touch.LocationInView(this).ToAvalonia();
                if (iOSPlatform.MouseDevice.Captured != null)
                    Input?.Invoke(new RawMouseEventArgs(iOSPlatform.MouseDevice, (uint)touch.Timestamp, _inputRoot,
                        RawMouseEventType.Move, location, InputModifiers.LeftMouseButton));
                else
                {
                    //magic number based on test - correction of 0.02 is working perfect
                    double correction = 0.02;

                    Input?.Invoke(new RawMouseWheelEventArgs(iOSPlatform.MouseDevice, (uint)touch.Timestamp,
                        _inputRoot, location, (location - _touchLastPoint) * correction, InputModifiers.LeftMouseButton));
                }
                _touchLastPoint = location;
            }
        }

        public void SetIcon(IWindowIconImpl icon)
        {
        }

        public ILockedFramebuffer Lock() => new EmulatedFramebuffer(this);
    }
}
