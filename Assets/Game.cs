using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Color = UnityEngine.Color;

public class Game : MonoBehaviour
{
    private static Game INSTANCE;
    
    public Floaty floatyPrefab;
    public GameObject canvas;

    private AudioSourcePool audioSourcePool;

    public LeaderAgent player;
    public LeaderAgent enemy;

    public ParticleSystem endOfRoundPs;

    public float timeLeft = 60;
    public float timeLeftTarget = 60;

    public PauseMenu pauseMenu;
    public SettingsMenu settingsMenu;
    public bool endRound;
    public AudioMixer mainMixer;
    public bool disableInvisibleRooms;

    private FogOfWarMesh fogOfWar;
    
    private Dictionary<int, GameObject> _currentlyVisibleTileAreas = new();
    public float viewingDistance = 100f;

    public void OnLevelSpawned(List<Room> newRooms, List<Hallway> newHallways)
    {
        foreach (var room in newRooms)
        {
            var go = room.TileArea.gameObject;
            _currentlyVisibleTileAreas[go.GetInstanceID()] = go;
        }
        foreach (var hallway in newHallways)
        {
            var go = hallway.TileArea.gameObject;
            _currentlyVisibleTileAreas[go.GetInstanceID()] = go;
        }
    }

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

        ShowVisibleTileAreas();
    }

    private void ShowVisibleTileAreas()
    {
        if (!disableInvisibleRooms)
        {
            return;
        }
        
        var areas = new Collider[20];
        var areasFound = Physics.OverlapSphereNonAlloc(player.transform.position, viewingDistance, areas, LayerMask.GetMask("TileArea"));

        var visible = new Dictionary<int, GameObject>();
        for (int i = 0; i < areasFound; i++)
        {
            var go = areas[i].gameObject;
            visible[go.GetInstanceID()] = go;
        }
        
        areasFound = Physics.OverlapSphereNonAlloc(enemy.transform.position, viewingDistance, areas, LayerMask.GetMask("TileArea"));
        for (int i = 0; i < areasFound; i++)
        {
            var go = areas[i].gameObject;
            visible[go.GetInstanceID()] = go;
        }
        
        foreach (var (id, area) in _currentlyVisibleTileAreas)
        {
            if (!visible.ContainsKey(id))
            {
                for (var i = 0; i < area.transform.childCount; i++)
                {
                    var go = area.transform.GetChild(i).gameObject;
                    go.SetActive(false);
                }
            }
        }
        foreach (var (id, area) in visible)
        {
            if (!_currentlyVisibleTileAreas.ContainsKey(id))
            {
                for (var i = 0; i < area.transform.childCount; i++)
                {
                    var go = area.transform.GetChild(i).gameObject;
                    go.SetActive(true);
                }
            }
        }

        _currentlyVisibleTileAreas = visible;
    }

    private void OnDrawGizmos()
    {
        if (disableInvisibleRooms)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.transform.position, viewingDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, viewingDistance);
        }
    }

    public static void EnlargeFogOfWar(LeaderAgent agent)
    {
        if (INSTANCE.player == agent)
        {
            if (INSTANCE.fogOfWar)
            {
                INSTANCE.fogOfWar.lightRange = 7f;
            }
        }
    }
    
    public static void ResetFogOfWar(LeaderAgent agent)
    {
        if (INSTANCE.player == agent)
        {
            if (INSTANCE.fogOfWar)
            {
                INSTANCE.fogOfWar.Reset();
            }
        }
    }
}
