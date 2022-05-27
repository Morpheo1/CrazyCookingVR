using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentlyAttached : MonoBehaviour
{
    //Stores all the objects currently attached to either hand, in case we need to get such a list at some point
    [System.NonSerialized]
    public List<ObjectAnchor> currentlyAttached;

    private void Start()
    {
        currentlyAttached = new List<ObjectAnchor>();
    }
}
