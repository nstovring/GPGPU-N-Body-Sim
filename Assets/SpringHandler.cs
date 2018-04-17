using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


static class SpringHandler
{
    public struct spring
    {
        public float damping;
        public float stiffness;
        public float restLength;
        public int connectionA;
        public int connectionB;
        public int springType;
    };

    public struct particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
        public int isFixed;
        public int iD;
        public int4 structuralSprings;
        public int4 shearSprings;
        public int4 structutralBendingSprings;
        public int4 shearBendingSprings;
    };

    public enum SpringType { StructuralSpring, ShearSpring, StructuralBendingSpring, ShearBendingSpring };

    public static spring ApplyChanges(spring spring, ClothSimulator.SpringVariables vars)
    {
        spring.stiffness = vars.stiffness;
        spring.damping = vars.damping;
        return spring;
    }

    static Vector3 GetParticlePos(int x, int y, int rows)
    {
        return new Vector3(x / (float)rows, 0, y / (float)rows);
    }

    public static spring GetSpring(int x, int y, int rows, out int connectedParticleIndex, int rangeX, int rangeY, ClothSimulator.SpringVariables vars, SpringType type)
    {
        spring Spring = new spring();
        Spring.connectionA = y + (x * rows);
        Spring.connectionB = (y + ((x + rangeX) * rows) + rangeY);
        Spring.stiffness = vars.stiffness;
        Spring.damping = vars.damping;
        Spring.springType = (int)type;
        Spring.restLength = Vector3.Distance(GetParticlePos(x,y,rows), GetParticlePos(x + rangeX, y + rangeY, rows));
        connectedParticleIndex = (y + ((x + rangeX) * rows) + rangeY);
        return Spring;
    }

    public static void DrawStructuralSprings(particle[] ps, spring[] ss, int i)
    {
        if (IsValidSpring(ps[i].structuralSprings.x))
            DrawSpring(ss[ps[i].structuralSprings.x], ref ps, Color.blue);

        if (IsValidSpring(ps[i].structuralSprings.y))
            DrawSpring(ss[ps[i].structuralSprings.y], ref ps, Color.blue);

        if (IsValidSpring(ps[i].structuralSprings.z))
            DrawSpring(ss[ps[i].structuralSprings.z], ref ps, Color.blue);

        if (IsValidSpring(ps[i].structuralSprings.w))
            DrawSpring(ss[ps[i].structuralSprings.w], ref ps, Color.blue);
    }

    public static void DrawShearSprings(particle[] ps, spring[] ss, int i)
    {
        if (IsValidSpring(ps[i].shearSprings.x))
            DrawSpring(ss[ps[i].shearSprings.x], ref ps, Color.green);

        if (IsValidSpring(ps[i].shearSprings.y))
            DrawSpring(ss[ps[i].shearSprings.y], ref ps, Color.green);

        if (IsValidSpring(ps[i].shearSprings.z))
            DrawSpring(ss[ps[i].shearSprings.z], ref ps, Color.green);

        if (IsValidSpring(ps[i].shearSprings.w))
            DrawSpring(ss[ps[i].shearSprings.w], ref ps, Color.green);
    }

    public static void DrawStructuralBendingSprings(particle[] ps, spring[] ss, int i)
    {
        if (IsValidSpring(ps[i].structutralBendingSprings.x))
            DrawSpring(ss[ps[i].structutralBendingSprings.x], ref ps, Color.blue);

        if (IsValidSpring(ps[i].structutralBendingSprings.y))
            DrawSpring(ss[ps[i].structutralBendingSprings.y], ref ps, Color.blue);

        if (IsValidSpring(ps[i].structutralBendingSprings.z))
            DrawSpring(ss[ps[i].structutralBendingSprings.z], ref ps, Color.blue);

        if (IsValidSpring(ps[i].structutralBendingSprings.w))
            DrawSpring(ss[ps[i].structutralBendingSprings.w], ref ps, Color.blue);
    }

    public static void DrawShearBendingSprings(particle[] ps, spring[] ss, int i)
    {
        if (IsValidSpring(ps[i].shearBendingSprings.x))
            DrawSpring(ss[ps[i].shearBendingSprings.x], ref ps, Color.green);

        if (IsValidSpring(ps[i].shearBendingSprings.y))
            DrawSpring(ss[ps[i].shearBendingSprings.y], ref ps, Color.green);

        if (IsValidSpring(ps[i].shearBendingSprings.z))
            DrawSpring(ss[ps[i].shearBendingSprings.z], ref ps, Color.green);

        if (IsValidSpring(ps[i].shearBendingSprings.w))
            DrawSpring(ss[ps[i].shearBendingSprings.w], ref ps, Color.green);
    }

    public static void DrawVelocity(particle[] ps)
    {
        foreach (var item in ps)
        {
            Debug.DrawRay(item.position, item.velocity, Color.white);
        }
    }

    public static void DrawSpring(SpringHandler.spring ss, ref SpringHandler.particle[] ps)
    {
        Debug.DrawLine(ps[ss.connectionA].position, ps[ss.connectionB].position, Color.blue);
    }

    public static void DrawSpring(SpringHandler.spring ss, ref SpringHandler.particle[] ps, Color col)
    {
        Debug.DrawLine(ps[ss.connectionA].position, ps[ss.connectionB].position, col);
    }


    public static void GetSpringForce(ref spring[] springs, ref particle[] particles, out Vector3 dampingForce, out Vector3 springForce, particle p, int springIndex)
    {
        spring s = springs[springIndex];

        dampingForce = s.damping * (-1 * Vector3.Normalize(p.velocity)) * Vector3.Magnitude(p.velocity);

        bool N = Vector3.Dot(particles[s.connectionB].position, p.position) == Vector3.Dot(p.position, p.position);
        if (N)
            springForce = -s.stiffness * (p.position - particles[s.connectionA].position);
        else
            springForce = -s.stiffness * (p.position - particles[s.connectionB].position);
    }

    public static bool IsValidSpring(int x)
    {
        if (x != -1)
            return true;
        return false;
    }
}

