using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhysicsTools
{
    public struct particle
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 color;
        public float radius;
        public float density;
        public float pressure;
        public float mass;
        public uint morton;
    }
    public struct internalNode
    {
        public int objectId;
        public int nodeId;
        public int parentId;
        public int2 intNodes;
        public int2 leaves;
        public int2 bLeaves;
        public Vector3 minPos;
        public Vector3 maxPos;
        public float sRadius;
        public int visited;
        public uint mortonId;
    };
    static class PUtility
    {
        public static Vector3[] GetVectorPoints(int count, float size)
        {
            Vector3[] points = new Vector3[count];
            Random.InitState(0);

            for (int i = 0; i < count; i++)
            {
                points[i] = new Vector3(Random.Range(-size, size), Random.Range(0, size * 2), 0);
            }
            return points;
        }

        public static particle[] GetParticlePoints(int count, float size, float radius, float mass)
        {
            particle[] points = new particle[count];
            Random.InitState(1422347532);

            for (int i = 0; i < count; i++)
            {
                points[i].position = new Vector3(Random.Range(0, size), Random.Range(0, size), Random.Range(0, size));
                points[i].direction = Vector3.zero;// new Vector3(Random.Range(-size, size), Random.Range(-size, size), Random.Range(-size, size));
                points[i].color = new Vector3(Random.Range(0, size), Random.Range(0, size), Random.Range(0, size));
                points[i].radius = radius;
                points[i].mass = mass;
                points[i].density = 0.0001f;
                points[i].pressure = 1f;

            }
            return points;
        }
    }
   
}
