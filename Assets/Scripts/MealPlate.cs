using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MealPlate : MonoBehaviour
{
    public List<Ingredient.IngredientType> ingredientsRequired;

    //Script handling meals and meal images
    public MealImages mealImages;

    //Screen where the image corresponding to this plate is displayed
    public GameObject screen;

    //Store the spawn point to know where to spawn the new plate once we delivered this one
    public Vector3 plateSpawnPoint;

    public MealTimer mealTimer;

    public float pointsMultiplier;

    public bool isDummyTuto;

    public bool isInTutorial;

    //list of ingredients currently inside the plate
    private List<Ingredient.IngredientType> ingredients = new List<Ingredient.IngredientType>();

    private bool mealPrepared = false;

    private bool inDeliveryZone = false;

    private bool inHardDeliveryZone = false;

    //Used during the tutorial to avoid giving points for the first tutorial delivery
    private bool inDummyDeliveryZone = false;

    private PointsCounter playerPoints;

    private AudioSource deliveryAudioSource;

    [NonSerialized]
    public HandController handController;

    // Start is called before the first frame update
    void Start()
    {
        if (playerPoints == null) playerPoints = FindObjectsOfType<PointsCounter>()[0];
    }



    // Update is called once per frame
    void Update()
    {
        //If the timer is over, penalize the player by subtracting points and generate a new meal
        if (mealTimer.timeRemaining == 0 && !mealTimer.isRunning)
        {
            DestroyAndSpawnNewMeal();
        }

        //Assume contained ingredients are the ones required, and if one doesn't match then they aren't
        bool ingredientsEqual = true;

        foreach (Ingredient.IngredientType i in Enum.GetValues(typeof(Ingredient.IngredientType)))
        {
            if (ingredientsRequired.Count(ingredient => ingredient == i) != ingredients.Count(ingredient => ingredient == i))
            {
                ingredientsEqual = false;
            }
        }

        mealPrepared = ingredientsEqual;

        //If we are delivering the correct meal, increase the score, generate a new meal and meal plate, and destroy this plate and contained ingredients
        if (mealPrepared && inDeliveryZone)
        {
            DestroyAndSpawnNewMeal();
        }
    }

    //Add the ingredient that entered the container to the list of contained ingredients
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponent<Ingredient>() != null)
        {
            ingredients.Add(other.gameObject.GetComponent<Ingredient>().ingredientType);
        }

        inDeliveryZone = other.CompareTag("Delivery") || other.CompareTag("DeliveryHard") || other.CompareTag("DeliveryDummy");

        if(other.CompareTag("Delivery") || other.CompareTag("DeliveryHard") || other.CompareTag("DeliveryDummy"))
        {
            deliveryAudioSource = other.GetComponent<AudioSource>();
        }

        inHardDeliveryZone = other.CompareTag("DeliveryHard");

        inDummyDeliveryZone = other.CompareTag("DeliveryDummy");
    }

    //Remove the ingredient that entered the container from the list of contained ingredients
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.GetComponent<Ingredient>() != null)
        {
            ingredients.Remove(other.gameObject.GetComponent<Ingredient>().ingredientType);
        }


        //If we leave the delivery area, we are no longer in it
        if (other.CompareTag("Delivery") || other.CompareTag("DeliveryHard") || other.CompareTag("DeliveryDummy"))
        {
            inDeliveryZone = false;
            deliveryAudioSource = null;
        }
    }

    //generate a new meal and meal plate, and destroy this plate and contained ingredients
    public void DestroyAndSpawnNewMeal()
    {
        bool mealSuccess = false;

        //To encourage speed, points are a fraction of the time remaining rescaled between 0 and 6.907 to get a score between 1 and 1000
        if (mealPrepared && !isDummyTuto)
        {
            float points = pointsMultiplier * 100 * mealTimer.timeRemaining / mealTimer.baseTime;
            if (inHardDeliveryZone)
            {
                playerPoints.SetPoints(playerPoints.points + points * 1.5f);
            }

            //If in normal delivery zone (and not in the tutorial dummy one), add normal amount of points
            else if (!inDummyDeliveryZone)
            {
                playerPoints.SetPoints(playerPoints.points + points);
            }
            mealSuccess = true;
        }
        
        //Stop the game if this method was called due to a timer reaching 0
        if(!(mealPrepared && inDeliveryZone) && !isInTutorial)
        {
            mealImages.StopGame();
        }
        //Otherwise play delivery sound and generate a new meal
        else
        {
            if (deliveryAudioSource != null)
            {
                deliveryAudioSource.Play();
            }

            DetachAndDestroyAllContained();

            if (mealImages != null)
            {
                mealImages.GenerateNewMeal(screen, plateSpawnPoint, mealTimer, transform.parent.GetComponentInChildren<ObjectAnchor>().baseMaterial, transform.parent.GetComponentInChildren<ObjectAnchor>().outlineMaterial, false, mealSuccess);
            }
            Destroy(transform.parent.gameObject);
        }
    }

    public void DetachAndDestroyAllContained()
    {
        GetComponent<ContainerScript>().DetachContained(handController);
        GetComponent<ContainerScript>().DestroyAllContained();
        foreach (ObjectAnchor obj in transform.parent.gameObject.GetComponentsInChildren<ObjectAnchor>())
        {
            obj.detach_from(handController);
        }
    }
}