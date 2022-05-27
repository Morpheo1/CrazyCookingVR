using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PointsCounter : MonoBehaviour
{
    [NonSerialized]
    public float points = 0;

    //Text used to display points
    private TextMeshPro scoreText;

    private void Start()
    {
        //Initialize score text
        scoreText = GetComponent<TextMeshPro>();
        SetPoints(0);
    }

    //Set points and text to specified amount
    public void SetPoints(float points)
    {
        this.points = points;
        scoreText.text = "Score :\n" + Mathf.FloorToInt(points).ToString();
    }
}
