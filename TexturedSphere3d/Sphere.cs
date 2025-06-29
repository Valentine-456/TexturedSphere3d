using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace TexturedSphere3d
{
    internal class Sphere
    {
        public List<Vector3> Vertices { get; } = new();
        public List<(int A, int B, int C)> Faces { get; } = new();
        public List<Vector2> TexCoords { get; } = new();
        Matrix4x4 ModelMatrix { get; set; } = Matrix4x4.Identity;

        public Sphere(float radius, int latSubdivisions, int lonSubdivisions)
        {
            for (int i = 0; i <= latSubdivisions; i++)
            {
                float theta = (float)(Math.PI * i / latSubdivisions);
                float y = (float)Math.Cos(theta);
                float r = (float)Math.Sin(theta);

                for (int j = 0; j <= lonSubdivisions; j++)
                {
                    float phi = (float)(2 * Math.PI * j / lonSubdivisions);
                    float x = r * (float)Math.Cos(phi);
                    float z = r * (float)Math.Sin(phi);

                    Vertices.Add(new Vector3(x, y, z) * radius);
                    TexCoords.Add(new Vector2(j / (float)lonSubdivisions, i / (float)latSubdivisions));
                }
            }

            for (int i = 0; i < latSubdivisions; i++)
            {
                for (int j = 0; j < lonSubdivisions; j++)
                {
                    int current = i * (lonSubdivisions + 1) + j;
                    int next = current + lonSubdivisions + 1;

                    Faces.Add((current, next, current + 1));
                    Faces.Add((current + 1, next, next + 1));
                }
            }
        }

        public void Render(Camera camera, Canvas canvas)
        {
            canvas.Children.Clear();

            Matrix4x4 mvp = ModelMatrix * camera.ViewMatrix * camera.ProjectionMatrix;

            foreach (var face in Faces)
            {
                var p1 = Project(Vertices[face.A], mvp, canvas);
                var p2 = Project(Vertices[face.B], mvp, canvas);
                var p3 = Project(Vertices[face.C], mvp, canvas);

                var polygon = new System.Windows.Shapes.Polygon
                {
                    Points = new System.Windows.Media.PointCollection { p1, p2, p3 },
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                canvas.Children.Add(polygon);
            }
        }

        private System.Windows.Point Project(Vector3 v, Matrix4x4 mvp, Canvas canvas)
        {
            var transformed = Vector3.Transform(v, mvp);
            if (transformed.Z != 0)
                transformed /= transformed.Z;

            return new System.Windows.Point(
                (transformed.X + 1) * canvas.ActualWidth / 2,
                (-transformed.Y + 1) * canvas.ActualHeight / 2
            );
        }
    }
}
