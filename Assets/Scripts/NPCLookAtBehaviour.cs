using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class NPCLookAtBehaviour : MonoBehaviour
{
    private Rig m_Constraint;

    [SerializeField] private float m_MaxDistance = 5f;
    [SerializeField] private Transform m_Target;
    [SerializeField] private Transform m_Source;
    [SerializeField] private float m_TurnSmoothSpeed = 6f;


    // Start is called before the first frame update
    void Start()
    {
        m_Constraint = GetComponent<MultiAimConstraint>().GetComponentInParent<Rig>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dir = m_Source.transform.position - m_Target.position;
        float angle = Quaternion.FromToRotation(m_Source.transform.forward, dir).eulerAngles.y;

        if ((m_Source.transform.position - m_Target.position).magnitude > m_MaxDistance)
        {
            m_Constraint.weight = Mathf.Lerp(m_Constraint.weight, 0, Time.deltaTime * m_TurnSmoothSpeed);
        }
        else
        {

            if (angle < 260 && angle > 100)
            {
                m_Constraint.weight = Mathf.Lerp(m_Constraint.weight, 1, Time.deltaTime * m_TurnSmoothSpeed);
            }
            else
            {
                m_Constraint.weight = Mathf.Lerp(m_Constraint.weight, 0, Time.deltaTime * m_TurnSmoothSpeed);
            }
        }


        //Debug.Log(angle);
    }
}
