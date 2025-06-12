using EngineCore;
using EngineInternal;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Rendering;
using System;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Collections.Generic;
using System.IO;


namespace EngineCore
{
    public class Mesh : IDisposable
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; private set; }
        public Vector3 Scale { get; private set; }

        private int Ebo;
        private int Vao;
        private int Vbo;
        private int UvVbo;

        private int IndexCount;
        public int[] RenderPasses { get; private set; }
        private Material? Material;

        private bool Cache;

        public void Dispose()
        {
            if (!Cache)
            {
                GL.DeleteBuffer(Ebo);
                GL.DeleteBuffer(Vbo);
                GL.DeleteBuffer(UvVbo);
                GL.DeleteVertexArray(Vao);
            }
        }

        // Constructor
        public Mesh(ReadOnlyMemory<Vector3> vertices, ReadOnlyMemory<int> indices, Vector2[] uvs, bool cache = true, params int[] program)
        {
            if (cache)
            {
                var data = MeshManager.GetMeshBufferData(vertices, indices);
                Ebo = data.Ebo;
                Vao = data.Vao;
                Vbo = data.Vbo;
            }
            else
            {
                Vao = GL.GenVertexArray();
                Vbo = GL.GenBuffer();
                Ebo = GL.GenBuffer();

                GL.BindVertexArray(Vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, Vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Vector3.SizeInBytes, vertices.ToArray(), BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, Ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices.ToArray(), BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
                GL.EnableVertexAttribArray(0);

                GL.BindVertexArray(0);
            }

            Cache = cache;

            IndexCount = indices.Length;
            RenderPasses = program;
            Scale = Vector3.One;
            Rotation = Quaternion.Identity;

            // Create and bind the VAO
            GL.BindVertexArray(Vao);

            // Position VBO (already setup)
            GL.BindBuffer(BufferTarget.ArrayBuffer, Vbo);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);
            GL.EnableVertexAttribArray(0);

            // Upload UVs to the GPU
            UvVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, UvVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, uvs.Length * Vector2.SizeInBytes, uvs, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, sizeof(float) * 2, 0);
            GL.EnableVertexAttribArray(2);

            // Bind the Element Buffer Object (EBO) for indices
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Ebo);

            // Unbind VAO and buffers after setup
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        private static void CheckError()
        {
            ErrorCode errorCode = GL.GetError();
            if (errorCode != ErrorCode.NoError)
            {
                throw new Exception("Error code: " + errorCode);
            }
        }

        public void RecalculateNormals()
        {
            // Step 1: Retrieve vertex positions from the GPU
            Vector3[] vertices = new Vector3[IndexCount];
            GL.BindBuffer(BufferTarget.ArrayBuffer, Vbo); // Bind the VBO containing vertex data

            // Assuming the positions are stored first in the VBO
            GL.GetBufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(vertices.Length * Vector3.SizeInBytes), vertices);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Unbind the VBO

            // Step 2: Recalculate normals
            Vector3[] normals = new Vector3[vertices.Length];
            for (int i = 0; i < IndexCount; i += 3)
            {
                int idx0 = i;
                int idx1 = i + 1;
                int idx2 = i + 2;

                Vector3 v0 = vertices[idx0];
                Vector3 v1 = vertices[idx1];
                Vector3 v2 = vertices[idx2];

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                Vector3 normal = Vector3.Cross(edge1, edge2).Normalized();

                normals[idx0] += normal;
                normals[idx1] += normal;
                normals[idx2] += normal;
            }

            // Normalize the normals
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = normals[i].Normalized();
            }

            // Step 3: Upload the new normals to the GPU
            GL.BindBuffer(BufferTarget.ArrayBuffer, Vbo); // Bind the VBO containing the vertex data

            // Offset the location for where the normals are stored in the VBO
            IntPtr normalOffset = new IntPtr(Vector3.SizeInBytes * IndexCount);  // Assuming positions are stored before normals in the VBO
            GL.BufferSubData(BufferTarget.ArrayBuffer, normalOffset, (IntPtr)(normals.Length * Vector3.SizeInBytes), normals);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Unbind the VBO
        }

        // Draw Method (No need to overwrite vertex attribute pointers in Draw method)
        private void Draw(int pass)
        {
            if (pass > RenderPasses.Length - 1) return;

            Window.BuildWindow.shaderProgram = RenderPasses[pass];
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Ebo);

            // Calculate transformations
            var model = Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
            var projection = Camera.CurrentViewMatrix;
            var normalMatrix = new Matrix3(model).Inverted().Transposed();

            // Send matrices and material to the shader
            Window.SendMatricesToShader(RenderPasses[pass], model, projection, normalMatrix);
            Material?.ApplyFrame();

            // Bind the VAO and draw elements
            GL.BindVertexArray(Vao);
            GL.DrawElements(PrimitiveType.Triangles, IndexCount, DrawElementsType.UnsignedInt, 0);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            CheckError();
        }

        public Mesh Clone()
        {
            return (Mesh)MemberwiseClone();
        }
    }

}

namespace EngineInternal
{
    public static class MeshManager
    {
        private static Dictionary<MeshKey, MeshBufferData> meshCache = new Dictionary<MeshKey, MeshBufferData>();
        private static Dictionary<string, Mesh> LoadedMeshes = new Dictionary<string, Mesh>();

        public static MeshBufferData GetMeshBufferData(ReadOnlyMemory<Vector3> vertices, ReadOnlyMemory<int> indices)
        {
            MeshKey key = GenerateMeshKey(vertices, indices);

            if (meshCache.ContainsKey(key))
            {
                return meshCache[key];
            }
            else
            {
                MeshBufferData bufferData = CreateMeshBuffers(vertices, indices);
                meshCache[key] = bufferData;
                return bufferData;
            }
        }

        private static MeshBufferData CreateMeshBuffers(ReadOnlyMemory<Vector3> vertices, ReadOnlyMemory<int> indices)
        {
            int vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();
            int ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Vector3.SizeInBytes, vertices.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices.ToArray(), BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);

            return new MeshBufferData(vao, vbo, ebo);
        }

        private static MeshKey GenerateMeshKey(ReadOnlyMemory<Vector3> vertices, ReadOnlyMemory<int> indices)
        {
            return new MeshKey(vertices, indices);
        }

        public static Mesh LoadMeshFromModel(string modelName, int shaderProgram, Vector3 position)
        {
            if (!LoadedMeshes.ContainsKey(modelName))
            {
                List<Vector3> Verts = new List<Vector3>();
                List<Vector2> uvList = new List<Vector2>();

                List<Vector3> finalVerts = new List<Vector3>();
                List<Vector2> finalUVs = new List<Vector2>();
                List<int> finalIndices = new List<int>();

                Dictionary<string, int> uniqueVertexMap = new();

                using (FileStream stream = File.Open($"../../../Models/{modelName}", FileMode.Open))
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line.StartsWith("v "))
                        {
                            string[] parts = line.Substring(2).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 3)
                            {
                                float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                                float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                                float z = float.Parse(parts[2], CultureInfo.InvariantCulture);
                                Verts.Add(new Vector3(x, y, z));
                            }
                        }
                        else if (line.StartsWith("vt "))
                        {
                            string[] parts = line.Substring(3).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2 || parts.Length == 3)
                            {
                                float u = float.Parse(parts[0], CultureInfo.InvariantCulture);
                                float v = float.Parse(parts[1], CultureInfo.InvariantCulture);

                                uvList.Add(new Vector2(u, v));
                            }
                        }
                        else if (line.StartsWith("f "))
                        {
                            string[] parts = line.Substring(2).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 3) continue;

                            List<int> faceIndices = new();

                            foreach (var part in parts)
                            {
                                string[] indices = part.Split('/');
                                if (!int.TryParse(indices[0], out int vIdx)) continue;
                                vIdx -= 1;

                                int uvIdx = (indices.Length > 1 && !string.IsNullOrEmpty(indices[1])) ? int.Parse(indices[1]) - 1 : -1;

                                string key = $"{vIdx}/{uvIdx}";
                                if (!uniqueVertexMap.TryGetValue(key, out int index))
                                {
                                    uniqueVertexMap[key] = finalVerts.Count;
                                    finalVerts.Add(Verts[vIdx]);
                                    finalUVs.Add(uvIdx >= 0 && uvIdx < uvList.Count ? uvList[uvIdx] : Vector2.Zero);
                                    index = finalVerts.Count - 1;
                                }

                                faceIndices.Add(index);
                            }

                            if (faceIndices.Count >= 3)
                            {
                                for (int i = 1; i < faceIndices.Count - 1; i++)
                                {
                                    finalIndices.Add(faceIndices[0]);
                                    finalIndices.Add(faceIndices[i]);
                                    finalIndices.Add(faceIndices[i + 1]);
                                }
                            }
                        }
                    }
                }

                Mesh mesh = new Mesh(
                    new ReadOnlyMemory<Vector3>(finalVerts.ToArray()),
                    new ReadOnlyMemory<int>(finalIndices.ToArray()),
                    finalUVs.ToArray(),
                    true,
                    shaderProgram
                );

                LoadedMeshes.Add(modelName, mesh);
                return mesh;
            }
            else
            {
                if (!LoadedMeshes.TryGetValue(modelName, out Mesh mesh)) throw new Exception("Model name was in the dictionary but the associeted mesh was null");

                return mesh.Clone();
            }
        }
    }

    public struct MeshBufferData
    {
        public MeshBufferData(int v, int b, int e)
        {
            Vao = v;
            Vbo = b;
            Ebo = e;
        }

        public int Vao;
        public int Vbo;
        public int Ebo;
    }

    public readonly struct MeshKey : IEquatable<MeshKey>
    {
        public readonly int VertexHash;
        public readonly int IndexHash;

        public MeshKey(ReadOnlyMemory<Vector3> vertices, ReadOnlyMemory<int> indices)
        {
            if (vertices.Length == 0) return;
            VertexHash = HashCode.Combine(vertices.Length, vertices.Span[0].GetHashCode());
            IndexHash = HashCode.Combine(indices.Length, indices.Span[0]);
        }

        public override int GetHashCode() => HashCode.Combine(VertexHash, IndexHash);
        public bool Equals(MeshKey other) => VertexHash == other.VertexHash && IndexHash == other.IndexHash;
    }

    public enum MeshType
    {
        ClockWise,
        CounterClockWise,
    }
}