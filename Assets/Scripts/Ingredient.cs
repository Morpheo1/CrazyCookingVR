using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ingredient : MonoBehaviour
{
    //Defines which kind of ingredient we use
    public enum IngredientType : int {PlaceHolder, Carrot, Tomato, HalfTomato, Steak, Dough, Rice, Cheese, HalfCheese, Bread, HalfBread, Ham, Chicken, Salad};

    public IngredientType ingredientType;
}
