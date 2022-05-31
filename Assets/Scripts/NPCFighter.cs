using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCFighter : MonoBehaviour
{
    [SerializeField] public AudioSource m_SFX;
    [SerializeField] public bool m_IsTraining;


    // Start is called before the first frame update
    void Start()
    {
        if (m_IsTraining)
            GetComponent<Animator>().SetBool("isTraining", true);

    }
    public void playSound()
    {
        m_SFX.Play();
    }

   
}
