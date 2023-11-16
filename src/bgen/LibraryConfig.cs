using System.Collections.Generic;
using System.Linq;
using ObjCRuntime;
using Xamarin.Utils;

#nullable enable

public class LibraryConfig {
	List<string> libs = new List<string> ();
	public bool skipSystemDrawing = false;
	public PlatformName CurrentPlatform;
	TargetFramework? target_framework;
	public TargetFramework TargetFramework {
		get { return target_framework!.Value; }
	}
	internal bool IsDotNet {
		get { return TargetFramework.IsDotNet; }
	}

	public string GetAttributeLibraryPath (string attributedll)
	{
		if (!string.IsNullOrEmpty (attributedll))
			return attributedll!;

		if (IsDotNet)
			return CurrentPlatform.GetPath ("lib", "Xamarin.Apple.BindingAttributes.dll");

		switch (CurrentPlatform) {
		case PlatformName.iOS:
			return CurrentPlatform.GetPath ("lib", "bgen", "Xamarin.iOS.BindingAttributes.dll");
		case PlatformName.WatchOS:
			return CurrentPlatform.GetPath ("lib", "bgen", "Xamarin.WatchOS.BindingAttributes.dll");
		case PlatformName.TvOS:
			return CurrentPlatform.GetPath ("lib", "bgen", "Xamarin.TVOS.BindingAttributes.dll");
		case PlatformName.MacCatalyst:
			return CurrentPlatform.GetPath ("lib", "bgen", "Xamarin.MacCatalyst.BindingAttributes.dll");
		case PlatformName.MacOSX:
			if (target_framework == TargetFramework.Xamarin_Mac_4_5_Full) {
				return CurrentPlatform.GetPath ("lib", "bgen", "Xamarin.Mac-full.BindingAttributes.dll");
			} else if (target_framework == TargetFramework.Xamarin_Mac_4_5_System) {
				return CurrentPlatform.GetPath ("lib", "bgen", "Xamarin.Mac-full.BindingAttributes.dll");
			} else if (target_framework == TargetFramework.Xamarin_Mac_2_0_Mobile) {
				return CurrentPlatform.GetPath ("lib", "bgen", "Xamarin.Mac-mobile.BindingAttributes.dll");
			} else {
				throw ErrorHelper.CreateError (1053, target_framework);
			}
		default:
			throw new BindingException (1047, CurrentPlatform);
		}
	}

	public IEnumerable<string> GetLibraryDirectories ()
	{
		if (!IsDotNet) {
			switch (CurrentPlatform) {
			case PlatformName.iOS:
				yield return CurrentPlatform.GetPath ("lib", "mono", "Xamarin.iOS");
				break;
			case PlatformName.WatchOS:
				yield return CurrentPlatform.GetPath ("lib", "mono", "Xamarin.WatchOS");
				break;
			case PlatformName.TvOS:
				yield return CurrentPlatform.GetPath ("lib", "mono", "Xamarin.TVOS");
				break;
			case PlatformName.MacCatalyst:
				yield return CurrentPlatform.GetPath ("lib", "mono", "Xamarin.MacCatalyst");
				break;
			case PlatformName.MacOSX:
				if (target_framework == TargetFramework.Xamarin_Mac_4_5_Full) {
					yield return CurrentPlatform.GetPath ("lib", "reference", "full");
					yield return CurrentPlatform.GetPath ("lib", "mono", "4.5");
				} else if (target_framework == TargetFramework.Xamarin_Mac_4_5_System) {
					yield return "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5";
					yield return CurrentPlatform.GetPath ("lib", "mono", "4.5");
				} else if (target_framework == TargetFramework.Xamarin_Mac_2_0_Mobile) {
					yield return CurrentPlatform.GetPath ("lib", "mono", "Xamarin.Mac");
				} else {
					throw ErrorHelper.CreateError (1053, target_framework);
				}
				break;
			default:
				throw new BindingException (1047, CurrentPlatform);
			}
		}
		foreach (var lib in libs)
			yield return lib;
	}

	public void SetTargetFramework (string fx)
	{
		TargetFramework tf;
		if (!TargetFramework.TryParse (fx, out tf))
			throw ErrorHelper.CreateError (68, fx);
		target_framework = tf;

		if (!TargetFramework.IsValidFramework (target_framework.Value))
			throw ErrorHelper.CreateError (70, target_framework.Value,
				string.Join (" ", TargetFramework.ValidFrameworks.Select ((v) => v.ToString ()).ToArray ()));
	}

	public bool SetBaseLibDllAndReferences(ref string baselibdll, ref List<string> references) // TODO is ref good idea?
	{
		bool nostdlib = false; // TODO make sure default to false is recommended
		if (!target_framework.HasValue)
			throw ErrorHelper.CreateError(86);

		switch (target_framework.Value.Platform)
		{
		case ApplePlatform.iOS:
			CurrentPlatform = PlatformName.iOS;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
				baselibdll = CurrentPlatform.GetPath( "lib/mono/Xamarin.iOS/Xamarin.iOS.dll");
			if (!IsDotNet)
			{
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(CurrentPlatform, "lib/mono/Xamarin.iOS", references);
			}

			break;
		case ApplePlatform.TVOS:
			CurrentPlatform = PlatformName.TvOS;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
				baselibdll = CurrentPlatform.GetPath( "lib/mono/Xamarin.TVOS/Xamarin.TVOS.dll");
			if (!IsDotNet)
			{
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(CurrentPlatform, "lib/mono/Xamarin.TVOS", references);
			}

			break;
		case ApplePlatform.WatchOS:
			CurrentPlatform = PlatformName.WatchOS;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
				baselibdll = CurrentPlatform.GetPath( "lib/mono/Xamarin.WatchOS/Xamarin.WatchOS.dll");
			if (!IsDotNet)
			{
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(CurrentPlatform, "lib/mono/Xamarin.WatchOS", references);
			}

			break;
		case ApplePlatform.MacCatalyst:
			CurrentPlatform = PlatformName.MacCatalyst;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
				baselibdll = CurrentPlatform.GetPath( "lib/mono/Xamarin.MacCatalyst/Xamarin.MacCatalyst.dll");
			if (!IsDotNet)
			{
				// references.Add ("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(CurrentPlatform, "lib/mono/Xamarin.MacCatalyst", references);
			}

			break;
		case ApplePlatform.MacOSX:
			CurrentPlatform = PlatformName.MacOSX;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
			{
				if (target_framework == TargetFramework.Xamarin_Mac_2_0_Mobile)
					baselibdll = CurrentPlatform.GetPath( "lib", "reference", "mobile", "Xamarin.Mac.dll");
				else if (target_framework == TargetFramework.Xamarin_Mac_4_5_Full ||
				         target_framework == TargetFramework.Xamarin_Mac_4_5_System)
					baselibdll = CurrentPlatform.GetPath( "lib", "reference", "full", "Xamarin.Mac.dll");
				else if (target_framework == TargetFramework.DotNet_macOS)
					baselibdll = CurrentPlatform.GetPath( "lib", "mono", "Xamarin.Mac", "Xamarin.Mac.dll");
				else
					throw ErrorHelper.CreateError(1053, target_framework);
			}

			if (target_framework == TargetFramework.Xamarin_Mac_2_0_Mobile)
			{
				skipSystemDrawing = true;
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(CurrentPlatform, "lib/mono/Xamarin.Mac", references);
			}
			else if (target_framework == TargetFramework.Xamarin_Mac_4_5_Full)
			{
				skipSystemDrawing = true;
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(CurrentPlatform, "lib/mono/4.5", references);
			}
			else if (target_framework == TargetFramework.Xamarin_Mac_4_5_System)
			{
				skipSystemDrawing = false;
				ReferenceFixer.FixSDKReferences("/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5",
					references, forceSystemDrawing: true);
			}
			else if (target_framework == TargetFramework.DotNet_macOS)
			{
				skipSystemDrawing = false;
			}
			else
			{
				throw ErrorHelper.CreateError(1053, target_framework);
			}

			break;
		default:
			throw ErrorHelper.CreateError(1053, target_framework);
		}

		return nostdlib;
	}
}

