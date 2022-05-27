using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportationController : MonoBehaviour
{
    //Indicates left or right hand action
    public HandController.HandType handType;

    //Used to draw the teleportation line
    public LineRenderer lineRenderer;

    //Teleportation reach parameter
    public float reachFactor = 10.0f;
    
    //Player object
    public GameObject player;

    public AudioSource audioSource;

    //Object on which the teleportation line lands
    private GameObject landingObject;

    //Line parameters
    private List<Vector3> linePoints;
    private bool hasLanded = false;
    private float g = Physics.gravity.y;
    private float timeStep = 0.01f;
    private int maxSteps = 400;

    //Cooldown parameters
    private bool recentlyTeleported = false;
    private float teleportationCooldown = 0.25f;

    private bool characterControllerDisabled;


    private bool IsPreparingTeleportation()
    {
        // Case of a left hand
        if (handType == HandController.HandType.LeftHand) return OVRInput.Get(OVRInput.RawAxis2D.LThumbstick).y > 0.5;   //Check that the player holds the joystick forward

        // Case of a right hand
        else return OVRInput.Get(OVRInput.RawAxis2D.RThumbstick).y > 0.5; //Check that the player holds the joystick forward
    }

    private bool WantsToTeleport()
    {
        // Case of a left hand
        if (handType == HandController.HandType.LeftHand) return OVRInput.Get(OVRInput.Button.PrimaryThumbstick);   //Check that the player holds the joystick forward

        // Case of a right hand
        else return OVRInput.Get(OVRInput.Button.SecondaryThumbstick); //Check that the player holds the joystick forward
    }

    // Start is called before the first frame update
    void Start()
    {
        linePoints = new List<Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        //Update teleportation line trajectory and render line if the player inputs mean a preparation to teleport, otherwise do not render any line
        if (!recentlyTeleported && IsPreparingTeleportation())
        {
            UpdateTrajectory();
            lineRenderer.enabled = true;
        }
        else
        {
            lineRenderer.enabled = false;
        }


        //Enable character controller again after teleporting
        if(characterControllerDisabled)
        {
            player.GetComponent<CharacterController>().enabled = true;
        }
    }

    private void FixedUpdate()
    {
        //Teleport the player to the new location if the player wants to and the target teleportation point is valid
        if (!recentlyTeleported && IsPreparingTeleportation() && WantsToTeleport() && hasLanded && landingObject.CompareTag("Floor"))
        {
            audioSource.Play();

            //Only change x and z coordinates, not elevation (y)
            Vector3 newPlayerPosition = player.transform.position;
            newPlayerPosition.x = linePoints[linePoints.Count - 1].x;
            newPlayerPosition.z = linePoints[linePoints.Count - 1].z;

            //Disable character controller, otherwise can't teleport through walls
            player.GetComponent<CharacterController>().enabled = false;
            characterControllerDisabled = true;

            //Teleport to desired position
            player.transform.position = newPlayerPosition;

            //Setup teleportation cooldown
            recentlyTeleported = true;
            StartCoroutine(TeleportationCooldown(teleportationCooldown));
        }
    }

    private void UpdateTrajectory()
    {
        //Initialize line parameters
        linePoints.Clear();

        Vector3 initialVelocity = reachFactor * transform.forward.normalized;

        hasLanded = false;

        float timePassed = 0f;

        //Start ballistic curve at the hand
        Vector3 previousPoint = transform.position;
        linePoints.Add(previousPoint);
        Vector3 nextPoint;

        int i = 0;

        //Draw until an object is hit or the maximum number of steps has been processed
        while (!hasLanded && i < maxSteps)
        {
            //Advance time
            timePassed += timeStep;

            //Compute next point position according to gravity to get a parabolic trajectory
            nextPoint = transform.position + new Vector3(initialVelocity.x * timePassed, initialVelocity.y * timePassed + 0.5f * g * Mathf.Pow(timePassed, 2), initialVelocity.z * timePassed);

            //Cast a ray to check for a collision with an object
            RaycastHit hit;

            Vector3 direction = nextPoint - previousPoint;

            //Interact with everything that can have a collider in the game except the player which is on it own layer
            int layer = 1 << LayerMask.NameToLayer("Default");
            layer += 1 << LayerMask.NameToLayer("Floor");
            if (Physics.Raycast(previousPoint, direction.normalized, out hit, direction.magnitude, layer, QueryTriggerInteraction.Ignore))
            {
                //If an object is hit we stop the loop, and store the object
                hasLanded = true;
                landingObject = hit.collider.gameObject;
            }

            //Add the next point at each iteration
            linePoints.Add(nextPoint);

            ++i;
        }

        //Display the parabolic line (made of many small straight lines going from point to point)
        lineRenderer.positionCount = linePoints.Count;
        lineRenderer.SetPositions(linePoints.ToArray());
    }

    // Used to enable the player to teleport again after a cooldown
    IEnumerator TeleportationCooldown(float time)
    {
        yield return new WaitForSeconds(time);

        recentlyTeleported = false;
    }
}
