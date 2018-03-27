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
        public int connectionA;
        public int connectionB;
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
        public int4 bendingSprings;
    };

    public static spring GetSpring(int x, int y, int rows, out int connectedParticleIndex, int rangeX, int rangeY)
    {
        spring Spring = new spring();
        Spring.connectionA = y + (x * rows);
        Spring.connectionB = (y + ((x + rangeX) * rows) + rangeY);
        connectedParticleIndex = (y + ((x + rangeX) * rows) + rangeY);
        return Spring;
    }

    public static spring GetHorizontalSpring(int x, int y, int rows, particle[] particles, out int connectedParticleIndex, int range)
    {
        spring Spring = new spring();
        Spring.connectionA = y + (x * rows);
        Spring.connectionB = y + ((x + range) * rows);
        connectedParticleIndex = y + ((x + range) * rows);
        return Spring;
    }

    public static spring GetVerticalSpring(int x, int y, int rows, particle[] particles, int springIndex, int range)
    {
        spring Spring = new spring();
        Spring.connectionA = y + (x * rows);
        Spring.connectionB = (y + (x * rows) + range);
        particles[(y + (x * rows) + range)].structuralSprings.w = springIndex;
        return Spring;
    }

    public static spring GetRightUpDiagonalSpring(int x, int y, int rows, particle[] particles, int springIndex, int range)
    {
        spring Spring = new spring();
        Spring.connectionA = y + (x * rows);
        Spring.connectionB = (y + ((x + range) * rows) + range);
        particles[(y + ((x + range) * rows) + range)].shearSprings.z = springIndex;
        return Spring;
    }

    public static spring GetDownRightDiagonalSpring(int x, int y, int rows, particle[] particles, int springIndex, int range)
    {
        spring Spring = new spring();
        Spring.connectionA = y + (x * rows);
        Spring.connectionB = (y + ((x - range) * rows) + range);
        particles[(y + ((x - range) * rows) + range)].shearSprings.w = springIndex;
        return Spring;
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
}

