using UnityEngine;
using MatrixXD = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using DenseMatrixXD = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix;

/// <summary>
/// Class representing each vertex physical attributes necessary
/// to compute the deformable physics
/// </summary>

public class ENode
{
    public bool m_Fixed;

    public readonly int m_Id;
    public Vector3 m_Pos;
    public Vector3 m_Vel;
    public Vector3 m_Force;
    public float m_ForceFactor;
    public float m_NodeMass;

    public Vector3 m_offset;            //Only if fixed enabled

    public GameObject m_Fixer;          //Only if fixed enabled

    public Vector3 m_WindForce;         //Only if wind enabled
    public Vector3 m_PenaltyForce;      //Only if collisions enabled

    public Vector2 m_UV;
    public ElasticBehaviour m_Manager;


    public ENode(int iD, Vector3 pos, ElasticBehaviour manager)
    {
        m_Fixed = false;
        m_Id = iD;
        m_Pos = pos;
        m_Vel = Vector3.zero;
        m_ForceFactor = 1f;
        m_Fixer = null;

        m_Manager = manager;

    }


    /// <summary>
    /// Computes the resultant force of the node without taking in count the spring contraction force
    /// </summary>
    /// 
    public void ComputeForces()
    {

        m_Force += m_NodeMass * m_Manager.m_Gravity - m_Manager.m_NodeDamping * m_NodeMass * m_Vel;

        m_Force += m_WindForce;

        m_Force *= m_ForceFactor;

        m_PenaltyForce = Vector3.zero;

    }
    /// <summary>
    /// Checks if the node is inside the collider of an object. Currently, it only supports thre types of colliders: spheric, plane and box.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>Returns the type of collision. 1 if its sphere type. -1 if its plane type. 0 if theres no collision</returns>
    public int isColliding(GameObject obj, float offset)
    {
        if (obj.GetComponent<SphereCollider>() != null)
        {
            SphereCollider collider = obj.GetComponent<SphereCollider>();

            if (collider.transform.InverseTransformPoint(m_Pos).magnitude < collider.radius + offset) return 1;

        }
        else if (obj.GetComponent<MeshCollider>() != null)
        {
            MeshCollider collider = obj.GetComponent<MeshCollider>();
            float xExtension = collider.sharedMesh.bounds.extents.x;
            float zExtension = collider.sharedMesh.bounds.extents.z;
            Vector3 nodeLocalPos = obj.transform.InverseTransformPoint(m_Pos);
            if (nodeLocalPos.y <= 0f + offset && nodeLocalPos.y >= -1.0f && Mathf.Abs(nodeLocalPos.z) <= zExtension && Mathf.Abs(nodeLocalPos.x) <= xExtension) return 2;
        }
        else
        {
            BoxCollider collider = obj.GetComponent<BoxCollider>();
            Vector3 offsetDir = (m_Pos - collider.transform.position).normalized;
            if (collider.bounds.Contains(m_Pos + (offsetDir * offset))) return 3;

        }
        return 0;

    }
    /// <summary>
    /// Computes the closest point in any kind of supported collider surface to the node position
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="condition"></param>
    /// <returns>Returns the coordinates of the impact point</returns>
    public Vector3 ComputeCollision(GameObject obj, float condition)
    {
        Vector3 impactPoint;
        switch (condition)
        {
            case 1:
                impactPoint = ComputeSphereCollision(obj); break;
            case 2:
                impactPoint = ComputePlaneCollision(obj); break;
            case 3:
                impactPoint = ComputeBoxCollision(obj); break;
            default:
                throw new System.Exception("[ERROR] Should never happen!");
        }
        return impactPoint;
    }
    /// <summary>
    /// Computes the closest point in the sphere collider surface to the node position
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>Returns the coordinates of the impact point</returns>
    private Vector3 ComputeSphereCollision(GameObject obj)
    {
        SphereCollider collider = obj.GetComponent<SphereCollider>();
        Vector3 impactPoint;
        Vector3 impactDir = m_Pos - collider.transform.position;
        impactPoint = m_Pos + Vector3.ClampMagnitude(impactDir, collider.radius * obj.transform.lossyScale.x);
        return impactPoint;

    }
    /// <summary>
    /// Computes the closest point in the plane collider surface to the node position
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>Returns the coordinates of the impact point</returns>
    private Vector3 ComputePlaneCollision(GameObject obj)
    {
        MeshCollider collider = obj.GetComponent<MeshCollider>();
        Vector3 impactPoint = m_Pos + (collider.transform.up * (-collider.transform.InverseTransformPoint(m_Pos).y + m_Manager.m_CollisionOffsetDistance));
        return impactPoint;
    }
    /// <summary>
    /// Computes the closest point in the box collider surface to the node position
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>Returns the coordinates of the impact point</returns>
    private Vector3 ComputeBoxCollision(GameObject obj)
    {
        BoxCollider collider = obj.GetComponent<BoxCollider>();
        Vector3 localNodePos = collider.transform.InverseTransformPoint(m_Pos).normalized;
        float angleX = Vector3.Angle(collider.transform.right, localNodePos);
        float angleY = Vector3.Angle(collider.transform.up, localNodePos);
        float angleZ = Vector3.Angle(collider.transform.forward, localNodePos);
        if (angleX < angleY && angleX < angleZ) return collider.transform.right;
        if (angleY < angleX && angleY < angleZ) return collider.transform.right;
        if (angleZ < angleY && angleZ < angleX) return collider.transform.right;
        throw new System.Exception("[ERROR] Should never happen!");

    }
    /// <summary>
    /// Implicitly computes the penalty force by calculating the penalty force and the penalty force differential matrix.
    /// </summary>
    /// <param name="impactPoint"></param>
    /// <param name="k"></param>
    /// <returns>Returns the differential</returns>
    public MatrixXD ComputeImplicitPenaltyForce(Vector3 impactPoint, float k)
    {

        Vector3 u = m_Pos - impactPoint;
        Vector3 oX = u;
        u.Normalize();
        MatrixXD normal = new DenseMatrixXD(3, 1);
        normal[0, 0] = u[0];
        normal[1, 0] = u[1];
        normal[2, 0] = u[2];
        MatrixXD oXProxy = new DenseMatrixXD(3, 1);
        oXProxy[0, 0] = oX[0];
        oXProxy[1, 0] = oX[1];
        oXProxy[2, 0] = oX[2];

        var normalT = normal.Transpose();

        var diff = -k * normal * normalT;

        MatrixXD penaltyForce = -k * normal * normalT * oXProxy;

        m_PenaltyForce[0] = (float)penaltyForce[0, 0];
        m_PenaltyForce[1] = (float)penaltyForce[1, 0];
        m_PenaltyForce[2] = (float)penaltyForce[2, 0];

        return diff;

    }
    /// <summary>
    /// Explicitly computes the penalty force on a point in time.
    /// </summary>
    /// <param name="impactPoint"></param>
    /// <param name="k"></param>
    public void ComputeExplicitPenaltyForce(Vector3 impactPoint, float k)
    {
        Vector3 normal = m_Pos - impactPoint;
        float deepness = normal.magnitude;

        normal.Normalize();

        m_PenaltyForce = -k * deepness * normal;

    }
}

/// <summary>
///Struct that contains vertex weights to its containing tetrahedron along its Id.
/// </summary>
public struct VertexInfo
{
    public readonly int id;
    public readonly int tetra_id;

    public readonly float w_A;
    public readonly float w_B;
    public readonly float w_C;
    public readonly float w_D;


    public VertexInfo(int id, int tetra_id, float weight1, float weight2, float weight3, float weight4)
    {
        this.id = id;
        this.tetra_id = tetra_id;
        this.w_A = weight1;
        this.w_B = weight2;
        this.w_C = weight3;
        this.w_D = weight4;

    }
   
}