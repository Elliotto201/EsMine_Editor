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
        private static Dictionary<string, dynamic> Scripts = new Dictionary<string, dynamic>();

        static ScriptEngine _engine;
        static ScriptScope _scope;

        public static void LoadScript(string sourceCode, string className)
        {
            _engine = Python.CreateEngine();
            _scope = _engine.CreateScope();

            _engine.Execute(sourceCode, _scope);

            dynamic scriptClass = _scope.GetVariable(className);
            Scripts.TryAdd(className, scriptClass);
        }

        public static void SetVariable(string name, object value)
        {
            _scope.SetVariable(name, value);
        }

        public static dynamic ReadVariable(string name)
        {
            return _scope.GetVariable(name);
        }
        
        public static void CallUpdate(float dt)
        {
            foreach(var script in Scripts.Values)
            {
                script.update(dt);
            }
        }
    }
}
