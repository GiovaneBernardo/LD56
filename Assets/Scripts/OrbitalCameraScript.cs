
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plaza;
using static Plaza.InternalCalls;
using static Plaza.Input;
using static Plaza.Physics;

public class OrbitalCameraScript : Entity
{
    public List<Matrix4> Points = new List<Matrix4>();
    public int Index = 31510;
    public void OnStart()
    {
        Points.Add(new Matrix4());
    }
    public void OnUpdate()
    {
        Console.WriteLine(Index);
        if (Points.Count == 0)
            return;

        if (Index > Points.Count)
            Index = 0;

        this.GetComponent<Transform>().WorldMatrix = Points[Index];
        Index++;
    }
}