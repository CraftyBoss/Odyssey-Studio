using System;
using System.Collections.Generic;
using System.Text;
using GLFrameworkEngine;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace RedStarLibrary.Rendering.Area.Shapes
{
    public class CylinderShape : RenderMesh<VertexPositionNormal>
    {
        public CylinderShape(float radius, float height, PrimitiveType primitiveType = PrimitiveType.LineLoop)
            : base(GetCylinderLineVertices(radius, height, 16),
                  primitiveType)
        {

        }

        public static VertexPositionNormal[] GetCylinderLineVertices(float radius, float height, float slices)
        {
            List<VertexPositionNormal> vertices = new List<VertexPositionNormal>();

            List<Vector3> discPointsBottom = new List<Vector3>();
            List<Vector3> discPointsTop = new List<Vector3>();

            // generates verticies for entire cylinder
            float sliceArc = 360.0f / (float)slices;
            float angle = 0;
            for (int i = 0; i < slices; i++)
            {
                float x = radius * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                float z = radius * (float)Math.Sin(MathHelper.DegreesToRadians(angle));
                discPointsBottom.Add(new Vector3(x, 0, z));

                x = radius * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                z = radius * (float)Math.Sin(MathHelper.DegreesToRadians(angle));

                discPointsTop.Add(new Vector3(x, height, z));
                angle += sliceArc;
            }

            // Creates Cylinder Lines for each vertex in the cylinder.
            for (int i = 0; i < slices; i++)
            {
                Vector3 p2 = discPointsBottom[i % discPointsBottom.Count];
                Vector3 p1 = new Vector3(discPointsBottom[(i + 1) % discPointsBottom.Count]);

                // Top perimeter lines
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p2.X, 0, p2.Z) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p1.X, 0, p1.Z) });

                // Top cap lines
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(0, 0, 0) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p1.X, 0, p1.Z) });

                p2 = discPointsTop[i % discPointsTop.Count];
                p1 = discPointsTop[(i + 1) % discPointsTop.Count];

                // Bottom perimeter lines
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p1.X, height, p1.Z) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p2.X, height, p2.Z) });

                // Bottom cap lines
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(0, height, 0) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p1.X, height, p1.Z) });
            }

            Console.WriteLine("Vertex Count: " + vertices.Count);

            return vertices.ToArray();
        }
    }
}
