using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BladeColliderVelocity : MonoBehaviour
{
    //Controls how fast the object is pushed away
    public float velocityFactor;

    //Points checking if they passed through a collider
    public List<GameObject> points;

    //Discrete velocities of the points
    private List<Vector3> velocities;

    //Previous positions of the points
    private List<Vector3> lastPositions;

    private void Start()
    {
        //Initialize attributes
        velocities = new List<Vector3>();
        lastPositions = new List<Vector3>();
        for (int i = 0; i < points.Count; ++i)
        {
            velocities.Add(Vector3.zero);
            lastPositions.Add(points[i].transform.position);
        }
    }


    //Handles the case where a movable object is going through the knife due tu imprecise collision detection
    private void Update()
    {
        int hitsCount = 0;

        for (int i = 0; i < points.Count; ++i)
        {
            //Compute new discrete velocity
            velocities[i] = points[i].transform.position - lastPositions[i];

            //Compute direction in which to check for collider
            //direction is the vector from the new position to the last position
            Vector3 direction = -velocities[i];

            //Cast a ray to check for a collision with an object
            RaycastHit hit;

            //Only check the default layer, not the player or the knife itself to avoid interacting with those
            int layer = 1 << LayerMask.NameToLayer("Default");
            layer += 1 << LayerMask.NameToLayer("Grab");

            //Cast a ray between the point's current position and its last, to check if it went through a collider
            if (Physics.Raycast(points[i].transform.position, direction.normalized, out hit, direction.magnitude, layer, QueryTriggerInteraction.Ignore))
            {

                //If the object is grabbable and available, and there were not already several other points that detected the collision
                if (hit.collider.gameObject.GetComponent<ObjectAnchor>() != null && hit.collider.gameObject.GetComponent<ObjectAnchor>().is_available() && hitsCount++ < 4)
                {
                    //Position update to move the object past the knife, where it should be
                    Vector3 addedPos = points[i].transform.position - hit.point;

                    //Additionally we need to add some velocity to the object
                    Vector3 addedVelocity = velocities[i];

                    //We do not want to change the height as this would often cause the objects to go through the surface they're on
                    addedPos.y = 0;
                    addedVelocity.y = 0;

                    //Add position, and velocity scaled by the given factor
                    hit.collider.gameObject.transform.parent.position += addedPos;
                    hit.collider.gameObject.GetComponentInParent<Rigidbody>().velocity += addedVelocity * velocityFactor;
                    
                    if(hit.collider.transform.parent.CompareTag("Container"))
                    {
                        //Since a single object can have several ObjectAnchors, a container's contained list, which stores anchors' gameobject, can have several anchors belonging to the same object
                        //So we select only unique objects
                        List<GameObject> actualObjectsInContainer = hit.collider.transform.parent.GetComponentInChildren<ContainerScript>().contained.Select(gameObject => gameObject.transform.parent.gameObject).Distinct().ToList();

                        //Push all objects within the container
                        foreach (GameObject o in actualObjectsInContainer)
                        {
                            if(!o.CompareTag("Knife"))
                            o.transform.position += addedPos;
                            o.GetComponent<Rigidbody>().velocity += addedVelocity * velocityFactor;
                        }
                    }
                }
            }

            //Update previous positions
            lastPositions[i] = points[i].transform.position;
        }
    }
}