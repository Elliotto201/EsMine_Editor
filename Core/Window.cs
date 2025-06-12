using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using EngineCore;
using EngineInternal;
using System.Collections.Generic;
using ImGuiNET;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using EngineExclude;
using System.Security.Cryptography.Xml;
using OpenTK.Compute.OpenCL;


namespace EngineInternal
{
    internal class Window : GameWindow
    {
        ImGuiController _controller;

        private Dictionary<string, string> CompiledShaders;

        public event Action OnFrame;
        public event Action ToBeDrawnMeshes;
        public event Action GameUpdate;

        public static Window BuildWindow { get; private set; }

        public List<int> RenderPasses { get; private set; } = new(2);
        public List<RenderPass> ExecutableRenderPasses = new();

        private int _shaderProgram;

        public int currentFrameBuffer;
        public int currentFrameTexture;
        public int currentQuadVao;

        internal GameWindowType GameType { get; private set; }
        internal bool IsPlaying { get; private set; }

        public int shaderProgram
        {
            get => _shaderProgram;
            set
            {
                if (value != _shaderProgram)
                {
                    GL.UseProgram(value);
                    _shaderProgram = value;
                }
            }
        }

        ImGuiViewportUI _ui; // Add this field

        // Constructor
        public Window(int x, int y, NativeWindowSettings setts, GameWindowType type)
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Size = new Vector2i(x, y),
                Title = setts.Title,
                APIVersion = setts.APIVersion,
                Flags = ContextFlags.Debug,
                Profile = setts.Profile,
                API = setts.API
            })
        {
            GL.DebugMessageCallback(DebugMessageDelegate, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);

            Size = new Vector2i(x, y);
            BuildWindow = this;

            CompiledShaders = ShaderLoader.LoadAllShadersWithIncludes();

            GameType = type;

            if(type == GameWindowType.Build || type == GameWindowType.EditorBuild)
            {
                IsPlaying = true;
            }
            else
            {
                IsPlaying = false;
            }

            if(type == GameWindowType.Editor || type == GameWindowType.EditorBuild)
            {
                _ui = new ImGuiViewportUI();
                _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
            }
        }

        private DebugProc DebugMessageDelegate = OnDebugMessage;

        private static void OnDebugMessage(
            DebugSource source,     // Source of the debugging message.
            DebugType type,         // Type of the debugging message.
            int id,                 // ID associated with the message.
            DebugSeverity severity, // Severity of the message.
            int length,             // Length of the string in pMessage.
            IntPtr pMessage,        // Pointer to message string.
            IntPtr pUserParam)      // The pointer you gave to OpenGL, explained later.
        {
            // In order to access the string pointed to by pMessage, you can use Marshal
            // class to copy its contents to a C# string without unsafe code. You can
            // also use the new function Marshal.PtrToStringUTF8 since .NET Core 1.1.
            string message = Marshal.PtrToStringAnsi(pMessage, length);

            // The rest of the function is up to you to implement, however a debug output
            // is always useful.
            Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source, type, id, message);

            // Potentially, you may want to throw from the function for certain severity
            // messages.
            if (type == DebugType.DebugTypeError)
            {
                throw new Exception(message);
            }
        }

        //Just makes the camera good
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);

            int framebuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

            // Create texture to attach to framebuffer
            int screenTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, screenTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Size.X, Size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Attach texture to framebuffer
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, screenTexture, 0);

            int depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, Size.X, Size.Y);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);


            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            currentFrameBuffer = framebuffer;
            currentFrameTexture = screenTexture;

            // Fullscreen quad (two triangles forming a rectangle)
            float[] quadVertices = {
            // positions   // texCoords
            -1f,  1f,      0f, 1f,
            -1f, -1f,      0f, 0f,
            1f,  1f,      1f, 1f,
            1f, -1f,      1f, 0f
            };

            int quadVAO = GL.GenVertexArray();
            int quadVBO = GL.GenBuffer();

            GL.BindVertexArray(quadVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

            // Position (vec2)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            // TexCoord (vec2)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            currentQuadVao = quadVAO;
            if (_controller is not null) { _controller.WindowResized(ClientSize.X, ClientSize.Y); }
        }

        // here we draw all objects that are supposed to be drawn. Is called before frame render and not after
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            if (IsPlaying)
            {
                GameUpdate?.Invoke();
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, currentFrameBuffer);

            _controller.Update(this, (float)args.Time);

            GL.ClearColor(new Color4(0.6f, 0.3f, 1.0f, 1f));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);

            GL.UseProgram(shaderProgram);

            OnFrame?.Invoke();
            ToBeDrawnMeshes?.Invoke();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(true);

            foreach (RenderPass pass in ExecutableRenderPasses)
            {
                pass.Execute();
            }

            _ui.RenderUI(); // Use the single instance
            _controller.Render();
            ImGuiController.CheckGLError("End of frame");

            SwapBuffers();

            GL.UseProgram(0);
        }

        //Initializes the GL class and the gpu buffers to be used
        protected override void OnLoad()
        {
            base.OnLoad();
            CursorState = CursorState.Normal;

            foreach (string shaderName in CompiledShaders.Keys)
            {
                Rendering.ProgramManager.AddProgram(CreateProgram(shaderName), shaderName);
                Console.WriteLine(shaderName);
            }

            int framebuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

            // Create texture to attach to framebuffer
            int screenTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, screenTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Size.X, Size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Attach texture to framebuffer
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, screenTexture, 0);

            int depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, Size.X, Size.Y);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);


            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            currentFrameBuffer = framebuffer;
            currentFrameTexture = screenTexture;

            // Fullscreen quad (two triangles forming a rectangle)
            float[] quadVertices = {
        // positions   // texCoords
         -1f,  1f,      0f, 1f,
         -1f, -1f,      0f, 0f,
          1f,  1f,      1f, 1f,
          1f, -1f,      1f, 0f
        };

            int quadVAO = GL.GenVertexArray();
            int quadVBO = GL.GenBuffer();

            GL.BindVertexArray(quadVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

            // Position (vec2)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            // TexCoord (vec2)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            currentQuadVao = quadVAO;
        }

        //just clearing up some resources
        protected override void OnUnload()
        {
            base.OnUnload();
        }

        // this mess of code creates the shader program which means Compiling the shaders and starting the pipeline which kinda just means making the gpu use the shaders on the things we use
        private int CreateProgram(string shaderKey)
        {
            int program = GL.CreateProgram();
            List<int> shadersToDelete = new();

            void CompileShader(string key)
            {
                if (!CompiledShaders.TryGetValue(key, out string source))
                {
                    Console.WriteLine($"Shader source not found: {key}");
                    return;
                }

                ShaderType type;
                if (key.EndsWith(".vert"))
                    type = ShaderType.VertexShader;
                else if (key.EndsWith(".frag"))
                    type = ShaderType.FragmentShader;
                else
                    return;

                int shader = GL.CreateShader(type);
                GL.ShaderSource(shader, source);
                GL.CompileShader(shader);

                GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
                if (status == 0)
                {
                    string log = GL.GetShaderInfoLog(shader);
                    Console.WriteLine($"{key} compilation failed:\n{log}");
                    GL.DeleteShader(shader);
                    return;
                }

                GL.AttachShader(program, shader);
                shadersToDelete.Add(shader);
            }

            // If passed "Default.vert" or "Default.frag", extract base name and build keys:
            string baseName = shaderKey.EndsWith(".vert") ? shaderKey[..^5] :
                              shaderKey.EndsWith(".frag") ? shaderKey[..^5] : null;

            if (baseName == null)
            {
                Console.WriteLine($"Shader key must end with .vert or .frag: {shaderKey}");
                return 0;
            }

            CompileShader(baseName + ".vert");
            CompileShader(baseName + ".frag");

            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
            string linkLog = GL.GetProgramInfoLog(program);
            if (linkStatus == 0)
            {
                Console.WriteLine($"Program link failed for {baseName}:\n{linkLog}");
                foreach (var shader in shadersToDelete)
                    GL.DeleteShader(shader);
                GL.DeleteProgram(program);
                return 0;
            }

            foreach (var shader in shadersToDelete)
                GL.DeleteShader(shader);

            return program;
        }


        //Sets the world space position of a shader. All shaders of vertex type should be able to use this
        public static void SendMatricesToShader(int program, Matrix4 model, Matrix4 view, Matrix3 normal)
        {
            int modelLoc = GL.GetUniformLocation(program, "uModel");
            int viewLoc = GL.GetUniformLocation(program, "uViewProjection");
            int normalMatrixLoc = GL.GetUniformLocation(program, "normalMatrix");

            GL.UseProgram(program);

            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix3(normalMatrixLoc, false, ref normal);
        }
    }

    internal enum GameWindowType
    {
        Build,
        Editor,
        EditorBuild
    }
}

namespace Rendering
{
    public static class ProgramManager
    {
        private static Dictionary<string, int> usablePrograms = new();

        public static void AddProgram(int program, string name)
        {
            name = name.Replace(".vert", "");
            name = name.Replace(".frag", "");

            usablePrograms[name] = program;
            EngineInternal.Window.BuildWindow.RenderPasses.Add(program);
        }

        public static int GetProgram(string name)
        {
            usablePrograms.TryGetValue(name, out int programKey);

            return programKey;
        }
    }
}

namespace EngineCore
{
    public static class Input
    {
        private static KeyboardState State;

        public static bool IsAnyKeyDown
        {
            get
            {
                return State.IsAnyKeyDown;
            }
        }

        static Input()
        {
            State = EngineInternal.Window.BuildWindow.KeyboardState;
        }

        public static bool IsKeyDown(Keys key)
        {
            return State.IsKeyDown(key);
        }

        public static bool IsKeyPressed(Keys key)
        {
            return State.IsKeyPressed(key);
        }

        public static bool IsKeyReleased(Keys key)
        {
            return State.IsKeyReleased(key);
        }
    }

    public abstract class RenderPass
    {
        public abstract void Execute();
        public abstract int GetProgram();
    }
}