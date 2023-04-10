using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using RedStarLibrary.Rendering.Area.Shapes;
using Toolbox.Core.ViewModels;

namespace RedStarLibrary.Rendering.Area
{
    public class AreaRender : EditableObject, IColorPickable
    {

        public enum AreaType
        {
            CubeBase, // origin at bottom of cube
            CubeCenter, // origin in center of cube
            CubeTop, // origin at top of cube
            Sphere, // origin always in center
            CylinderBase, // origin at bottom of cyl
            CylinderTop, // origin at top of cyl
            CylinderCenter // origin in center of cyl
        }

        public static bool DrawFilled = false;
        public static float Transparency = 0.1f;

        public Vector4 Color = Vector4.One;
        public Vector4 FillColor = new Vector4(0.4f, 0.7f, 1.0f, 0.3f);

        public AreaType AreaShape;

        CubeCrossedRenderer CubeOutlineRender = null;
        CubeRenderer CubeFilledRenderer = null;

        SphereRender SphereOutlineRender = null;
        SphereRender SphereFilledRenderer = null;

        CylinderShape CylinderOutlineRender = null;
        CylinderShape CylinderFilledRenderer = null;

        //Area boxes have an inital transform
        static Matrix4 InitalTransform => new Matrix4(
            500, 0, 0, 0,
            0, 500, 0, 0,
            0, 0, 500, 0,
            0, 500, 0, 1);

        public AreaRender(NodeBase parent, Vector4 color) : base(parent)
        {
            Color = color;
        }

        public override BoundingNode BoundingNode { get; } = new BoundingNode()
        {
            Box = new BoundingBox(
                new OpenTK.Vector3(-100, -100, -50),
                new OpenTK.Vector3(100, 100, 150)),
        };

        public void DrawColorPicking(GLContext context)
        {
            Prepare();

            //Thicker picking region
            GL.LineWidth(32);

            switch (AreaShape)
            {
                case AreaType.CubeBase:
                    CubeOutlineRender.DrawPicking(context, this, InitalTransform * Transform.TransformMatrix);
                    break;
                case AreaType.Sphere:
                    SphereOutlineRender.DrawPicking(context, this, InitalTransform * Transform.TransformMatrix);
                    break;
                case AreaType.CylinderBase:
                    CylinderOutlineRender.DrawPicking(context, this, InitalTransform * Transform.TransformMatrix);
                    break;
                default:
                    break;
            }
                
            GL.LineWidth(1);
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if (pass != Pass.OPAQUE)
                return;

            var matrix = InitalTransform * Transform.TransformMatrix;

            Prepare();

            GL.Disable(EnableCap.CullFace);

            //Draw a filled in region
            if (DrawFilled)
            {
                GLMaterialBlendState.TranslucentAlphaOne.RenderBlendState();
                GLMaterialBlendState.TranslucentAlphaOne.RenderDepthTest();

                switch (AreaShape)
                {
                    case AreaType.CubeBase:
                        CubeFilledRenderer.DrawSolid(context, matrix, new Vector4(Color.Xyz, Transparency));
                        break;
                    case AreaType.Sphere:
                        SphereFilledRenderer.DrawSolid(context, matrix, new Vector4(Color.Xyz, Transparency));
                        break;
                    case AreaType.CylinderBase:
                        CylinderFilledRenderer.DrawSolid(context, matrix, new Vector4(Color.Xyz, Transparency));
                        break;
                    default:
                        break;
                }
                    
                GLMaterialBlendState.Opaque.RenderBlendState();
                GLMaterialBlendState.Opaque.RenderDepthTest();
            }

            //Draw lines of the region
            GL.LineWidth(4);

            switch (AreaShape)
            {
                case AreaType.CubeBase:
                    CubeOutlineRender.DrawSolidWithSelection(context, matrix, Color, IsSelected | IsHovered);
                    break;
                case AreaType.Sphere:
                    SphereOutlineRender.DrawSolidWithSelection(context, matrix, Color, IsSelected | IsHovered);
                    break;
                case AreaType.CylinderBase:
                    CylinderOutlineRender.DrawSolidWithSelection(context, matrix, Color, IsSelected | IsHovered);
                    break;
                default:
                    break;
            }
                
            GL.LineWidth(1);

            GL.Enable(EnableCap.CullFace);
        }

        private void Prepare()
        {

            switch (AreaShape)
            {
                case AreaType.CubeBase:
                    if (CubeOutlineRender == null)
                        CubeOutlineRender = new CubeCrossedRenderer(1, PrimitiveType.LineStrip);
                    if (CubeFilledRenderer == null)
                        CubeFilledRenderer = new CubeRenderer(1);
                    break;
                case AreaType.Sphere:
                    if (SphereOutlineRender == null)
                        SphereOutlineRender = new SphereRender(0.5f, 10, 10, PrimitiveType.LineStrip);
                    if (SphereFilledRenderer == null)
                        SphereFilledRenderer = new SphereRender(0.5f, 10, 10);
                    break;
                case AreaType.CylinderBase:
                    if (CylinderOutlineRender == null)
                        CylinderOutlineRender = new CylinderShape(1, -1, PrimitiveType.LineStrip);
                    if (CylinderFilledRenderer == null)
                        CylinderFilledRenderer = new CylinderShape(1, -1, PrimitiveType.LineStrip);
                    break;
                default:
                    break;
            }
        }

        public override void Dispose()
        {
            CubeOutlineRender?.Dispose();
            CubeFilledRenderer?.Dispose();

            SphereOutlineRender?.Dispose();
            SphereFilledRenderer?.Dispose();
        }

        class CubeCrossedRenderer : RenderMesh<VertexPositionNormal>
        {
            public CubeCrossedRenderer(float size = 1.0f, PrimitiveType primitiveType = PrimitiveType.Triangles) :
                base(DrawingHelper.GetCubeVertices(size), Indices, primitiveType)
            {

            }

            public static int[] Indices = new int[]
            {
            // front face
            0, 1, 2, 2, 3, 0,
            // top face
            3, 2, 6, 6, 7, 3,
            // back face
            7, 6, 5, 5, 4, 7,
            // left face
            4, 0, 3, 3, 7, 4,
            // bottom face
            0, 5, 1, 4, 5, 0, //Here we swap some indices for a cross section at the bottom
            // right face
            1, 5, 6, 6, 2, 1,};
        }
    }
}
