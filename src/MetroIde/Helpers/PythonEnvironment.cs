using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Quickbeam.Low.ByteArray;
using System.IO;

namespace MetroIde.Helpers
{
    public class PythonEnvironment
    {
        protected static readonly string ProjectPath = Path.GetDirectoryName(
            Path.GetDirectoryName(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        System.IO.Directory.GetCurrentDirectory()
                    )
                )
            )
        );

        protected static readonly string StdlibLocation = ProjectPath + @"\vendor\PythonStdLib";
        protected static readonly string DllsLocation = ProjectPath + @"\src\bin\Debug";
        protected static readonly string HalolibLocation = ProjectPath + @"\src\halolib";

        protected ScriptEngine Engine;
        protected ScriptScope Scope;

        private PythonEnvironment()
        {
            Engine = Python.CreateEngine();
            var assembly = typeof (IByteArray).Assembly;
            Engine.Runtime.LoadAssembly(assembly);

            Scope = Engine.CreateScope();
            Engine.CreateScriptSourceFromString("import sys\n" +
                                                "sys.path.append(r'" + StdlibLocation + "')\n" +
                                                "sys.path.append(r'" + DllsLocation + "')\n" +
                                                "sys.path.append(r'" + HalolibLocation + "')\n" +
                                                "import halolib\n" +
                                                "halolib.load_plugins()\n", SourceCodeKind.Statements)
                                                .Execute(Scope);
        }

        private dynamic _rootStruct = null;

        public dynamic RootObservableStruct
        {
            get
            {
                if (_rootStruct != null) return _rootStruct;
                var x = Engine.CreateScriptSourceFromString("curr_tag = halolib.load_map()",
                    SourceCodeKind.SingleStatement)
                    .Execute(Scope);
                _rootStruct = Scope.GetVariable("curr_tag").__dict__;
                return _rootStruct;
            }
        }

        public void Execute(string code)
        {
            Engine.Execute(code, Scope);
        }


        public static void Initialize()
        {
            if (_instance == null)
                _instance = new PythonEnvironment();
        }

        private static PythonEnvironment _instance;
        public static PythonEnvironment Instance
        {
            get { return _instance ?? (_instance = new PythonEnvironment()); }
        }
    }
}
