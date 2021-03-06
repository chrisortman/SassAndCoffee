using System;
using System.IO;
using System.Web;

namespace SassAndCoffee
{
    public class JavascriptPassthroughCompiler : ISimpleFileCompiler
    {
        public string[] InputFileExtensions {
            get { return new[] {".js"}; }
        }

        public string OutputFileExtension {
            get { return ".js"; }
        }

        public string OutputMimeType {
            get { return "text/javascript"; }
        }

        public void Init(HttpApplication context)
        {
        }

        public string ProcessFileContent(string inputFileContent)
        {
            return File.ReadAllText(inputFileContent);
        }

        public string GetFileChangeToken(string inputFileContent)
        {
            return "";
        }
    }
}