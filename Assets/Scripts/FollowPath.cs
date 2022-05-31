using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPath : MonoBehaviour
{
    [SerializeField] List<Transform> m_Positions;

    [SerializeField] float m_Speed = 0.001f;
    [SerializeField] AnimationCurve m_Curve;
    [SerializeField] float m_GoalScale=2;

    [SerializeField] float m_LerpTime;
    private float m_Current, m_Target;

    int posIdx;
    float t = 0f;


    void Start()
    {
        posIdx = 0;
        transform.position = m_Positions[0].position;
    }

    // Update is called once per frame
    void Update()
    {
        ComputeMovement();
    }
    private void ComputeMovement()
    {
        //if (transform.position == m_Positions[posIdx].position)
        //{
        //    if (posIdx + 1 == m_Positions.Count) posIdx = 0; else posIdx++;
        //}
        //else
        //{
        //    //transform.position = Vector3.MoveTowards(transform.position, m_Positions[pos].position, Time.deltaTime * m_Speed);
        //    transform.position = Vector3.Lerp(transform.position, m_Positions[posIdx].position, Mathf.SmoothStep(0, 1, Time.deltaTime * m_Speed));
        //}
        //transform.position = Vector3.Lerp(transform.position, m_Positions[posIdx].position, m_LerpTime * Time.deltaTime);
        //t = Mathf.Lerp(t, 1f, m_LerpTime * Time.deltaTime);
        //if (t > .9f)
        //{
        //    t = 0f;
        //    posIdx++;
        //    posIdx = (posIdx >= m_Positions.Count) ? 0 : posIdx;
        //}
        if(transform.position==m_Positions[0].position)m_Target = m_Target == 0 ? 1 : 0;
        m_Current = Mathf.MoveTowards(m_Current, m_Target, m_Speed * Time.deltaTime);
        //Vector3.sl
        transform.position = Vector3.Lerp(m_Positions[0].position, m_Positions[1].position, m_Current);

    }
}
