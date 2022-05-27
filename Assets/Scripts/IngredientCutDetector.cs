using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IngredientCutDetector : MonoBehaviour
{
    //Prefabs of the objects to spawn when this ingredient is cut
    public GameObject rightHalf;
    public GameObject leftHalf;

    //Amount to offset the objects to have 2 separate close objects
    public float rightOffset;
    public float leftOffset;

    public Quaternion additionalRotation;

    //the container, if any, that this object is in
    public ContainerScript container;

    //Record if this is the object that detected a cut to instantiate the children correctly
    private bool hasBeenCutHere = false;

    //Set to true whenever one of the cut detectors of the object has detected a cut
    public bool hasBeenCut = false;

    //Used to carry information from the CutSelf method to the OnDestroy method
    private Quaternion toSpawnRotationRight = Quaternion.identity;
    private Quaternion toSpawnRotationLeft = Quaternion.identity;
    private Vector3 bladeRight = Vector3.zero;

    private bool inDispenser = false;

    //If the trigger detected is a blade that is fast enough to cut, and this ingredient is not being held, has not already been cut and is not in a container, cut this ingredient
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<IngredientChecker>() != null)
        {
            inDispenser = true;
        }

        if (other.GetComponent<BladeCutter>() != null && other.GetComponent<BladeCutter>().isFastEnoughToCut() && IsAvailable() && !hasBeenCut && container == null && !inDispenser)
        {
            hasBeenCutHere = true;
            foreach(IngredientCutDetector i in transform.parent.GetComponentsInChildren<IngredientCutDetector>())
            {
                i.hasBeenCut = true;
            }

            CutSelf(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<IngredientChecker>() != null)
        {
            inDispenser = false;
        }
    }

    //Returns whether this object is held or not by checking if the object anchors are held or not
    private bool IsAvailable()
    {
        foreach (ObjectAnchor o in transform.parent.GetComponentsInChildren<ObjectAnchor>())
        {
            if(!o.is_available())
            {
                return false;
            }
        }
        return true;
    }

    //Cut this ingredient
    private void CutSelf(GameObject cutting)
    {
        cutting.GetComponent<AudioSource>().Play();

        //What we would call the knife's right side is actually its forward vector in the prefab
        bladeRight = cutting.transform.forward;

        //Compute the rotations for the two halves to spawn
        toSpawnRotationRight = cutting.transform.rotation * additionalRotation * Quaternion.Euler(0, 180, 0);
        toSpawnRotationLeft = cutting.transform.rotation * additionalRotation;

        Destroy(transform.parent.gameObject);
    }

    private void OnDestroy()
    {
        if(hasBeenCutHere)
        {
            //Spawn the two halves with the correct position and rotation
            Instantiate(rightHalf, transform.position + rightOffset * bladeRight, toSpawnRotationRight);
            Instantiate(leftHalf, transform.position + leftOffset * -1 * bladeRight, toSpawnRotationLeft);
        }
    }
}
