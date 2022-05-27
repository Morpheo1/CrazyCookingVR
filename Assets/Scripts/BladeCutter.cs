using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeCutter : MonoBehaviour
{
    public float requiredVelocity;

    private float currentVelocity;

    private Vector3 lastPos;

    // Start is called before the first frame update
    void Start()
    {
        currentVelocity = 0;
        lastPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        //Update the velocity to an estimate based on position
        currentVelocity = (lastPos - transform.position).magnitude;
        lastPos = transform.position;
    }

    //Only returns true if the blade has sufficient velocity to cut, for now the requiredVelocity is set to 0 as this is what felt the best
    public bool isFastEnoughToCut()
    {
        return currentVelocity > requiredVelocity;
    }
}
