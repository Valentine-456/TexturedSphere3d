using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TexturedSphere3d
{
    internal class Sphere
    {
        public List<Vector3> Vertices { get; } = new();
        public List<(int A, int B, int C)> Faces { get; } = new();
        public List<Vector2> TextureCoords { get; } = new();
        Matrix4x4 ModelMatrix { get; set; } = Matrix4x4.Identity;
        public WriteableBitmap textureBitmap = null;
        private WriteableBitmap frameBuffer = null;



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
                    TextureCoords.Add(new Vector2(j / (float)lonSubdivisions, i / (float)latSubdivisions));
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
            int w = (int)canvas.ActualWidth, h = (int)canvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            var mvp = camera.ViewMatrix * camera.ProjectionMatrix;

            if (textureBitmap == null)
            {
                canvas.Children.Clear();

                foreach (var (A, B, C) in Faces)
                {
                    var w1 = Vector3.Transform(Vertices[A], ModelMatrix);
                    var w2 = Vector3.Transform(Vertices[B], ModelMatrix);
                    var w3 = Vector3.Transform(Vertices[C], ModelMatrix);

                    var normal = Vector3.Cross(w2 - w1, w3 - w1);
                    if (Vector3.Dot(normal, camera.Position - w1) <= 0)
                        continue;

                    var p1 = Project(w1, mvp, w, h);
                    var p2 = Project(w2, mvp, w, h);
                    var p3 = Project(w3, mvp, w, h);

                    var poly = new System.Windows.Shapes.Polygon
                    {
                        Stroke = Brushes.White,
                        StrokeThickness = 1,
                        Points = new PointCollection { p1, p2, p3 }
                    };
                    canvas.Children.Add(poly);
                }
                return;
            }

            if (frameBuffer == null
             || frameBuffer.PixelWidth != w
             || frameBuffer.PixelHeight != h)
            {
                frameBuffer = new WriteableBitmap(
                    w, h, 96, 96, PixelFormats.Bgra32, null);
            }

            frameBuffer.Lock();
            unsafe
            {
                System.Buffer.MemoryCopy(
                    source: IntPtr.Zero.ToPointer(),
                    destination: frameBuffer.BackBuffer.ToPointer(),
                    destinationSizeInBytes: (uint)(frameBuffer.BackBufferStride * h),
                    sourceBytesToCopy: 0);
            }


            for (int fi = 0; fi < Faces.Count; fi++)
            {
                var (A, B, C) = Faces[fi];

                var w1 = Vector3.Transform(Vertices[A], ModelMatrix);
                var w2 = Vector3.Transform(Vertices[B], ModelMatrix);
                var w3 = Vector3.Transform(Vertices[C], ModelMatrix);
                var n = Vector3.Cross(w2 - w1, w3 - w1);
                if (Vector3.Dot(n, camera.Position - w1) <= 0)
                    continue;

                var c1 = Vector3.Transform(w1, mvp);
                var c2 = Vector3.Transform(w2, mvp);
                var c3 = Vector3.Transform(w3, mvp);

                var p1 = ToScreen(c1, w, h, out float z1);
                var p2 = ToScreen(c2, w, h, out float z2);
                var p3 = ToScreen(c3, w, h, out float z3);

                var uv1 = TextureCoords[A];
                var uv2 = TextureCoords[B];
                var uv3 = TextureCoords[C];

                DrawTexturedTriangle(
                  frameBuffer, textureBitmap,
                  p1, p2, p3,
                  z1, z2, z3,
                  uv1, uv2, uv3);
            }

            frameBuffer.AddDirtyRect(new Int32Rect(0, 0, w, h));
            frameBuffer.Unlock();

            canvas.Children.Clear();
            var img = new Image
            {
                Source = frameBuffer,
                Width = w,
                Height = h
            };
            canvas.Children.Add(img);
        }

        private Point Project(Vector3 worldPos, Matrix4x4 mvp, int w, int h)
        {
            var clip = Vector3.Transform(worldPos, mvp);
            clip /= clip.Z;
            return new Point(
              (clip.X + 1) * 0.5 * w,
              (-clip.Y + 1) * 0.5 * h);
        }


        private Point Project(Vector3 v, Matrix4x4 mvp, Canvas canvas)
        {
            var transformed = Vector3.Transform(v, mvp);
            if (transformed.Z != 0)
                transformed /= transformed.Z;

            return new Point(
                (transformed.X + 1) * canvas.ActualWidth / 2,
                (-transformed.Y + 1) * canvas.ActualHeight / 2
            );
        }

        private void DrawTexturedTriangle(
            WriteableBitmap target,
            WriteableBitmap texture,
            Point p1, Point p2, Point p3,
            float z1, float z2, float z3,
            Vector2 uv1, Vector2 uv2, Vector2 uv3
        )
        {
            if (texture == null) return;

            float o1 = 1f / z1, o2 = 1f / z2, o3 = 1f / z3;
            float u1z = uv1.X * o1, u2z = uv2.X * o2, u3z = uv3.X * o3;
            float v1z = uv1.Y * o1, v2z = uv2.Y * o2, v3z = uv3.Y * o3;

            int minX = (int)Math.Max(0, Math.Floor(Math.Min(p1.X, Math.Min(p2.X, p3.X))));
            int maxX = (int)Math.Min(target.PixelWidth - 1, Math.Ceiling(Math.Max(p1.X, Math.Max(p2.X, p3.X))));
            int minY = (int)Math.Max(0, Math.Floor(Math.Min(p1.Y, Math.Min(p2.Y, p3.Y))));
            int maxY = (int)Math.Min(target.PixelHeight - 1, Math.Ceiling(Math.Max(p1.Y, Math.Max(p2.Y, p3.Y))));

            unsafe
            {
                byte* dst0 = (byte*)target.BackBuffer.ToPointer();
                int dstStride = target.BackBufferStride;
                byte* src0 = (byte*)texture.BackBuffer.ToPointer();
                int srcStride = texture.BackBufferStride;
                int tw = texture.PixelWidth, th = texture.PixelHeight;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        var bc = Barycentric(p1, p2, p3, x + .5f, y + .5f);
                        if (bc.X < 0 || bc.Y < 0 || bc.Z < 0) continue;

                        float invZ = bc.X * o1 + bc.Y * o2 + bc.Z * o3;
                        float u = (bc.X * u1z + bc.Y * u2z + bc.Z * u3z) / invZ;
                        float v = (bc.X * v1z + bc.Y * v2z + bc.Z * v3z) / invZ;

                        int tx = Math.Clamp((int)(u * (tw - 1)), 0, tw - 1);
                        int ty = Math.Clamp((int)(v * (th - 1)), 0, th - 1);

                        byte* ps = src0 + ty * srcStride + tx * 4;
                        byte* pd = dst0 + y * dstStride + x * 4;
                        pd[0] = ps[0]; pd[1] = ps[1]; pd[2] = ps[2]; pd[3] = ps[3];
                    }
                }
            }
        }


        private Point ToScreen(Vector3 clip, int w, int h, out float depth)
        {
            depth = clip.Z;
            float ooz = 1f / clip.Z;
            float x = (clip.X * ooz + 1f) * 0.5f * w;
            float y = (-clip.Y * ooz + 1f) * 0.5f * h;
            return new Point((int)x, (int)y);
        }

        private Vector3 Barycentric(Point a, Point b, Point c, float px, float py)
        {
            double v0x = b.X - a.X, v0y = b.Y - a.Y;
            double v1x = c.X - a.X, v1y = c.Y - a.Y;
            double v2x = px - a.X, v2y = py - a.Y;
            double d00 = v0x * v0x + v0y * v0y, d01 = v0x * v1x + v0y * v1y, d11 = v1x * v1x + v1y * v1y;
            double d20 = v2x * v0x + v2y * v0y, d21 = v2x * v1x + v2y * v1y;
            double denom = d00 * d11 - d01 * d01;
            if (Math.Abs(denom) < 1e-6f) return new Vector3(-1, -1, -1);
            double v = (d11 * d20 - d01 * d21) / denom;
            double w = (d00 * d21 - d01 * d20) / denom;
            double u = 1f - v - w;
            return new Vector3((float)u, (float)v, (float)w);
        }

    }
}
