using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class TutorialHandler : MonoBehaviour
{
    //Player object
    public GameObject player;

    //Coordinates where the player is teleported
    public List<Vector3> roomCenters;

    //Index of the scene to load when finishing the tutorial
    public int gameSceneIndex;

    [System.NonSerialized]
    public ObjectAnchor[] anchorsInScene;

    private int currentRoomCenter = 0;

    //Used to avoid multiple teleportations in a single button press
    private bool pressedBPreviousFrame;
    private bool pressedYPreviousFrame;

    //Used for teleportation
    private bool characterControllerDisabled;

    // Update is called once per frame
    void Update() 
    {
        //Pressing B but not holding from before and all anchors available, aka player isn't holding anything
        if (OVRInput.Get(OVRInput.Button.Two) && !pressedBPreviousFrame && anchorsInScene.Aggregate(true, (isAvailabe, next) => isAvailabe && next.is_available()))
        {
            //If not in the last room, go to next room
            if (currentRoomCenter != roomCenters.Count - 1)
            {
                currentRoomCenter += 1;
                TeleportPlayer(roomCenters[currentRoomCenter]);
            }

            //If in last room, start game
            else
            {
                SceneManager.LoadScene(gameSceneIndex);
            }

            //Remember that we were pressing B to avoid multiple teleportations
            pressedBPreviousFrame = true;
        }
        
        //Player is not holding B
        if(!OVRInput.Get(OVRInput.Button.Two))
        {
            pressedBPreviousFrame = false;
        }

        //Pressing Y and not in 1st room and not holding Y from before and all anchors available, aka player isn't holding anything
        if (OVRInput.Get(OVRInput.Button.Four) && currentRoomCenter != 0 && !pressedYPreviousFrame && anchorsInScene.Aggregate(true, (isAvailabe, next) => isAvailabe && next.is_available())) 
        {
            //Teleport player to previous room
            currentRoomCenter -= 1;
            TeleportPlayer(roomCenters[currentRoomCenter]);

            //Player is now holding Y
            pressedYPreviousFrame = true;
        }

        //Player is no longer holding Y
        if(!OVRInput.Get(OVRInput.Button.Four))
        {
            pressedYPreviousFrame = false;
        }

        //If disabled character controller to allow for teleportation, enable it back
        if(characterControllerDisabled)
        {
            player.GetComponent<CharacterController>().enabled = true;
        }
    }

    private void TeleportPlayer(Vector3 newPos)
    {
        //Only change x and z coordinates, not elevation (y)
        Vector3 newPlayerPosition = player.transform.position;
        newPlayerPosition.x = newPos.x;
        newPlayerPosition.z = newPos.z;

        //Disable character controller, otherwise can't teleport through walls
        player.GetComponent<CharacterController>().enabled = false;
        characterControllerDisabled = true;

        //Teleport player
        player.transform.position = newPlayerPosition;
    }
}
