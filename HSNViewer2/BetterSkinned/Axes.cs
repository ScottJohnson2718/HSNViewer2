using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//summary
// Procedurally created axes.  Lets you specify the length, radius and vertex count around the base.  It is a length because it points along X.  It doesn't currently 
// have a polygon for the base of the cone.
//
public class Axes
{
    Cone    x, y, z;

    public Axes(float aLength, float aRadius, int aPointCount, GraphicsDeviceManager aGraphics)
    {
        x = new Cone(aLength, aRadius, aPointCount, new Color(1.0f, 0.0f, 0.0f), aGraphics);
        y = new Cone(aLength, aRadius, aPointCount, new Color(0.0f, 1.0f, 0.0f), aGraphics);
        z = new Cone(aLength, aRadius, aPointCount, new Color(0.0f, 0.0f, 1.0f), aGraphics);
    }

    public void LoadGraphicsContent(bool loadAllContent)
    {
        if (loadAllContent)
        {
            x.LoadGraphicsContent(loadAllContent);
            y.LoadGraphicsContent(loadAllContent);
            z.LoadGraphicsContent(loadAllContent);
        }
    }

    public void Draw(Matrix view, Matrix world, Matrix projection)
    {
        x.Draw(view, world, projection);
        y.Draw(view, Matrix.CreateRotationZ(MathHelper.PiOver2) * world, projection);
        z.Draw(view, Matrix.CreateRotationY(-MathHelper.PiOver2) * world, projection);
    }
}