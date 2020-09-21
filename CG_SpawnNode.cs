using UnityEngine;

namespace CG_CharacterSkinner
{
    // A structure to hold the data of all the user defineable variables
    [System.Serializable]
    public struct SpawningData
    {
        // User customisable variables for creating a crowd
        [Header("How many characters do you want to appear per line?")]
        public int CharactersPerLine;

        [Header("How many lines do you want to spawn at once?")]
        public int NumberOfLines;

        [Header("What distance should the characters spawn apart?")]
        public float CharacterSpacing;
    };

    public class CG_SpawnNode : MonoBehaviour
    {
        [Header("The spawning details for the characters")]
        public SpawningData CharacterSpawningData;

        private int PreviousSpawningLine;

        // Use this for initialization
        void Start()
        {
            // Hide the node at runtime
            MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
            renderer.enabled = false;
        }

        // This is used to determine where the characters will spawn as a visual guide
        private void OnDrawGizmos()
        {
            Color newColor = new Color(1, 0, 0, .25f);
            for (int i = PreviousSpawningLine; i < PreviousSpawningLine + CharacterSpawningData.NumberOfLines; i++)
            {
                for (int j = 0; j < CharacterSpawningData.CharactersPerLine; j++)
                {
                    Vector3 position = transform.position - Vector3.forward * i * CharacterSpawningData.CharacterSpacing + Vector3.right * j * CharacterSpawningData.CharacterSpacing;

                    Gizmos.DrawWireCube(position, new Vector3(CharacterSpawningData.CharacterSpacing, CharacterSpawningData.CharacterSpacing, CharacterSpawningData.CharacterSpacing));
                }
            }
        }
    }
}