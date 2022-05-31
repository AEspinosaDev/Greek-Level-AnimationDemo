using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    Rigidbody m_RBody;
    // Start is called before the first frame update
    void Start()
    {
        m_RBody= GetComponent<Rigidbody>();
    }
    
    public void Launch(Vector3 direction)
    {
        //m_RBody.AddForce(direction);
        print("Lanza");
    }

}
