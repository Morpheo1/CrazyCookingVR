using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MealImages : MonoBehaviour
{
    //Tool class needed to circumvent the fact the unity doesn't allow to serialize nested loops, to allow for a list of lists of ingredients
    [System.Serializable]
    public class Meal
    {
        public List<Ingredient.IngredientType> meal;
    }

    private static System.Random random = new System.Random();

    public List<Meal> meals;

    //Names of images of possible meals
    public List<string> mealImages;
    
    //Time limit of all possible meals
    public List<float> mealTimeLimits;
    
    //Lists of actual screens, spawn points and timer objects
    public List<GameObject> screens;
    public List<Vector3> plateSpawnPoints;
    public List<MealTimer> mealTimers;
    public List<Material> basePlateMaterials;
    public List<Material> outlinePlateMaterials;

    public AudioSource musicSource;

    private List<GameObject> mealPlates;

    public GameObject platePrefab;

    public bool isTutorial;

    //Used to inform the hand controllers that there is a new grabbable object
    [System.NonSerialized]
    public HandController[] handControllers;


    private bool updateAnchorsNextFrame = false;

    //Depends on number of meals prepared
    private float timerMultiplier;
    private float pointsMultiplier;

    // Start is called before the first frame update
    void Start()
    {
        //Get hand controllers in the scene
        handControllers = FindObjectsOfType<HandController>();

        mealPlates = new List<GameObject>();

        timerMultiplier = 1;
        pointsMultiplier = 1;

        //For each screen, spawn a plate with a random meal required, initialize its attributes, display the corresponding image and start the corresponding timer
        for (int i = 0; i < screens.Count; ++i)
        {
            GenerateNewMeal(screens[i], plateSpawnPoints[i], mealTimers[i], basePlateMaterials[i], outlinePlateMaterials[i], true, false);

            updateAnchorsNextFrame = true;
        }
    }

    private void Update()
    {
        if(updateAnchorsNextFrame)
        {
            //Update anchors to add new ingredient to anchor list
            foreach (HandController h in handControllers)
            {
                h.UpdateAnchors();
            }

            updateAnchorsNextFrame = false;
        }
    }

    //Spawn a new plate with a new random meal required, display the corresponding images and start the corresponding timer
    public void GenerateNewMeal(GameObject screen, Vector3 plateSpawnPoint, MealTimer mealTimer, Material basePlateMaterial, Material outlinePlateMaterial, bool firstExecution, bool mealSuccess)
    {
        GameObject newObject = Instantiate(platePrefab, plateSpawnPoint, Quaternion.identity);
        MealPlate newPlate = newObject.GetComponentInChildren<MealPlate>();

        //if this is the first time spawning the plates, we need to initialize the array
        if(firstExecution)
        {
            mealPlates.Add(newObject);
        }
        else
        {
            mealPlates[screens.IndexOf(screen)] = newObject;

            //Speed up the music, lower the available time for each meal and increase the maximum amount of points obtainable
            if (mealSuccess)
            {
                timerMultiplier *= 0.92f;

                musicSource.pitch *= 1.025f;
                pointsMultiplier *= (1.0f / 0.92f);
            }
        }

        //The rest of this method initializes all the necessary parameters of the new plate, sets up a new timer and displays an image corresponding to the meal

        newObject.GetComponent<Renderer>().material = basePlateMaterial;

        foreach (ObjectAnchor o in newObject.GetComponentsInChildren<ObjectAnchor>())
        {
            o.baseMaterial = basePlateMaterial;
            o.outlineMaterial = outlinePlateMaterial;
        }

        int mealIdx = random.Next(meals.Count);

        mealTimer.StartNewTimer(timerMultiplier * mealTimeLimits[mealIdx]);

        Debug.LogWarning(timerMultiplier * mealTimeLimits[mealIdx]);

        newPlate.ingredientsRequired = meals[mealIdx].meal;
        newPlate.screen = screen;
        newPlate.mealTimer = mealTimer;
        newPlate.mealImages = this;
        newPlate.plateSpawnPoint = plateSpawnPoint;
        newPlate.isInTutorial = isTutorial;
        newPlate.pointsMultiplier = pointsMultiplier;

        Material screenMaterial = (Material)Resources.Load("ImageDisplay");

        screen.GetComponent<Renderer>().material = screenMaterial;

        Texture2D myTexture = Resources.Load(mealImages[mealIdx]) as Texture2D;

        screen.GetComponent<Renderer>().material.mainTexture = myTexture;

        updateAnchorsNextFrame = true;
    }

    //Reset the meal plate to its original position and rotation, and prevent problems from occuring by detaching it from both hands and setting its speed to zero
    public void ResetMealPlate(GameObject screen)
    {
        int idx = screens.IndexOf(screen);

        mealPlates[idx].GetComponentInChildren<ObjectAnchor>().detach_from(handControllers[0]);
        mealPlates[idx].GetComponentInChildren<ObjectAnchor>().detach_from(handControllers[1]);
        mealPlates[idx].transform.rotation = Quaternion.identity;
        mealPlates[idx].transform.position = plateSpawnPoints[idx];
        mealPlates[idx].GetComponent<Rigidbody>().velocity = Vector3.zero;
    }

    //Stop all timers and destroy all plates to prevent the player from continuing
    public void StopGame()
    {
        Debug.LogWarning("Game Stopped");
        foreach (MealTimer t in mealTimers)
        {
            t.isRunning = false;
        }
        
        foreach(GameObject p in mealPlates)
        {
            p.GetComponentInChildren<MealPlate>().DetachAndDestroyAllContained();
            Destroy(p);
        }

        musicSource.Stop();
    }
}
