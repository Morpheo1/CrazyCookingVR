using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HandGrabber : MonoBehaviour
{
    public HandController.HandType handType;

    public AudioClip attractionAudio;

    //store the line renderer to manipulate it and show a straight line coming from your hand
    public LineRenderer lineRenderer;

    //line parameters
    public float maxReach = 10.0f;

    //cooldown between object attractions
    public float attractCooldown = 2.0f;

    //line parameter
    private bool lineEnabled = false;

    //Used to know which object is being pointed at
    private GameObject pointedAt;

    //Used for the outline
    private GameObject lastPointedAt;
    private bool outlineCleared = false;

    private bool pressingGrab = false;

    //Used for the attraction cooldown
    private bool recentlyAttracted = false;

    private MealImages mealImagesHandler;

    // Start is called before the first frame update
    void Start()
    {
        //Setup line renderer and disable it
        Vector3[] startLinePositions = new Vector3[2] { Vector3.zero, Vector3.zero };
        lineRenderer.SetPositions(startLinePositions);
        lineRenderer.enabled = false;

        mealImagesHandler = FindObjectOfType<MealImages>();
    }

    // Update is called once per frame
    void Update()
    {


        //Get player inputs
        float triggerInput = handType == HandController.HandType.RightHand ? OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) : OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);

        pressingGrab = handType == HandController.HandType.RightHand ? OVRInput.Get(OVRInput.Button.One) : OVRInput.Get(OVRInput.Button.Three);

        //Display the line if we press the trigger
        if (triggerInput > 0.9)
        {
            lineEnabled = true;
            lineRenderer.enabled = true;
        } else {
            lineEnabled = false;
            lineRenderer.enabled = false;
        }

        //Only check for ray collisions if we display the line
        if(lineEnabled)
        {
            Raycast(transform.position, transform.forward, maxReach);
        }

        //If we stop pointing with the line, and we were previously pointing at an object, we stop outlining it
        else if(lastPointedAt != null && !outlineCleared)
        {
            lastPointedAt.GetComponentInParent<Renderer>().material = lastPointedAt.GetComponentInParent<ObjectAnchor>().baseMaterial;
            outlineCleared = true;
        }
    }

    void Raycast(Vector3 position, Vector3 forward, float maxReach)
    {
        //Cast the ray and compute maximum line end position
        RaycastHit hit;

        Ray grabbingRay = new Ray(position, forward);

        Vector3 endPos = position + (maxReach * forward);

        //Only if the ray hit an object
        int layer = 1 << LayerMask.NameToLayer("Default");
        layer += 1 << LayerMask.NameToLayer("Knife");
        layer += 1 << LayerMask.NameToLayer("Floor");
        layer += 1 << LayerMask.NameToLayer("Grab");
        if (Physics.Raycast(grabbingRay, out hit, maxReach, layer, QueryTriggerInteraction.Ignore)) 
        {
            //Get the hit position to stop the line there, and get the collided object
            endPos = hit.point;
            pointedAt = hit.collider.gameObject;

            //If we "Attract" a screen, reset the corresponding plate to its initial position
            if (pointedAt.CompareTag("Screen") && pressingGrab && !recentlyAttracted)
            {
                mealImagesHandler.ResetMealPlate(pointedAt.gameObject);

                recentlyAttracted = true;
                StartCoroutine(AttractionCooldown(attractCooldown));
            }

            //If we "Attract" the restart text, reset the corresponding plate to its initial position
            if (pointedAt.CompareTag("Restart") && pressingGrab && !recentlyAttracted)
            {
                SceneManager.LoadScene(1);
            }

            //If pointing at an attractable/grabbable object, outline it
            if (pointedAt.CompareTag("Attractable"))
            {
                if (pointedAt != lastPointedAt)
                {
                    //if we previously pointed at another grabbable object, stop outlining it (only happens when pointing one such object directly after another,
                    //otherwise the outline would have been cleared as soon as we stopped pointing the last object)
                    if (lastPointedAt != null)
                    {
                        lastPointedAt.GetComponentInParent<Renderer>().material = lastPointedAt.GetComponent<ObjectAnchor>().baseMaterial;
                    }
                    lastPointedAt = pointedAt;
                }

                //outline the object only if it is not being held by the player
                if (pointedAt.GetComponent<ObjectAnchor>().is_available())
                {
                    pointedAt.GetComponentInParent<Renderer>().material = pointedAt.GetComponent<ObjectAnchor>().outlineMaterial;
                }
                outlineCleared = false;
            }


            //If we just stopped pointing at an attractable object but we are still pointing at something, stop outlining the previous object
            else if (!outlineCleared && lastPointedAt != null)
            {
                lastPointedAt.GetComponentInParent<Renderer>().material = lastPointedAt.GetComponent<ObjectAnchor>().baseMaterial;
                outlineCleared = true;
            }

            //We want the object's center to arrive in our hand
            Vector3 objectCenter = pointedAt.transform.position;

            //If we point at a grabbable object, give it a velocity so that it gets flung towards the player, who can then grab it in the 
            if(!recentlyAttracted && pressingGrab && pointedAt.CompareTag("Attractable") && pointedAt.GetComponentInParent<Rigidbody>() != null)
            {
                //Compute vectors necessary to computation
                Vector3 fromObjectCenter = position - objectCenter;

                Vector2 xzVect = new Vector2(fromObjectCenter.x, fromObjectCenter.z);

                float halfGravity = (Physics.gravity.magnitude / 2);

                //Compute time value t used to compute necessary initial velocity vector values 
                //      for grabbed object to arrive to the player following a ballistic motion
                //We want the object to exit the ground with a 45° = pi/4rad angle
                float t = Mathf.Sqrt(Mathf.Abs(Mathf.Sin(Mathf.PI / 4.0f) * (xzVect.magnitude) + position.y - endPos.y) / halfGravity);

                Vector3 velocity = new Vector3();

                //Then compute speed vector values
                velocity.y = (position.y - endPos.y + halfGravity * Mathf.Pow(t, 2)) / t;

                velocity.z = Mathf.Sqrt(Mathf.Abs(Mathf.Pow(xzVect.magnitude / t, 2) / (1 + Mathf.Pow(fromObjectCenter.x / fromObjectCenter.z, 2))));

                velocity.x = Mathf.Abs(fromObjectCenter.x) * velocity.z / Mathf.Abs(fromObjectCenter.z);

                float additionalVelocity = 0f;

                //Make sure the velocity goes towards the player, and not away from them
                //Also possibly add a small value to avoid the object arriving just out of reach
                velocity.z = velocity.z * Mathf.Sign(fromObjectCenter.z) + additionalVelocity;
                velocity.x = velocity.x * Mathf.Sign(fromObjectCenter.x) + additionalVelocity;

                //Limit the speed at which the object can go as otherwise if object is way above the player, it goes way too fast and can go through walls
                float velocityThreshold = 15;

                if(velocity.magnitude > velocityThreshold)
                {
                    velocity = velocity.normalized * 15;
                }

                //Give the contained objects the necessariy velocity
                if (pointedAt.GetComponentInParent<Transform>().parent.CompareTag("Container"))
                {
                    pointedAt.GetComponentInParent<Transform>().parent.GetComponentInChildren<ContainerScript>().AddVelocityToContained(velocity);
                }

                //Give the object the necessary velocity to go towards the player in a nice way, and prevent grabbing it again for a bit
                pointedAt.GetComponentInParent<Rigidbody>().velocity = velocity;

                pointedAt.GetComponent<AudioSource>().clip = attractionAudio;
                pointedAt.GetComponent<AudioSource>().volume = 1f;
                pointedAt.GetComponent<AudioSource>().pitch = 2;
                pointedAt.GetComponent<AudioSource>().Play();

                pointedAt.tag = "RecentlyAttracted";

                //Allow the player to attract the object again after some time
                StartCoroutine(MakeAttractableAfterTime(attractCooldown, pointedAt));

                //Allow player to attract something again only after some time
                recentlyAttracted = true;
                StartCoroutine(AttractionCooldown(attractCooldown));

                //To make the game feel better to play, disable attracted object's hitbox to allow it to go through things, such as other ingredients in the ingredient dispenser
                StartCoroutine(pointedAt.GetComponent<ObjectAnchor>().DisableHitboxAfterAttract(0.25f));
                
                //Add some haptics
                StartCoroutine(MakeControllerVibrate(0.1f));
            }
        }
        
        //Display a line exiting from the hand and, if there is one on the way, stopping when it encounters an object
        lineRenderer.SetPosition(0, position);
        lineRenderer.SetPosition(1, endPos);
    }

    // Used to make attracted objects attractable again after some time
    IEnumerator MakeAttractableAfterTime(float time, GameObject obj)
    {
        yield return new WaitForSeconds(time);

        Debug.Log(obj.name + "Attractable again");

        obj.tag = "Attractable";
    }

    // Used to enable the player to attract a new object after a cooldown
    IEnumerator AttractionCooldown(float time)
    {
        yield return new WaitForSeconds(time);

        Debug.LogWarning("Can attract new object");

        recentlyAttracted = false;
    }

    //Handle haptics
    IEnumerator MakeControllerVibrate(float time)
    {
        if(handType == HandController.HandType.RightHand)
        {
            OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
        }
        else
        {
            OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.LTouch);
        }

        yield return new WaitForSeconds(time);

        if (handType == HandController.HandType.RightHand)
        {
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        }
        else
        {
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
        }
    }
}
