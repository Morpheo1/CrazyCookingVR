using System.Collections;
using UnityEngine;
using System.Linq;

public class ObjectAnchor : MonoBehaviour
{

	[Header("Grasping Properties")]
	public float graspingRadius = 0.5f;

	// Store initial transform parent
	protected Transform initial_transform_parent;

	//We store a normal material, and one to use when the player points at this object
	public Material baseMaterial;
	public Material outlineMaterial;

	//Control how fast the player can throw the object
	public float speedFactor = 1.5f;

	public float distanceFromHand = 0.1f;

	//Store the velocity in the last few frames to avoid problems when the player wants to throw the object but releases it too late which would normally not throw it
	private Vector3[] velocityFrames = new Vector3[5];
	private int frameStep = 0;

	// Store the hand controller this object will be attached to
	private HandController hand_controller = null;

	//Store the kind of controller that is currently grabbing the object (left or right)
	private OVRInput.Controller controller;

	//Used to inform the hand controllers that there is a new grabbable object
	private HandController[] handControllers;

	//Objects can more than one anchor, but when grabbed only one of those will be considered to be the main one
	public bool isMainAnchor;

	void Start()
	{
		//Initialize transform parent
		initial_transform_parent = transform.parent.parent;
		
		//Initialize with 0 velocities
		System.Array.Fill(velocityFrames, Vector3.zero, 0, velocityFrames.Length);

		//Get hand controllers in the scene
		handControllers = FindObjectsOfType<HandController>();

		//Update anchors to inform self is a new instance of anchor
		foreach (HandController h in handControllers)
		{
			h.UpdateAnchors();
		}
	}

    private void FixedUpdate()
    {
		//Keep track of velocity when grasped
		if (!is_available())
        {
			VelocityUpdate();
		}
    }

	private void VelocityUpdate()
    {
		//go to the next index to store the next velocity, and loop if the next index is outside the velocity arrays' bounds
		frameStep = (frameStep + 1) % velocityFrames.Length;

		Vector3 controllerVelocityCross;

		//Account for angular velocity with respect to the controller when computing total velocity

		/*For some reason the local controller angular velocity is indeed local when running from pc, but when building on quest it is not only not local, 
		hence the inverse local controller rotation, but it's also the negative of what it should be ???*/
		if(Application.platform == RuntimePlatform.Android)
        {
			//On quest, makes no sense
			controllerVelocityCross = Vector3.Cross(hand_controller.transform.rotation * (Quaternion.Inverse(OVRInput.GetLocalControllerRotation(controller)) * -OVRInput.GetLocalControllerAngularVelocity(controller)), transform.position - hand_controller.transform.position);
		}
		else
        {
			//On PC, makes sense
			controllerVelocityCross = Vector3.Cross(hand_controller.transform.rotation * OVRInput.GetLocalControllerAngularVelocity(controller), transform.position - hand_controller.transform.position);
		}

		//Object velocity is the sum of the linear controller velocity and its angular velocity with respect to the controller, scaled by the speed factor
		velocityFrames[frameStep] = speedFactor * (OVRInput.GetLocalControllerVelocity(controller) + controllerVelocityCross);
	}

	private void actuallyAttachTo(HandController hand_controller, OVRInput.Controller controller, bool displace)
	{
		if (is_available())
        {
			//Go through all anchors this anchor's object has, and check that none of them is already considered to be the main anchor
			//If we did not encounter any main anchor, then this anchor becomes the main anchor
			isMainAnchor = !transform.parent.GetComponentsInChildren<ObjectAnchor>().Aggregate(false, (isMain, next) => isMain || next.isMainAnchor);

			//store the kind of controller (left or right)
			this.controller = controller;

			// Store the hand controller in memory
			this.hand_controller = hand_controller;

			// Set the object to be placed in the hand controller referential
			GetComponentInParent<Rigidbody>().isKinematic = true;

			transform.parent.SetParent(hand_controller.transform);

			Vector3 displacement = Vector3.zero;

			if (transform.parent.CompareTag("Knife"))
			{
				//Displace and rotate the knife so that the handle is in our hand and the knife is held in a natural way
				transform.parent.rotation = hand_controller.transform.rotation;
				Vector3 additionalRotation = new Vector3(0, 90, 0);
				transform.parent.Rotate(additionalRotation);

				//The handle is the 1st child of the knife object so we use its transform's position as a target point to put in the player's hand
				displacement = hand_controller.transform.position - transform.parent.GetChild(0).transform.position;
				transform.parent.position += displacement;
			}

			//Only one of any object's anchors should displace it, so displace will always only be true for one of them
			else if (displace)
            {
				//Compute displacement of the object
				displacement = hand_controller.transform.position + distanceFromHand * hand_controller.transform.forward - transform.position;

				//put the object at a specific suitable spot in front of the hand
				transform.parent.position += displacement;
            }

			//If the grabbed object is a container, also attach all contained objects to the hand
			if (GetComponentInParent<Transform>().parent.CompareTag("Container"))
			{
				GetComponentInParent<Transform>().parent.GetComponentInChildren<ContainerScript>().AttachContained(hand_controller, controller, displacement);
			}

			//If grabbed object is a meal plate, it needs to know which hand it is attached to to handle detaching everything from it when delivered (and thus destroyed)
			if (transform.parent.gameObject.GetComponentInChildren<MealPlate>() != null)
            {
				transform.parent.gameObject.GetComponentInChildren<MealPlate>().handController = hand_controller;

			}
		}
	}

	public void attach_to(HandController hand_controller, OVRInput.Controller controller)
    {
		if (is_available())
        {
			//First attach this anchor, which is the one that was found to be the closest by the hand controller, so only it will displace the object
			//It will become the main anchor
			actuallyAttachTo(hand_controller, controller, true);

			foreach (ObjectAnchor o in transform.parent.GetComponentsInChildren<ObjectAnchor>())
			{
				//Then attach this anchor's object's all other anchors without displacing the object
				o.actuallyAttachTo(hand_controller, controller, false);
			}
		}
    }

	//Overload used to displace objects inside containers
	public void attach_to(HandController hand_controller, OVRInput.Controller controller, Vector3 containerDisplacement)
	{
		if (is_available())
		{
			//Go through all anchors this anchor's object has, and check that none of them is already considered to be the main anchor
			//If we did not encounter any main anchor, then this anchor becomes the main anchor
			isMainAnchor = !transform.parent.GetComponentsInChildren<ObjectAnchor>().Aggregate(false, (isMain, next) => isMain || next.isMainAnchor);

			//store the kind of controller (left or right)
			this.controller = controller;

			// Store the hand controller in memory
			this.hand_controller = hand_controller;

			
			// Set the object to be placed in the hand controller referential
			GetComponentInParent<Rigidbody>().isKinematic = true;

			transform.parent.SetParent(hand_controller.transform);

			//Displace the contained object by the same amount as the container
			if(isMainAnchor)
            {
				transform.parent.position += containerDisplacement;
			}

			//If grabbed object is a meal plate, it needs to know which hand it is attached to to handle detaching everything from it when delivered (and thus destroyed)
			if (transform.parent.gameObject.GetComponentInChildren<MealPlate>() != null)
			{
				transform.parent.gameObject.GetComponentInChildren<MealPlate>().handController = hand_controller;

			}
		}
	}

	public void detachContained(HandController handController)
    {

		//If holding a container, detach all contained objects from the controller
		if (GetComponentInParent<Transform>().parent.CompareTag("Container"))
		{
			GetComponentInParent<Transform>().parent.GetComponentInChildren<ContainerScript>().DetachContained(handController);
		}
	}

	public void actuallyDetachFrom(HandController hand_controller)
	{
		if (hand_controller == null) return;

		// Make sure that the right hand controller ask for the release
		if (this.hand_controller != hand_controller) return;

		// Detach the hand controller
		this.hand_controller = null;

		// Set the object to be placed in the original transform parent
		transform.parent.SetParent(initial_transform_parent);

		//The object is now subject to gravity
		GetComponentInParent<Rigidbody>().isKinematic = false;

		detachContained(hand_controller);

		//The object should have some velocity related to hand movement, add it
		if(isMainAnchor)
        {
			isMainAnchor = false;
			AddAverageVelocity();
		}

		//Reset the velocities arrays
		System.Array.Fill(velocityFrames, Vector3.zero, 0, velocityFrames.Length);
	}

	public void detach_from(HandController hand_controller)
	{
		if (hand_controller == null) return;

		// Make sure that the right hand controller ask for the release
		if (this.hand_controller != hand_controller) return;

		foreach (ObjectAnchor o in transform.parent.GetComponentsInChildren<ObjectAnchor>())
		{
			o.actuallyDetachFrom(hand_controller);
		}
	}

private void AddAverageVelocity()
	{
		Vector3 velocityAverage = Vector3.zero;

		//Compute the average of linear velocities over the last few frames
		for (int i = 0; i < velocityFrames.Length; ++i)
		{
			velocityAverage += velocityFrames[i] / velocityFrames.Length;
		}

		//Apply the average velocities to the object
		GetComponentInParent<Rigidbody>().velocity = velocityAverage;

		//If the grabbed object was a controller, apply the same velocity to all contained objects
		if (GetComponentInParent<Transform>().parent.CompareTag("Container"))
		{
			GetComponentInParent<Transform>().parent.GetComponentInChildren<ContainerScript>().AddVelocityToContained(velocityAverage);
		}

	}

    private void OnDestroy()
    {
		//Update anchors to remove self from anchor list
		foreach (HandController h in handControllers)
		{
			h.UpdateAnchors();
		}
	}

    public bool is_available() { return hand_controller == null; }

	public float get_grasping_radius() { return graspingRadius; }

	//When attracting from a distance, disable collisions for a short time to make it more enjoyable
	public IEnumerator DisableHitboxAfterAttract(float time)
	{
		SetCollidersToTrigger(true);

		yield return new WaitForSeconds(time);

		SetCollidersToTrigger(false);
	}

	public void SetCollidersToTrigger(bool isTrigger)
	{
		foreach (Collider c in transform.parent.GetComponentsInChildren<Collider>())
		{
			//If the collider is a trigger to begin with, never modify it
			if (!c.CompareTag("BaseTrigger"))
			{
				c.isTrigger = isTrigger;
			}
		}
	}
}