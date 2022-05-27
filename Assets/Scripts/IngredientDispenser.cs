using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientDispenser : MonoBehaviour
{
    //Collider checking for how many ingredients are available
    public IngredientChecker ingredientChecker;

    //The ingredient object that will be spawned
    public GameObject ingredientPrefab;

    //Used to inform the hand controllers that there is a new grabbable object
    [System.NonSerialized]
    public HandController[] handControllers;

    //Used to initialize spawned ingredients' ObjectAnchor
    public Transform cameraForward;

    //The target amount of available ingredients
    public int minIngredientCount = 10;

    //Spawn ingredients at a given rate
    public float spawnCooldown = 0.5f;
    private bool recentlySpawned = false;

    private void Start()
    {
        //Get hand controllers in the scene
        handControllers = FindObjectsOfType<HandController>();
    }

    // Update is called once per frame
    void Update()
    {
        //Spawn a new ingredient if the cooldown has passed and the target amount is not met
        if(!recentlySpawned && ingredientChecker.ingredientCounter < minIngredientCount)
        {
            SpawnIngredient();

            //Update anchors to add new ingredient to anchor list
            foreach(HandController h in handControllers)
            {
                h.UpdateAnchors();
            }

            //Handle spawn cooldown
            recentlySpawned = true;
            StartCoroutine(SpawnCooldown(spawnCooldown));
        }
    }

    private void SpawnIngredient()
    {
        //Spawn the object at a random position in the dispenser
        Instantiate(ingredientPrefab, new Vector3(transform.position.x + Random.Range(-0.2f * transform.localScale.x, 0.2f * transform.localScale.x), transform.position.y, transform.position.z + Random.Range(-0.2f * transform.localScale.x, 0.2f * transform.localScale.x)), Quaternion.identity);
    }

    IEnumerator SpawnCooldown(float time)
    {
        yield return new WaitForSeconds(time);

        recentlySpawned = false;
    }
}
