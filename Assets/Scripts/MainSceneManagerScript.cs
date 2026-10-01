using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEditor.EditorTools;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(AudioSource))]
public class MainSceneManagerScript : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private Transform _playerTransform;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text eggCountText; 
    [SerializeField] private float tileSize;
    [SerializeField] public float gameSpeed;
    private int playerPosition = 0;
    private float scale = 0f;
    private int numFalling = 0;
    private int score = 0;
    private const int ScoreIncrementUnit = 5; 
    private int eggCount = 0; 
    private bool running = true;
    private List<GameObject>[] columns = {new(), new(), new(), new()};
    private List<GameObject> falling = new();

    // Audio
    AudioSource _audioSource; 
    public AudioClip moveSound, swapSound, matchSound, eggSound; 

    // For now just colors but we'll want to have a list of image sprites later on
    [SerializeField] private Sprite sprite1;
    [SerializeField] private Sprite sprite2;
    [SerializeField] private Sprite sprite3;
    [SerializeField] private Sprite sprite4;
    [SerializeField] private Sprite topEgg;
    [SerializeField] private Sprite bottomEgg;
    private Sprite[] sprites = null;
    private string[] spriteTags = new string[]{"sprite1", "sprite2", "sprite3", "sprite4", "topEgg", "bottomEgg"}; 

    private void Start()
    {
        sprites = new Sprite[]{sprite1, sprite2, sprite3, sprite4, topEgg, bottomEgg};

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
        int spriteIdx = Random.Range(0, sprites.Length);
        // [TODO]: Spawn eggshells less often
        obj.tag = spriteTags[spriteIdx]; 
        SpriteRenderer sprite = obj.GetComponentInChildren<SpriteRenderer>();
        sprite.sprite = sprites[spriteIdx];
        // === Begin Chat GPT advised code for scaling sprite to size of collider
        BoxCollider2D collider = obj.GetComponent<BoxCollider2D>();
        Vector2 spriteSize = sprite.sprite.bounds.size;
        Vector2 colliderSize = collider.size;
        sprite.transform.localScale = new Vector3(colliderSize.x / spriteSize.x, colliderSize.y / spriteSize.y, 1f);
        // === End GPT advised code
        BlockScript block = obj.GetComponent<BlockScript>();
        block.manager = this;
        block.column = col;
        falling.Add(obj);
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
                block.transform.position += Vector3.right * scale;
            }
            foreach (GameObject block in right)
            {
                block.transform.position += Vector3.left * scale;
            }
            columns[leftIdx] = right;
            columns[rightIdx] = left;

            foreach (GameObject obj in falling)
            {
                float y = obj.transform.position.y;
                BlockScript block = obj.GetComponent<BlockScript>();
                if (block.column == leftIdx && right.Count() > 0 && right.Last().transform.position.y + scale > y)
                {
                    obj.transform.position += Vector3.right * scale;
                    block.column++;

                }
                else if (block.column == rightIdx && left.Count() > 0 && left.Last().transform.position.y + scale > y)
                {
                    obj.transform.position += Vector3.left * scale;
                    block.column--;
                }
            }

            _audioSource.PlayOneShot(swapSound); 
        }
    }

    public void StopBlock(GameObject block, int column)
    {
        float height = block.transform.position.y;
        falling.Remove(block);

        if (block.tag == "topEgg" && columns[column].FindLastIndex(block => block.tag == "bottomEgg") != -1)
        {
            // If the topEgg column has a bottomEgg
            DestroyEgg(block, column); 
        }
        else if (columns[column].Count() > 0 && columns[column].Last().tag == block.tag && block.tag != "bottomEgg")
        {
            // If blocks that are not eggs match
            DestroyMatchBlock(block, column);  
        }
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
    private void DestroyEgg(GameObject block, int column)
    {
        // Destroy topEgg
        Destroy(block);

        // Remove egg content
        int numBlocksBetween = 0; 
        List<GameObject> currColumn = columns[column]; 
        while (currColumn.Last().tag != "bottomEgg")
        {
            GameObject currBlock = currColumn.Last(); 
            currColumn.Remove(currBlock);
            Destroy(currBlock); 
            numBlocksBetween++; 
        }

        // Remove bottomEgg
        GameObject bottomEgg = currColumn.Last(); 
        currColumn.Remove(bottomEgg); 
        Destroy(bottomEgg); 

        UpdateScore(2 * ScoreIncrementUnit * numBlocksBetween); 
        eggCount++; 
        eggCountText.text = $"Egg #: {eggCount.ToString("D2")}";  // pad to 2-digit

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
