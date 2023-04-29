using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Inheritance.Geometry.Visitor
{
    public abstract class Body
    {
        public Vector3 Position { get; }

        protected Body(Vector3 position)
        {
            Position = position;
        }

        public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
    }

    public class Ball : Body
    {
        public double Radius { get; }

        public Ball(Vector3 position, double radius) : base(position)
        {
            Radius = radius;
        }

        public override TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public class RectangularCuboid : Body
    {
        public double SizeX { get; }
        public double SizeY { get; }
        public double SizeZ { get; }

        public RectangularCuboid(Vector3 position, double sizeX, double sizeY, double sizeZ) : base(position)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
        }

        public Vector3 GetMinValue()
        {
            return new Vector3(Position.X - this.SizeX / 2, Position.Y - this.SizeY / 2, Position.Z - this.SizeZ / 2);
        }

        public Vector3 GetMaxValue()
        {
            return new Vector3(Position.X + this.SizeX / 2, Position.Y + this.SizeY / 2, Position.Z + this.SizeZ / 2);
        }

        public override TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public class Cylinder : Body
    {
        public double SizeZ { get; }

        public double Radius { get; }

        public Cylinder(Vector3 position, double sizeZ, double radius) : base(position)
        {
            SizeZ = sizeZ;
            Radius = radius;
        }

        public override TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public class CompoundBody : Body
    {
        public IReadOnlyList<Body> Parts { get; }

        public CompoundBody(IReadOnlyList<Body> parts) : base(parts[0].Position)
        {
            Parts = parts;
        }

        public override TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public interface IVisitor<TResult>
    {
        TResult Visit(Ball ball);
        TResult Visit(RectangularCuboid cuboid);
        TResult Visit(Cylinder cylinder);
        TResult Visit(CompoundBody compound);
    }

    public class BoundingBoxVisitor : IVisitor<RectangularCuboid>
    {
        public RectangularCuboid Visit(Ball ball)
        {
            double length = ball.Radius * 2;
            return new RectangularCuboid(ball.Position, length, length, length);
        }

        public RectangularCuboid Visit(RectangularCuboid cuboid)
        {
            return new RectangularCuboid(cuboid.Position, cuboid.SizeX, cuboid.SizeY, cuboid.SizeZ);
        }

        public RectangularCuboid Visit(Cylinder cylinder)
        {
            return new RectangularCuboid(cylinder.Position, cylinder.Radius * 2, cylinder.Radius * 2, cylinder.SizeZ);
        }

        public RectangularCuboid Visit(CompoundBody compound)
        {
            ////говнокод, не ругайтесь :)
            var minValueX = compound.Parts.Select(x => x.TryAcceptVisitor<RectangularCuboid>(new BoundingBoxVisitor())
            .GetMinValue().X).Min();
            var maxValueX = compound.Parts.Select(x => x.TryAcceptVisitor<RectangularCuboid>(new BoundingBoxVisitor())
            .GetMaxValue().X).Max();

            var minValueY = compound.Parts.Select(x => x.TryAcceptVisitor<RectangularCuboid>(new BoundingBoxVisitor())
            .GetMinValue().Y).Min();
            var maxValueY = compound.Parts.Select(x => x.TryAcceptVisitor<RectangularCuboid>(new BoundingBoxVisitor())
            .GetMaxValue().Y).Max();

            var minValueZ = compound.Parts.Select(x => x.TryAcceptVisitor<RectangularCuboid>(new BoundingBoxVisitor())
            .GetMinValue().Z).Min();
            var maxValueZ = compound.Parts.Select(x => x.TryAcceptVisitor<RectangularCuboid>(new BoundingBoxVisitor())
            .GetMaxValue().Z).Max();


            return new RectangularCuboid(
                new Vector3((maxValueX + minValueX) / 2, (maxValueY + minValueY) / 2, (maxValueZ + minValueZ) / 2),
                maxValueX - minValueX, maxValueY - minValueY, maxValueZ - minValueZ);
            throw new System.NotImplementedException();
        }
    }

    public class BoxifyVisitor : IVisitor<Body>
    {
        public Body Visit(Ball ball)
        {
            return ball.TryAcceptVisitor<RectangularCuboid>(new BoundingBoxVisitor());
        }

        public Body Visit(RectangularCuboid cuboid)
        {
            return cuboid.TryAcceptVisitor<RectangularCuboid>(new BoundingBoxVisitor());
        }

        public Body Visit(Cylinder cylinder)
        {
            return cylinder.TryAcceptVisitor<RectangularCuboid>(new BoundingBoxVisitor());
        }

        public Body Visit(CompoundBody compound)
        {
            List<Body> parts = new List<Body>();

            foreach (var part in compound.Parts)
            {
                parts.Add(part.TryAcceptVisitor<Body>(new BoxifyVisitor()));
            }

            return new CompoundBody(parts);
        }
    }
}