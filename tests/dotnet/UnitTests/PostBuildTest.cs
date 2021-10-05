using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using NUnit.Framework;

using Xamarin.Utils;

using Microsoft.Build.Framework;
using Microsoft.Build.Logging.StructuredLogger;
using Mono.Cecil;

namespace Xamarin.Tests {
	[TestFixture]
	public class PostBuildTest : TestBaseClass {
		[Test]
		[TestCase (ApplePlatform.iOS, "ios-arm64")]
		[TestCase (ApplePlatform.iOS, "ios-arm64;ios-arm")]
		[TestCase (ApplePlatform.TVOS, "tvos-arm64")]
		[TestCase (ApplePlatform.MacCatalyst, "maccatalyst-arm64")]
		[TestCase (ApplePlatform.MacCatalyst, "maccatalyst-arm64;maccatalyst-x64")]
		[TestCase (ApplePlatform.MacOSX, "osx-x64")]
		[TestCase (ApplePlatform.MacOSX, "osx-arm64;osx-x64")]
		public void ArchiveTest (ApplePlatform platform, string runtimeIdentifiers)
		{
			var project = "MySimpleApp";
			Configuration.IgnoreIfIgnoredPlatform (platform);

			var project_path = GetProjectPath (project, platform: platform);
			Clean (project_path);
			var properties = GetDefaultProperties (runtimeIdentifiers);
			properties ["ArchiveOnBuild"] = "true";

			var result = DotNet.AssertBuild (project_path, properties);
			var reader = new BinLogReader ();
			var records = reader.ReadRecords (result.BinLogPath).ToList ();
			var findString = "Output Property: ArchiveDir";
			var archiveDirRecord = records.Where (v => v?.Args?.Message?.Contains (findString) == true).ToList ();
			Assert.That (archiveDirRecord.Count, Is.GreaterThan (0), "ArchiveDir");
			var archiveDir = archiveDirRecord [0].Args.Message.Substring (findString.Length + 1).Trim ();
			Assert.That (archiveDir, Does.Exist, "Archive directory existence");
		}

		[Test]
		[TestCase (ApplePlatform.iOS, "ios-arm64")]
		[TestCase (ApplePlatform.iOS, "ios-arm64;ios-arm")]
		[TestCase (ApplePlatform.TVOS, "tvos-arm64")]
		public void BuildIpaTest (ApplePlatform platform, string runtimeIdentifiers)
		{
			var project = "MySimpleApp";
			Configuration.IgnoreIfIgnoredPlatform (platform);

			var project_path = GetProjectPath (project, runtimeIdentifiers: runtimeIdentifiers, platform: platform, out var appPath, configuration: "Release");
			Clean (project_path);
			var properties = GetDefaultProperties (runtimeIdentifiers);
			properties ["BuildIpa"] = "true";
			properties ["Configuration"] = "Release";

			DotNet.AssertBuild (project_path, properties);

			var pkgPath = Path.Combine (appPath, "..", $"{project}.ipa");
			Assert.That (pkgPath, Does.Exist, "pkg creation");

			AssertBundleAssembliesStripStatus (appPath, true);
		}

		[Test]
		[TestCase (ApplePlatform.iOS, "ios-arm64", true)]
		[TestCase (ApplePlatform.iOS, "ios-arm64", false)]
		public void AssemblyStripping (ApplePlatform platform, string runtimeIdentifiers, bool shouldStrip)
		{
			var project = "MySimpleApp";
			Configuration.IgnoreIfIgnoredPlatform (platform);

			var project_path = GetProjectPath (project, runtimeIdentifiers: runtimeIdentifiers, platform: platform, out var appPath);
			Clean (project_path);
			var properties = GetDefaultProperties (runtimeIdentifiers);

			// Force EnableAssemblyILStripping since we are building debug which never will by default
			properties ["EnableAssemblyILStripping"] = shouldStrip ? "true" : "false";

			DotNet.AssertBuild (project_path, properties);

			AssertBundleAssembliesStripStatus (appPath, shouldStrip);
		}

		[Test]
		[TestCase (ApplePlatform.MacCatalyst, "maccatalyst-arm64")]
		[TestCase (ApplePlatform.MacCatalyst, "maccatalyst-arm64;maccatalyst-x64")]
		[TestCase (ApplePlatform.MacOSX, "osx-x64")]
		[TestCase (ApplePlatform.MacOSX, "osx-arm64;osx-x64")]
		public void BuildPackageTest (ApplePlatform platform, string runtimeIdentifiers)
		{
			var project = "MySimpleApp";
			var projectVersion = "3.14";
			Configuration.IgnoreIfIgnoredPlatform (platform);

			var project_path = GetProjectPath (project, runtimeIdentifiers: runtimeIdentifiers, platform: platform, out var appPath);
			Clean (project_path);
			var properties = GetDefaultProperties (runtimeIdentifiers);
			properties ["CreatePackage"] = "true";

			DotNet.AssertBuild (project_path, properties);

			var pkgPath = Path.Combine (appPath, "..", $"{project}-{projectVersion}.pkg");
			Assert.That (pkgPath, Does.Exist, "pkg creation");
		}

		[TestCase (ApplePlatform.iOS, "ios-arm64")]
		[TestCase (ApplePlatform.iOS, "ios-arm64;ios-arm")]
		[TestCase (ApplePlatform.TVOS, "tvos-arm64")]
		[TestCase (ApplePlatform.MacCatalyst, "maccatalyst-arm64")]
		[TestCase (ApplePlatform.MacCatalyst, "maccatalyst-arm64;maccatalyst-x64")]
		[TestCase (ApplePlatform.MacOSX, "osx-x64")]
		[TestCase (ApplePlatform.MacOSX, "osx-arm64;osx-x64")]
		public void PublishTest (ApplePlatform platform, string runtimeIdentifiers)
		{
			var project = "MySimpleApp";
			Configuration.IgnoreIfIgnoredPlatform (platform);

			var project_path = GetProjectPath (project, runtimeIdentifiers, platform: platform, out var appPath);
			Clean (project_path);

			string packageExtension;
			string pathVariable;
			switch (platform) {
			case ApplePlatform.iOS:
			case ApplePlatform.TVOS:
				packageExtension = "ipa";
				pathVariable = "IpaPackagePath";
				break;
			case ApplePlatform.MacCatalyst:
			case ApplePlatform.MacOSX:
				packageExtension = "pkg";
				pathVariable = "PkgPackagePath";
				break;
			default:
				throw new ArgumentOutOfRangeException ($"Unknown platform: {platform}");
			}
			var tmpdir = Cache.CreateTemporaryDirectory ();
			var pkgPath = Path.Combine (tmpdir, $"MyPackage.{packageExtension}");

			var properties = GetDefaultProperties (runtimeIdentifiers);
			properties [pathVariable] = pkgPath;

			DotNet.AssertPublish (project_path, properties);

			Assert.That (pkgPath, Does.Exist, "ipa/pkg creation");
		}
	}
}
