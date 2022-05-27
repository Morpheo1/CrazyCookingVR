using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class IngredientChecker : MonoBehaviour
{
    //Store the amount of ingredients contained below the dispenser
    [System.NonSerialized]
    public int ingredientCounter = 0;

    //Which kind of ingredient should this check for
    public Ingredient.IngredientType ingredientType;

    private void OnTriggerEnter(Collider other)
    {
        //If the entering object is an ingredient of the correct type then increase the counter
        if (other.GetComponent<Ingredient>() != null && other.GetComponent<Ingredient>().ingredientType == ingredientType)
        {
            ingredientCounter++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //If the exiting object is an ingredient of the correct type then decrease the counter
        if (other.GetComponent<Ingredient>() != null && other.GetComponent<Ingredient>().ingredientType == ingredientType)
        {
            ingredientCounter--;
        }
    }
}
