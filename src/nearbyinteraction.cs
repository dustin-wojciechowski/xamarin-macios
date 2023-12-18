//
// NearbyInteraction bindings
//
// Authors:
//	Whitney Schmidt  <whschm@microsoft.com>
//
// Copyright 2020 Microsoft Inc.
//

using ObjCRuntime;
using Foundation;
using CoreFoundation;
using System;
#if __MACCATALYST__ || !IOS
using ARSession = Foundation.NSObject;
#else
using ARKit;
#endif

#if NET
using Vector3 = global::System.Numerics.Vector3;
using MatrixFloat4x4 = global::CoreGraphics.NMatrix4;
#else
using Vector3 = global::OpenTK.Vector3;
using MatrixFloat4x4 = global::OpenTK.NMatrix4;
#endif

#if !NET
using NativeHandle = System.IntPtr;
#endif

namespace NearbyInteraction {

	[Watch (8, 0), NoTV, NoMac, iOS (14, 0)]
	[MacCatalyst (14, 0)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	interface NIConfiguration : NSCopying, NSSecureCoding { }

	[Watch (8, 0), NoTV, NoMac, iOS (14, 0)]
	[MacCatalyst (14, 0)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	interface NIDiscoveryToken : NSCopying, NSSecureCoding {
		[Watch (10, 0), NoTV, NoMac, iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("deviceCapabilities", ArgumentSemantic.Copy)]
		INIDeviceCapability DeviceCapabilities { get; }
	}

	[Watch (8, 0), NoTV, NoMac, iOS (14, 0)]
	[MacCatalyst (14, 0)]
	[BaseType (typeof (NIConfiguration))]
	[DisableDefaultCtor]
	interface NINearbyPeerConfiguration {
		[Export ("peerDiscoveryToken", ArgumentSemantic.Copy)]
		NIDiscoveryToken PeerDiscoveryToken { get; }

		[Export ("initWithPeerToken:")]
		NativeHandle Constructor (NIDiscoveryToken peerToken);

		[NoWatch, iOS (16, 0), MacCatalyst (16, 0), NoTV, NoMac]
		[Export ("cameraAssistanceEnabled")]
		bool CameraAssistanceEnabled { [Bind ("isCameraAssistanceEnabled")] get; set; }

		[Watch (10, 0), NoTV, NoMac, iOS (17, 0), MacCatalyst (17, 0)]
		[Export ("extendedDistanceMeasurementEnabled")]
		bool ExtendedDistanceMeasurementEnabled { [Bind ("isExtendedDistanceMeasurementEnabled")] get; set; }
	}

	[Watch (8, 0), NoTV, NoMac, iOS (14, 0)]
	[MacCatalyst (14, 0)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	partial interface NINearbyObject : NSCopying, NSSecureCoding {
		[Export ("discoveryToken", ArgumentSemantic.Copy)]
		NIDiscoveryToken DiscoveryToken { get; }

		[Export ("distance")]
		float Distance { get; }

		[Export ("direction")]
		Vector3 Direction {
			[MarshalDirective (NativePrefix = "xamarin_simd__", Library = "__Internal")]
			get;
		}

		[Field ("NINearbyObjectDistanceNotAvailable")]
		float DistanceNotAvailable { get; }

		[Watch (9, 0), NoTV, NoMac, iOS (16, 0), MacCatalyst (16, 0)]
		[Field ("NINearbyObjectAngleNotAvailable")]
		float AngleNotAvailable { get; }

		[Watch (9, 0), NoTV, NoMac, iOS (16, 0), MacCatalyst (16, 0)]
		[Export ("verticalDirectionEstimate")]
		NINearbyObjectVerticalDirectionEstimate VerticalDirectionEstimate { get; }

		[Watch (9, 0), NoTV, NoMac, iOS (16, 0), MacCatalyst (16, 0)]
		[Export ("horizontalAngle")]
		float HorizontalAngle { get; }
	}

	[Watch (8, 0), NoTV, NoMac, iOS (14, 0)]
	[MacCatalyst (14, 0)]
	[BaseType (typeof (NSObject))]
	interface NISession {
		[Static]
		[Export ("supported")]
		bool IsSupported { [Bind ("isSupported")] get; }

		[Wrap ("WeakDelegate")]
		[NullAllowed]
		INISessionDelegate Delegate { get; set; }

		[NullAllowed, Export ("delegate", ArgumentSemantic.Weak)]
		NSObject WeakDelegate { get; set; }

		[NullAllowed, Export ("delegateQueue", ArgumentSemantic.Strong)]
		DispatchQueue DelegateQueue { get; set; }

		[NullAllowed, Export ("discoveryToken", ArgumentSemantic.Copy)]
		NIDiscoveryToken DiscoveryToken { get; }

		[NullAllowed, Export ("configuration", ArgumentSemantic.Copy)]
		NIConfiguration Configuration { get; }

		[Export ("runWithConfiguration:")]
		void Run (NIConfiguration configuration);

		[Export ("pause")]
		void Pause ();

		[Export ("invalidate")]
		void Invalidate ();

		[NoMacCatalyst] // We don't have ARKit bindings for Mac Catalyst (because ARKit doesn't work on Mac Catalyst), so we can't bind this method.
		[NoWatch, NoTV, NoMac, iOS (16, 0)]
		[Export ("setARSession:")]
		void SetARSession (ARSession session);

		[NoWatch, NoTV, NoMac, iOS (16, 0), MacCatalyst (16, 0)]
		[Export ("worldTransformForObject:")]
		[MarshalDirective (NativePrefix = "xamarin_simd__", Library = "__Internal")]
		MatrixFloat4x4 GetWorldTransform (NINearbyObject @object);

		[Watch (9, 0), NoTV, NoMac, iOS (16, 0), MacCatalyst (16, 0)]
		[Static]
		[Export ("deviceCapabilities")]
		INIDeviceCapability DeviceCapabilities { get; }
	}

	interface INISessionDelegate { }

	[Watch (8, 0), NoTV, NoMac, iOS (14, 0)]
	[MacCatalyst (14, 0)]
#if NET
	[Protocol, Model]
#else
	[Protocol]
	[Model (AutoGeneratedName = true)]
#endif
	[BaseType (typeof (NSObject))]
	interface NISessionDelegate {
		[Export ("session:didUpdateNearbyObjects:")]
		void DidSessionUpdateNearbyObjects (NISession session, NINearbyObject [] nearbyObjects);

		[Export ("session:didRemoveNearbyObjects:withReason:")]
		void DidSessionRemoveNearbyObjects (NISession session, NINearbyObject [] nearbyObjects, NINearbyObjectRemovalReason reason);

		[Export ("sessionWasSuspended:")]
		void SessionWasSuspended (NISession session);

		[Export ("sessionSuspensionEnded:")]
		void SessionSuspensionEnded (NISession session);

		[Export ("session:didInvalidateWithError:")]
		void DidSessionInvalidate (NISession session, NSError error);

		[Watch (8, 0), NoTV, NoMac, iOS (15, 0), MacCatalyst (15, 0)]
		[Export ("session:didGenerateShareableConfigurationData:forObject:")]
		void DidGenerateShareableConfigurationData (NISession session, NSData shareableConfigurationData, NINearbyObject @object);

		[Watch (9, 0), NoTV, NoMac, iOS (16, 0), MacCatalyst (16, 0)]
		[Export ("session:didUpdateAlgorithmConvergence:forObject:")]
		void DidUpdateAlgorithmConvergence (NISession session, NIAlgorithmConvergence convergence, [NullAllowed] NINearbyObject @object);

		[Watch (9, 0), NoTV, NoMac, iOS (16, 0), MacCatalyst (16, 0)]
		[Export ("sessionDidStartRunning:")]
		void DidSessionStartRunning (NISession session);
	}

	[Watch (8, 0), NoTV, NoMac, iOS (15, 0), MacCatalyst (15, 0)]
	[BaseType (typeof (NIConfiguration))]
	[DisableDefaultCtor]
	interface NINearbyAccessoryConfiguration {
		[Export ("accessoryDiscoveryToken", ArgumentSemantic.Copy)]
		NIDiscoveryToken AccessoryDiscoveryToken { get; }

		[Export ("initWithData:error:")]
		NativeHandle Constructor (NSData data, [NullAllowed] out NSError error);

		[iOS (16, 0), NoMac, NoWatch, NoTV, MacCatalyst (16, 0)]
		[Export ("initWithAccessoryData:bluetoothPeerIdentifier:error:")]
		NativeHandle Constructor (NSData accessoryData, NSUuid identifier, [NullAllowed] out NSError error);

		[iOS (16, 0), NoMac, NoWatch, NoTV, MacCatalyst (16, 0)]
		[Export ("cameraAssistanceEnabled")]
		bool CameraAssistanceEnabled { [Bind ("isCameraAssistanceEnabled")] get; set; }
	}

	[iOS (16, 0), NoMac, Watch (9, 0), NoTV, MacCatalyst (16, 0)]
	public enum NIAlgorithmConvergenceStatusReason {
		[Field ("NIAlgorithmConvergenceStatusReasonInsufficientHorizontalSweep")]
		InsufficientHorizontalSweep,

		[Field ("NIAlgorithmConvergenceStatusReasonInsufficientVerticalSweep")]
		InsufficientVerticalSweep,

		[Field ("NIAlgorithmConvergenceStatusReasonInsufficientMovement")]
		InsufficientMovement,

		[Field ("NIAlgorithmConvergenceStatusReasonInsufficientLighting")]
		InsufficientLighting,
	}

	interface INIDeviceCapability { }

	[Watch (9, 0), NoTV, NoMac, iOS (16, 0), MacCatalyst (16, 0)]
	[Protocol]
	interface NIDeviceCapability {
		[Abstract]
		[Export ("supportsPreciseDistanceMeasurement")]
		bool SupportsPreciseDistanceMeasurement { get; }

		[Abstract]
		[Export ("supportsDirectionMeasurement")]
		bool SupportsDirectionMeasurement { get; }

		[Abstract]
		[Export ("supportsCameraAssistance")]
		bool SupportsCameraAssistance { get; }

		[Watch (10, 0), NoTV, NoMac, iOS (17, 0), MacCatalyst (17, 0)]
#if XAMCORE_5_0
		[Abstract]
#endif
		[Export ("supportsExtendedDistanceMeasurement")]
		bool SupportsExtendedDistanceMeasurement { get; }
	}

	[Watch (9, 0), NoTV, NoMac, iOS (16, 0), MacCatalyst (16, 0)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	interface NIAlgorithmConvergence : NSCopying, NSSecureCoding {
		[Export ("status")]
		NIAlgorithmConvergenceStatus Status { get; }

		[Export ("reasons")]
		string [] Reasons { get; }
	}
}
