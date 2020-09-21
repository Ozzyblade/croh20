using UnityEngine;
using Unity.Entities;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CG_CharacterSkinner {

    // The two animations that can be switched between during runtime
    public enum CharacterAnimationState
    {
        Animation1,
        Animation2
    }

    public class CG_GameManager : MonoBehaviour
    {

        // The amount of characters on screen
        private int CharacterCount;
        private int PreviousSpawningLine;

        // The entity manager of the project
        private EntityManager EntityManager;
        // Singleton instance of this game mananger
        [HideInInspector]
        public static CG_GameManager Instance;

        #region User Variables

        // The gameobject to spawn
        [Header("What character do you wish to spawn?")]
        public GameObject CharacterPrefab;
 

        [Header("Animation Data")]
        // The animation that should play
        public CharacterAnimationState AnimationState;

        // Animation data for the first animation
        [Header("Animation 1")]
        [Range(0.1f, 10.0f)]
        public float Animation1Speed = 1;
        public AnimationClip Animation1;
        public bool LoopAnimation1 = true;

        // Animation data for the second animation
        [Header("Animation 2")]
        [Range(0.1f, 10.0f)]
        public float Animation2Speed = 1;
        public AnimationClip Animation2;
        public bool LoopAnimation2 = true;

        // How fast should the character transition through animations
        [Header("Transition Speed")]
        [Range(0.1f, 1f)]
        public float TransitionSpeed = 0.25f;

        #endregion

        #region GUI Vars
        public Vector4 RectData = new Vector4(100, 100, 100, 35);
        GUIStyle GUIFont = new GUIStyle();
        #endregion

        public GameObject[] SpawnNodes;
        public List<CG_SpawnNode> SpawnNodeData;

        // Use these variables for the editor
        //[HideInInspector]
        public List<CG_SpawnNode> EditorSpawnNodeData;
        public CG_SpawnNode SpawnNodeGameObject;

        #region Singleton

        // Singleton instance setup
        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        #endregion

        private void Start()
        {
            // Set the font for the gui
            GUIFont.fontSize = 32;

            // Create the entity manager and get all the spawn nodes within the scene
            EntityManager = World.Active.GetOrCreateManager<EntityManager>();

            GetSpawnNodes();

            // This check is put in place to stop characters being spawned with multiple spawn points
            if (SpawnNodeData.Count == 1)
                SpawnFullCharacters(SpawnNodeData[0].CharacterSpawningData.NumberOfLines);
            else
                SpawnFullCharacters(0);

        }

        // Get the spawn node data ready for spawning the characters
        void GetSpawnNodes()
        {
            SpawnNodes = GameObject.FindGameObjectsWithTag("SpawnNodeTag");

            for (int i = 0; i < SpawnNodes.Length; i++)
            {
                SpawnNodeData.Add(SpawnNodes[i].GetComponent<CG_SpawnNode>());
            }

            Debug.Log(SpawnNodeData.Count);
        }

        // During the update function, by switching the animation state, we can switch between the animations
        private void Update()
        {
            // Only spawn more characters if there is a single spawn node
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (SpawnNodeData.Count == 1)
                    SpawnFullCharacters(SpawnNodeData[0].CharacterSpawningData.NumberOfLines);
            }

            // Change the animation between the 2

            if (Animation1 && Animation2)
            {
                if (Input.GetKeyDown(KeyCode.Y))
                {
                    if (AnimationState == CharacterAnimationState.Animation1)
                        AnimationState = CharacterAnimationState.Animation2;
                    else
                        AnimationState = CharacterAnimationState.Animation1;
                }
            }

            // Quit the application, used in fullscreen mode
            if (Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();
        }

        private void SpawnFullCharacters(int NumberOfLines)
        {
            // Obtain the spawing positions of the characters
            List<Vector3> PositionsBuffer = new List<Vector3>();

            for (int SpawnGroups = 0; SpawnGroups < SpawnNodeData.Count; SpawnGroups++)
            {
                for (int i = PreviousSpawningLine; i < PreviousSpawningLine + SpawnNodeData[SpawnGroups].CharacterSpawningData.NumberOfLines; i++)
                {
                    for (int j = 0; j < SpawnNodeData[SpawnGroups].CharacterSpawningData.CharactersPerLine; j++)
                    {
                        // Add these positions into the spawn buffer
                        Vector3 position = SpawnNodeData[SpawnGroups].transform.position - Vector3.forward * i * SpawnNodeData[SpawnGroups].CharacterSpawningData.CharacterSpacing + Vector3.right * j * SpawnNodeData[SpawnGroups].CharacterSpawningData.CharacterSpacing;
                        PositionsBuffer.Add(position);
                    }
                }
            }

            // Instanctate the character that will be skinned into the scene
            GameObject prefab = CharacterPrefab;
            GameObject instantiatedPrefab = Instantiate(prefab);
            Entity prefabEntity = instantiatedPrefab.GetComponent<CG_CharacterSkinner>().entity;

            for (int t = 0; t < PositionsBuffer.Count; t++)
            {
                // Get the current position to spawn
                Vector3 position = PositionsBuffer[t];
                Entity CharacterEntity = EntityManager.Instantiate(prefabEntity);

                // Set the character positional data
                CharacterTransformData CharacterTransformationData = EntityManager.GetComponentData<CharacterTransformData>(CharacterEntity);
                CharacterTransformationData.Position = position;
                CharacterTransformationData.Forward = transform.forward;

                // Set the transform component of this new entitiy
                EntityManager.SetComponentData(CharacterEntity, CharacterTransformationData);

                CharacterCount++;

            }
            // Destroy the instanciated prefab, change the spawning line and update the character count
            Destroy(instantiatedPrefab);
            PreviousSpawningLine += NumberOfLines;
        }

        // Instanciate a spawn node, used in the editor script
        public void CreateSpawnNode()
        {
            CG_SpawnNode gameObjectTemp;

            gameObjectTemp = Instantiate(SpawnNodeGameObject, Vector3.zero, Quaternion.identity);

            EditorSpawnNodeData.Add(gameObjectTemp);
        }

        public void UpdateGUI()
        {
            List<CG_SpawnNode> TempSpawnNodeData = new List<CG_SpawnNode>();

            for (int i = 0; i < SpawnNodeData.Count; i++)
            {
                if (SpawnNodeData[i])
                {
                    TempSpawnNodeData.Add(SpawnNodeData[i]);
                }
            }

            SpawnNodeData.Clear();
            SpawnNodeData = TempSpawnNodeData;
        }

        // Return the character prefab
        public GameObject GetCharacterPrefab()
        {
            return CharacterPrefab;
        }

        // The display for the amount of characters on screen
        void OnGUI()
        {
            GUI.Box(new Rect(0, 35, 170, 30), "Characters: " + CharacterCount.ToString(), GUIFont);
            GUI.Box(new Rect(0, 70, 170, 30), "'T' - More Characters 'Y' - Change Animation", GUIFont);

            // Only used for the EXE
            //GUI.Box(new Rect(0, 105, 170, 30), "Press Esc to exit", GUIFont);
        }
    }
}

