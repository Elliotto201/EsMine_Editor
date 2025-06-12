using EngineExclude;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EngineInternal
{
    internal static class Program
    {
        public const bool IsEditor = true;

        static void Main()
        {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(1280, 760),
                Title = "My Compute Shader App",
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new Version(4, 3), // 👈 Must be 4.3 or higher
                Flags = ContextFlags.Debug
            };

            Window window = new Window(1280, 760, nativeWindowSettings, GameWindowType.Editor);
            window.Run();
        }
    }

    internal static class ShaderLoader
    {
        private static readonly string ShaderRoot = Path.GetFullPath("../../../Assets/");

        // Load all .vert and .frag shaders, including those with #include directives pointing to .glsl files
        public static Dictionary<string, string> LoadAllShadersWithIncludes()
        {
            var result = new Dictionary<string, string>();

            Console.WriteLine(ShaderRoot);

            // Get all .vert and .frag shader files in the shader directory and its subdirectories
            string[] shaderFiles = Directory.GetFiles(ShaderRoot, "*.*", SearchOption.AllDirectories);
            foreach (var file in shaderFiles)
            {
                if (file.EndsWith(".vert") || file.EndsWith(".frag"))
                {
                    string processed = ProcessShaderIncludes(file, new HashSet<string>());
                    Console.WriteLine(processed);

                    string fileName = Path.GetFileName(file);

                    result[fileName] = processed;
                    Console.WriteLine(fileName);
                }
            }

            return result;
        }

        private static string ProcessShaderIncludes(string filePath, HashSet<string> includedFiles)
        {
            if (includedFiles.Contains(Path.GetFullPath(filePath)))
                throw new InvalidOperationException($"Circular include detected: {filePath}");

            includedFiles.Add(Path.GetFullPath(filePath));

            var sb = new StringBuilder();
            string[] lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("#include"))
                {
                    int start = line.IndexOf('"') + 1;
                    int end = line.LastIndexOf('"');
                    string includePath = line.Substring(start, end - start);

                    // Always look for .glsl files for #include
                    string fullIncludePath = Path.Combine(ShaderRoot, includePath);
                    if (!File.Exists(fullIncludePath))
                    {
                        // If the file isn't found, try adding the .glsl extension
                        fullIncludePath = Path.Combine(ShaderRoot, includePath + ".glsl");
                        if (!File.Exists(fullIncludePath))
                            throw new FileNotFoundException($"Included file not found: {includePath}");
                    }

                    // Recursively process the include file
                    string includedSource = ProcessShaderIncludes(fullIncludePath, includedFiles);
                    sb.AppendLine(includedSource);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            return sb.ToString();
        }
    }
}