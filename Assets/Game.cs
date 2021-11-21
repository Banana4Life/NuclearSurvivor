using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    private static Game INSTANCE;
    
    public Floaty floatyPrefab;
    public GameObject canvas;

    private AudioSourcePool audioSourcePool;

    public LeaderAgent player;

    public ParticleSystem endOfRoundPs;

    public float timeLeft = 60;
    public float timeLeftTarget = 60;

    public PauseMenu pauseMenu;
    public SettingsMenu settingsMenu;
    public bool endRound;
    public AudioMixer mainMixer;

    private FogOfWarMesh fogOfWar;
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 50, 20), ((int)(1.0f / Time.smoothDeltaTime)).ToString());        
        GUI.Label(new Rect(10, 30, 50, 20), timeLeft.ToString());        
    }
    private void Awake()
    {
        INSTANCE = this;
    }

    public static AudioSourcePool audioPool()
    {
        return INSTANCE.audioSourcePool;
    }
    
    public static void SpawnFloaty(string text, Vector3 pos)
    {
        var floaty = Instantiate(INSTANCE.floatyPrefab, INSTANCE.canvas.transform, false);
        floaty.Init(text, pos);
    }

    private void Start()
    {
        audioSourcePool = GetComponent<AudioSourcePool>();
        fogOfWar = GetComponentInChildren<FogOfWarMesh>();
        roundActive = true;
    }

    public static void ExtendTimer()
    {
        if (INSTANCE.roundActive)
        {
            INSTANCE.timeLeftTarget += 5f;
        }
    }

    public bool roundActive;

    public float muffleValue = 3000f;
    
    private void Update()
    {
        if (player.isInHiding)
        {
            muffleValue = Mathf.Lerp(muffleValue, 320f, Time.deltaTime * 3f);
        }
        else
        {
            muffleValue = Mathf.Lerp(muffleValue, 3200f, Time.deltaTime * 3f);
        }
        mainMixer.SetFloat("muffler", muffleValue);
        timeLeft -= Time.deltaTime;
        timeLeftTarget -= Time.deltaTime;
        timeLeft = Mathf.Lerp(timeLeft, timeLeftTarget, Time.deltaTime);
        if (timeLeft < 0)
        {
            if (fogOfWar)
            {
                fogOfWar.lightRange += -timeLeft;
            }
            if (roundActive)
            {
                roundActive = false;
                endOfRoundPs.Play();
            }
            else
            {
                if (timeLeft < -4f && endRound)
                {
                    if (player.SurvivesEnd()) // If player is in Hiding Spot - continue game
                    {
                        player.NewFollowers();
                        timeLeft = 1f;
                        timeLeftTarget = 30f;
                        roundActive = true;
                        fogOfWar.Reset();
                    }
                    else
                    {
                        EndOfRoundMenu.Score(this, player);
                        SceneManager.LoadScene("EndOfRound");
                    }
                }
            }
        }
        
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (settingsMenu.gameObject.activeSelf)
            {
                settingsMenu.CloseMenu();
                pauseMenu.Pause();
            }
            else
            {
                pauseMenu.TogglePause();
            }
        }
    }

    public static void EnlargeFogOfWar(LeaderAgent agent)
    {
        if (INSTANCE.player == agent)
        {
            INSTANCE.fogOfWar.lightRange = 7f;
        }
    }
    
    public static void ResetFogOfWar(LeaderAgent agent)
    {
        if (INSTANCE.player == agent)
        {
            INSTANCE.fogOfWar.Reset();
        }
    }
}
