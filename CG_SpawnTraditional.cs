using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CG_SpawnTraditional : MonoBehaviour
{
    #region Singleton
    private static CG_SpawnTraditional _instance;

    public static CG_SpawnTraditional Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;
    }
    #endregion

    [Header("Spawn radius details:")]
    [SerializeField] private float radius;
    [SerializeField] private int amountPerRow;

    private int _charCount;

    GUIStyle GUIFont = new GUIStyle();

    [Header("Item to spawn:")]
    public GameObject objectToSpawn;

    Vector3 nextSpawnPos;
    Vector3 currentSpawnPos;
    int currentRow;
    int currentColumn;
    int amountTemp;


    // Start is called before the first frame update
    private void Start()
    {
        GUIFont.fontSize = 32;
        amountTemp = 0;
        currentRow = 1;
        currentColumn = 1;

        currentSpawnPos = new Vector3(StarterPosition().x, StarterPosition().y, StarterPosition().z);
        nextSpawnPos = NextPosition();

        for (int i = 0; i < amountPerRow; i++)
            SpawnObject();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            for (int i = 0; i < amountPerRow; i++)
                SpawnObject();
        }
    }

    // Spawn a player
    private void SpawnObject()
    {
        if (objectToSpawn)
        {
            // Spawn object
            Instantiate(objectToSpawn, currentSpawnPos, transform.rotation);

            // Increase the object count 
            if (amountTemp != amountPerRow)
                amountTemp++;
            else
            {
                amountTemp = 1;
                currentColumn++;
            }


            currentSpawnPos = NextPosition();
            _charCount++;
        }
    }

    // Get the next poistion of the spawn
    private Vector3 NextPosition()
    {
        Vector3 pos = Vector3.zero;

        if (amountTemp != amountPerRow)
        {
            pos = new Vector3(currentSpawnPos.x, currentSpawnPos.y, currentSpawnPos.z + radius);
        }
        else
        {
            pos = new Vector3(currentSpawnPos.x + radius, StarterPosition().y, StarterPosition().z);
        }

        return pos;
    }

    // Get the starter position of the spawner
    private Vector3 StarterPosition()
    {
        return new Vector3(transform.position.x, transform.position.y, (transform.position.z - ((amountPerRow / 2) * radius)) + (radius / 2));
    }

    void OnGUI()
    {
        GUI.Box(new Rect(0, 35, 170, 30), "Characters: " + _charCount.ToString(), GUIFont);
        GUI.Box(new Rect(0, 70, 170, 30), "Press 'T' to add more characters", GUIFont);

    }
}