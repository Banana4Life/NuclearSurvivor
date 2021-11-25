using System.Collections.Generic;
using System.Linq;
using FlatTop;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Color = UnityEngine.Color;

[RequireComponent(typeof(TileGenerator))]
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
    private Dictionary<CubeCoord, GameObject[]> _visibleTileAreaLookup = new();
    private Dictionary<int, GameObject> _currentlyVisibleTileAreas = new();
    private TileGenerator _tileGenerator;

    private CubeCoord playerRoom = CubeCoord.Origin;
    private CubeCoord enemyRoom = CubeCoord.Origin;

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

    public static void OnWorldGenerated(List<Room> rooms, List<Hallway> hallways)
    {
        INSTANCE.UpdateVisibleTileAreaLookup(rooms, hallways);
    }

    private void UpdateVisibleTileAreaLookup(List<Room> rooms, List<Hallway> hallways)
    {
        _currentlyVisibleTileAreas = new Dictionary<int, GameObject>();
        _currentlyVisibleTileAreas.AddRange(rooms.Select(r => new KeyValuePair<int, GameObject>(r.TileArea.gameObject.GetInstanceID(), r.TileArea.gameObject)));
        _currentlyVisibleTileAreas.AddRange(hallways.Select(h => new KeyValuePair<int, GameObject>(h.TileArea.gameObject.GetInstanceID(), h.TileArea.gameObject)));
        var roomLookup = rooms.ToDictionary(room => room.RoomCoord);

        _visibleTileAreaLookup = rooms.Select(room =>
        {
            var neighbors = room.RoomCoord.Neighbors().SelectMany(neighborCoord =>
            {
                if (roomLookup.TryGetValue(neighborCoord, out var neighbor))
                {
                    return new List<Room> { neighbor };
                }

                return new List<Room>();
            }).ToArray();

            var connectingHallways = neighbors.SelectMany(neighbor => hallways.Where(h =>
                    h.From == room && h.To == neighbor || h.From == neighbor && h.To == room));

            return (room, new List<GameObject> {room.TileArea.gameObject}.Concat(neighbors.Select(r => r.TileArea.gameObject)).Concat(connectingHallways.Select(h => h.TileArea.gameObject)).ToArray());
        }).ToDictionary(room => room.Item1.RoomCoord, room => room.Item2);
    }

    private void Start()
    {
        audioSourcePool = GetComponent<AudioSourcePool>();
        fogOfWar = GetComponentInChildren<FogOfWarMesh>();
        roundActive = true;
        _tileGenerator = GetComponent<TileGenerator>();
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
        playerRoom = CubeCoordFlatTop.FromWorld(player.transform.position,
            _tileGenerator.tiledict.TileSize()) * (1f / TileGenerator.RoomSize);
        enemyRoom = CubeCoordFlatTop.FromWorld(enemy.transform.position,
            _tileGenerator.tiledict.TileSize()) * (1f / TileGenerator.RoomSize);
        var visible = new Dictionary<int, GameObject>();
        if (_visibleTileAreaLookup.TryGetValue(playerRoom, out var playerVisible))
        {
            foreach (var o in playerVisible)
            {
                visible[o.GetInstanceID()] = o;
            }
        }
        if (_visibleTileAreaLookup.TryGetValue(enemyRoom, out var enemyVisible))
        {
            foreach (var o in enemyVisible)
            {
                visible[o.GetInstanceID()] = o;
            }
        }

        if (!disableInvisibleRooms)
        {
            return;
        }

        
        foreach (var (id, area) in _currentlyVisibleTileAreas)
        {
            if (!visible.ContainsKey(id))
            {
                area.SetActive(false);
            }
        }
        foreach (var (id, area) in visible)
        {
            if (!_currentlyVisibleTileAreas.ContainsKey(id))
            {
                area.SetActive(true);
            }
        }

        _currentlyVisibleTileAreas = visible;
    }

    private void OnDrawGizmos()
    {
        if (!_tileGenerator)
        {
            return;
        }
        var size = _tileGenerator.tiledict.TileSize() * TileGenerator.RoomSize;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerRoom.ToWorld(_tileGenerator.floorHeight, size), size.z / 2f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(enemyRoom.ToWorld(_tileGenerator.floorHeight, size), size.z / 2f);
        
        Gizmos.color = Color.yellow;
        foreach (var (_, go) in _currentlyVisibleTileAreas)
        {
            Gizmos.DrawWireSphere(go.transform.position, 4);
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
