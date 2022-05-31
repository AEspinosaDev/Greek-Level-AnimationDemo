using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class representing each edge interacting with nodes and aplying compression and traction forces
/// </summary>
public class ESpring
{

    public ENode m_NodeA, m_NodeB;

    public float m_Length0;
    public float m_Length;
    public float m_Volume;

    public List<Tetrahedron> m_AffectingTetras;

    ElasticBehaviour m_Manager;

    public ESpring(ENode nodeA, ENode nodeB, ElasticBehaviour manager, List<Tetrahedron> tetras)
    {
        m_NodeA = nodeA;
        m_NodeB = nodeB;
        m_Length0 = (m_NodeA.m_Pos - m_NodeB.m_Pos).magnitude;
        m_Length = m_Length0;
        m_Manager = manager;
        m_Volume = 0.0f;
        m_AffectingTetras = tetras;

        foreach (var tetra in tetras)
        {
            m_Volume += tetra.m_Volume / 6;
        }
    }

    /// <summary>
    /// Compute de direction of the force
    /// </summary>
    public void ComputeForces()
    {
        m_Volume = 0.0f;
        foreach (var tetra in m_AffectingTetras)
        {
            m_Volume += tetra.m_Volume / 6;
        }

        Vector3 u = m_NodeA.m_Pos - m_NodeB.m_Pos;
        m_Length = u.magnitude;
        u.Normalize();

        float dampForce = -m_Manager.m_SpringDamping * Vector3.Dot(u, (m_NodeA.m_Vel - m_NodeB.m_Vel));
        //float stress = -m_Manager.m_TractionStiffness * (m_Length - m_Length0) + dampForce;
        float stress = -(m_Volume / (m_Length0 * m_Length0)) * m_Manager.m_Stiffness * (m_Length - m_Length0) + dampForce;
        Vector3 force = stress * u;
        m_NodeA.m_Force += force;
        m_NodeB.m_Force -= force;

    }
}



//----------------------------------------------
//    AUXILIAR CLASSES
//----------------------------------------------



/// <summary>
/// Auxiliar class to temporary compare the edges in the mesh looking for repeated ones.
/// </summary>
public class EEdge
{
    public int m_A, m_B;
    public List<Tetrahedron> m_Tetras = new List<Tetrahedron>();

    public EEdge(int a, int b, Tetrahedron tetra)
    {
        if (a < b)
        {
            m_A = a;
            m_B = b;

        }
        else
        {
            m_B = a;
            m_A = b;
        }
        m_Tetras.Add(tetra);
    }
}
/// <summary>
/// Custom comparer class designed for the Edge class
/// </summary>
public class EEdgeQualityComparer : IEqualityComparer<EEdge>
{
    public bool Equals(EEdge a, EEdge b)
    {
        if (a.m_A == b.m_A && a.m_B == b.m_B || a.m_A == b.m_B && a.m_B == b.m_A) return true;
        return false;
    }

    public int GetHashCode(EEdge e)
    {
        List<int> pts = new List<int>(); pts.Add(e.m_A); pts.Add(e.m_B);
        pts.Sort();
        //CANTOR PAIRING FUNCTION
        int hcode = ((pts[0] + pts[1]) * (pts[0] + pts[1] + 1)) / 2 + pts[1];
        return hcode.GetHashCode();
    }
}
/// <summary>
///  Custom comparer class designed to compare repeated triangles
/// </summary>
public class TriangleQualityComparer : IEqualityComparer<Vector3Int>
{
    public bool Equals(Vector3Int x, Vector3Int y)
    {
        if (GetHashCode(x) == GetHashCode(y)) return true;
        return false;
    }

    public int GetHashCode(Vector3Int obj)
    {
        List<int> pts = new List<int>(); pts.Add(obj.x); pts.Add(obj.y); pts.Add(obj.z);
        pts.Sort();
        Vector3Int objSorted = new Vector3Int(pts[0], pts[1], pts[2]);
        return objSorted.GetHashCode();
    }
}

/// <summary>
/// Class that represents the Tetahedron, the poligons which the proxy mesh is made of.
/// </summary>
public class Tetrahedron
{
    public int id;

    public ENode m_A, m_B, m_C, m_D;

    public float m_Volume;

    public Tetrahedron(int id, ENode a, ENode b, ENode c, ENode d, float meshDensity)
    {
        this.id = id;
        m_A = a;
        m_B = b;
        m_C = c;
        m_D = d;

        m_Volume = ComputeVolume();
        ComputeNodesMass(meshDensity);

    }
    public float ComputeVolume()
    {

        Vector3 crossProduct = Vector3.Cross(m_A.m_Pos - m_D.m_Pos, m_B.m_Pos - m_D.m_Pos);
        return Mathf.Abs(Vector3.Dot(crossProduct, m_C.m_Pos - m_D.m_Pos)) / 6;
    }
    public void ComputeNodesMass(float meshDensity)
    {
        float nodeMass = meshDensity * m_Volume / 4;
        m_A.m_NodeMass = nodeMass;
        m_B.m_NodeMass = nodeMass;
        m_C.m_NodeMass = nodeMass;
        m_D.m_NodeMass = nodeMass;
    }

    public void ComputeVertexWeights(Vector3 p, out float wA, out float wB, out float wC, out float wD)
    {
        Vector3 vP_A = m_A.m_Pos - p;
        Vector3 vP_B = m_B.m_Pos - p;
        Vector3 vP_C = m_C.m_Pos - p;
        Vector3 vP_D = m_D.m_Pos - p;


        //wA
        Vector3 crossProduct = Vector3.Cross(vP_B, vP_C);
        float vA = (Mathf.Abs(Vector3.Dot(crossProduct, vP_D)) / 6);
        wA = vA / m_Volume;

        //wB
        crossProduct = Vector3.Cross(vP_A, vP_C);
        float vB = (Mathf.Abs(Vector3.Dot(crossProduct, vP_D)) / 6);
        wB = vB / m_Volume;

        //wC
        crossProduct = Vector3.Cross(vP_A, vP_B);
        float vC = (Mathf.Abs(Vector3.Dot(crossProduct, vP_D)) / 6);
        wC = vC / m_Volume;

        //wD
        crossProduct = Vector3.Cross(vP_A, vP_B);
        float vD = (Mathf.Abs(Vector3.Dot(crossProduct, vP_C)) / 6);
        wD = vD / m_Volume;

    }

    public bool PointInside(Vector3 p)
    {
        Vector3 normal1 = Vector3.Cross(m_B.m_Pos - m_A.m_Pos, m_C.m_Pos - m_A.m_Pos);
        Vector3 normal2 = Vector3.Cross(m_C.m_Pos - m_A.m_Pos, m_D.m_Pos - m_A.m_Pos);
        Vector3 normal3 = Vector3.Cross(m_D.m_Pos - m_A.m_Pos, m_B.m_Pos - m_A.m_Pos);
        Vector3 normal4 = Vector3.Cross(m_D.m_Pos - m_B.m_Pos, m_C.m_Pos - m_B.m_Pos);


        return normal1.x * (m_A.m_Pos.x - p.x) + normal1.y * (m_A.m_Pos.y - p.y) + normal1.z * (m_A.m_Pos.z - p.z) <=0.000001 &&
            normal2.x * (m_A.m_Pos.x - p.x) + normal2.y * (m_A.m_Pos.y - p.y) + normal2.z * (m_A.m_Pos.z - p.z) <= 0.000001 &&
            normal3.x * (m_A.m_Pos.x - p.x) + normal3.y * (m_A.m_Pos.y - p.y) + normal3.z * (m_A.m_Pos.z - p.z) <= 0.000001 &&
            normal4.x * (m_D.m_Pos.x - p.x) + normal4.y * (m_D.m_Pos.y - p.y) + normal4.z * (m_D.m_Pos.z - p.z) <= 0.000001;

    }

}
