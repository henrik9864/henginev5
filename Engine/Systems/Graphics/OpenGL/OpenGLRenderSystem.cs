﻿using EnCS.Attributes;
using Engine.Components;
using Engine.Components.Graphics;
using Engine.Graphics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Text;
using Shader = Engine.Graphics.Shader;

namespace Engine
{
	public struct OpenGLRenderContext
	{
		//public Camera camera;
		public Vector3f cameraPosition;
		public Quaternionf cameraRotation;
	}

	[System]
	[SystemContext<OpenGLRenderContext>]
	[UsingResource<OpenGLMeshResourceManager>]
	[UsingResource<OpenGLTextureResourceManager>]
	public partial class OpenGLRenderSystem
	{
		private static readonly Material defaultMaterial = new Material
		{
			Ambient = new Vector3f(0.0215f, 0.1745f, 0.0215f),
			Diffuse = new Vector3f(0.07568f, 0.61424f, 0.07568f),
			Specular = new Vector3f(0.633f, 0.727811f, 0.633f),
			Shininess = 2f
		};

		private static readonly Light defaultLight = new Light
		{
			Ambient = new Vector3f(0.2f, 0.2f, 0.2f),
			Diffuse = new Vector3f(0.5f, 0.5f, 0.5f),
			Specular = new Vector3f(1, 1, 1)
		};

		GL gl;
		IWindow window;

		Shader shader;
		ShaderProgram shaderProgram;

        public OpenGLRenderSystem(GL gl, IWindow window)
        {
            this.gl = gl;
			this.window = window;
		}

		public void Init()
		{
			shader = Shader.FromFiles("Shaders/Shader.vert", "Shaders/Shader.frag");
			shaderProgram = CreateShaderProgram(gl, shader);
		}

		public void Dispose()
		{

		}

		public void PreRun()
		{
			window.DoEvents();
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			Thread.Sleep(10);
		}

		[SystemUpdate, SystemLayer(0)]
		public void CameraUpdate(ref OpenGLRenderContext context, ref Position position, ref Rotation rotation, ref Camera camera)
		{
			/*
			context.camera = new Camera()
			{
				zNear = camera.zNear,
				zFar = camera.zFar,
				fov = camera.fov,
				width = camera.width,
				height = camera.height,
			};
			*/

			context.cameraPosition = new Vector3f(position.x, position.y, position.z);
			context.cameraRotation = new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w);
		}

		[SystemUpdate, SystemLayer(1)]
		public unsafe void Update(ref OpenGLRenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref GlMeshBuffer mesh, ref GlTextureBuffer texture)
		{
			ShaderUniforms shaderUniforms = GetShaderUniforms(gl, shaderProgram);

			gl.BindVertexArray(mesh.vao.ID);
			gl.UseProgram((uint)shaderProgram.ID);

			SetModelUniforms(ref position, ref rotation, ref scale, shaderUniforms);
			//SetCameraUniforms(ref context.camera, ref context.cameraPosition, ref context.cameraRotation, shaderUniforms);
			SetMaterialUniforms(defaultMaterial, shaderUniforms);
			SetLightUniforms(defaultLight, new Position(), shaderUniforms);

			gl.DrawElements(PrimitiveType.Triangles, mesh.vao.length, DrawElementsType.UnsignedInt, null);
		}

		public void PostRun()
		{
			window.SwapBuffers();
		}

		unsafe void SetModelUniforms(ref Position position, ref Rotation rotation, ref Scale scale, in ShaderUniforms uniforms)
		{
			// Model matrix components
			Matrix4x4f translationMatrix = Matrix4x4f.CreateTranslation(new Vector3f(position.x, position.y, position.z));
			Matrix4x4f rotationMatrix = Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			Matrix4x4f scaleMatrix = Matrix4x4f.CreateScale(new Vector3f(scale.x, scale.y, scale.z));

			UniformMatrix4(gl, uniforms.Model.Translation, false, translationMatrix);
			UniformMatrix4(gl, uniforms.Model.Rotation, false, rotationMatrix);
			UniformMatrix4(gl, uniforms.Model.Scale, false, scaleMatrix);
		}

		unsafe void SetCameraUniforms(ref Camera camera, ref Position position, ref Rotation rotation, ref ShaderUniforms uniforms)
		{
			Matrix4x4f view = Matrix4x4f.CreateTranslation(-new Vector3f(position.x, position.y, position.z)) * Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			Matrix4x4f projection = Matrix4x4f.CreatePersperctive(camera.fov, camera.width / camera.height, camera.zNear, camera.zFar);

			UniformMatrix4(gl, uniforms.Camera.View, false, view);
			gl.Uniform3(uniforms.Camera.ViewPos, 1, in position.x);
			UniformMatrix4(gl, uniforms.Camera.Projection, false, projection);
		}

		void SetMaterialUniforms(in Material material, in ShaderUniforms uniforms)
		{
			gl.Uniform3(uniforms.Material.Ambient, 1, material.Ambient.x);
			gl.Uniform3(uniforms.Material.Diffuse, 1, material.Diffuse.x);
			gl.Uniform3(uniforms.Material.Specular, 1, material.Specular.x);
			gl.Uniform1(uniforms.Material.Shininess, material.Shininess);
		}

		void SetLightUniforms(in Light light, in Position position, in ShaderUniforms uniforms)
		{
			gl.Uniform3(uniforms.Light.Ambient, 1, light.Ambient.x);
			gl.Uniform3(uniforms.Light.Diffuse, 1, light.Diffuse.x);
			gl.Uniform3(uniforms.Light.Specular, 1, light.Specular.x);
			gl.Uniform3(uniforms.Light.Position, 1, in position.x);
		}

		unsafe void UniformMatrix4(GL gl, int location, bool transpose, in Matrix4x4f matrix)
		{
			gl.UniformMatrix4(location, 1, transpose, matrix.m11);
		}

		static ShaderProgram CreateShaderProgram(GL gl, Shader shader)
		{
			uint program = gl.CreateProgram();
			uint vertexID = gl.CreateShader(ShaderType.VertexShader);
			uint fragmentID = gl.CreateShader(ShaderType.FragmentShader);

			CompileShader(gl, vertexID, shader.Vertex);
			CompileShader(gl, fragmentID, shader.Fragment);

			gl.AttachShader(program, vertexID);
			gl.AttachShader(program, fragmentID);

			gl.LinkProgram(program);
			gl.ValidateProgram(program);

			gl.DeleteShader(vertexID);
			gl.DeleteShader(fragmentID);

			gl.GetProgram(program, GLEnum.LinkStatus, out var status);
			if (status == 0)
				Console.WriteLine($"Error linking shader {gl.GetProgramInfoLog(program)}");

			return new ShaderProgram()
			{
				ID = program,
			};
		}

		static ShaderUniforms GetShaderUniforms(GL gl, in ShaderProgram program)
		{
			return new ShaderUniforms
			{
				Model = new ModelUniforms
				{
					Translation = gl.GetUniformLocation((uint)program.ID, "u_Translation"),
					Rotation = gl.GetUniformLocation((uint)program.ID, "u_Rotation"),
					Scale = gl.GetUniformLocation((uint)program.ID, "u_Scale"),
				},

				Camera = new CameraUniforms
				{
					View = gl.GetUniformLocation((uint)program.ID, "u_View"),
					ViewPos = gl.GetUniformLocation((uint)program.ID, "u_ViewPos"),
					Projection = gl.GetUniformLocation((uint)program.ID, "u_Projection"),
				},

				Light = new LightUniforms
				{
					Ambient = gl.GetUniformLocation((uint)program.ID, "u_Light.ambient"),
					Diffuse = gl.GetUniformLocation((uint)program.ID, "u_Light.diffuse"),
					Specular = gl.GetUniformLocation((uint)program.ID, "u_Light.specular"),
					Position = gl.GetUniformLocation((uint)program.ID, "u_Light.position"),
				},

				Material = new MaterialUniforms
				{
					Ambient = gl.GetUniformLocation((uint)program.ID, "u_Material.ambient"),
					Diffuse = gl.GetUniformLocation((uint)program.ID, "u_Material.diffuse"),
					Specular = gl.GetUniformLocation((uint)program.ID, "u_Material.specular"),
					Shininess = gl.GetUniformLocation((uint)program.ID, "u_Material.shininess"),
				}
			};
		}

		static void CompileShader(GL gl, uint id, byte[] source)
		{
			gl.ShaderSource(id, Encoding.UTF8.GetString(source));
			gl.CompileShader(id);

			gl.GetShader(id, ShaderParameterName.CompileStatus, out int result);
			if (result == 0)
				throw new Exception(gl.GetShaderInfoLog(id));
		}

		/*
		public void Update(ref Position.Vectorized position, ref Rotation.Vectorized rotation, ref Scale.Vectorized scale, ref VertexBuffer.Vectorized vb, ref ElementBuffer.Vectorized eb, ref ShaderProgram.Vectorized shader)
		{
		}
		*/
	}
}
