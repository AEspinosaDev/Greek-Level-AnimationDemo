using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Parser class useful for reading the TetGen mesh generated files
/// </summary>
public class Parser : MonoBehaviour
{

    private string[] m_NodesRaw;
    private string[] m_TetrasRaw;


    [Tooltip("Insert the .node file generated from TetGen here")]
    [SerializeField] public TextAsset m_NodeFile;

    [Tooltip("Insert the .ele file generated from TetGen here")]
    [SerializeField] public TextAsset m_TetraFile;


    void Awake()
    {
        m_NodesRaw = m_NodeFile.text.Split(new string[] { " ", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        m_TetrasRaw = m_TetraFile.text.Split(new string[] { " ", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Parse the .node file and return a List with the positions of each one of them
    /// </summary>
    /// <returns></returns>
    public Vector3[] ParseNodes()
    {
        int numNodes = int.Parse(m_NodesRaw[0]);
        Vector3[] nodeList = new Vector3[numNodes];

        int idx = 5;
        for (int i = 0; i < numNodes; i++)
        {
            nodeList[i] = new Vector3(float.Parse(m_NodesRaw[idx]), float.Parse(m_NodesRaw[idx + 1]), float.Parse(m_NodesRaw[idx + 2]));
            idx += 4;
        }

        return nodeList;
    }
    /// <summary>
    /// Parse the .ele file and return a List with the nodes indexes each tetra is composed of. TetGen first element is equal to 1, but this parser change its value to 0 in order 
    /// to be better to work by code.
    /// </summary>
    /// <returns></returns>
    public int[,] ParseTetras()
    {
        int numTetras = int.Parse(m_TetrasRaw[0]);
        int[,] tetraList = new int[numTetras, 4];

        int idx = 4;
        for (int i = 0; i < numTetras; i++)
        {
            tetraList[i, 0] = int.Parse(m_TetrasRaw[idx]) - 1;
            tetraList[i, 1] = int.Parse(m_TetrasRaw[idx + 1]) - 1;
            tetraList[i, 2] = int.Parse(m_TetrasRaw[idx + 2]) - 1;
            tetraList[i, 3] = int.Parse(m_TetrasRaw[idx + 3]) - 1;

            idx += 5;
        }
        print(numTetras);

        return tetraList;
    }
    /// <summary>
    /// Optimized version of the parser function. This function will set all the data structures necessary for the deforming component ready.
    /// </summary>
    /// <param name="nodesList"></param>
    /// <param name="tetrasList"></param>
    /// <param name="manager"></param>
    public void CompleteParse(List<ENode> nodesList, List<Tetrahedron> tetrasList,List<Vector3Int> trianglesList, ElasticBehaviour manager)
    {
        CultureInfo tetgenCulture = new CultureInfo("en-US");
        CultureInfo localCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = tetgenCulture;

        int numNodes = int.Parse(m_NodesRaw[0]);

        int idx = 5;
        Vector3 nodePos;
        for (int i = 0; i < numNodes; i++)
        {
            nodePos = new Vector3(float.Parse(m_NodesRaw[idx]), float.Parse(m_NodesRaw[idx + 1]), float.Parse(m_NodesRaw[idx + 2]));
            nodePos = transform.TransformPoint(nodePos);
            nodesList.Add(new ENode(i, new Vector3(nodePos.x, nodePos.y, nodePos.z), manager));

            idx += 4;
        }

        int numTetras = int.Parse(m_TetrasRaw[0]);
        idx = 4;
        for (int i = 0; i < numTetras; i++)
        {
            int idx0 = int.Parse(m_TetrasRaw[idx]) - 1;
            int idx1 = int.Parse(m_TetrasRaw[idx+1]) - 1;
            int idx2 = int.Parse(m_TetrasRaw[idx+2]) - 1;
            int idx3 = int.Parse(m_TetrasRaw[idx+3]) - 1;
            

            tetrasList.Add(new Tetrahedron(i,
                nodesList[idx0],
                nodesList[idx1],
                nodesList[idx2],
                nodesList[idx3],manager.m_MeshDensity));

            trianglesList.Add(new Vector3Int(idx0,idx1,idx2));
            trianglesList.Add(new Vector3Int(idx0,idx2,idx3));
            trianglesList.Add(new Vector3Int(idx0,idx3,idx1));
            trianglesList.Add(new Vector3Int(idx1,idx3,idx2));

            idx += 5;
        }

        System.Globalization.CultureInfo.CurrentCulture = localCulture;

    }

}
