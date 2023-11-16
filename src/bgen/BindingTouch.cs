//
// Authors:
//   Miguel de Icaza
//
// Copyright 2011-2014 Xamarin Inc.
// Copyright 2009-2010 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Mono.Options;

using ObjCRuntime;
using Foundation;

using Xamarin.Bundler;
using Xamarin.Utils;

public class BindingTouch : IDisposable {
	TargetFramework? target_framework;
#if NET
	public static ApplePlatform [] AllPlatforms = new ApplePlatform [] { ApplePlatform.iOS, ApplePlatform.MacOSX, ApplePlatform.TVOS, ApplePlatform.MacCatalyst };
	public static PlatformName [] AllPlatformNames = new PlatformName [] { PlatformName.iOS, PlatformName.MacOSX, PlatformName.TvOS, PlatformName.MacCatalyst };
#else
	public static ApplePlatform [] AllPlatforms = new ApplePlatform [] { ApplePlatform.iOS, ApplePlatform.MacOSX, ApplePlatform.TVOS, ApplePlatform.WatchOS };
	public static PlatformName [] AllPlatformNames = new PlatformName [] { PlatformName.iOS, PlatformName.MacOSX, PlatformName.TvOS, PlatformName.WatchOS };
#endif
	public PlatformName CurrentPlatform;
	public bool BindThirdPartyLibrary = true;
	public bool skipSystemDrawing;
	public string? outfile;

#if !NET
	const string DefaultCompiler = "/Library/Frameworks/Mono.framework/Versions/Current/bin/csc";
#endif
	string compiler = string.Empty;
	string []? compile_command = null;
	string? baselibdll;
	string? attributedll;
	string compiled_api_definition_assembly = string.Empty;
	bool noNFloatUsing;

	List<string> libs = new List<string> ();
	List<string> references = new List<string> ();

	public MetadataLoadContext? universe;
	public Frameworks? Frameworks;

	AttributeManager? attributeManager;
	public AttributeManager AttributeManager => attributeManager!;

	TypeManager? typeManager;
	public TypeManager TypeManager => typeManager!;

	NamespaceManager? namespaceManager;
	public NamespaceManager NamespaceManager => namespaceManager!;

	TypeCache? typeCache;
	public TypeCache TypeCache => typeCache!;

	bool disposedValue;
	readonly Dictionary<System.Type, Type> ikvm_type_lookup = new Dictionary<System.Type, Type> ();
	internal Dictionary<System.Type, Type> IKVMTypeLookup {
		get { return ikvm_type_lookup; }
	}

	public TargetFramework TargetFramework {
		get { return target_framework!.Value; }
	}

	public static string ToolName {
		get { return "bgen"; }
	}

	internal bool IsDotNet {
		get { return TargetFramework.IsDotNet; }
	}

	static void ShowHelp (OptionSet os)
	{
		Console.WriteLine ("{0} - Mono Objective-C API binder", ToolName);
		Console.WriteLine ("Usage is:\n {0} [options] apifile1.cs [--api=apifile2.cs [--api=apifile3.cs]] [-s=core1.cs [-s=core2.cs]] [core1.cs [core2.cs]] [-x=extra1.cs [-x=extra2.cs]]", ToolName);

		os.WriteOptionDescriptions (Console.Out);
	}

	public static int Main (string [] args)
	{
		try {
			return Main2 (args);
		} catch (Exception ex) {
			ErrorHelper.Show (ex, false);
			return 1;
		}
	}

	string GetAttributeLibraryPath ()
	{
		if (!string.IsNullOrEmpty (attributedll))
			return attributedll!;

		if (IsDotNet)
			return Path.Combine (GetSDKRoot (), "lib", "Xamarin.Apple.BindingAttributes.dll");

		switch (CurrentPlatform) {
		case PlatformName.iOS:
			return Path.Combine (GetSDKRoot (), "lib", "bgen", "Xamarin.iOS.BindingAttributes.dll");
		case PlatformName.WatchOS:
			return Path.Combine (GetSDKRoot (), "lib", "bgen", "Xamarin.WatchOS.BindingAttributes.dll");
		case PlatformName.TvOS:
			return Path.Combine (GetSDKRoot (), "lib", "bgen", "Xamarin.TVOS.BindingAttributes.dll");
		case PlatformName.MacCatalyst:
			return Path.Combine (GetSDKRoot (), "lib", "bgen", "Xamarin.MacCatalyst.BindingAttributes.dll");
		case PlatformName.MacOSX:
			if (target_framework == TargetFramework.Xamarin_Mac_4_5_Full) {
				return Path.Combine (GetSDKRoot (), "lib", "bgen", "Xamarin.Mac-full.BindingAttributes.dll");
			} else if (target_framework == TargetFramework.Xamarin_Mac_4_5_System) {
				return Path.Combine (GetSDKRoot (), "lib", "bgen", "Xamarin.Mac-full.BindingAttributes.dll");
			} else if (target_framework == TargetFramework.Xamarin_Mac_2_0_Mobile) {
				return Path.Combine (GetSDKRoot (), "lib", "bgen", "Xamarin.Mac-mobile.BindingAttributes.dll");
			} else {
				throw ErrorHelper.CreateError (1053, target_framework);
			}
		default:
			throw new BindingException (1047, CurrentPlatform);
		}
	}

	IEnumerable<string> GetLibraryDirectories ()
	{
		if (!IsDotNet) {
			switch (CurrentPlatform) {
			case PlatformName.iOS:
				yield return Path.Combine (GetSDKRoot (), "lib", "mono", "Xamarin.iOS");
				break;
			case PlatformName.WatchOS:
				yield return Path.Combine (GetSDKRoot (), "lib", "mono", "Xamarin.WatchOS");
				break;
			case PlatformName.TvOS:
				yield return Path.Combine (GetSDKRoot (), "lib", "mono", "Xamarin.TVOS");
				break;
			case PlatformName.MacCatalyst:
				yield return Path.Combine (GetSDKRoot (), "lib", "mono", "Xamarin.MacCatalyst");
				break;
			case PlatformName.MacOSX:
				if (target_framework == TargetFramework.Xamarin_Mac_4_5_Full) {
					yield return Path.Combine (GetSDKRoot (), "lib", "reference", "full");
					yield return Path.Combine (GetSDKRoot (), "lib", "mono", "4.5");
				} else if (target_framework == TargetFramework.Xamarin_Mac_4_5_System) {
					yield return "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5";
					yield return Path.Combine (GetSDKRoot (), "lib", "mono", "4.5");
				} else if (target_framework == TargetFramework.Xamarin_Mac_2_0_Mobile) {
					yield return Path.Combine (GetSDKRoot (), "lib", "mono", "Xamarin.Mac");
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

	string GetSDKRoot ()
	{
		switch (CurrentPlatform) {
		case PlatformName.iOS:
		case PlatformName.WatchOS:
		case PlatformName.TvOS:
		case PlatformName.MacCatalyst:
			var sdkRoot = Environment.GetEnvironmentVariable ("MD_MTOUCH_SDK_ROOT");
			if (string.IsNullOrEmpty (sdkRoot))
				sdkRoot = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current";
			return sdkRoot;
		case PlatformName.MacOSX:
			var macSdkRoot = Environment.GetEnvironmentVariable ("XamarinMacFrameworkRoot");
			if (string.IsNullOrEmpty (macSdkRoot))
				macSdkRoot = "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current";
			return macSdkRoot;
		default:
			throw new BindingException (1047, CurrentPlatform);
		}
	}

	void SetTargetFramework (string fx)
	{
		TargetFramework tf;
		if (!TargetFramework.TryParse (fx, out tf))
			throw ErrorHelper.CreateError (68, fx);
		target_framework = tf;

		if (!TargetFramework.IsValidFramework (target_framework.Value))
			throw ErrorHelper.CreateError (70, target_framework.Value, string.Join (" ", TargetFramework.ValidFrameworks.Select ((v) => v.ToString ()).ToArray ()));
	}

	static int Main2 (string [] args)
	{
		using var touch = new BindingTouch ();
		return touch.Main3 (args);
	}

	int Main3 (string [] args)
	{
		// TODO there's a lot of logic where code is just returning 1 or 0 immediately. I need to work on the control flow for these. They should probably just throw an exception up based on where they are.
		ErrorHelper.ClearWarningLevels ();
		BindingTouchConfig bindingTouchConfig = new();
		// TODO CreateOptionSet creates that value in the datablob. Is that really necessary?
		if (!CreateOptionSet (bindingTouchConfig, args) || !InitializeApi (ref bindingTouchConfig) ||
		    !InitializeManagers (ref bindingTouchConfig) || !TestLinkWith (bindingTouchConfig))
			return 1; // TODO Notate in the PR every time program returns 1 or 0. Also, highly disagreeable to have this "Main3" return numbers

		PopulateTypesAndStrongDictionaries( bindingTouchConfig.api, bindingTouchConfig.process_enums, out List<Type> types2, out List<Type> strong_dictionaries2);
		PerformGenerate (ref bindingTouchConfig, types2, strong_dictionaries2);

		return 0;
	}

	private void PerformGenerate (ref BindingTouchConfig bindingTouchConfig, List<Type> types, List<Type> strong_dictionaries)
	{
		// Slit this here. Ideally, init handled in its own thing. We just create the generator object
		// and call Go on it.
		try {
			var g =
				new Generator (this, bindingTouchConfig.public_mode, bindingTouchConfig.external, bindingTouchConfig.debug, types.ToArray (), strong_dictionaries.ToArray ()) {
					BaseDir = bindingTouchConfig.basedir ?? bindingTouchConfig.tmpdir,
					ZeroCopyStrings = bindingTouchConfig.zero_copy,
					InlineSelectors = bindingTouchConfig.inline_selectors ?? (CurrentPlatform != PlatformName.MacOSX),
				};


			g.Go ();
			List<string> cargs = CreateCompilationArguments (bindingTouchConfig, g.GeneratedFiles);

			if (bindingTouchConfig.generate_file_list is not null) {
				using (var f = File.CreateText (bindingTouchConfig.generate_file_list)) {
					foreach (var x in g.GeneratedFiles.OrderBy ((v) => v))
						f.WriteLine (x);
				}

				return;
			}

			AddNFloatUsing (cargs, bindingTouchConfig.tmpdir);

			Compile (cargs, 1000, bindingTouchConfig.tmpdir);
		} finally {
			if (bindingTouchConfig.delete_temp)
				Directory.Delete (bindingTouchConfig.tmpdir, true);
		}
	}

	// TODO Want this to be in BindingTouchConfig or even its own thing, but it's coupled to even more class variables
	private List<string> CreateCompilationArguments (BindingTouchConfig bindingTouchConfig, IEnumerable<string> generatedFiles)
	{
		List<string> cargs = new();
		if (bindingTouchConfig.unsafef)
			cargs.Add ("-unsafe");
		cargs.Add ("-target:library");
		cargs.Add ("-out:" + outfile);
		foreach (var def in bindingTouchConfig.defines)
			cargs.Add ("-define:" + def);
#if NET
			cargs.Add ("-define:NET");
#endif
		cargs.AddRange (generatedFiles);
		cargs.AddRange (bindingTouchConfig.core_sources);
		cargs.AddRange (bindingTouchConfig.extra_sources);
		cargs.AddRange (bindingTouchConfig.refs);
		cargs.Add ("-r:" + baselibdll);
		cargs.AddRange (bindingTouchConfig.resources);
		if (bindingTouchConfig.nostdlib) {
			cargs.Add ("-nostdlib");
			cargs.Add ("-noconfig");
		}

		if (!string.IsNullOrEmpty (Path.GetDirectoryName (baselibdll)))
			cargs.Add ("-lib:" + Path.GetDirectoryName (baselibdll));

		return cargs;
	}

	private bool CreateOptionSet ( BindingTouchConfig bindingTouchConfig, string[] args)
	{
		 bindingTouchConfig.os = new OptionSet () {
			{ "h|?|help", "Displays the help", v => bindingTouchConfig.show_help = true },
			{ "a", "Include alpha bindings (Obsolete).", v => {}, true },
			{ "outdir=", "Sets the output directory for the temporary binding files", v => { bindingTouchConfig.basedir = v; }},
			{ "o|out=", "Sets the name of the output library", v => outfile = v },
			{ "tmpdir=", "Sets the working directory for temp files", v => { bindingTouchConfig.tmpdir = v; bindingTouchConfig.delete_temp = false; }},
			{ "debug", "Generates a debugging build of the binding", v => bindingTouchConfig.debug = true },
			{ "sourceonly=", "Only generates the source", v => bindingTouchConfig.generate_file_list = v },
			{ "ns=", "Sets the namespace for storing helper classes", v => bindingTouchConfig.ns = v },
			{ "unsafe", "Sets the unsafe flag for the build", v=> bindingTouchConfig.unsafef = true },
			{ "core", "Use this to build product assemblies", v => BindThirdPartyLibrary = false },
			{ "r|reference=", "Adds a reference", v => references.Add (v) },
			{ "lib=", "Adds the directory to the search path for the compiler", v => libs.Add (v) },
			{ "compiler=", "Sets the compiler to use (Obsolete) ", v => compiler = v, true },
			{ "compile-command=", "Sets the command to execute the C# compiler (this be an executable + arguments).", v =>
				{
					if (!StringUtils.TryParseArguments (v, out compile_command, out var ex))
						throw ErrorHelper.CreateError (27, "--compile-command", ex);
				}
			},
			{ "sdk=", "Sets the .NET SDK to use (Obsolete)", v => {}, true },
			{ "new-style", "Build for Unified (Obsolete).", v => { Console.WriteLine ("The --new-style option is obsolete and ignored."); }, true},
			{ "d=", "Defines a symbol", v => bindingTouchConfig.defines.Add (v) },
			{ "api=", "Adds a API definition source file", v => bindingTouchConfig.api_sources.Add (v) },
			{ "s=", "Adds a source file required to build the API", v => bindingTouchConfig.core_sources.Add (v) },
			{ "q", "Quiet", v => ErrorHelper.Verbosity-- },
			{ "v", "Sets verbose mode", v => ErrorHelper.Verbosity++ },
			{ "x=", "Adds the specified file to the build, used after the core files are compiled", v => bindingTouchConfig.extra_sources.Add (v) },
			{ "e", "Generates smaller classes that can not be subclassed (previously called 'external mode')", v => bindingTouchConfig.external = true },
			{ "p", "Sets private mode", v => bindingTouchConfig.public_mode = false },
			{ "baselib=", "Sets the base library", v => baselibdll = v },
			{ "attributelib=", "Sets the attribute library", v => attributedll = v },
			{ "use-zero-copy", v=> bindingTouchConfig.zero_copy = true },
			{ "nostdlib", "Does not reference mscorlib.dll library", l => bindingTouchConfig.nostdlib = true },
			{ "no-mono-path", "Launches compiler with empty MONO_PATH", l => { }, true },
			{ "native-exception-marshalling", "Enable the marshalling support for Objective-C exceptions", (v) => { /* no-op */} },
			{ "inline-selectors:", "If Selector.GetHandle is inlined and does not need to be cached (enabled by default in Xamarin.iOS, disabled in Xamarin.Mac)",
				v => bindingTouchConfig.inline_selectors = string.Equals ("true", v, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty (v)
			},
			{ "process-enums", "Process enums as bindings, not external, types.", v => bindingTouchConfig.process_enums = true },
			{ "link-with=,", "Link with a native library {0:FILE} to the binding, embedded as a resource named {1:ID}",
				(path, id) => {
					if (path is null || path.Length == 0)
						throw new Exception ("-link-with=FILE,ID requires a filename.");

					if (id is null || id.Length == 0)
						id = Path.GetFileName (path);

					if (bindingTouchConfig.linkwith.Contains (id))
						throw new Exception ("-link-with=FILE,ID cannot assign the same resource id to multiple libraries.");

					bindingTouchConfig.resources.Add (string.Format ("-res:{0},{1}", path, id));
					bindingTouchConfig.linkwith.Add (id);
				}
			},
			{ "unified-full-profile", "Launches compiler pointing to XM Full Profile", l => { /* no-op*/ }, true },
			{ "unified-mobile-profile", "Launches compiler pointing to XM Mobile Profile", l => { /* no-op*/ }, true },
			{ "target-framework=", "Specify target framework to use. Always required, and the currently supported values are: 'Xamarin.iOS,v1.0', 'Xamarin.TVOS,v1.0', 'Xamarin.WatchOS,v1.0', 'XamMac,v1.0', 'Xamarin.Mac,Version=v2.0,Profile=Mobile', 'Xamarin.Mac,Version=v4.5,Profile=Full' and 'Xamarin.Mac,Version=v4.5,Profile=System')", v => SetTargetFramework (v) },
			{ "warnaserror:", "An optional comma-separated list of warning codes that should be reported as errors (if no warnings are specified all warnings are reported as errors).", v => {
					try {
						if (!string.IsNullOrEmpty (v)) {
							foreach (var code in v.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
								ErrorHelper.SetWarningLevel (ErrorHelper.WarningLevel.Error, int.Parse (code));
						} else {
							ErrorHelper.SetWarningLevel (ErrorHelper.WarningLevel.Error);
						}
					} catch (Exception ex) {
						throw ErrorHelper.CreateError (26, ex.Message);
					}
				}
			},
			{ "nowarn:", "An optional comma-separated list of warning codes to ignore (if no warnings are specified all warnings are ignored).", v => {
					try {
						if (!string.IsNullOrEmpty (v)) {
							foreach (var code in v.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
								ErrorHelper.SetWarningLevel (ErrorHelper.WarningLevel.Disable, int.Parse (code));
						} else {
							ErrorHelper.SetWarningLevel (ErrorHelper.WarningLevel.Disable);
						}
					} catch (Exception ex) {
						throw ErrorHelper.CreateError (26, ex.Message);
					}
				}
			},
			{ "no-nfloat-using:", "If a global using alias directive for 'nfloat = System.Runtime.InteropServices.NFloat' should automatically be created.", (v) => {
					noNFloatUsing = string.Equals ("true", v, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty (v);
				}
			},
			{ "compiled-api-definition-assembly=", "An assembly with the compiled api definitions.", (v) => compiled_api_definition_assembly = v },
			new Mono.Options.ResponseFileSource (),
		};
		
		try {
			bindingTouchConfig.sources = bindingTouchConfig.os.Parse (args);
		} catch (Exception e) {
			Console.Error.WriteLine ("{0}: {1}", ToolName, e.Message); // TODO: Are these going to correctly appear in the messages screen?
			Console.Error.WriteLine ("see {0} --help for more information", ToolName);
			return false;
		}

		if (bindingTouchConfig.show_help) {
			ShowHelp (bindingTouchConfig.os);
			return false;
		}

		return true; // TODO change this, I just want to get moving

		//return bindingTouchConfig;
	}

	// TODO: I think this and the other api stuff can be separated into its own class probably
	private bool InitializeApi (ref BindingTouchConfig bindingTouchConfig)
	{
		bindingTouchConfig.nostdlib = Nostdlib();

		if (bindingTouchConfig.sources.Count > 0) {
			bindingTouchConfig.api_sources.Insert (0, bindingTouchConfig.sources [0]);
			for (int i = 1; i < bindingTouchConfig.sources.Count; i++)
				bindingTouchConfig.core_sources.Insert (i - 1, bindingTouchConfig.sources [i]);
		}

		if (bindingTouchConfig.api_sources.Count == 0) {
			Console.WriteLine ("Error: no api file provided");
			ShowHelp (bindingTouchConfig.os);
			return false;
		}

		if (bindingTouchConfig.tmpdir is null)
			bindingTouchConfig.tmpdir = GetWorkDir ();

		bindingTouchConfig.firstApiDefinitionName = Path.GetFileNameWithoutExtension (bindingTouchConfig.api_sources [0]);
		bindingTouchConfig.firstApiDefinitionName = bindingTouchConfig.firstApiDefinitionName.Replace ('-', '_'); // This is not exhaustive, but common.
		if (outfile is null)
			outfile = bindingTouchConfig.firstApiDefinitionName + ".dll";

		bindingTouchConfig.refs = references.Select ((v) => "-r:" + v);
		bindingTouchConfig.paths = libs.Select ((v) => "-lib:" + v);

		try {
			var tmpass =
				GetCompiledApiBindingsAssembly (bindingTouchConfig.tmpdir, bindingTouchConfig.refs, bindingTouchConfig.nostdlib, bindingTouchConfig.api_sources,
					bindingTouchConfig.core_sources, bindingTouchConfig.defines, bindingTouchConfig.paths, ref bindingTouchConfig);
			universe = new MetadataLoadContext (
				new SearchPathsAssemblyResolver (
					GetLibraryDirectories ().ToArray (),
					references.ToArray ()),
				"mscorlib"
			);

			if (!TryLoadApi (tmpass, out bindingTouchConfig.api) || !TryLoadApi (baselibdll, out bindingTouchConfig.baselib))
				return false;

			// Explicitly load our attribute library so that IKVM doesn't try (and fail) to find it.
			universe.LoadFromAssemblyPath (GetAttributeLibraryPath ());
			foreach (var r in references) {
				// IKVM has a bug where it doesn't correctly compare assemblies, which means it
				// can end up loading the same assembly (in particular any System.Runtime whose
				// version > 4.0, but likely others as well) more than once. This is bad, because
				// we compare types based on reference equality, which breaks down when there are
				// multiple instances of the same type.
				// 
				// So just don't ask IKVM to load assemblies that have already been loaded.
				var fn = Path.GetFileNameWithoutExtension (r);
				var assemblies = universe.GetAssemblies ();
				if (assemblies.Any ((v) => v.GetName ().Name == fn))
					continue;

				if (File.Exists (r)) {
					try {
						universe.LoadFromAssemblyPath (r);
					} catch (Exception ex) {
						ErrorHelper.Warning (1104, r, ex.Message);
					}
				}
			}
		} catch (Exception ex) {
			ErrorHelper.Show (ex);
		}

		return true;
	}

	private bool InitializeManagers  (ref BindingTouchConfig bindingTouchConfig)
	{
			attributeManager ??= new AttributeManager (this);
			Frameworks = new Frameworks (CurrentPlatform);

			typeCache ??= new(universe, Frameworks, CurrentPlatform, bindingTouchConfig.api, universe.CoreAssembly, bindingTouchConfig.baselib,
				BindThirdPartyLibrary);
			typeManager ??= new(this);
			
			namespaceManager ??= new NamespaceManager (
				CurrentPlatform,
				bindingTouchConfig.ns ?? bindingTouchConfig.firstApiDefinitionName,
				skipSystemDrawing);

			// Perhaps the above is a method without output for types and strong-dictionaryies?

			return true;
	}

	private bool TestLinkWith (BindingTouchConfig bindingTouchConfig)
	{
		foreach (var linkWith in AttributeManager.GetCustomAttributes<LinkWithAttribute> (bindingTouchConfig.api)) {
#if NET
				if (string.IsNullOrEmpty (linkWith.LibraryName))
#else
			if (linkWith.LibraryName is null || string.IsNullOrEmpty (linkWith.LibraryName))
#endif
				continue;

			if (!bindingTouchConfig.linkwith.Contains (linkWith.LibraryName)) {
				Console.Error.WriteLine (
					"Missing native library {0}, please use `--link-with' to specify the path to this library.",
					linkWith.LibraryName);
				return false;
			}
		}

		return true;
	}

	private void PopulateTypesAndStrongDictionaries (Assembly api, bool process_enums, out List<Type> types, out List<Type> strong_dictionaries)
	{
		types = new List<Type> ();
		strong_dictionaries = new List<Type> ();
		foreach (var t in api.GetTypes ()) {
			if ((process_enums && t.IsEnum) ||
			    AttributeManager.HasAttribute<BaseTypeAttribute> (t) ||
			    AttributeManager.HasAttribute<ProtocolAttribute> (t) ||
			    AttributeManager.HasAttribute<StaticAttribute> (t) ||
			    AttributeManager.HasAttribute<PartialAttribute> (t))
				types.Add (t);
			if (AttributeManager.HasAttribute<StrongDictionaryAttribute> (t))
				strong_dictionaries.Add (t);
		}
	}

	bool TryLoadApi (string name, out Assembly api)
	{
		api = null;
		try {
			api = universe.LoadFromAssemblyPath (name);
		} catch (Exception e) {
			if (Driver.Verbosity > 0)
				Console.WriteLine (e);

			Console.Error.WriteLine ("Error loading  {0}", name);
			return false;
		}

		return true;
	}

	private bool Nostdlib()
	{
		bool nostdlib;
		if (!target_framework.HasValue)
			throw ErrorHelper.CreateError(86);

		switch (target_framework.Value.Platform)
		{
		case ApplePlatform.iOS:
			CurrentPlatform = PlatformName.iOS;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
				baselibdll = Path.Combine(GetSDKRoot(), "lib/mono/Xamarin.iOS/Xamarin.iOS.dll");
			if (!IsDotNet)
			{
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(GetSDKRoot(), "lib/mono/Xamarin.iOS", references);
			}

			break;
		case ApplePlatform.TVOS:
			CurrentPlatform = PlatformName.TvOS;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
				baselibdll = Path.Combine(GetSDKRoot(), "lib/mono/Xamarin.TVOS/Xamarin.TVOS.dll");
			if (!IsDotNet)
			{
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(GetSDKRoot(), "lib/mono/Xamarin.TVOS", references);
			}

			break;
		case ApplePlatform.WatchOS:
			CurrentPlatform = PlatformName.WatchOS;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
				baselibdll = Path.Combine(GetSDKRoot(), "lib/mono/Xamarin.WatchOS/Xamarin.WatchOS.dll");
			if (!IsDotNet)
			{
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(GetSDKRoot(), "lib/mono/Xamarin.WatchOS", references);
			}

			break;
		case ApplePlatform.MacCatalyst:
			CurrentPlatform = PlatformName.MacCatalyst;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
				baselibdll = Path.Combine(GetSDKRoot(), "lib/mono/Xamarin.MacCatalyst/Xamarin.MacCatalyst.dll");
			if (!IsDotNet)
			{
				// references.Add ("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(GetSDKRoot(), "lib/mono/Xamarin.MacCatalyst", references);
			}

			break;
		case ApplePlatform.MacOSX:
			CurrentPlatform = PlatformName.MacOSX;
			nostdlib = true;
			if (string.IsNullOrEmpty(baselibdll))
			{
				if (target_framework == TargetFramework.Xamarin_Mac_2_0_Mobile)
					baselibdll = Path.Combine(GetSDKRoot(), "lib", "reference", "mobile", "Xamarin.Mac.dll");
				else if (target_framework == TargetFramework.Xamarin_Mac_4_5_Full ||
				         target_framework == TargetFramework.Xamarin_Mac_4_5_System)
					baselibdll = Path.Combine(GetSDKRoot(), "lib", "reference", "full", "Xamarin.Mac.dll");
				else if (target_framework == TargetFramework.DotNet_macOS)
					baselibdll = Path.Combine(GetSDKRoot(), "lib", "mono", "Xamarin.Mac", "Xamarin.Mac.dll");
				else
					throw ErrorHelper.CreateError(1053, target_framework);
			}

			if (target_framework == TargetFramework.Xamarin_Mac_2_0_Mobile)
			{
				skipSystemDrawing = true;
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(GetSDKRoot(), "lib/mono/Xamarin.Mac", references);
			}
			else if (target_framework == TargetFramework.Xamarin_Mac_4_5_Full)
			{
				skipSystemDrawing = true;
				references.Add("Facades/System.Drawing.Common");
				ReferenceFixer.FixSDKReferences(GetSDKRoot(), "lib/mono/4.5", references);
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

	List<string> GetCompiledApiBindingsArgs (string tmpass, ref BindingTouchConfig bindingTouchConfig) // TODO what is tmpass??
	{
		// -nowarn:436 is to avoid conflicts in definitions between core.dll and the sources
		// Keep source files at the end of the command line - csc will create TWO assemblies if any sources preceed the -out parameter
		var cargs = new List<string> ();

		cargs.Add ("-debug");
		cargs.Add ("-unsafe");
		cargs.Add ("-target:library");
		cargs.Add ("-nowarn:436");
		cargs.Add ("-out:" + tmpass);
		cargs.Add ("-r:" + GetAttributeLibraryPath ());
		cargs.AddRange (bindingTouchConfig.refs);
		cargs.Add ("-r:" + baselibdll);
		foreach (var def in bindingTouchConfig.defines)
			cargs.Add ("-define:" + def);
#if NET
		cargs.Add ("-define:NET");
#endif
		cargs.AddRange (bindingTouchConfig.paths);
		if (bindingTouchConfig.nostdlib) {
			cargs.Add ("-nostdlib");
			cargs.Add ("-noconfig");
		}
		cargs.AddRange (bindingTouchConfig.api_sources);
		cargs.AddRange (bindingTouchConfig.core_sources);
		if (!string.IsNullOrEmpty (Path.GetDirectoryName (baselibdll)))
			cargs.Add ("-lib:" + Path.GetDirectoryName (baselibdll));

		return cargs;
	}

	// If anything is modified in this function, check if the _CompileApiDefinitions MSBuild target needs to be updated as well.
	string GetCompiledApiBindingsAssembly (string tmpdir, IEnumerable<string> refs, bool nostdlib, List<string> api_sources, List<string> core_sources, List<string> defines, IEnumerable<string> paths, ref BindingTouchConfig bindingTouchConfig)
	{
		if (!string.IsNullOrEmpty (compiled_api_definition_assembly))
			return compiled_api_definition_assembly;

		var tmpass = Path.Combine (tmpdir, "temp.dll");
		List<string> cargs = GetCompiledApiBindingsArgs (tmpass, ref bindingTouchConfig);
		AddNFloatUsing (cargs, tmpdir);
		Compile (cargs, 2, tmpdir);

		return tmpass;
	}

	void AddNFloatUsing (List<string> cargs, string tmpdir)
	{
#if NET
		if (noNFloatUsing)
			return;
		var tmpusing = Path.Combine (tmpdir, "GlobalUsings.g.cs");
		File.WriteAllText (tmpusing, "global using nfloat = global::System.Runtime.InteropServices.NFloat;\n");
		cargs.Add (tmpusing);
#endif
	}

	void Compile (List<string> arguments, int errorCode, string tmpdir)
	{
		var responseFile = Path.Combine (tmpdir, $"compile-{errorCode}.rsp");
		// The /noconfig argument is not allowed in a response file, so don't put it there.
		var responseFileArguments = arguments
			.Where (arg => !string.Equals (arg, "/noconfig", StringComparison.OrdinalIgnoreCase) && !string.Equals (arg, "-noconfig", StringComparison.OrdinalIgnoreCase))
			.ToArray (); // StringUtils.QuoteForProcess only accepts IList, not IEnumerable
		File.WriteAllLines (responseFile, StringUtils.QuoteForProcess (responseFileArguments));
		// We create a new list here on purpose to not modify the input argument.
		arguments = arguments.Where (arg => !responseFileArguments.Contains (arg)).ToList ();
		arguments.Add ($"@{responseFile}");

		if (compile_command is null || compile_command.Length == 0) {
#if !NET
			if (string.IsNullOrEmpty (compiler))
				compiler = DefaultCompiler;
#endif
			if (string.IsNullOrEmpty (compiler))
				throw ErrorHelper.CreateError (28);
			compile_command = new string [] { compiler };
		}

		for (var i = 1; i < compile_command.Length; i++) {
			arguments.Insert (i - 1, compile_command [i]);
		}

		if (Driver.RunCommand (compile_command [0], arguments, null, out var compile_output, true, Driver.Verbosity) != 0)
			throw ErrorHelper.CreateError (errorCode, $"{compiler} {StringUtils.FormatArguments (arguments)}\n{compile_output}".Replace ("\n", "\n\t"));
		var output = string.Join (Environment.NewLine, compile_output.ToString ().Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
		if (!string.IsNullOrEmpty (output))
			Console.WriteLine (output);
	}

	static string GetWorkDir ()
	{
		while (true) {
			string p = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
			if (Directory.Exists (p))
				continue;

			var di = Directory.CreateDirectory (p);
			return di.FullName;
		}
	}

	protected virtual void Dispose (bool disposing)
	{
		if (!disposedValue) {
			if (disposing) {
				universe?.Dispose ();
				universe = null;
			}

			disposedValue = true;
		}
	}

	public void Dispose ()
	{
		Dispose (disposing: true);
		GC.SuppressFinalize (this);
	}
}
