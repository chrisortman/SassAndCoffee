using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using IronRuby;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace SassAndCoffee
{
    public class SassFileCompiler : ISimpleFileCompiler
    {
        dynamic _sassModule;
        dynamic _scssOption, _sassOption;

        public string[] InputFileExtensions {
            get { return new[] {".scss", ".sass"}; }
        }

        public string OutputFileExtension {
            get { return ".css"; }
        }

        public string OutputMimeType {
            get { return "text/css"; }
        }

        public void Init(HttpApplication context)
        {
        	var setup = IronRuby.Ruby.CreateRubySetup();

					  setup.Options.Add("SearchPaths",new List<string>() { 
							@"E:\Code\SassAndCoffee\packages\compass.net.0.0.1\tools\sass-3.1.1\lib",
							@"E:\Code\SassAndCoffee\packages\compass.net.0.0.1\tools\compass-0.11.1\lib",
							@"E:\Code\SassAndCoffee\packages\compass.net.0.0.1\tools\chunky_png-1.1.1\lib",
							@"E:\Code\SassAndCoffee\packages\compass.net.0.0.1\tools\fssm-0.2.7\lib",
						});
					var srs = new ScriptRuntimeSetup();
        	srs.LanguageSetups.Add(setup);
        	var runtime = Ruby.CreateRuntime(srs);
            var engine = runtime.GetRubyEngine();
						

            var source = engine.CreateScriptSourceFromString(@"
require 'compass'
require 'compass/exec'
", SourceCodeKind.Statements);

					
            var scope = engine.CreateScope();
            source.Execute(scope);

            _scssOption = engine.Execute("{:syntax => :scss, :load_paths => ['E:/Code/SassAndCoffee/packages/compass.net.0.0.1/tools/compass-0.11.1/frameworks/blueprint/stylesheets','E:/Code/SassAndCoffee/packages/compass.net.0.0.1/tools/compass-0.11.1/frameworks/compass/stylesheets','E:/Code/SassAndCoffee/WebTest/Content/sass']}"); 
            _sassOption = engine.Execute("{:syntax => :sass, :load_paths => ['E:\\Code/SassAndCoffee/packages/compass.net.0.0.1/tools/compass-0.11.1/frameworks/blueprint/stylesheets','E:/Code/SassAndCoffee/WebTest/Content/sass']}"); 
            _sassModule = scope.Engine.Runtime.Globals.GetVariable("Sass");
        }

        public string ProcessFileContent(string inputFileContent)
        {
            dynamic opt = (inputFileContent.ToLowerInvariant().EndsWith("scss") ? _scssOption : _sassOption);
            try {
                return (string) _sassModule.compile(File.ReadAllText(inputFileContent), opt);
            } catch (Exception ex) {
                return ex.Message;
            }
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
			private string[] _searchDirectories = new string[]
			                                      	{
			@"E:\Code\SassAndCoffee\packages\compass.net.0.0.1\tools\compass-0.11.1\frameworks\blueprint\stylesheets\"                                      		
    };
        public override System.IO.Stream OpenInputFileStream(string path)
        {
            var ret = Assembly.GetExecutingAssembly().GetManifestResourceStream(pathToResourceName(path));
            if (ret != null) {
                return ret;
            }

					var searchPaths = _searchDirectories.Select(x => Path.Combine(x, path.Replace('/', '\\')));
        	var foundPath = searchPaths.FirstOrDefault(File.Exists);
					if(foundPath != null)
					{
						return base.OpenInputFileStream(foundPath);
					}
					
            return base.OpenInputFileStream(path);
        }

        public override bool FileExists(string path)
        {
            if (Assembly.GetExecutingAssembly().GetManifestResourceInfo(pathToResourceName(path)) != null) {
                return true;
            }

        	var searchPaths = _searchDirectories.Select(x => Path.Combine(x, path.Replace('/', '\\')));
					if(searchPaths.Any(File.Exists))
					{
						return true;
					}
            var baseExists =  base.FileExists(path);

        	return baseExists;
        }

        string pathToResourceName(string path)
        {
            var ret = path
                .Replace("1.9.1", "_1._9._1")
                .Replace('\\', '.')
                .Replace('/', '.')
                .Replace("R:", "SassAndCoffee");
            return ret;
        }
    }
}
