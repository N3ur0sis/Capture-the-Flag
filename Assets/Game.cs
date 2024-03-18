using Cinemachine;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    #region Variables
    //---------------------------------------------------VARIABLES-----------------------------------------------------//
    //Maze generation variable
    public float holep;
    public int w, h, x, y;
    public bool[,] hwalls, vwalls;
    public Transform Level, Player, Goal, Enemies, levelParent;
    public GameObject Floor, Wall, Enemy, floatingBombTime, floatingtextParent, floatingLevel, levelParentfloating, flag;
    public CinemachineVirtualCamera cam;
    //Level properties and player variables
    public float remainingTime, bombMalus, levelTime;
    private float shakeTimer, shakeTimerTotal, startIntensity;
    public bool timerIsRunning = false, goalSet = false, gameOver = false, isPlaying = false, flagset = false;
    public int levelCount, bombCount;
    //Scene, light, effet variables
    public UnityEngine.Experimental.Rendering.Universal.Light2D globalLight, pointLight;
    private CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin;
    public TextMeshProUGUI timeText, levelText;
    public Slider slider;
    public AudioClip bombeSD;
    public AudioClip levelUpSD;
    public AudioSource  source;
    private void Awake()
    {
        cinemachineBasicMultiChannelPerlin = cam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        Cursor.lockState = CursorLockMode.Locked;
    }
    //-----------------------------------------------------------------------------------------------------------------//
    #endregion
    #region Start Function
    //----------------------------------------------------START-------------------------------------------------------//
    //Fonction to load when player hit the goal or enemies
    void Start()
    {
        //Clear the level
        foreach (Transform child in Level)
            Destroy(child.gameObject);
        foreach (Transform child in Enemies)
            Destroy(child.gameObject);
        //Spawn Floor and Walls
        hwalls = new bool[w + 1, h];
        vwalls = new bool[w, h + 1];
        var st = new int[w, h];
        void dfs(int x, int y)
        {
            st[x, y] = 1;
            Instantiate(Floor, new Vector3(x, y), Quaternion.identity, Level);
            var dirs = new[]
            {
                (x - 1, y, hwalls, x, y, Vector3.right, 90, KeyCode.A),
                (x + 1, y, hwalls, x + 1, y, Vector3.right, 90, KeyCode.D),
                (x, y - 1, vwalls, x, y, Vector3.up, 0, KeyCode.S),
                (x, y + 1, vwalls, x, y + 1, Vector3.up, 0, KeyCode.W),
            };
            foreach (var (nx, ny, wall, wx, wy, sh, ang, k) in dirs.OrderBy(d => Random.value))
                if (!(0 <= nx && nx < w && 0 <= ny && ny < h) || (st[nx, ny] == 2 && Random.value > holep))
                {
                    wall[wx, wy] = true;
                    Instantiate(Wall, new Vector3(wx, wy) - sh / 2, Quaternion.Euler(0, 0, ang), Level);
                }
                else if (st[nx, ny] == 0) dfs(nx, ny);
            st[x, y] = 2;
        }
        dfs(0, 0);
        //Spawn the bomb
        for (int i = 0; i < bombCount; i++)
        {
            Instantiate(Enemy, new Vector3(Random.Range(0, w), Random.Range(0, h)), Quaternion.identity, Enemies );
        }
        //Spawn the player and the goal
        x = Random.Range(0, w);
        y = Random.Range(0, h);
        Player.position = new Vector3(x, y);
        cam.m_Lens.OrthographicSize = Mathf.Pow(w / 3 + h / 2, 0.7f) + 1;
        levelText.text = "LEVEL " + levelCount;
        slider.minValue = 0;
        slider.maxValue = levelTime;
        DisplayTime(remainingTime);
        if (goalSet)
        {
            flag.transform.position = Player.position;
            flagset = false;
        }
        
    }
    //-----------------------------------------------------------------------------------------------------------------//
    #endregion
    #region Update Function
    //----------------------------------------------------UPDATE------------------------------------------------------//
    //Some shit when player move
    void Update()
    {
        if(goalSet &&flag.transform.position != Goal.position)
        {
            if(!flagset)
             flag.transform.position = Vector3.Lerp(flag.transform.position, Goal.position, Time.deltaTime * 1);
        }
        else
        {
            flag.transform.position = new Vector3(-10, -10);
            Goal.gameObject.SetActive(true);
            flagset = true;
        }

        if (!gameOver)
        {
            //Apply shaking if bomb touch
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = Mathf.Lerp(startIntensity, 0f, 1 - (shakeTimer / shakeTimerTotal));
            }
            //CHeck if Timer's on
            if (timerIsRunning)
            {
                if (remainingTime > 0)
                {
                    remainingTime -= Time.deltaTime;
                    DisplayTime(remainingTime);
                }
                else
                {
                    //GameOver
                    GameOver();
                    remainingTime = 0;
                    DisplayTime(remainingTime);
                    timerIsRunning = false;
                }
            }

            //and Play
            var dirs = new[]
                {
                (x - 1, y, hwalls, x, y, Vector3.right, 90, KeyCode.LeftArrow),
                (x + 1, y, hwalls, x + 1, y, Vector3.right, 90, KeyCode.RightArrow),
                (x, y - 1, vwalls, x, y, Vector3.up, 0, KeyCode.DownArrow),
                (x, y + 1, vwalls, x, y + 1, Vector3.up, 0, KeyCode.UpArrow),
            };
            foreach (var (nx, ny, wall, wx, wy, sh, ang, k) in dirs.OrderBy(d => Random.value))
                if (Input.GetKeyDown(k))
                {
                        timerIsRunning = true;
                        if (wall[wx, wy])
                            Player.position = Vector3.Lerp(Player.position, new Vector3(nx, ny), 0.1f);
                        else
                            (x, y) = (nx, ny);
                }

            Player.position = Vector3.Lerp(Player.position, new Vector3(x, y), Time.deltaTime * 12);
            // If player touch the goal
            if (Vector3.Distance(Player.position, Goal.position) < 0.12f)
            {

                GameObject go = GameObject.Find("FloatingLevelText(Clone)");
                //if the tree exist then destroy it
                if (go)
                {
                    Destroy(go.gameObject);
                }
                source.PlayOneShot(levelUpSD);
                ShakeCamera(4, 1);
                levelCount++;
                w++; h++;
                holep = 1f; ;
                globalLight.intensity = 0.8f;
                pointLight.shadowIntensity = 0.5f;
                pointLight.pointLightOuterRadius = 12;
                bombCount = 4 + levelCount;
                levelTime = 40 + levelCount*10 + remainingTime;
                remainingTime = levelTime;
                goalSet = false;
                timerIsRunning = false;
                Goal.position = new Vector3(-100, -100);
                DisplayTime(remainingTime);
                levelText.gameObject.SetActive(false);
                GameObject levelFloating = Instantiate(floatingLevel, levelParentfloating.transform.position, Quaternion.identity);
                levelFloating.transform.SetParent(levelParentfloating.transform);
                levelFloating.GetComponentInChildren<TextMeshProUGUI>().text = "LEVEL " + levelCount;
                Start();
            }

            //If player touch a bomb
            for (int i = 0; i < Enemies.childCount; i++)
            {
                
                if(Enemies.GetChild(i).transform.position == Goal.position || Enemies.GetChild(i).transform.position == Player.position)
                {
                    Enemies.GetChild(i).transform.position = new Vector3(Random.Range(0, w), Random.Range(0, h));
                }
                if (Vector3.Distance(Player.position, Enemies.GetChild(i).transform.position) < 0.12f)
                {
                    source.PlayOneShot(bombeSD);
                    bombCount--;
                    ShakeCamera(6f, 0.5f);
                    Destroy(Enemies.GetChild(i).gameObject);
                    //floatingtextParent.transform.position = Player.position;
                    GameObject bombFloating = Instantiate(floatingBombTime, floatingtextParent.transform.position, Quaternion.identity);
                    bombFloating.transform.SetParent(floatingtextParent.transform);
                    bombFloating.GetComponentInChildren<TextMeshProUGUI>().text = "-" + Mathf.Clamp(Mathf.FloorToInt(levelTime / 60), 1, 1000).ToString() + "s";
                    Destroy(bombFloating, 1);
                    remainingTime -= levelTime/60;
                    if (!goalSet)
                    {
                        if (bombCount == 0)
                        {
                            Goal.gameObject.SetActive(false);
                            do Goal.position = new Vector3(Random.Range(0, w), Random.Range(0, h));
                            while (Vector3.Distance(Player.position, Goal.position) < (w + h) / 4);
                            
                            goalSet = true;
                            bombCount = 4 + levelCount;
                            holep -= (1.0f / (4 + levelCount)) ;
                            Start();
                        }
                        else
                        {
                            holep -= ( 1.0f / (4 + levelCount ));
                            Start();
                        }
                    }

                }
            }
            //Reduce the light on the map
            if (pointLight.pointLightOuterRadius > 2 && timerIsRunning)
            {
                if (!goalSet)
                {
                    if (globalLight.intensity > 0.01)
                    {
                        globalLight.intensity -= 0.0002f;
                        pointLight.shadowIntensity += 0.0002f;
                        pointLight.pointLightOuterRadius -= 0.002f;
                    }
                }
                else
                {
                    if (globalLight.intensity < 0.01f)
                    {
                        globalLight.intensity -= 0.00002f;
                        pointLight.shadowIntensity += 0.00002f;
                        pointLight.pointLightOuterRadius -= 0.0002f;
                    }
                    else
                    {
                        globalLight.intensity -= 0.001f;
                        pointLight.shadowIntensity += 0.001f;
                        pointLight.pointLightOuterRadius -= 0.01f;
                    }


                }

            }
        }
        else
        {
            ShakeCamera(8, 3);
            if(pointLight.pointLightOuterRadius >= 0)
            {
                globalLight.intensity -= 0.005f;
                pointLight.pointLightOuterRadius -= 0.05f;
            }
            else
            {
                SceneManager.LoadScene(0);
            }
            levelParent.gameObject.SetActive(false);

        }
       
    }
    //-----------------------------------------------------------------------------------------------------------------//
    #endregion
    private void GameOver()
    {
        gameOver = true;
    }

    public void ActiveText()
    {
        levelText.gameObject.SetActive(true);
    }

    void DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        float milliSeconds = (timeToDisplay % 1) * 1000;

        timeText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
        slider.value = remainingTime;
    }


    public void ShakeCamera(float intensity, float timer)
    {
        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = intensity;
        shakeTimer = timer;
        shakeTimerTotal = timer;
        startIntensity = intensity;
    }

}