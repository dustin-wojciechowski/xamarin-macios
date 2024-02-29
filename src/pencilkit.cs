//
// PencilKit C# bindings
//
// Authors:
//	TJ Lambert  <t-anlamb@microsoft.com>
//	Whitney Schmidt  <whschm@microsoft.com>
//
// Copyright 2019, 2020 Microsoft Corporation All rights reserved.
//

#if MONOMAC
using AppKit;
using UIColor = AppKit.NSColor;
using UIImage = AppKit.NSImage;

using UIScrollViewDelegate = Foundation.NSObjectProtocol;
using UIScrollView = Foundation.NSObject;
using UIGestureRecognizer = Foundation.NSObject;
using UIResponder = Foundation.NSObject;
using UIView = Foundation.NSObject;
using UIWindow = Foundation.NSObject;
using UIUserInterfaceStyle = Foundation.NSObject;
using BezierPath = AppKit.NSBezierPath;
#else
using UIKit;
using BezierPath = UIKit.UIBezierPath;
#endif

using System;
using System.ComponentModel;
using ObjCRuntime;
using Foundation;
using CoreGraphics;

#if !NET
using NativeHandle = System.IntPtr;
#endif

namespace PencilKit {

	[iOS (13, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[Native]
	enum PKEraserType : long {
		Vector,
		Bitmap,
		[iOS (16, 4), Mac (13, 3), MacCatalyst (16, 4)]
		FixedWidthBitmap,
	}

	[iOS (13, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	enum PKInkType {
		[Field ("PKInkTypePen")]
		Pen,

		[Field ("PKInkTypePencil")]
		Pencil,

		[Field ("PKInkTypeMarker")]
		Marker,

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Field ("PKInkTypeMonoline")]
		Monoline,

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Field ("PKInkTypeFountainPen")]
		FountainPen,

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Field ("PKInkTypeWatercolor")]
		Watercolor,

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Field ("PKInkTypeCrayon")]
		Crayon,
	}

	[iOS (14, 0), NoMac]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[Native]
	enum PKCanvasViewDrawingPolicy : ulong {
		Default,
		AnyInput,
		PencilOnly,
	}

	[iOS (17, 0), Mac (14, 0)]
	[Introduced (PlatformName.MacCatalyst, 17, 0)]
	[Native]
	public enum PKContentVersion : long {
		Version1 = 1,
		Version2 = 2,
	}

	[iOS (13, 0), NoMac]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[BaseType (typeof (NSObject))]
#if NET
	[Protocol, Model]
#else
	[Model (AutoGeneratedName = true)]
	[Protocol]
#endif
	interface PKCanvasViewDelegate : UIScrollViewDelegate {

		[Export ("canvasViewDrawingDidChange:")]
		void DrawingDidChange (PKCanvasView canvasView);

		[Export ("canvasViewDidFinishRendering:")]
		void DidFinishRendering (PKCanvasView canvasView);

		[Export ("canvasViewDidBeginUsingTool:")]
		void DidBeginUsingTool (PKCanvasView canvasView);

		[Export ("canvasViewDidEndUsingTool:")]
		void EndUsingTool (PKCanvasView canvasView);
	}

	interface IPKCanvasViewDelegate { }

	[iOS (13, 0), NoMac]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[BaseType (typeof (UIScrollView))]
	interface PKCanvasView : PKToolPickerObserver {

		// This exists in the base class
		// [Export ("delegate", ArgumentSemantic.Weak), NullAllowed]
		// NSObject WeakDelegate { get; set; }

		[Unavailable (PlatformName.MacCatalyst), Advice ("This API is not available when using Catalyst on macOS.")]
		[Wrap ("WeakDelegate"), NullAllowed, New]
		IPKCanvasViewDelegate Delegate { get; set; }

		[Export ("drawing", ArgumentSemantic.Copy)]
		PKDrawing Drawing { get; set; }

		[Export ("tool", ArgumentSemantic.Copy)]
		PKTool Tool { get; set; }

		[Export ("rulerActive")]
		bool RulerActive { [Bind ("isRulerActive")] get; set; }

		[Export ("drawingGestureRecognizer")]
		UIGestureRecognizer DrawingGestureRecognizer { get; }

		[Deprecated (PlatformName.iOS, 14, 0, message: "Use 'DrawingPolicy' property instead.")]
		[Deprecated (PlatformName.MacCatalyst, 14, 0, message: "Use 'DrawingPolicy' property instead.")]
		[Export ("allowsFingerDrawing")]
		bool AllowsFingerDrawing { get; set; }

		[iOS (14, 0)]
		[MacCatalyst (14, 0)]
		[Export ("drawingPolicy", ArgumentSemantic.Assign)]
		PKCanvasViewDrawingPolicy DrawingPolicy { get; set; }

		[iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("maximumSupportedContentVersion", ArgumentSemantic.Assign)]
		PKContentVersion MaximumSupportedContentVersion { get; set; }
	}

	[iOS (13, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[BaseType (typeof (NSObject))]
	[DesignatedDefaultCtor]
	interface PKDrawing : NSCopying, NSSecureCoding {

		[Field ("PKAppleDrawingTypeIdentifier")]
		NSString AppleDrawingTypeIdentifier { get; }

		[DesignatedInitializer]
		[Export ("initWithData:error:")]
		NativeHandle Constructor (NSData data, [NullAllowed] out NSError error);

		[iOS (14, 0)]
		[MacCatalyst (14, 0)]
		[Export ("initWithStrokes:")]
		NativeHandle Constructor (PKStroke [] strokes);

		[Export ("dataRepresentation")]
		NSData DataRepresentation { get; }

		[Export ("bounds")]
		CGRect Bounds { get; }

		[iOS (14, 0)]
		[MacCatalyst (14, 0)]
		[Export ("strokes")]
		PKStroke [] Strokes { get; }

		[Export ("imageFromRect:scale:")]
		UIImage GetImage (CGRect rect, nfloat scale);

		[Export ("drawingByApplyingTransform:")]
		PKDrawing GetDrawing (CGAffineTransform transform);

		[Export ("drawingByAppendingDrawing:")]
		PKDrawing GetDrawing (PKDrawing drawing);

		[iOS (14, 0)]
		[MacCatalyst (14, 0)]
		[Export ("drawingByAppendingStrokes:")]
		PKDrawing GetDrawing (PKStroke [] strokes);

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("requiredContentVersion")]
		PKContentVersion RequiredContentVersion { get; }
	}

	[iOS (13, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[BaseType (typeof (PKTool))]
	[DisableDefaultCtor]
	interface PKEraserTool {

		[Export ("eraserType")]
		PKEraserType EraserType { get; }

		[DesignatedInitializer]
		[Export ("initWithEraserType:")]
		NativeHandle Constructor (PKEraserType eraserType);

		[Mac (13, 3), iOS (16, 4), MacCatalyst (16, 4)]
		[Export ("initWithEraserType:width:")]
		[DesignatedInitializer]
		NativeHandle Constructor (PKEraserType eraserType, nfloat width);

		[Mac (13, 3), iOS (16, 4), MacCatalyst (16, 4)]
		[Export ("width")]
		nfloat Width { get; }

		[Mac (13, 3), iOS (16, 4), MacCatalyst (16, 4)]
		[Static]
		[Export ("defaultWidthForEraserType:")]
		nfloat GetDefaultWidth (PKEraserType eraserType);

		[Mac (13, 3), iOS (16, 4), MacCatalyst (16, 4)]
		[Static]
		[Export ("minimumWidthForEraserType:")]
		nfloat GetMinimumWidth (PKEraserType eraserType);

		[Mac (13, 3), iOS (16, 4), MacCatalyst (16, 4)]
		[Static]
		[Export ("maximumWidthForEraserType:")]
		nfloat GetMaximumWidth (PKEraserType eraserType);
	}

	[iOS (13, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[BaseType (typeof (PKTool))]
	[DisableDefaultCtor]
	interface PKInkingTool {

		[DesignatedInitializer]
		[Export ("initWithInkType:color:width:")]
		NativeHandle Constructor ([BindAs (typeof (PKInkType))] NSString type, UIColor color, nfloat width);

		[Export ("initWithInkType:color:")]
		NativeHandle Constructor ([BindAs (typeof (PKInkType))] NSString type, UIColor color);

		[iOS (14, 0)]
		[MacCatalyst (14, 0)]
		[Export ("initWithInk:width:")]
		NativeHandle Constructor (PKInk ink, nfloat width);

		[Static]
		[Export ("defaultWidthForInkType:")]
		nfloat GetDefaultWidth ([BindAs (typeof (PKInkType))] NSString inkType);

		[Static]
		[Export ("minimumWidthForInkType:")]
		nfloat GetMinimumWidth ([BindAs (typeof (PKInkType))] NSString inkType);

		[Static]
		[Export ("maximumWidthForInkType:")]
		nfloat GetMaximumWidth ([BindAs (typeof (PKInkType))] NSString inkType);

		[Export ("inkType")]
		[BindAs (typeof (PKInkType))]
		NSString InkType { get; }

		[NoMac]
		[MacCatalyst (13, 1)]
		[Static]
		[Export ("convertColor:fromUserInterfaceStyle:to:")]
		UIColor ConvertColor (UIColor color, UIUserInterfaceStyle fromUserInterfaceStyle, UIUserInterfaceStyle toUserInterfaceStyle);

		[Export ("color")]
		UIColor Color { get; }

		[Export ("width")]
		nfloat Width { get; }

		[iOS (14, 0)]
		[MacCatalyst (14, 0)]
		[Export ("ink")]
		PKInk Ink { get; }

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("requiredContentVersion")]
		PKContentVersion RequiredContentVersion { get; }
	}

	[iOS (13, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[BaseType (typeof (PKTool))]
	[DesignatedDefaultCtor]
	interface PKLassoTool { }

	[iOS (13, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	interface PKTool : NSCopying { }

	interface IPKToolPickerObserver { }

	[iOS (13, 0), NoMac]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[Protocol]
	interface PKToolPickerObserver {

		[Export ("toolPickerSelectedToolDidChange:")]
		void SelectedToolDidChange (PKToolPicker toolPicker);

		[Export ("toolPickerIsRulerActiveDidChange:")]
		void IsRulerActiveDidChange (PKToolPicker toolPicker);

		[Export ("toolPickerVisibilityDidChange:")]
		void VisibilityDidChange (PKToolPicker toolPicker);

		[Export ("toolPickerFramesObscuredDidChange:")]
		void FramesObscuredDidChange (PKToolPicker toolPicker);
	}

	[iOS (13, 0), NoMac]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[DisableDefaultCtor]
	[BaseType (typeof (NSObject))]
	interface PKToolPicker {

		[iOS (14, 0)]
		[MacCatalyst (14, 0)]
		[Export ("init")]
		NativeHandle Constructor ();

		[Export ("addObserver:")]
		void AddObserver (IPKToolPickerObserver observer);

		[Export ("removeObserver:")]
		void RemoveObserver (IPKToolPickerObserver observer);

		[Export ("setVisible:forFirstResponder:")]
		void SetVisible (bool visible, UIResponder responder);

		[Export ("selectedTool", ArgumentSemantic.Strong)]
		PKTool SelectedTool { get; set; }

		[Export ("rulerActive")]
		bool RulerActive { [Bind ("isRulerActive")] get; set; }

		[Export ("isVisible")]
		bool IsVisible { get; }

		[Export ("frameObscuredInView:")]
		CGRect GetFrameObscured (UIView view);

		[Export ("overrideUserInterfaceStyle", ArgumentSemantic.Assign)]
		UIUserInterfaceStyle OverrideUserInterfaceStyle { get; set; }

		[Export ("colorUserInterfaceStyle", ArgumentSemantic.Assign)]
		UIUserInterfaceStyle ColorUserInterfaceStyle { get; set; }

		[Deprecated (PlatformName.iOS, 14, 0, message: "Create individual instances instead.")]
		[Deprecated (PlatformName.MacCatalyst, 14, 0, message: "Create individual instances instead.")]
		[Static]
		[return: NullAllowed]
		[Export ("sharedToolPickerForWindow:")]
		PKToolPicker GetSharedToolPicker (UIWindow window);

		[iOS (14, 0)]
		[MacCatalyst (14, 0)]
		[Export ("showsDrawingPolicyControls")]
		bool ShowsDrawingPolicyControls { get; set; }

		[iOS (14, 0)]
		[MacCatalyst (14, 0)]
		[NullAllowed]
		[Export ("stateAutosaveName")]
		string StateAutosaveName { get; set; }

		[iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("maximumSupportedContentVersion", ArgumentSemantic.Assign)]
		PKContentVersion MaximumSupportedContentVersion { get; set; }
	}

	[iOS (14, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[DisableDefaultCtor]
	[BaseType (typeof (NSObject))]
	interface PKInk : NSCopying {
		[EditorBrowsable (EditorBrowsableState.Advanced)]
		[Export ("initWithInkType:color:")]
		[DesignatedInitializer]
		NativeHandle Constructor (/* enum PKInkType */ NSString type, UIColor color);

		[Wrap ("this (type.GetConstant ()!, color)")]
		NativeHandle Constructor (PKInkType type, UIColor color);

		[EditorBrowsable (EditorBrowsableState.Advanced)]
		[Export ("inkType")]
		NSString WeakInkType { get; }

		[Wrap ("PKInkTypeExtensions.GetValue(WeakInkType)")]
		PKInkType InkType { get; }

		[Export ("color")]
		UIColor Color { get; }

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("requiredContentVersion")]
		PKContentVersion RequiredContentVersion { get; }
	}

	[iOS (14, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[DisableDefaultCtor]
	[BaseType (typeof (NSObject))]
	interface PKFloatRange : NSCopying {
		[Export ("initWithLowerBound:upperBound:")]
		NativeHandle Constructor (nfloat lowerBound, nfloat upperBound);

		[Export ("lowerBound")]
		nfloat LowerBound { get; }

		[Export ("upperBound")]
		nfloat UpperBound { get; }
	}

	[iOS (14, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[DisableDefaultCtor]
	[BaseType (typeof (NSObject))]
	interface PKStroke : NSCopying {
		[Export ("initWithInk:strokePath:transform:mask:")]
		NativeHandle Constructor (PKInk ink, PKStrokePath path, CGAffineTransform transform, [NullAllowed] BezierPath mask);

		[Mac (13, 0), iOS (16, 0), MacCatalyst (16, 0)]
		[Export ("initWithInk:strokePath:transform:mask:randomSeed:")]
		NativeHandle Constructor (PKInk ink, PKStrokePath strokePath, CGAffineTransform transform, [NullAllowed] BezierPath mask, uint randomSeed);

		[Export ("ink")]
		PKInk Ink { get; }

		[Export ("transform")]
		CGAffineTransform Transform { get; }

		[Export ("path")]
		PKStrokePath Path { get; }

		[NullAllowed, Export ("mask")]
		BezierPath Mask { get; }

		[Export ("renderBounds")]
		CGRect RenderBounds { get; }

		[Export ("maskedPathRanges")]
		PKFloatRange [] MaskedPathRanges { get; }

		[Mac (13, 0), iOS (16, 0), MacCatalyst (16, 0)]
		[Export ("randomSeed")]
		uint RandomSeed { get; }

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("requiredContentVersion")]
		PKContentVersion RequiredContentVersion { get; }
	}

	delegate void PKInterpolatedPointsEnumeratorHandler (PKStrokePoint strokePoint, out bool stop);

	[iOS (14, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[DisableDefaultCtor]
	[BaseType (typeof (NSObject))]
	interface PKStrokePath : NSCopying {
		[Export ("initWithControlPoints:creationDate:")]
		[DesignatedInitializer]
		NativeHandle Constructor (PKStrokePoint [] controlPoints, NSDate creationDate);

		[Export ("count")]
		nuint Count { get; }

		[Export ("creationDate")]
		NSDate CreationDate { get; }

		[Export ("pointAtIndex:")]
		PKStrokePoint GetPoint (nuint index);

		[Export ("objectAtIndexedSubscript:")]
		PKStrokePoint GetObject (nuint indexedSubscript);

		[Export ("interpolatedLocationAt:")]
		CGPoint GetInterpolatedLocation (nfloat parametricValue);

		[Export ("interpolatedPointAt:")]
		PKStrokePoint GetInterpolatedPoint (nfloat parametricValue);

		[Export ("enumerateInterpolatedPointsInRange:strideByDistance:usingBlock:")]
		void EnumerateInterpolatedPointsByDistanceStep (PKFloatRange range, nfloat distanceStep, PKInterpolatedPointsEnumeratorHandler enumeratorHandler);

		[Export ("enumerateInterpolatedPointsInRange:strideByTime:usingBlock:")]
		void EnumerateInterpolatedPointsByTimeStep (PKFloatRange range, double timeStep, PKInterpolatedPointsEnumeratorHandler enumeratorHandler);

		[Export ("enumerateInterpolatedPointsInRange:strideByParametricStep:usingBlock:")]
		void EnumerateInterpolatedPointsByParametricStep (PKFloatRange range, nfloat parametricStep, PKInterpolatedPointsEnumeratorHandler enumeratorHandler);

		[Export ("parametricValue:offsetByDistance:")]
		nfloat GetParametricValue (nfloat parametricValue, nfloat distanceStep);

		[Export ("parametricValue:offsetByTime:")]
		nfloat GetParametricValue (nfloat parametricValue, double timeStep);
	}

	[iOS (14, 0)]
	[Introduced (PlatformName.MacCatalyst, 14, 0)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	interface PKStrokePoint : NSCopying {
		[Export ("initWithLocation:timeOffset:size:opacity:force:azimuth:altitude:")]
		[DesignatedInitializer]
		NativeHandle Constructor (CGPoint location, double timeOffset, CGSize size, nfloat opacity, nfloat force, nfloat azimuth, nfloat altitude);

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("initWithLocation:timeOffset:size:opacity:force:azimuth:altitude:secondaryScale:")]
		[DesignatedInitializer]
		NativeHandle Constructor (CGPoint location, double timeOffset, CGSize size, nfloat opacity, nfloat force, nfloat azimuth, nfloat altitude, nfloat secondaryScale);

		[Export ("location")]
		CGPoint Location { get; }

		[Export ("timeOffset")]
		double TimeOffset { get; }

		[Export ("size")]
		CGSize Size { get; }

		[Export ("opacity")]
		nfloat Opacity { get; }

		[Export ("azimuth")]
		nfloat Azimuth { get; }

		[Export ("force")]
		nfloat Force { get; }

		[Export ("altitude")]
		nfloat Altitude { get; }

		[Mac (14, 0), iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("secondaryScale")]
		nfloat SecondaryScale { get; }
	}
}
