using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Catapult : MonoBehaviour
{
    private Animator m_Animator;
    [SerializeField] Transform m_Dummy;
    [SerializeField] GameObject m_Projectile;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
    }

    public void LaunchProjectile()
    {
        var projectile = Instantiate(m_Projectile, m_Dummy);
        projectile.GetComponent<Projectile>().Launch(m_Dummy.up);
    }
}

