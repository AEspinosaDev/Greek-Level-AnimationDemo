using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shutDown : MonoBehaviour
{
    [SerializeField] GameObject mainCam;
    // Start is called before the first frame update
   public void ShutDown()
    {
        gameObject.SetActive(false);
        //mainCam.GetComponent<Camera>().enabled = true;
        mainCam.SetActive(true);
    }
}
