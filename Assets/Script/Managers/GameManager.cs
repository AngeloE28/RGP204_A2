using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    [Header("Gameplay Loop")]
    public Spawner coralSpawner;
    public Spawner starfishSpawner;
    public bool isGameRunning = true;
    public bool isGamePaused;
    private bool playerWin;

    [Header("UI")]
    public GameObject finishedWindow;
    public TMP_Text winMsg;
    public GameObject pauseWindow;
    public GameObject crosshair;

    // Start is called before the first frame update
    void Start()
    {
        isGameRunning = true;
        isGamePaused = false;
        Time.timeScale = 1.0f;        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (isGamePaused)
                Resume();
            else
                Pause();
        }

        if (isGameRunning && isGamePaused)
            Cursor.visible = true;
        if (isGameRunning && !isGamePaused)
            Cursor.visible = false;
        if (!isGameRunning)
            Cursor.visible = true;
    }

    private void LateUpdate()
    {
        WinCondition();
    }

    private void WinCondition()
    {
        if (starfishSpawner.prefabCounter.Length <= 0)
        {
            playerWin = true;
            isGameRunning = false;
        }
        if(coralSpawner.prefabCounter.Length <= 0)
        {
            playerWin = false;
            isGameRunning = false;
        }

        if (!isGameRunning)
            GameOver(playerWin);
    }

    private void Pause()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        crosshair.SetActive(false);
        pauseWindow.SetActive(true);

        Time.timeScale = 0.0f;

        isGamePaused = true;
    }

    public void Resume()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        crosshair.SetActive(true);
        pauseWindow.SetActive(false);

        Time.timeScale = 1.0f;

        isGamePaused = false;
    }

    public void GameOver(bool isWin)
    {
        if (isWin)
            winMsg.text = "You saved the reef!";
        else
            winMsg.text = "The starfish ate all the corals!";

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        crosshair.SetActive(false);
        finishedWindow.SetActive(true);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        SceneManager.LoadScene(0);     
    }
}
