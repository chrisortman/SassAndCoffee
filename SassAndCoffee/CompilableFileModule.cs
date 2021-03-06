﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace SassAndCoffee
{
    public class CompilableFileModule : IHttpModule, ICompilerHost
    {
        Dictionary<ISimpleFileCompiler, IHttpHandler> _handlers;

        public void Init(HttpApplication context)
        {
            var coffeeEngine = new CoffeeScriptCompiler();

            var compilers = new ISimpleFileCompiler[] {
                new ConcatenationFileHandler(this),
                new MinifyingFileCompiler(coffeeEngine),
                new CoffeeScriptFileCompiler(coffeeEngine),
                new SassFileCompiler(),
                new JavascriptPassthroughCompiler(),
            };

            _handlers = new Dictionary<ISimpleFileCompiler, IHttpHandler>();
            foreach (var compiler in compilers) {
                compiler.Init(context);
                _handlers[compiler] = new CompilableFileHandler(compiler);
            }

            context.PostResolveRequestCache += (o, e) => {
                var app = o as HttpApplication;
                var compiler = MapPathToCompiler(app.Request.PhysicalPath);

                if (compiler == null) {
                    return;
                }

                app.Context.RemapHandler(_handlers[compiler]);
            };
        }

        public ISimpleFileCompiler MapPathToCompiler(string physicalPath)
        {
            string path = physicalPath.ToLowerInvariant();

            return _handlers.Keys
                .FirstOrDefault(x => path.EndsWith(x.OutputFileExtension) && x.FindInputFileGivenOutput(path) != null);
        }

        public void Dispose()
        {
        }
    }
}
