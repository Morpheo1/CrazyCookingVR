using UnityEngine;

public class HandController : MonoBehaviour
{

	// Store the hand type to know which button should be pressed
	public enum HandType : int { LeftHand, RightHand };
	[Header("Hand Properties")]
	public HandType handType;

	// Store all gameobjects containing an Anchor
	// N.B. This list is static as it is the same list for all hands controller
	// thus there is no need to duplicate it for each instance
	static protected ObjectAnchor[] anchors_in_the_scene;

	public TutorialHandler tutorialHandler;

	void Start()
	{
		// Prevent multiple fetch
		if (anchors_in_the_scene == null) anchors_in_the_scene = FindObjectsOfType<ObjectAnchor>();
	}

	//Used to update the anchor list when spawning or destroying anchors such as ingredients
	public void UpdateAnchors()
    {
		anchors_in_the_scene = FindObjectsOfType<ObjectAnchor>();
		if(tutorialHandler != null)
        {
			tutorialHandler.anchorsInScene = anchors_in_the_scene;
		}
	}

	// This method checks that the hand is closed depending on the hand side
	protected bool is_hand_closed()
	{
		// Case of a left hand
		if (handType == HandType.LeftHand) return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.5;   // Check that the index finger is pressing


		// Case of a right hand
		else return OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.5; // Check that the index finger is pressing
	}

	//This checks if the player wants objects in a held container to experience gravity
	protected bool isReleasingContained()
	{
		return (OVRInput.Get(OVRInput.Button.Four) || OVRInput.Get(OVRInput.Button.Two)); // Check the Y or X button
	}


	// Automatically called at each frame
	void Update() 
	{
		handle_controller_behavior();
	}


	// Store the previous state of triggers to detect edges
	protected bool is_hand_closed_previous_frame = false;

	// Store the object atached to this hand
	// N.B. This can be extended by using a list to attach several objects at the same time
	public ObjectAnchor object_grasped = null;

	/// <summary>
	/// This method handles the linking of object anchors to this hand controller
	/// </summary>
	protected void handle_controller_behavior()
	{
		//Check if we want to release objects within a container, needs to 
		if (isReleasingContained() && object_grasped != null)
		{
			object_grasped.detachContained(this);
		}


		// Check if there is a change in the grasping state (i.e. an edge) otherwise do nothing
		bool hand_closed = is_hand_closed();
		if (hand_closed == is_hand_closed_previous_frame) return;
		is_hand_closed_previous_frame = hand_closed;

		//==============================================//
		// Define the behavior when the hand get closed //
		//==============================================//
		if (hand_closed)
		{

			// Log hand action detection
			Debug.LogWarningFormat("{0} get closed", this.transform.parent.name);
			Debug.Log("closed");

			// Determine which object available is the closest from the left hand
			int best_object_id = -1;
			float best_object_distance = float.MaxValue;
			float object_distance;

			// Iterate over objects to determine if we can interact with it
			for (int i = 0; i < anchors_in_the_scene.Length; i++)
			{

				// Skip object not available
				if (!anchors_in_the_scene[i].is_available()) continue;

				// Compute the distance to the closest point of the object, not its center
				object_distance = Vector3.Distance(this.transform.position, anchors_in_the_scene[i].GetComponent<Collider>().ClosestPointOnBounds(this.transform.position));

				// Keep in memory the closest object
				// N.B. We can extend this selection using priorities
				if (object_distance < best_object_distance && object_distance <= anchors_in_the_scene[i].get_grasping_radius())
				{
					best_object_id = i;
					best_object_distance = object_distance;
				}
			}

			// If the best object is in range grab it
			if (best_object_id != -1)
			{

				// Store in memory the object grasped
				object_grasped = anchors_in_the_scene[best_object_id];

				// Log the grasp
				Debug.LogWarningFormat("{0} grasped {1}", this.transform.parent.name, object_grasped.name);

				// Grab this object
				if (handType == HandType.LeftHand)
				{
					object_grasped.attach_to(this, OVRInput.Controller.LTouch);
				}
				else
				{
					object_grasped.attach_to(this, OVRInput.Controller.RTouch);
				}
			}


			//==============================================//
			// Define the behavior when the hand get opened //
			//==============================================//
		}
		else if (object_grasped != null)
		{
			// Log the release
			Debug.LogWarningFormat("{0} released {1}", this.transform.parent.name, object_grasped.name);

			// Release the object
			object_grasped.detach_from(this);
		}
	}
}