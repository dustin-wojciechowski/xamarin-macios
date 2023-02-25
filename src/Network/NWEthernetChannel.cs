//
// NWEthernetChannel.cs: Bindings the Network nw_ethernet_channel_t API.
//
// Authors:
//   Manuel de la Pena (mandel@microsoft.com)
//
// Copyright 2019 Microsoft
//

#nullable enable

#if MONOMAC
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ObjCRuntime;
using Foundation;
using CoreFoundation;

using OS_nw_ethernet_channel=System.IntPtr;
using OS_nw_interface=System.IntPtr;
using OS_dispatch_data=System.IntPtr;
using OS_nw_parameters=System.IntPtr;


#if !NET
using NativeHandle = System.IntPtr;
#endif

namespace Network {

#if NET
	// [SupportedOSPlatform ("macos10.15")]  -  Not valid on Delegates
	// [UnsupportedOSPlatform ("tvos")]
	// [UnsupportedOSPlatform ("ios")]
#else
	[NoWatch]
	[NoTV]
	[NoiOS]
	[Mac (10,15)]
#endif
	public delegate void NWEthernetChannelReceiveDelegate (DispatchData? content, ushort vlanTag, string? localAddress, string? remoteAddress);

#if NET
	[SupportedOSPlatform ("macos10.15")]
	[UnsupportedOSPlatform ("tvos")]
	[UnsupportedOSPlatform ("ios")]
#else
	[Mac (10,15)]
#endif
	public class NWEthernetChannel : NativeObject {

		[Preserve (Conditional = true)]
		internal NWEthernetChannel (NativeHandle handle, bool owns) : base (handle, owns) {}

		[DllImport (Constants.NetworkLibrary)]
		static extern OS_nw_ethernet_channel nw_ethernet_channel_create (ushort ether_type, OS_nw_interface networkInterface);

		// we cannot pass an enum! As per docs in the headers:
		// The custom EtherType to be used for all Ethernet frames in this channel. The
		// EtherType is the two-octet field in an Ethernet frame, indicating the protocol
		// encapsulated in the payload of the frame.  This parameter is in little-endian
		// byte order.  Only custom EtherType values are supported. This parameter cannot
		// be an EtherType already handled by the system, such as IPv4, IPv6, ARP, VLAN Tag,
		// or 802.1x.
		// 
		// Calling processes must hold the "com.apple.developer.networking.custom-protocol"
		// entitlement.
		public NWEthernetChannel (ushort ethernetType, NWInterface networkInterface)
		{
			if (networkInterface is null)
				ObjCRuntime.ThrowHelper.ThrowArgumentNullException (nameof (networkInterface));

			InitializeHandle (nw_ethernet_channel_create (ethernetType, networkInterface.Handle));
		}

#if NET
		[SupportedOSPlatform ("macos13.0")]
		[UnsupportedOSPlatform ("tvos")]
		[UnsupportedOSPlatform ("ios")]
#else
		[Mac (13,0)]
#endif
		[DllImport (Constants.NetworkLibrary)]
		static extern OS_nw_ethernet_channel nw_ethernet_channel_create_with_parameters (ushort ether_type, OS_nw_interface @interface, OS_nw_parameters parameters);

#if NET
		[SupportedOSPlatform ("macos13.0")]
		[UnsupportedOSPlatform ("tvos")]
		[UnsupportedOSPlatform ("ios")]
#else
		[Mac (13,0)]
#endif
		public NWEthernetChannel (ushort ethernetType, NWInterface networkInterface, NWParameters parameters) =>
			InitializeHandle (nw_ethernet_channel_create_with_parameters (ethernetType, 
						networkInterface.GetNonNullHandle (nameof (networkInterface)), parameters.GetNonNullHandle (nameof (parameters))));

		[DllImport (Constants.NetworkLibrary)]
		static extern void nw_ethernet_channel_start (OS_nw_ethernet_channel ethernet_channel);

		public void Start () => nw_ethernet_channel_start (GetCheckedHandle ()); 

		[DllImport (Constants.NetworkLibrary)]
		static extern void nw_ethernet_channel_cancel (OS_nw_ethernet_channel ethernet_channel);

		public void Cancel () => nw_ethernet_channel_cancel (GetCheckedHandle ());

		[DllImport (Constants.NetworkLibrary)]
		static extern void nw_ethernet_channel_set_queue (OS_nw_ethernet_channel ethernet_channel, IntPtr queue);

		public void SetQueue (DispatchQueue queue)
		{
			if (queue is null)
				ObjCRuntime.ThrowHelper.ThrowArgumentNullException (nameof (queue));
			nw_ethernet_channel_set_queue (GetCheckedHandle (), queue.Handle);
		}

		[DllImport (Constants.NetworkLibrary)]
		unsafe static extern void nw_ethernet_channel_send (OS_nw_ethernet_channel ethernet_channel, OS_dispatch_data content, ushort vlan_tag, string remote_address, BlockLiteral* completion);

		delegate void nw_ethernet_channel_send_completion_t (IntPtr block, IntPtr error);
		static nw_ethernet_channel_send_completion_t static_SendCompletion = TrampolineSendCompletion;

		[MonoPInvokeCallback (typeof (nw_ethernet_channel_send_completion_t))]
		static void TrampolineSendCompletion (IntPtr block, IntPtr error)
		{
			var del = BlockLiteral.GetTarget<Action<NWError?>> (block);
			if (del is not null) {
				using NWError? err = error == IntPtr.Zero ? null : new NWError (error, owns: false);
				del (err);
			}
		}

		[BindingImpl (BindingImplOptions.Optimizable)]
		public void Send (ReadOnlySpan<byte> content, ushort vlanTag, string remoteAddress, Action<NWError?> callback)
		{
			if (callback is null)
				ObjCRuntime.ThrowHelper.ThrowArgumentNullException (nameof (callback));

			using (var dispatchData = DispatchData.FromReadOnlySpan (content)) {
				unsafe {
					using var block = new BlockLiteral ();
					block.SetupBlockUnsafe (static_SendCompletion, callback);
					nw_ethernet_channel_send (GetCheckedHandle (), dispatchData.GetHandle (), vlanTag, remoteAddress, &block);
				}
			}
		}

		[DllImport (Constants.NetworkLibrary)]
		unsafe static extern void nw_ethernet_channel_set_receive_handler (OS_nw_ethernet_channel ethernet_channel, /* [NullAllowed] */ BlockLiteral *handler);

		delegate void nw_ethernet_channel_receive_handler_t (IntPtr block, OS_dispatch_data content, ushort vlan_tag, byte[] local_address, byte[] remote_address);
		static nw_ethernet_channel_receive_handler_t static_ReceiveHandler = TrampolineReceiveHandler;

		[MonoPInvokeCallback (typeof (nw_ethernet_channel_receive_handler_t))]
		static void TrampolineReceiveHandler (IntPtr block, OS_dispatch_data content, ushort vlanTag, byte[] localAddress, byte[] remoteAddress)
		{
			var del = BlockLiteral.GetTarget<NWEthernetChannelReceiveDelegate> (block);
			if (del is not null) {

				var dispatchData = (content == IntPtr.Zero) ? null : new DispatchData (content, owns: false);
				var local = (localAddress is null) ? null : Encoding.UTF8.GetString (localAddress);
				var remote = (remoteAddress is null) ? null : Encoding.UTF8.GetString (remoteAddress);

				del (dispatchData, vlanTag, local, remote);
			}
		}

		[BindingImpl (BindingImplOptions.Optimizable)]
		public void SetReceiveHandler (NWEthernetChannelReceiveDelegate handler)
		{
			unsafe {
				if (handler is null) {
					nw_ethernet_channel_set_receive_handler (GetCheckedHandle (), null);
					return;
				}

				using var block = new BlockLiteral ();
				block.SetupBlockUnsafe (static_ReceiveHandler, handler);
				nw_ethernet_channel_set_receive_handler (GetCheckedHandle (), &block);
			}
		}

		[DllImport (Constants.NetworkLibrary)]
		unsafe static extern void nw_ethernet_channel_set_state_changed_handler (OS_nw_ethernet_channel ethernet_channel, /* [NullAllowed] */ BlockLiteral *handler);

		delegate void nw_ethernet_channel_state_changed_handler_t (IntPtr block, NWEthernetChannelState state, IntPtr error);
		static nw_ethernet_channel_state_changed_handler_t static_StateChangesHandler = TrampolineStateChangesHandler;

		[MonoPInvokeCallback (typeof (nw_ethernet_channel_state_changed_handler_t))]
		static void TrampolineStateChangesHandler (IntPtr block, NWEthernetChannelState state, IntPtr error)
		{
			var del = BlockLiteral.GetTarget<Action<NWEthernetChannelState, NWError?>> (block);
			if (del is not null) {
				NWError? nwError = (error == IntPtr.Zero) ? null : new NWError (error, owns: false);
				del (state, nwError);
			}
		}

		[BindingImpl (BindingImplOptions.Optimizable)]
		public void SetStateChangesHandler (Action<NWBrowserState, NWError?> handler)
		{
			unsafe {
				if (handler is null) {
					nw_ethernet_channel_set_state_changed_handler (GetCheckedHandle (), null);
					return;
				}

				using var block = new BlockLiteral ();
				block.SetupBlockUnsafe (static_StateChangesHandler, handler);
				nw_ethernet_channel_set_state_changed_handler (GetCheckedHandle (), &block);
			}
		}	

#if NET
		[SupportedOSPlatform ("macos13.0")]
		[UnsupportedOSPlatform ("tvos")]
		[UnsupportedOSPlatform ("ios")]
#else
		[Mac (13,0)]
#endif
		[DllImport (Constants.NetworkLibrary)]
		static extern uint nw_ethernet_channel_get_maximum_payload_size (OS_nw_ethernet_channel ethernet_channel);

#if NET
		[SupportedOSPlatform ("macos13.0")]
		[UnsupportedOSPlatform ("tvos")]
		[UnsupportedOSPlatform ("ios")]
#else
		[Mac (13,0)]
#endif
		public uint MaximumPayloadSize  => nw_ethernet_channel_get_maximum_payload_size (GetCheckedHandle ());
	}
}
#endif
