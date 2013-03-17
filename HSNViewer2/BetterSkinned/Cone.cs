using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//summary
// A procedurally created cone.  Lets you specify the length, radius and vertex count around the base.  It is a length because it points along X.  It doesn't currently 
// have a polygon for the base of the cone.
//
public class Cone
{
    GraphicsDeviceManager graphics;
    BasicEffect basicEffect;

    Color color;

    VertexPositionColor[] pointList;
    short[] triangleListIndices;

    int pointCount = 8;
    float length = 1.0f;
    float radius = 0.2f;

    public Cone(float aLength, float aRadius, int aPointCount, Color aColor, GraphicsDeviceManager aGraphics)
    {
        length = aLength;
        radius = aRadius;
        pointCount = aPointCount;
        graphics = aGraphics;
        color = aColor;

    }

    public void LoadGraphicsContent(bool loadAllContent)
    {
        if (loadAllContent)
        {
            InitializeEffect();
            InitializePointList();
            InitializeTriangleList();
        }
    }

    private void InitializePointList()
    {
        pointList = new VertexPositionColor[pointCount + 1];

        // Create the point of the cone.
        pointList[0] = new VertexPositionColor( new Vector3(length, 0.0f, 0.0f), color );

        // Create the circular base
        float angle = 0.0f;
        float step = MathHelper.TwoPi / pointCount;

        for (int i = 1; i <= pointCount; ++i)
        {
            float y = (float) (Math.Sin(angle) * radius);
            float z = (float) (Math.Cos(angle) * radius);

            // normals are all forward somewhat arbitrarily
            pointList[i] = new VertexPositionColor(new Vector3(0.0f, y, z), color);

            angle += step;
        }
    }

    private void InitializeTriangleList()
    {
        triangleListIndices = new short[pointCount * 3];

        for (int i = 0; i < pointCount; ++i)
        {
            triangleListIndices[i * 3] = 0; // each vertex uses the pointy part of the cone
            triangleListIndices[i * 3 + 1] = (short)(i + 1);
            triangleListIndices[i * 3 + 2] = (short)(i + 2);
        }
        triangleListIndices[(pointCount * 3) - 1] = 1;
    }

    public void InitializeEffect()
    {
        basicEffect = new BasicEffect(graphics.GraphicsDevice);
        basicEffect.DiffuseColor = new Vector3(color.R, color.G, color.B);

        basicEffect.Projection = new Matrix();
        basicEffect.View = new Matrix();
    }


    public void Draw(Matrix view, Matrix world, Matrix projection)
    {

        // Get these from the main loop.  They are not independent.  This is odd.  Hmmm.
        basicEffect.Projection = projection;
        basicEffect.World = world;
        basicEffect.View = view;

        // I believe there is only one pass but I'm just being thorough and
        // safe by following this pattern
        foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
        {
            basicEffect.CurrentTechnique = basicEffect.Techniques[0];

            pass.Apply();

            graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                 PrimitiveType.TriangleList,
                 pointList,
                 0,   // vertex buffer offset to add to each element of the index buffer
                 pointCount + 1,   // number of vertices to draw
                 triangleListIndices,
                 0,   // first index element to read
                 pointCount,   // number of primitives to draw
                 VertexPositionColor.VertexDeclaration
             );
        }
    }
}