﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using BuildHelpers;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Octokit;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.IO.TextTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.Tools.Npm.NpmTasks;

[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Package);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Github Token")] readonly string GithubToken;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion(Framework = "netcoreapp3.1", UpdateAssemblyInfo = true, NoFetch = false)] readonly GitVersion GitVersion;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath WebProjectDirectory => RootDirectory / "Module.Web";

    private string devViewsPath = "http://localhost:3333/build/";
    private string prodViewsPath = "DesktopModules/MyModule/resources/scripts/era-mymodule/";

    string releaseNotes = "";
    string owner = "";
    string name = "";
    string branch = "";
    GitHubClient gitHubClient;
    Release release;

    Target LogInfo => _ => _
        .Before(Release)
        .Executes(() =>
        {
            Logger.Info($"We are on branch {GitRepository.Branch} and IsOnMasterBranch is {GitRepository.IsOnMasterBranch()} and the version will be {GitVersion.SemVer}");
            branch = GitRepository.Branch.Split('/').Last();
            Logger.Info($"Set branch name to {branch}");
        });

    Target Clean => _ => _
        .Before(Restore)
        .Before(Package)
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution.GetProject("Module")));
        });

    Target Compile => _ => _
        .DependsOn(Clean)
        .DependsOn(Restore)
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetProjectFile(Solution.GetProject("Module"))
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.InformationalVersion));
        });

    Target SetManifestVersions => _ => _
        .Executes(() =>
        {
            var manifests = GlobFiles(RootDirectory, "**/*.dnn");
            foreach (var manifest in manifests)
            {
                var doc = new XmlDocument();
                doc.Load(manifest);
                var packages = doc.SelectNodes("dotnetnuke/packages/package");
                foreach (XmlNode package in packages)
                {
                    var version = package.Attributes["version"];
                    if (version != null)
                    {
                        Logger.Normal($"Found package {package.Attributes["name"].Value} with version {version.Value}");
                        version.Value = $"{GitVersion.Major.ToString("00", CultureInfo.InvariantCulture)}.{GitVersion.Minor.ToString("00", CultureInfo.InvariantCulture)}.{GitVersion.Patch.ToString("00", CultureInfo.InvariantCulture)}";
                        Logger.Normal($"Updated package {package.Attributes["name"].Value} to version {version.Value}");
                    }
                }
                doc.Save(manifest);
                Logger.Normal($"Saved {manifest}");
            }
        });

    Target DeployBinaries => _ => _
        .OnlyWhenDynamic(() => RootDirectory.Parent.ToString().EndsWith("DesktopModules", StringComparison.OrdinalIgnoreCase))
        .DependsOn(Compile)
        .Executes(() =>
        {
            var files = GlobFiles(RootDirectory, "bin/Debug/*.dll", "bin/debug/*.pdb");
            foreach (var file in files)
            {
                Helpers.CopyFileToDirectoryIfChanged(file, RootDirectory.Parent.Parent / "bin");
            }
        });

    Target SetRelativeScripts => _ => _
        .DependsOn(DeployFrontEnd)
        .Executes(() =>
        {
            var views = GlobFiles(RootDirectory, "resources/views/**/*.html");
            foreach (var view in views)
            {
                var content = ReadAllText(view);
                content = content.Replace(devViewsPath, prodViewsPath, StringComparison.OrdinalIgnoreCase);
                WriteAllText(view, content, System.Text.Encoding.UTF8);
                Logger.Info("Set scripts path to {0} in {1}", prodViewsPath, view);
            }
        });

    Target SetLiveServer => _ => _
        .DependsOn(DeployFrontEnd)
        .Executes(() =>
        {
            var views = GlobFiles(RootDirectory, "resources/views/**/*.html");
            foreach (var view in views)
            {
                var content = ReadAllText(view);
                content = content.Replace(prodViewsPath, devViewsPath, StringComparison.OrdinalIgnoreCase);
                WriteAllText(view, content, System.Text.Encoding.UTF8);
                Logger.Info("Set scripts path to {0} in {1}", devViewsPath, view);
            }
        });

    Target DeployFrontEnd => _ => _
        .DependsOn(BuildFrontEnd)
        .Executes(() =>
        {
            var scriptsDestination = RootDirectory / "resources" / "scripts" / "era-mymodule";
            EnsureCleanDirectory(scriptsDestination);
            CopyDirectoryRecursively(RootDirectory / "module.web" / "dist" / "era-mymodule", scriptsDestination, DirectoryExistsPolicy.Merge);
        });

    Target InstallNpmPackages => _ => _
        .Executes(() =>
        {
            NpmLogger = (type, output) => {
                if (type == OutputType.Std)
                {
                    Logger.Info(output);
                }
                if (type == OutputType.Err)
                {
                    if (output.StartsWith("npm WARN", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Warn(output);
                    }
                    else
                    {
                        Logger.Error(output);
                    }
                }
            };
            NpmInstall(s =>
                s.SetWorkingDirectory(WebProjectDirectory));
        });

    Target BuildFrontEnd => _ => _
        .DependsOn(InstallNpmPackages)
        .Executes(() =>
        {
            NpmRun(s => s
                .SetWorkingDirectory(WebProjectDirectory)
                .SetArgumentConfigurator(a => a.Add("build"))
            );
        });

    Target SetPackagesVersions => _ => _
        .Executes(() =>
        {
            Npm($"--no-git-tag-version --allow-same-version {GitVersion.FullSemVer}", WebProjectDirectory);
        });

    Target PackageRelease => _ => _
        .DependsOn(Compile)
        .After(Clean)
        .After(SetManifestVersions)
        .After(SetPackagesVersions)
        .After(SetRelativeScripts)
        .Executes(() =>
        {
            var stagingDirectory = ArtifactsDirectory / "staging";
            EnsureCleanDirectory(stagingDirectory);

            // Delete resources.zip.manifest so it does not get re-bundles on re-installs
            var zipManifests = GlobFiles(RootDirectory / "resources", "**/*.zip.manifest");
            zipManifests.ForEach(file => DeleteFile(file));

            // Resources
            ZipFile.CreateFromDirectory(RootDirectory / "resources", stagingDirectory / "resources.zip");

            // Symbols
            var symbolsFiles = GlobFiles(RootDirectory / "bin/Release/**/*.pdb");
            Helpers.AddFilesToZip(stagingDirectory / "symbols.zip", symbolsFiles.ToArray());

            // Install files
            var installFiles = GlobFiles(RootDirectory, "LICENSE", "manifest.dnn", "ReleaseNotes.html");
            installFiles.ForEach(file => CopyFileToDirectory(file, stagingDirectory));

            // Libraries
            var libraries = GlobFiles(RootDirectory / "bin/Release/**/*.dll");
            libraries.ForEach(lib => CopyFileToDirectory(lib, stagingDirectory / "bin"));

            // Install package
            string fileName = new DirectoryInfo(RootDirectory).Name;
            fileName += $"_{GitVersion.SemVer}_install.zip";
            ZipFile.CreateFromDirectory(stagingDirectory, ArtifactsDirectory / fileName);
            DeleteDirectory(stagingDirectory);
            Logger.Info("Packaged release: {0}", fileName);

            // Open explorer if on local windows machine
            if (IsWin)
            {
                Process.Start("explorer.exe", ArtifactsDirectory);
            }
        });


    Target setupGitHubClient => _ => _
        .OnlyWhenDynamic(() => branch == "master")
        .OnlyWhenDynamic(() => !string.IsNullOrWhiteSpace(GithubToken))
        .Executes(() =>
        {
            owner = GitRepository.Identifier.Split('/')[0];
            name = GitRepository.Identifier.Split('/')[1];
            gitHubClient = new GitHubClient(new ProductHeaderValue("Nuke"));
            var tokenAuth = new Credentials(GithubToken);
            gitHubClient.Credentials = tokenAuth;
        });

    Target GenerateReleaseNotes => _ => _
        .OnlyWhenDynamic(() => branch == "master")
        .OnlyWhenDynamic(() => !string.IsNullOrWhiteSpace(GithubToken))
        .DependsOn(setupGitHubClient)
        .Executes(() => {

            // Get the milestone
            var milestone = gitHubClient.Issue.Milestone.GetAllForRepository(owner, name).Result.Where(m => m.Title == GitVersion.MajorMinorPatch).FirstOrDefault();
            if (milestone == null)
            {
                Logger.Error("Milestone not found for this version");
                return;
            }

            // Get the PRs
            var prRequest = new PullRequestRequest()
            {
                State = ItemStateFilter.All
            };
            var pullRequests = gitHubClient.Repository.PullRequest.GetAllForRepository(owner, name, prRequest).Result.Where(p =>
                p.Milestone?.Title == milestone.Title &&
                p.Merged == true &&
                p.Milestone?.Title == GitVersion.MajorMinorPatch);

            // Build release notes
            var releaseNotesBuilder = new StringBuilder();
            releaseNotesBuilder.AppendLine($"# {name} {milestone.Title}")
                .AppendLine("")
                .AppendLine($"A total of {pullRequests.Count()} pull requests where merged in this release.").AppendLine();

            foreach (var group in pullRequests.GroupBy(p => p.Labels[0]?.Name, (label, prs) => new { label, prs }))
            {
                releaseNotesBuilder.AppendLine($"## {group.label}");
                foreach (var pr in group.prs)
                {
                    releaseNotesBuilder.AppendLine($"- #{pr.Number} {pr.Title}. Thanks @{pr.User.Login}");
                }
            }
            releaseNotes = releaseNotesBuilder.ToString();
            using (Logger.Block("Release Notes"))
            {
                Logger.Info(releaseNotes);
            }
        });

    Target TagRelease => _ => _
        .OnlyWhenDynamic(() => branch == "master")
        .OnlyWhenDynamic(() => !string.IsNullOrWhiteSpace(GithubToken))
        .DependsOn(setupGitHubClient)
        .After(GenerateReleaseNotes)
        .Executes(() =>
        {
            GitLogger = (type, output) => Logger.Info(output);
            Git($"tag v{GitVersion.MajorMinorPatch}");
            Git($"push --tags");
        });

    Target Release => _ => _
        .OnlyWhenDynamic(() => branch == "master")
        .OnlyWhenDynamic(() => !string.IsNullOrWhiteSpace(GithubToken))
        .DependsOn(setupGitHubClient)
        .DependsOn(TagRelease)
        .Executes(() =>
        {
            var newRelease = new NewRelease($"v{GitVersion.MajorMinorPatch}")
            {
                Body = releaseNotes,
                Draft = true,
                Name = $"v{GitVersion.MajorMinorPatch}",
                TargetCommitish = $"{GitVersion.Sha}",
                Prerelease = false
            };
            release = gitHubClient.Repository.Release.Create(owner, name, newRelease).Result;
            Logger.Info($"{release.Name} released !");

            var artifactFile = GlobFiles(RootDirectory, "artifacts/**/*.zip").FirstOrDefault();
            var artifact = File.OpenRead(artifactFile);
            var artifactInfo = new FileInfo(artifactFile);
            var assetUpload = new ReleaseAssetUpload()
            {
                FileName = artifactInfo.Name,
                ContentType = "application/zip",
                RawData = artifact
            };
            var asset = gitHubClient.Repository.Release.UploadAsset(release, assetUpload).Result;
            Logger.Info($"Asset {asset.Name} published at {asset.BrowserDownloadUrl}");
        });

    /// <summary>
    /// Lauch in deploy mode, updates the module on the current local site.
    /// </summary>
    Target Deploy => _ => _
        .DependsOn(DeployBinaries)
        .DependsOn(SetRelativeScripts)
        .Executes(() =>
        {

        });

    /// <summary>
    /// Watch frontend for changes
    /// </summary>
    Target Watch => _ => _
    .DependsOn(DeployBinaries)
    .DependsOn(InstallNpmPackages)
    .DependsOn(SetLiveServer)
    .DependsOn(GenerateAppConfig)
    .Executes(() =>
    {
        NpmRun(s => s
            .SetWorkingDirectory(WebProjectDirectory)
            .SetArgumentConfigurator(a => a.Add("start"))
            );
    });


    Target GenerateAppConfig => _ => _
    .OnlyWhenDynamic(() => RootDirectory.Parent.ToString().EndsWith("DesktopModules", StringComparison.OrdinalIgnoreCase))
    .Executes(() =>
    {
        var webConfigPath = RootDirectory.Parent.Parent / "web.config";
        var webConfigDoc = new XmlDocument();
        webConfigDoc.Load(webConfigPath);
        var connectionString = webConfigDoc.SelectSingleNode("/configuration/connectionStrings/add[@name='SiteSqlServer']");

        var appConfigPath = RootDirectory / "_build" / "App.config";
        var appConfig = new XmlDocument();
        var configurationNode = appConfig.AppendChild(appConfig.CreateElement("configuration"));
        var connectionStringsNode = configurationNode.AppendChild(appConfig.CreateElement("connectionStrings"));
        var importedNode = connectionStringsNode.OwnerDocument.ImportNode(connectionString, true);
        connectionStringsNode.AppendChild(importedNode);
        appConfig.Save(appConfigPath);

        Logger.Info("Generated {0} from {1}", appConfigPath, webConfigPath);
        Logger.Info("This file is local as it could contain credentials, it should not be committed to the repository.");
    });

    /// <summary>
    /// Package the module
    /// </summary>
    Target Package => _ => _
        .DependsOn(Clean)
        .DependsOn(SetManifestVersions)
        .DependsOn(Compile)
        .DependsOn(SetRelativeScripts)
        .DependsOn(GenerateAppConfig)
        .Executes(() =>
        {
            var stagingDirectory = ArtifactsDirectory / "staging";
            EnsureCleanDirectory(stagingDirectory);

            // Resources
            ZipFile.CreateFromDirectory(RootDirectory / "resources", stagingDirectory / "resources.zip");

            // Symbols
            var symbolFiles = GlobFiles(RootDirectory, "bin/Release/**/*.pdb");
            Helpers.AddFilesToZip(stagingDirectory / "symbols.zip", symbolFiles.ToArray());

            // Install files
            var installFiles = GlobFiles(RootDirectory, "LICENSE", "manifest.dnn", "ReleaseNotes.html");
            installFiles.ForEach(i => CopyFileToDirectory(i, stagingDirectory));

            // Libraries
            CopyDirectoryRecursively(RootDirectory / "bin" / "Release", stagingDirectory / "bin", excludeFile: (f) => !f.Name.EndsWith("dll", StringComparison.OrdinalIgnoreCase));

            // Install package
            string fileName = new DirectoryInfo(RootDirectory).Name;
            if (GitVersion != null)
            {
                fileName += $"_{GitVersion.FullSemVer}";
            }
            fileName += "_install.zip";
            ZipFile.CreateFromDirectory(stagingDirectory, ArtifactsDirectory / fileName);
            DeleteDirectory(stagingDirectory);

            // Open folder
            if (IsWin)
            {
                Process.Start("explorer.exe", ArtifactsDirectory);
            }

            Logger.Success("Packaging succeeded!");
        });

    Target CI => _ => _
        .DependsOn(LogInfo)
        .DependsOn(Package)
        .DependsOn(GenerateReleaseNotes)
        .DependsOn(TagRelease)
        .DependsOn(Release)
        .Executes(() =>
        {

        });
}