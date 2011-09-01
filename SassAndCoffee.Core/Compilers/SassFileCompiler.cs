using System.Collections;
using System.Configuration;
using System.Text;

namespace SassAndCoffee.Core.Compilers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using IronRuby;

    using Microsoft.Scripting;
    using Microsoft.Scripting.Hosting;

    public class SassFileCompiler : ISimpleFileCompiler
    {
        private class SassModule
        {
            public dynamic Engine { get; set; }
            public dynamic SassOption { get; set; }
            public dynamic ScssOption { get; set; }
            public Action<string> ExecuteRubyCode { get; set; }
        }

        static TrashStack<SassModule> _sassModule;
        internal static string RootAppPath;
        ICompilerHost _compilerHost;

        static SassFileCompiler()
        {
            _sassModule = new TrashStack<SassModule>(() => {
                var srs = new ScriptRuntimeSetup()
                              {
                                 // HostType = typeof (ResourceAwareScriptHost),
                                DebugMode = true,
                                
                              };
                srs.AddRubySetup();
                var runtime = Ruby.CreateRuntime(srs);
                var engine = runtime.GetRubyEngine();

                // NB: 'R:\' is a garbage path that the PAL override below will 
                // detect and attempt to find via an embedded Resource file
                                                               engine.SetSearchPaths(new List<string>()
                                                               {
                                                                   @"e:\code\sassandcoffee\sassandcoffee.gems\ironruby",
                                                                   @"e:\code\sassandcoffee\sassandcoffee.gems\ruby\1.9.1",
                                                                   @"e:\code\sassandcoffee\sassandcoffee.gems\gems\compass-0.11.1\lib",
                                                                   @"e:\code\sassandcoffee\sassandcoffee.gems\gems\chunky_png-1.1.1\lib",
                                                                   @"e:\code\sassandcoffee\sassandcoffee.gems\gems\fssm-0.2.7\lib",
                                                                   @"e:\code\sassandcoffee\sassandcoffee.gems\gems\sass-3.1.1\lib",
                                                                   @"e:\code\sassandcoffee\sassandcoffee.gems\gems\sass-3.1.1",
                                                               });
    
               // var source = engine.CreateScriptSourceFromString(Utility.ResourceAsString("SassAndCoffee.Core.lib.sass_in_one.rb"), SourceCodeKind.File);
                                                               var source =
                                                                   engine.CreateScriptSourceFromString(
                                                                       @"
require 'compass'



require 'compass/exec'
",
                                                                       SourceCodeKind.Statements);

                var scope = engine.CreateScope();
                source.Execute(scope);

                return new SassModule() {
                    Engine = scope.Engine.Runtime.Globals.GetVariable("Sass"),
                    SassOption = engine.Execute("{:syntax => :sass, :load_paths => " + GetSassLoadPaths() + " }"),
                    ScssOption = engine.Execute("{:syntax => :scss, :load_paths => " + GetSassLoadPaths() + " }"),
                    ExecuteRubyCode = code => engine.Execute(code, scope),
                };
            });
        }

        private static string GetSassLoadPaths()
        {
            var loadPaths = ConfigurationManager.AppSettings["SassAndCoffee.LoadPaths"].Split(',',';');
            var sb = new StringBuilder();
            sb.Append("[");
            foreach(var lp in loadPaths)
            {
                sb.AppendFormat("'{0}'", lp.Replace('\\','/'));
                sb.Append(",");
            }
            sb.Length--;
            sb.Append("]");
            return sb.ToString();
        }

        public string[] InputFileExtensions {
            get { return new[] {".scss", ".sass"}; }
        }

        public string OutputFileExtension {
            get { return ".css"; }
        }

        public string OutputMimeType {
            get { return "text/css"; }
        }

        public void Init(ICompilerHost host)
        {
            _compilerHost = host;
        }

        public string ProcessFileContent(string inputFileContent)
        {
            // NB: We do this here instead of in Init like we should, because in 
            // ASP.NET trying to get the PhysicalAppPath when a request isn't in-flight
            // is verboten, for no good reason.
            RootAppPath = RootAppPath ?? _compilerHost.ApplicationBasePath;

            using (var sassModule = _sassModule.Get()) {
                dynamic opt = (inputFileContent.ToLowerInvariant().EndsWith("scss") ? sassModule.Value.ScssOption : sassModule.Value.SassOption);

                if (!inputFileContent.Contains('\'')) {
                    sassModule.Value.ExecuteRubyCode(String.Format("Dir.chdir '{0}'", Path.GetDirectoryName(Path.GetDirectoryName(inputFileContent))));
                }

                string fileText = File.ReadAllText(inputFileContent);
                return (string) sassModule.Value.Engine.compile(fileText, opt);
            }
        }

        public string GetFileChangeToken(string inputFileContent)
        {
            return "";
        }
    }

    public class ResourceAwareScriptHost : ScriptHost
    {
        PlatformAdaptationLayer _innerPal = null;
        public override PlatformAdaptationLayer PlatformAdaptationLayer {
            get {
                if (_innerPal == null) {
                    _innerPal = new ResourceAwarePAL();
                }
                return _innerPal;
            }
        }
    }

    public class ResourceAwarePAL : PlatformAdaptationLayer
    {
       
        private static readonly Assembly GemAssembly = Assembly.Load("SassAndCoffee.Gems");

        public override Stream OpenInputFileStream(string path)
        {
            var ret = GemAssembly.GetManifestResourceStream(pathToResourceName(path));
            if (ret != null) {
                return ret;
            }

            if (SassFileCompiler.RootAppPath == null || !path.ToLowerInvariant().StartsWith(SassFileCompiler.RootAppPath)) {
                return null;
            }

            return base.OpenInputFileStream(path);
        }

        //This get shit when calling Sass::Version.version when it tries to read the VERSION file
        public override Stream OpenInputFileStream(string path, FileMode mode, FileAccess access, FileShare share) {

             var ret = GemAssembly.GetManifestResourceStream(pathToResourceName(path));
            if (ret != null) {
                return ret;
            }

            if (SassFileCompiler.RootAppPath == null || !path.ToLowerInvariant().StartsWith(SassFileCompiler.RootAppPath)) {
                return null;
            }

            return base.OpenInputFileStream(path, mode, access, share);
        }

       
        public override bool FileExists(string path)
        {
            if (GemAssembly.GetManifestResourceInfo(pathToResourceName(path)) != null) {
                return true;
            }

            if (path.EndsWith("css")) {
                int a = 1;
            }

            return base.FileExists(path);
        }

        string pathToResourceName(string path)
        {
            var ret = path
                .Replace("1.9.1", "_1._9._1")
                .Replace("-1.1.1","_1._1._1")
                .Replace("-0.11.1","_0._11._1")
                .Replace("-0.2.7","_0._2._7")
                .Replace("-3.1.1","_3._1._1")
                .Replace('\\', '.')
                .Replace('/', '.')
                .Replace("R:", "SassAndCoffee.Gems");
            return ret;
        }

        public override bool DirectoryExists(string path)
        {
            return base.DirectoryExists(path);
        }

        public override Stream OpenInputFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            return base.OpenInputFileStream(path, mode, access, share, bufferSize);
        }

        public override Stream OpenOutputFileStream(string path)
        {
            return base.OpenOutputFileStream(path);
        }

        public override void DeleteFile(string path, bool deleteReadOnly)
        {
            base.DeleteFile(path, deleteReadOnly);
        }

        public override string[] GetFileSystemEntries(string path, string searchPattern, bool includeFiles, bool includeDirectories)
        {
            if(path != null 
                && path.Equals(@"R:/gems/compass-0.11.1/frameworks",StringComparison.InvariantCultureIgnoreCase))
            {
                return base.GetFileSystemEntries(@"E:\Code\SassAndCoffee\WebTest\App_Compass", searchPattern,
                                                 includeFiles, includeDirectories);
            }
           
            return base.GetFileSystemEntries(path, searchPattern, includeFiles, includeDirectories);
        }

        public override string GetFullPath(string path)
        {
            return base.GetFullPath(path);
        }

        public override string CombinePaths(string path1, string path2)
        {
            return base.CombinePaths(path1, path2);
        }

        public override string GetFileName(string path)
        {
            return base.GetFileName(path);
        }

        public override string GetDirectoryName(string path)
        {
            return base.GetDirectoryName(path);
        }

        public override string GetExtension(string path)
        {
            return base.GetExtension(path);
        }

        public override string GetFileNameWithoutExtension(string path)
        {
            return base.GetFileNameWithoutExtension(path);
        }

        public override bool IsAbsolutePath(string path)
        {
            return base.IsAbsolutePath(path);
        }

        public override void CreateDirectory(string path)
        {
            base.CreateDirectory(path);
        }

        public override void DeleteDirectory(string path, bool recursive)
        {
            base.DeleteDirectory(path, recursive);
        }

        public override void MoveFileSystemEntry(string sourcePath, string destinationPath)
        {
            base.MoveFileSystemEntry(sourcePath, destinationPath);

        }

        public override string GetEnvironmentVariable(string key)
        {
            return base.GetEnvironmentVariable(key);
        }

        public override void SetEnvironmentVariable(string key, string value)
        {
            base.SetEnvironmentVariable(key, value);
        }

        public override IDictionary GetEnvironmentVariables()
        {
            return base.GetEnvironmentVariables();
        }
    }
}
