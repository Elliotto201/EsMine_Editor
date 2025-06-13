using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineCore
{
    public class Behaviour
    {

    }


    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class Export : Attribute
    {

    }
}