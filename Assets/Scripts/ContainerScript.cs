using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContainerScript : MonoBehaviour
{
    //Store all contained anchors
    public List<GameObject> contained;

    //Store all hand controllers in the scene
    [System.NonSerialized]
    public HandController[] handControllers;

    //Store controller this is currently attached to
    private HandController handController;
    private OVRInput.Controller controller;

    //Store anchors waiting to be attached to avoid starting more than one coroutine handling this
    private List<GameObject> waitingToBeAttached;

    private void Start()
    {
        contained = new List<GameObject>();

        waitingToBeAttached = new List<GameObject>();

        //Get hand controllers in the scene
        handControllers = FindObjectsOfType<HandController>();
    }

    private void FixedUpdate()
    {
        //If this container is held by the player, check that each contained object is already attached to the player's handcontroller as well, otherwise attach it after a short time
        foreach(GameObject anchor in contained)
        {
            if(!transform.parent.GetComponentInChildren<ObjectAnchor>().is_available() && anchor.GetComponent<ObjectAnchor>().is_available())
            {
                StartCoroutine(AttachAfterTime(0.5f, anchor));
            }
        }
    }

    //Add the object that entered the container to the list of contained objects
    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<ObjectAnchor>() != null)
        {
            Debug.Log("Object " + other.gameObject.name + " entered container");

            contained.Add(other.gameObject);
        }

        //Inform cut detectors that they are contained and thus should not allow a cut to happen
        foreach(IngredientCutDetector i in other.transform.parent.GetComponentsInChildren<IngredientCutDetector>())
        {
            i.container = this;
        }
    }

    //Remove the object that entered the container from the list of contained objects
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<ObjectAnchor>() != null)
        {
            Debug.Log("Object" + other.gameObject.name + "exited container");
            contained.Remove(other.gameObject);
        }

        //Inform cut detectors that they are no longer contained and thus should allow a cut to happen
        foreach (IngredientCutDetector i in other.transform.parent.GetComponentsInChildren<IngredientCutDetector>())
        {
            i.container = null;
        }
    }

    //Add velocity to all contained objects, used for throwing the container
    public void AddVelocityToContained(Vector3 velocity)
    {
        foreach (GameObject c in contained)
        {
            c.GetComponentInParent<Rigidbody>().velocity = velocity;
        }
    }

    //Attach all contained objects to the controller
    public void AttachContained(HandController handController, OVRInput.Controller controller, Vector3 displacement)
    {
        //Store this information to make sure that AttachAfterTime doesn't trigger an attach if the container is no longer grasped
        this.handController = handController;
        this.controller = controller;

        foreach (GameObject c in contained)
        {
            if(c.transform.parent.tag != "Container")
            {
                c.GetComponent<ObjectAnchor>().attach_to(handController, controller, displacement);
            }
        }
    }

    //Detach all contained objects from the controller
    public void DetachContained(HandController handController)
    {
        this.handController = null;

        foreach (GameObject c in contained)
        {
            if (c.transform.parent.tag != "Container")
            {
                c.GetComponent<ObjectAnchor>().detach_from(handController);
            }
        }
    }

    public void DestroyAllContained()
    {
        foreach(GameObject g in contained)
        {
            Destroy(g.transform.parent.gameObject);
        }

        //Update anchors to remove ingredients from anchor list
        foreach (HandController h in handControllers)
        {
            h.UpdateAnchors();
        }
    }

    //Attach the object to the handController holding this container after some time while checking that the container is still attached to it
    IEnumerator AttachAfterTime(float time, GameObject o)
    {
        waitingToBeAttached.Add(o);

        yield return new WaitForSeconds(time);
        
        if(handController != null)
        {
            AttachContained(handController, controller, Vector3.zero);
            waitingToBeAttached.Remove(o);
        }
    }
}
