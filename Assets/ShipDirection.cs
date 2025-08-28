using System;
using System.Linq;
using UnityEngine;

struct Axis
{
    public Dimension Dimension;
    public int Sign;
    
    public Axis(Dimension dimension, int sign)
    {
        Dimension = dimension;
        Sign = sign;
    }
}

enum Dimension
{
    X,
    Y,
    Z,
    W
}

public struct Location
{
    public int X, Y, Z, W;

    public Location(int x, int y, int z, int w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public int ToInt()
    {
        var pos = ((W * 3 + Y) * 3 + Z) * 3 + X;
        return pos;
    }
}

class ShipDirection
{
    public Axis Right = new Axis(Dimension.X, 1);
    public Axis Forwards = new Axis(Dimension.Y, 1);
    public Axis Up = new Axis(Dimension.Z, 1);
    public Axis Zag = new Axis(Dimension.W, 1);

    public ShipDirection(ShipDirection direction)
    {
        Right = direction.Right;
        Forwards = direction.Forwards;
        Up = direction.Up;
        Zag = direction.Zag;
    }

    public ShipDirection()
    {
        
    }
    
    public void Rotate(Axis rotation, int parity)
    {
        var oldForwards = Forwards;
        if (Right.Dimension == rotation.Dimension)
            Right = new Axis(oldForwards.Dimension, -oldForwards.Sign * parity);
        if (Up.Dimension == rotation.Dimension)
            Up = new Axis(oldForwards.Dimension, -oldForwards.Sign * parity);
        if (Zag.Dimension == rotation.Dimension)
            Zag = new Axis(oldForwards.Dimension, -oldForwards.Sign * parity);
        Forwards = rotation;
    }
    
    public Location GetLocationAt(Axis axis, Location location)
    {
        switch (axis.Dimension)
        {
            case Dimension.X:
                return new Location(location.X + axis.Sign, location.Y, location.Z, location.W);
            case Dimension.Y:
                return new Location(location.X, location.Y + axis.Sign, location.Z, location.W);
            case Dimension.Z:
                return new Location(location.X, location.Y, location.Z + axis.Sign, location.W);
            case Dimension.W:
                return new Location(location.X, location.Y, location.Z, location.W + axis.Sign);
            default:
                return location;
        }
    }
    
    public void MoveForward(ref Location position)
    {
        Debug.LogFormat("Right: {0}{1} Forward: {2}{3} Up: {4}{5} Zag: {6}{7}", Right.Sign == -1 ? "-" : "+", Right.Dimension, Forwards.Sign  == -1 ? "-" : "+", Forwards.Dimension, Up.Sign  == -1 ? "-" : "+", Up.Dimension, Zag.Sign  == -1 ? "-" : "+", Zag.Dimension);
        switch (Forwards.Dimension)
        {
            case Dimension.X:
                position.X += Forwards.Sign;
                break;
            case Dimension.Y:
                position.Y += Forwards.Sign;
                break;
            case Dimension.Z:
                position.Z += Forwards.Sign;
                break;
            case Dimension.W:
                position.W += Forwards.Sign;
                break;
        }
        Debug.LogFormat("(X, Y, Z, W) = ({0}, {1}, {2}, {3})", position.X, position.Y, position.Z, position.W);
    }
    
    public bool IsWall(Axis axis, Location position)
    {
        //Debug.LogFormat("(X, Y, Z, W) = ({0}, {1}, {2}, {3}) for axis {4} before", position.X, position.Y, position.Z, position.W, axis.Dimension);
        bool isWall = false;
        switch (axis.Dimension)
        {
            case Dimension.X:
                position.X += axis.Sign;
                isWall = position.X > 2 || position.X < 0;
                //Debug.LogFormat("(X, Y, Z, W) = ({0}, {1}, {2}, {3}) for axis {4} {5}", position.X, position.Y, position.Z, position.W, axis.Dimension, isWall);
                return isWall; 
            case Dimension.Y:
                position.Y += axis.Sign;
                isWall = position.Y > 2 || position.Y < 0;
                //Debug.LogFormat("(X, Y, Z, W) = ({0}, {1}, {2}, {3}) for axis {4} {5}", position.X, position.Y, position.Z, position.W, axis.Dimension, isWall);
                return isWall; 
            case Dimension.Z:
                position.Z += axis.Sign;
                isWall = position.Z > 2 || position.Z < 0;
                //Debug.LogFormat("(X, Y, Z, W) = ({0}, {1}, {2}, {3}) for axis {4} {5}", position.X, position.Y, position.Z, position.W, axis.Dimension, isWall);
                return isWall; 
            case Dimension.W:
                position.W += axis.Sign;
                isWall = position.W > 2 || position.W < 0;
                //Debug.LogFormat("(X, Y, Z, W) = ({0}, {1}, {2}, {3}) for axis {4} {5}", position.X, position.Y, position.Z, position.W, axis.Dimension, isWall);
                return isWall; 
        }
        return false;
    }
}
