using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlashingText : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Game game;
    public float remainingTime;
    public float blinkspeed;

    private void Start()
    {

        text = GetComponent<TextMeshProUGUI>();
        StartBlinking();
    }
    private void Update()
    {
        remainingTime = game.remainingTime;
    }

    IEnumerator Blink()
    {
        while (true)
        {
            switch (text.color.a.ToString())
            {
                case "0":
                    text.color = new Color(text.color.r, text.color.g, text.color.b, 1);
                    yield return new WaitForSeconds(remainingTime / 100);
                    break;
                case "1":
                    text.color = new Color(text.color.r, text.color.g, text.color.b, 0);
                    yield return new WaitForSeconds(remainingTime / 100);
                    break;
            }
        }
    }

    void StartBlinking()
    {
        StopCoroutine("Blink");
        StartCoroutine("Blink");
    }

    void StopBlinking()
    {
        StopCoroutine("Blink");
    }
}