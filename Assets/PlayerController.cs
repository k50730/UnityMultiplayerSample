using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public NetworkClient client;
    Vector3 positionVector3;
    Vector3 rotationVector3;


    // Start is called before the first frame update
    void Start()
    {

        InvokeRepeating("SendPos", 1, 0.033f);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            positionVector3 += transform.TransformVector(Vector3.forward) * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            positionVector3 -= transform.TransformVector(Vector3.forward) * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rotationVector3 += new Vector3(0, 1, 0) * Time.deltaTime * 90f;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            rotationVector3 -= new Vector3(0, 1, 0) * Time.deltaTime * 90f;
        }

    }

    void SendPos()
    {
        client.SendingPosition(positionVector3, rotationVector3);
    }
}
