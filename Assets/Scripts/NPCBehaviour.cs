using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCBehaviour : MonoBehaviour
{
    Animator m_Animator;
    [SerializeField] bool IsWalking = false;
    [SerializeField] List<Transform> m_Positions;

    [SerializeField] float m_Speed = 5f;
    [SerializeField] float m_TurnSpeed = 1f;

    int posIdx;
    
    void Start()
    {
        m_Animator = GetComponent<Animator>();
        if (IsWalking)
        {
            m_Animator.SetBool("walking", true);
            m_Animator.SetFloat("Blend", Random.Range(0.5f,0.75f));
            posIdx = 0;
            transform.position = m_Positions[0].position;
            transform.forward = -(transform.position - m_Positions[0].position).normalized;

            GetComponent<AudioSource>().pitch=0.87F;
            GetComponent<AudioSource>().Play();
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (IsWalking)
            ComputeMovement();
    }
    void ComputeMovement()
    {

        if (transform.position == m_Positions[posIdx].position)
        {
            if (posIdx + 1 == m_Positions.Count) posIdx = 0; else posIdx++;
            //turning = true;

            //transform.forward = newDir;

        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, m_Positions[posIdx].position, Time.deltaTime * m_Speed);
        }
        Vector3 newDir = -(transform.position - m_Positions[posIdx].position).normalized;
        transform.forward = Vector3.Lerp(transform.forward, newDir, Time.deltaTime * m_TurnSpeed);
    }

}
