using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TexturedSphere3d
{
    internal class Camera
    {
        public Matrix4x4 ViewMatrix { get; set; }
        public Matrix4x4 ProjectionMatrix { get; set; }
        public Vector3 Position { get; }

        public Camera(Vector3 position, Vector3 target, float aspectRatio, float fov = 45f, float near = 0.1f, float far = 100f)
        {
            Position = position;
            ViewMatrix = Matrix4x4.CreateLookAt(position, target, Vector3.UnitY);
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI * fov / 180f, aspectRatio, near, far);
        }
    }
}
