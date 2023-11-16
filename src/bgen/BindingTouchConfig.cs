// TODO Either break up data blob or try to just pass its members and avoid passing the ojbect as much as possible

using System.Collections.Generic;
using System.Reflection;
using Mono.Options;

#nullable enable

// TODO can I organize these properties into relevant categories? Can the categories be objects?
// TODO Should this Config be an interface?
// TODO Follow normal properties casing, and set all this stuff in a constructor or something
public class BindingTouchConfig { // TODO Perhaps it's correct after all for the BindingTouchConfig to have the OptionSet?
	public bool show_help = false; // Only used in CreateOptionSet
	public bool zero_copy = false; // Used in CreateOptionSet and PerformGenerate
	public string? basedir = null; // CreateOptionSet, PerformGenerate
	public string? tmpdir = null;  // Lots
	public string? ns = null;		// CreateOptionSet, InitializeManagers
	public bool delete_temp = true, debug = false; // CreateOptionSet, PerformGenerate
	public bool unsafef = true;     // CreateOptionSet, CreateCompilationArgs, 
	public bool external = false;
	public bool public_mode = true;
	public bool nostdlib = false;
	public bool? inline_selectors = null;
	public List<string> sources;
	public List<string> resources = new List<string> ();
	public List<string> linkwith = new List<string> ();
	public List<string> api_sources = new List<string> ();
	public List<string> core_sources = new List<string> ();
	public List<string> extra_sources = new List<string> ();
	public List<string> defines = new List<string> ();
	public string? generate_file_list = null;
	public bool process_enums = false;
	public Assembly api;
	public Assembly baselib;
	public string firstApiDefinitionName;

	public IEnumerable<string> refs;
	public IEnumerable<string> paths;
	public OptionSet os;
}
