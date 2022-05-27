using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContainerSpeedKiller : MonoBehaviour
{
    //The magnitude the modified velocities will have
    public float remainingSpeedFactor;

    public AudioSource audioSource;

    //Avoid killing an object's speed to frequently
    private List<ObjectAnchor> onCooldown = new List<ObjectAnchor>();

    private void OnTriggerEnter(Collider other)
    {
        //If the object is not yet in the container, is an objectAnchor, has some velocity, is not held by the player and is not on cooldown for killing its speed recently
        if (!transform.parent.GetComponentInChildren<ContainerScript>().contained.Contains(other.gameObject) && 
            other.GetComponent<ObjectAnchor>() != null && 
            other.transform.parent.GetComponentInChildren<Ingredient>() != null &&
            other.GetComponentInParent<Rigidbody>().velocity.magnitude != 0 &&
            transform.parent.GetComponentInChildren<ObjectAnchor>().is_available() &&
            !onCooldown.Contains(other.GetComponent<ObjectAnchor>()))
        {
            //Keep the velocity's direction but reduce it to a very small magnitude so that the object simply falls in the container
            other.GetComponentInParent<Rigidbody>().velocity = other.GetComponentInParent<Rigidbody>().velocity.normalized * remainingSpeedFactor;

            //If the object was just attracted by the player, make it solid again
            other.GetComponent<ObjectAnchor>().SetCollidersToTrigger(false);

            audioSource.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.GetComponent<ObjectAnchor>() != null)
        {
            //Set the object to be on cooldown for killing its speed for a short time
            onCooldown.Add(other.GetComponent<ObjectAnchor>());
            StartCoroutine(speedKillCooldown(1.0f, other.GetComponent<ObjectAnchor>()));
        }
    }

    public IEnumerator speedKillCooldown(float time, ObjectAnchor o)
    {
        yield return new WaitForSeconds(time);

        onCooldown.Remove(o);
    }
}
