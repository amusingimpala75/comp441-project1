using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEditor.EditorTools;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class MainSceneManagerScript : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private Transform _playerTransform;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private float tileSize;
    [SerializeField] public float gameSpeed;
    private int playerPosition = 0;
    private float scale = 0f;
    private int numFalling = 0;
    private int score = 0;
    private const int ScoreIncrementUnit = 5; 
    private bool running = true;
    private List<GameObject>[] columns = {new(), new(), new(), new()};

    // Audio
    AudioSource _audioSource; 
    public AudioClip moveSound, swapSound, matchSound, eggSound; 

    // For now just colors but we'll want to have a list of image sprites later on
    private static readonly Color[] COLORS = {Color.red, Color.blue, Color.yellow, Color.purple, Color.orange};

    private void Start()
    {
        _playerTransform = player.transform;
        scale = _playerTransform.localScale.x * 0.5f;
        SpawnBlocks();

        _audioSource = GetComponent<AudioSource>(); 
    }

    private void SpawnBlocks()
    {
        if (!running)
        {
            return;
        }
        // Get random two columns without replacement
        int firstCol = Random.Range(0, 4);
        int secondCol = Random.Range(0, 3);
        if (secondCol >= firstCol)
        {
            secondCol += 1;
        }
        numFalling = 2;
        SpawnBlock(firstCol);
        SpawnBlock(secondCol);
    }

    private void SpawnBlock(int col)
    {
        float pos = col - 1.5f;
        GameObject obj = Instantiate(blockPrefab, new Vector2(pos * scale, 4), Quaternion.identity);
        int colorIdx = Random.Range(0, COLORS.Length);
        obj.tag = $"block-{colorIdx}";
        obj.GetComponent<SpriteRenderer>().color = COLORS[colorIdx];
        BlockScript block = obj.GetComponent<BlockScript>();
        block.manager = this;
        block.column = col;
    }

    private void Update()
    {
        _playerTransform.position = (Vector2.down * 4) + (Vector2.right * (playerPosition * scale));
    }

    public void OnMove(InputValue input)
    {
        int direction = Mathf.RoundToInt(input.Get<float>());
        playerPosition = Mathf.Clamp(playerPosition + direction, -1, 1);

        _audioSource.PlayOneShot(moveSound); 
    }

    // [TODO]: fix if swap would place column inside currently-falling block
    public void OnSwap(InputValue input)
    {
        if (input.isPressed)
        {
            int leftIdx = playerPosition + 1;
            int rightIdx = playerPosition + 2;
            List<GameObject> left = columns[leftIdx];
            List<GameObject> right = columns[rightIdx];
            foreach (GameObject block in left)
            {
                block.transform.position = block.transform.position + (Vector3.right * scale);
            }
            foreach (GameObject block in right)
            {
                block.transform.position = block.transform.position + (Vector3.left * scale);
            }
            columns[leftIdx] = right;
            columns[rightIdx] = left;

            _audioSource.PlayOneShot(swapSound); 
        }
    }

    public void StopBlock(GameObject block, int column)
    {
        float height = block.transform.position.y;
        if (columns[column].Count() > 0 && columns[column].Last().tag == block.tag)
        {
            DestroyMatchBlock(block, column); 
        }
        // [TODO]: Egg match logic
        else
        {
            columns[column].Add(block);
        }
        numFalling--;
        if (height >= 3)
        {
            running = false;
        }
        else if (numFalling == 0)
        {
            SpawnBlocks();
        }
    }

    /// <summary>
    /// Destroy consecutive matching blocks in the same column
    /// </summary>
    /// <param name="block"></param>
    /// <param name="column"></param>
    private void DestroyMatchBlock(GameObject block, int column)
    {
        Destroy(block);
        GameObject prev = columns[column].Last();
        columns[column].Remove(prev);
        Destroy(prev);

        UpdateScore(); 

        _audioSource.PlayOneShot(matchSound); 
    }

    /// <summary>
    /// Destroy top and bottom eggshells, as well as everything in between
    /// </summary>
    /// <param name="column"></param>
    private void DestroyEgg(int column)
    {
        // [TODO]: Logic and audio
        int numBlocksBetween = 1; 

        UpdateScore(2 * ScoreIncrementUnit * numBlocksBetween); 

        _audioSource.PlayOneShot(eggSound); 
    }

    /// <summary>
    /// Update score and scoreText
    /// </summary>
    /// <param name="howMuch">How much to add to the score. Default 5</param>
    private void UpdateScore(int howMuch = ScoreIncrementUnit)
    {
        score += howMuch;
        scoreText.text = $"Score: {score}";
    }
}
