using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace EngineInternal
{
    public static class PythonInterpreter
    {
        static ScriptEngine _engine;
        static ScriptScope _scope;

        static PythonInterpreter()
        {
            _engine = Python.CreateEngine();
            _scope = _engine.CreateScope();
        }

        public static void Execute(string code)
        {
            _engine.Execute(code);
        }

        public static void SetVariable(string name, object value)
        {
            _scope.SetVariable(name, value);
        }

        public static dynamic ReadVariable(string name)
        {
            return _scope.GetVariable(name);
        }
    }
}
