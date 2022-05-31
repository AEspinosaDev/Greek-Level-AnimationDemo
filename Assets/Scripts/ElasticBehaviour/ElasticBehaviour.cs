using UnityEngine;
using System.Collections.Generic;
using VectorXD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixXD = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using DenseVectorXD = MathNet.Numerics.LinearAlgebra.Double.DenseVector;
using DenseMatrixXD = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix;
using UnityEditor;

///<author>
///Antonio Espinosa Garcia
///2022
///

/// <summary>
/// Elastic solid physics manager. 
/// </summary>

[RequireComponent(typeof(Parser))]
public class ElasticBehaviour : MonoBehaviour
{
    #region OtherVariables

    [HideInInspector] Parser m_Parser;

    [HideInInspector] Mesh m_Mesh;

    [HideInInspector] List<Tetrahedron> m_Tetras;

    [HideInInspector] private int m_TetrasCount;
    [HideInInspector] private int m_VertexCount;

    [HideInInspector] private List<VertexInfo> m_VerticesInfo;

    [HideInInspector] private Vector3[] m_Vertices;

    [HideInInspector] private List<ENode> m_Nodes;

    [HideInInspector] private List<Vector3Int> m_SurfaceTriangles;

    [HideInInspector] private List<ESpring> m_Springs;

    [HideInInspector] private List<ENode> m_FixedNodes;

    [HideInInspector] private WindZone[] m_WindObjs;

    [HideInInspector] private Vector3 m_AverageWindVelocity;

    [HideInInspector] private float m_SubTimeStep;

    [HideInInspector] public bool m_Ready = false;

    [HideInInspector] private bool m_ProxyMeshReady = false;


    #endregion 


    #region InEditorVariables

    [Tooltip("The difference between the solvers lays on the precission they have calculating each vertex future position.")]
    [SerializeField] public Solver m_SolvingMethod;

    [Tooltip("Less time means more precission. On high res meshes is recommended to lower the timestep.")]
    [SerializeField] [Range(0.001f, 0.02f)] private float m_TimeStep;

    [Tooltip("It divides the total timestep into the number of substeps. More means more precission, but higher computational load.")]
    [SerializeField] [Range(1, 20)] private int m_Substeps;

    [Tooltip("Press P to pause/resume.")]
    [SerializeField] public bool m_Paused;

    [SerializeField] public Vector3 m_Gravity;

    [Tooltip("Controls the mass of the entire mesh, assuming it will be equally divided into each node.")]
    [SerializeField] [Range(0, 50)] public float m_MeshDensity;

    [Tooltip("Higher values means more reduction in vertex movement.")]
    [SerializeField] [Range(0f, 5f)] public float m_NodeDamping;

    [Tooltip("Higher values means more reduction in spring contraction forces.")]
    [SerializeField] [Range(0f, 5f)] public float m_SpringDamping;

    [Tooltip("Controls the stiffness of the springs. The more stiff, the less the mesh will deform and the quicker it will return to its initial state.")]
    [SerializeField] public float m_Stiffness;

    [SerializeField] public List<GameObject> m_Fixers;

    //------Managed by custom editor class-----//

    [HideInInspector] public bool m_AffectedByWind;

    [HideInInspector] public bool m_CanCollide;
    #endregion

    #region ConditionalInEditorVariables
    //------Toggled by custom editor class only if enabled-----//

    [HideInInspector] [Range(0, 1)] public float m_WindFriction;
    [HideInInspector] public WindPrecission m_WindSolverPrecission;

    [HideInInspector] public List<GameObject> m_CollidingMeshes;

    [HideInInspector] public float m_PenaltyStiffness;
    [HideInInspector] public float m_CollisionOffsetDistance;
    #endregion

    public ElasticBehaviour()
    {

        m_TimeStep = 0.004f;
        m_Substeps = 5;

        m_Gravity = new Vector3(0.0f, -9.81f, 0.0f);

        m_Paused = true;

        m_Stiffness = 20f;

        m_MeshDensity = 3.63f;

        m_NodeDamping = 0.3f;
        m_SpringDamping = 0.3f;

        m_SolvingMethod = Solver.Simplectic;

        m_FixedNodes = new List<ENode>();

        m_AverageWindVelocity = Vector3.zero;

        m_AffectedByWind = false;
        m_WindFriction = 0.5f;
        m_WindSolverPrecission = WindPrecission.High;

        m_CanCollide = false;
        m_PenaltyStiffness = 10f;
        m_CollisionOffsetDistance = 0.3f;

        m_Ready = true;
    }
    public enum Solver
    {
        Explicit = 0,
        Simplectic = 1,
        Midpoint = 2,
        SimplecticWithImplicitCollisions = 3,
    };
    public enum WindPrecission
    {
        High = 1,
        Medium = 2,
        Low = 3,
    }



    #region MonoBehaviour

    public void Start()
    {

        m_Parser = GetComponent<Parser>();

        m_Nodes = new List<ENode>();
        m_Tetras = new List<Tetrahedron>();
        m_VerticesInfo = new List<VertexInfo>();
        m_SurfaceTriangles = new List<Vector3Int>();
        List<Vector3Int> triangleList = new List<Vector3Int>();

        m_Parser.CompleteParse(m_Nodes, m_Tetras, triangleList, this);

        m_TetrasCount = m_Tetras.Count;

        m_Mesh = GetComponent<MeshFilter>().mesh;

        m_VertexCount = m_Mesh.vertexCount;
        m_Vertices = m_Mesh.vertices;

        CheckContainingTetraPerVertex();

        GenerateSprings();

        m_ProxyMeshReady = true;

        FindSurfaceTriangles(triangleList);

        m_SubTimeStep = m_TimeStep / m_Substeps;

        CheckFixers();

        CheckWindObjects();
    }
    public void OnDrawGizmos()
    {
        if (m_ProxyMeshReady)
        {
            float factor = m_Mesh.bounds.size.magnitude*0.5f;
            foreach (var n in m_Nodes)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(n.m_Pos, 0.05f);
            }
            foreach (var s in m_Springs)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(s.m_NodeA.m_Pos, s.m_NodeB.m_Pos);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (m_ProxyMeshReady)
        {
            for (int i = 0; i < m_SurfaceTriangles.Count; i++)
            {
                ENode nodeA = m_Nodes[m_SurfaceTriangles[i].x];
                ENode nodeB = m_Nodes[m_SurfaceTriangles[i].y];
                ENode nodeC = m_Nodes[m_SurfaceTriangles[i].z];

                //Gizmos.color = new Color(nodeA.m_WindForce.magnitude * 5, 0.0f, 0.0f, 1.0f);
                //Gizmos.DrawSphere(nodeA.m_Pos, 0.2f);
                //Gizmos.color = new Color(nodeB.m_WindForce.magnitude * 5, 0.0f, 0.0f, 1.0f);
                //Gizmos.DrawSphere(nodeB.m_Pos, 0.2f);
                //Gizmos.color = new Color(nodeC.m_WindForce.magnitude * 5, 0.0f, 0.0f, 1.0f);
                //Gizmos.DrawSphere(nodeC.m_Pos, 0.2f);

                Gizmos.color = Color.blue;
                Vector3 crossProduct = -Vector3.Cross(nodeB.m_Pos - nodeA.m_Pos, nodeC.m_Pos - nodeA.m_Pos);
                crossProduct = Vector3.ClampMagnitude(crossProduct, 0.5f);
                Gizmos.DrawLine(nodeA.m_Pos, nodeA.m_Pos + crossProduct);
                Gizmos.DrawLine(nodeB.m_Pos, nodeB.m_Pos + crossProduct);
                Gizmos.DrawLine(nodeC.m_Pos, nodeC.m_Pos + crossProduct);
            }
        }
    }


    public void Update()
    {
        //if (Input.GetKeyUp(KeyCode.P))
        //    this.m_Paused = !this.m_Paused;

        m_SubTimeStep = m_TimeStep / m_Substeps;

        CheckWindObjects();
        if (m_AffectedByWind && m_WindSolverPrecission == WindPrecission.Low)
        {
            ComputeWindForces();
        }

        foreach (var node in m_FixedNodes)
        {

            node.m_Pos = node.m_Fixer.transform.TransformPoint(node.m_offset);

        }

        for (int i = 0; i < m_VertexCount; i++)
        {

            Vector3 newPos = m_VerticesInfo[i].w_A * m_Tetras[m_VerticesInfo[i].tetra_id].m_A.m_Pos +
                m_VerticesInfo[i].w_B * m_Tetras[m_VerticesInfo[i].tetra_id].m_B.m_Pos +
                m_VerticesInfo[i].w_C * m_Tetras[m_VerticesInfo[i].tetra_id].m_C.m_Pos +
                m_VerticesInfo[i].w_D * m_Tetras[m_VerticesInfo[i].tetra_id].m_D.m_Pos;

            newPos = transform.InverseTransformPoint(newPos);

            m_Vertices[i] = newPos;

        }
        m_Mesh.vertices = m_Vertices;

        m_Mesh.RecalculateNormals();
        //m_Mesh.RecalculateTangents();


    }

    public void FixedUpdate()
    {
        if (m_Paused)
            return; // Not simulating

        if (m_AffectedByWind && m_WindSolverPrecission == WindPrecission.Medium)
        {
            ComputeWindForces();
        }

        // Select integration method
        for (int i = 0; i < m_Substeps; i++)
        {
            if (m_AffectedByWind && m_WindSolverPrecission == WindPrecission.High)
            {
                ComputeWindForces();
            }

            foreach (var tetra in m_Tetras)
            {
                tetra.ComputeVolume();
                tetra.ComputeNodesMass(m_MeshDensity);
            }


            switch (m_SolvingMethod)
            {

                case Solver.Explicit: StepExplicit(); break;

                case Solver.Simplectic: StepSimplectic(); break;

                case Solver.Midpoint: StepRK2(); break;

                case Solver.SimplecticWithImplicitCollisions: StepSimplecticWithImplicitCollision(); break;

                default:
                    throw new System.Exception("[ERROR] Should never happen!");

            }
        }

    }

    #endregion

    #region PhysicsSolvers
    /// <summary>
    /// Worst solver. Good for simple assets or arcade physics.
    /// </summary>
    private void StepExplicit()
    {
        foreach (var n in m_Nodes)
        {
            n.m_Force = Vector3.zero;
            if (!m_AffectedByWind) n.m_WindForce = Vector3.zero;
            n.ComputeForces();
        }

        foreach (var s in m_Springs)
        {
            s.ComputeForces();
        }

        foreach (var n in m_Nodes)
        {
            if (!n.m_Fixed)
            {
                if (m_CanCollide)
                {
                    foreach (var obj in m_CollidingMeshes)
                    {
                        int condition = n.isColliding(obj, m_CollisionOffsetDistance);
                        if (condition > 0)
                        {
                            n.ComputeExplicitPenaltyForce(n.ComputeCollision(obj, condition), m_PenaltyStiffness);
                            n.m_Force += n.m_PenaltyForce;
                        }
                    }
                }
                n.m_Pos += m_SubTimeStep * n.m_Vel;
                n.m_Vel += m_SubTimeStep / n.m_NodeMass * n.m_Force;
            }
        }

    }
    /// <summary>
    /// Better solver. Either not perfect. Recommended.
    /// </summary>
    private void StepSimplectic()
    {

        foreach (var n in m_Nodes)
        {
            n.m_Force = Vector3.zero;
            if (!m_AffectedByWind) n.m_WindForce = Vector3.zero;
            n.ComputeForces();
        }

        foreach (var s in m_Springs)
        {
            s.ComputeForces();
        }

        foreach (var n in m_Nodes)
        {
            if (!n.m_Fixed)
            {
                Vector3 resVel = n.m_Vel + m_SubTimeStep / n.m_NodeMass * n.m_Force;
                if (m_CanCollide)
                {
                    foreach (var obj in m_CollidingMeshes)
                    {
                        int condition = n.isColliding(obj, m_CollisionOffsetDistance);
                        if (condition > 0)
                        {
                            n.ComputeExplicitPenaltyForce(n.ComputeCollision(obj, condition), m_PenaltyStiffness);
                            n.m_Force += n.m_PenaltyForce;
                            resVel = n.m_Vel + m_SubTimeStep / n.m_NodeMass * n.m_Force;
                        }
                    }
                }
                n.m_Vel = resVel;
                n.m_Pos += m_SubTimeStep * n.m_Vel;
            }

        }
    }

    /// <summary>
    /// Fairly good solver. Also known as midpoint.
    /// </summary>
    private void StepRK2()
    {
        Vector3[] m_Vel0 = new Vector3[m_Nodes.Count];
        Vector3[] m_Pos0 = new Vector3[m_Nodes.Count];

        //Midpoint
        for (int i = 0; i < m_Nodes.Count; i++)
        {

            m_Vel0[i] = m_Nodes[i].m_Vel;
            m_Pos0[i] = m_Nodes[i].m_Pos;


            m_Nodes[i].m_Force = Vector3.zero;

            if (!m_AffectedByWind) m_Nodes[i].m_WindForce = Vector3.zero;
            m_Nodes[i].ComputeForces();
        }

        foreach (var s in m_Springs)
        {
            s.ComputeForces();
        }

        foreach (var n in m_Nodes)
        {
            if (!n.m_Fixed)
            {
                Vector3 resVel = n.m_Vel + (m_SubTimeStep * 0.5f) / n.m_NodeMass * n.m_Force;
                if (m_CanCollide)
                {
                    foreach (var obj in m_CollidingMeshes)
                    {
                        int condition = n.isColliding(obj, m_CollisionOffsetDistance);
                        if (condition > 0)
                        {
                            n.ComputeExplicitPenaltyForce(n.ComputeCollision(obj, condition), m_PenaltyStiffness);
                            n.m_Force += n.m_PenaltyForce;
                            resVel = n.m_Vel + (m_SubTimeStep * 0.5f) / n.m_NodeMass * n.m_Force;
                        }
                    }
                }
                n.m_Vel += resVel;
                n.m_Pos += (m_SubTimeStep * 0.5f) * n.m_Vel;
            }
        }

        //EndPoint
        foreach (var n in m_Nodes)
        {
            n.m_Force = Vector3.zero;

            n.ComputeForces();
        }

        foreach (var s in m_Springs)
        {
            s.ComputeForces();
        }

        for (int i = 0; i < m_Nodes.Count; i++)
        {

            if (!m_Nodes[i].m_Fixed)
            {
                Vector3 resVel = m_Vel0[i] + m_SubTimeStep / m_Nodes[i].m_NodeMass * m_Nodes[i].m_Force;
                if (m_CanCollide)
                {
                    foreach (var obj in m_CollidingMeshes)
                    {
                        int condition = m_Nodes[i].isColliding(obj, m_CollisionOffsetDistance);
                        if (condition > 0)
                        {
                            m_Nodes[i].ComputeExplicitPenaltyForce(m_Nodes[i].ComputeCollision(obj, condition), m_PenaltyStiffness);
                            m_Nodes[i].m_Force += m_Nodes[i].m_PenaltyForce;
                            resVel = m_Vel0[i] + m_SubTimeStep / m_Nodes[i].m_NodeMass * m_Nodes[i].m_Force;
                        }
                    }
                }
                m_Nodes[i].m_Vel = resVel;
                m_Nodes[i].m_Pos = m_Pos0[i] + m_SubTimeStep * m_Nodes[i].m_Vel;
            }
        }


    }
    /// <summary>
    /// Simplectic solver using implicit aproximation for collisions. The best solver.
    /// </summary>
    private void StepSimplecticWithImplicitCollision()
    {
        foreach (var n in m_Nodes)
        {
            n.m_Force = Vector3.zero;
            if (!m_AffectedByWind) n.m_WindForce = Vector3.zero;
            n.ComputeForces();
        }

        foreach (var s in m_Springs)
        {
            s.ComputeForces();
        }

        Vector3 resVel;
        foreach (var n in m_Nodes)
        {
            if (!n.m_Fixed)
            {
                resVel = n.m_Vel + m_SubTimeStep / n.m_NodeMass * n.m_Force;
                if (m_CanCollide)
                {
                    foreach (var obj in m_CollidingMeshes)
                    {
                        int condition = n.isColliding(obj, m_CollisionOffsetDistance);
                        if (condition > 0)
                        {
                            MatrixXD diff = n.ComputeImplicitPenaltyForce(n.ComputeCollision(obj, condition), m_PenaltyStiffness);

                            MatrixXD i = new DenseMatrixXD(3);
                            i = DenseMatrixXD.CreateIdentity(3);

                            Vector3 b = n.m_Vel + m_SubTimeStep / n.m_NodeMass * (n.m_PenaltyForce + n.m_Force); //Spring and wind force already computed in n.m_Force

                            VectorXD bProxy = new DenseVectorXD(3);
                            bProxy[0] = b.x; bProxy[1] = b.y; bProxy[2] = b.z;

                            var x = (i - (m_SubTimeStep * m_SubTimeStep / n.m_NodeMass) * diff).Solve(bProxy);

                            resVel = new Vector3((float)x[0], (float)x[1], (float)x[2]);

                        }
                    }

                }

                n.m_Vel = resVel;
                n.m_Pos += m_SubTimeStep * n.m_Vel;
            }

        }
    }


    #endregion
    /// <summary>
    /// Called on start. Iterates through all mesh vertices and checks what is the tetrahedron in which each vertex is contained, assigning the weights necesary to compute the position
    /// in the physic solving.
    /// </summary>
    private void CheckContainingTetraPerVertex()
    {
        //Check if node is inside
        for (int i = 0; i < m_VertexCount; i++)
        {
            Vector3 globalPos = transform.TransformPoint(m_Mesh.vertices[i]);
            foreach (var tetra in m_Tetras)
            {
                if (tetra.PointInside(globalPos))
                {
                    tetra.ComputeVertexWeights(globalPos, out float wA, out float wB, out float wC, out float wD);
                    m_VerticesInfo.Add(new VertexInfo(i, tetra.id, wA, wB, wC, wD));
                    break;
                }

            }

        }

    }
    /// <summary>
    /// Called on start. Creates all springs needed for the proxy mesh, having first taken care of all repeated ones.
    /// </summary>
    private void GenerateSprings()
    {
        m_Springs = new List<ESpring>();

        EEdgeQualityComparer edgeComparer = new EEdgeQualityComparer();

        Dictionary<EEdge, EEdge> edgeDictionary = new Dictionary<EEdge, EEdge>(edgeComparer);

        EEdge repeatedEdge;
        for (int i = 0; i < m_TetrasCount; i++)
        {
            List<EEdge> edges = new List<EEdge>();
            edges.Add(new EEdge(m_Tetras[i].m_A.m_Id, m_Tetras[i].m_B.m_Id, m_Tetras[i]));
            edges.Add(new EEdge(m_Tetras[i].m_B.m_Id, m_Tetras[i].m_C.m_Id, m_Tetras[i]));
            edges.Add(new EEdge(m_Tetras[i].m_C.m_Id, m_Tetras[i].m_A.m_Id, m_Tetras[i]));
            edges.Add(new EEdge(m_Tetras[i].m_D.m_Id, m_Tetras[i].m_A.m_Id, m_Tetras[i]));
            edges.Add(new EEdge(m_Tetras[i].m_D.m_Id, m_Tetras[i].m_B.m_Id, m_Tetras[i]));
            edges.Add(new EEdge(m_Tetras[i].m_D.m_Id, m_Tetras[i].m_C.m_Id, m_Tetras[i]));

            foreach (var edge in edges)
            {
                if (!edgeDictionary.TryGetValue(edge, out repeatedEdge))
                {
                    edgeDictionary.Add(edge, edge);
                }
                else
                {
                    repeatedEdge.m_Tetras.Add(m_Tetras[i]);
                }


            }
        }
        foreach (var edge in edgeDictionary)
        {
            m_Springs.Add(new ESpring(m_Nodes[edge.Value.m_A], m_Nodes[edge.Value.m_B], this, edge.Value.m_Tetras));
        }

    }
    /// <summary>
    /// Called on start. Iterates through the initial triangle list to find the ones that are in the surface of the proxy mesh, and stores them in a dedicated data structure
    /// in order to use it for computing the wind forces.
    /// </summary>
    /// <param name="triangleList">The total triangle list generated from the parser. Each triangle is represented by a vector3int, which components store the index of the each node the triangle is formed of.</param>
    private void FindSurfaceTriangles(List<Vector3Int> triangleList)
    {

        TriangleQualityComparer trisComparer = new TriangleQualityComparer();

        Dictionary<Vector3Int, Vector3Int> trisDictionary = new Dictionary<Vector3Int, Vector3Int>(trisComparer);

        Vector3Int repeatedTris;
        for (int i = 0; i < triangleList.Count; i++)
        {
            Vector3Int tris = triangleList[i];
            if (!trisDictionary.TryGetValue(tris, out repeatedTris))
            {
                trisDictionary.Add(tris, tris);
            }
            else
            {
                trisDictionary.Remove(tris);
            }

        }
        foreach (var tris in trisDictionary)
        {
            m_SurfaceTriangles.Add(tris.Value);
        }
        //print(m_SurfaceTriangles.Count);
    }
    /// <summary>
    /// Checks whether the vertex is inside the fixer colliders in order to put it in a fixed state.
    /// </summary>
    private void CheckFixers()
    {
        foreach (var node in m_Nodes)
        {
            foreach (var obj in m_Fixers)
            {
                Collider collider = obj.GetComponent<Collider>();
                Vector3 n_pos = node.m_Pos;

                if (collider.bounds.Contains(n_pos))
                {
                    node.m_Fixed = true;
                    if (node.m_Fixer != null) Debug.LogWarning("[Warning] More than one fixer assinged to the vertex. It will only be accepted one");
                    node.m_Fixer = obj;
                    node.m_offset = node.m_Fixer.transform.InverseTransformPoint(node.m_Pos);
                    m_FixedNodes.Add(node);

                }
            }
        }
    }

    /// <summary>
    /// Automatically called on start. Checks for wind objects in order to take them into account to make the wind simulation.
    /// </summary>
    private void CheckWindObjects()
    {
        if (m_AffectedByWind) m_WindObjs = FindObjectsOfType<WindZone>();
        ComputeWindSpeed();
    }
    /// <summary>
    /// Called every update. Computes the average velocity vector of the resulting wind.
    /// </summary>
    private void ComputeWindSpeed()
    {
        m_AverageWindVelocity = Vector3.zero;

        if (m_AffectedByWind)
        {
            int total = m_WindObjs.Length;
            foreach (var obj in m_WindObjs)
            {
                if (obj.gameObject.activeSelf)
                {
                    //Takes in account all wind object parameters to simulate wind variation
                    Vector3 windVel = obj.transform.forward * (obj.windMain + (obj.windPulseMagnitude * obj.windMain * Mathf.Abs(Mathf.Sin(Time.time * (obj.windPulseFrequency * 10)))));
                    m_AverageWindVelocity += windVel;
                }

            }
            if (m_AverageWindVelocity.magnitude > 0)
                m_AverageWindVelocity = new Vector3(m_AverageWindVelocity.x / total, m_AverageWindVelocity.y / total, m_AverageWindVelocity.z / total);
        }
    }
    /// <summary>
    /// Called every wind solving iteration. Computes the applied wind force of every triangle in the mesh. 
    /// </summary>
    private void ComputeWindForces()
    {
        for (int i = 0; i < m_SurfaceTriangles.Count; i++)
        {

            ENode nodeA = m_Nodes[m_SurfaceTriangles[i].x];
            ENode nodeB = m_Nodes[m_SurfaceTriangles[i].y];
            ENode nodeC = m_Nodes[m_SurfaceTriangles[i].z];

            Vector3 crossProduct = -Vector3.Cross(nodeB.m_Pos - nodeA.m_Pos, nodeC.m_Pos - nodeA.m_Pos);

            float trisArea = crossProduct.magnitude / 2;

            Vector3 trisNormal = crossProduct.normalized;

            Vector3 trisSpeed = new Vector3((nodeA.m_Vel.x + nodeB.m_Vel.x + nodeC.m_Vel.x) / 3, (nodeA.m_Vel.y + nodeB.m_Vel.y + nodeC.m_Vel.y) / 3, (nodeA.m_Vel.z + nodeB.m_Vel.z + nodeC.m_Vel.z) / 3);

            Vector3 trisWindForce = m_WindFriction * trisArea * Vector3.Dot(trisNormal, m_AverageWindVelocity - trisSpeed) * trisNormal;
            Vector3 dispersedForce = trisWindForce / 3;

            nodeA.m_WindForce = dispersedForce;
            nodeB.m_WindForce = dispersedForce;
            nodeC.m_WindForce = dispersedForce;
        }
    }
}
