using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EngineExclude
{
    public static class InspectorFields
    {
        public static void DrawField(FieldInfo field, object obj)
        {
            if(field.FieldType == typeof(int))
            {
                int value = (int)field.GetValue(obj);

                ImGui.PushID(field.Name + obj.GetHashCode());

                if (ImGui.InputInt(field.Name, ref value, 1, 10, ImGuiInputTextFlags.None))
                {
                    field.SetValue(obj, value);
                }

                ImGui.PopID();
            }
        }
    }
}