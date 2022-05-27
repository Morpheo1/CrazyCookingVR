using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MealTimer : MonoBehaviour
{
    //Timer limit and text
    public float timeRemaining;
    public bool isRunning = false;

    public float baseTime;

    public AudioSource audioSource;
    public AudioClip twentyLeft;
    public AudioClip fiveLeft;

    private TextMeshPro timerText;

    private bool isAlreadyUnder20s = true;
    private bool isAlreadyUnder5s = true;

    private void Start()
    {
        //Initialize text
        timerText = GetComponent<TextMeshPro>();

        timeRemaining = baseTime;
    }

    public void StartNewTimer(float time)
    {
        baseTime = time;
        timeRemaining = time;
        isRunning = true;
        isAlreadyUnder20s = false;
        isAlreadyUnder5s = false;
        audioSource.Stop();
    }

    void Update()
    {
        if (isRunning)
        {
            if(timeRemaining < 20 && !isAlreadyUnder20s)
            {
                audioSource.clip = twentyLeft;
                audioSource.Play();
                isAlreadyUnder20s = true;
            }

            if (timeRemaining < 5 && !isAlreadyUnder5s)
            {
                audioSource.clip = fiveLeft;
                audioSource.Play();
                isAlreadyUnder5s = true;
            }

            //If timer is running and not over, compute remaining time and display it accordingly
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            } 
            //Stop the timer once it reaches 0
            else
            {
                timeRemaining = 0;
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                isRunning = false;
            }
        }
    }
}
