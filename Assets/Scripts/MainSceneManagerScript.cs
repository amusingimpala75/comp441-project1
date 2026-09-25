using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainSceneManagerScript : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private Transform _playerTransform;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private float tileSize;
    [SerializeField] public float gameSpeed;
    private int playerPosition = 0;
    private float scale = 0f;
    private int numFalling = 0;
    private bool running = true;
    private List<GameObject>[] columns = {new(), new(), new(), new()};

    // For now just colors but we'll want to have a list of image sprites later on
    private static readonly Color[] COLORS = {Color.red, Color.blue, Color.yellow, Color.purple, Color.orange};

    private void Start()
    {
        _playerTransform = player.transform;
        scale = _playerTransform.localScale.x * 0.5f;
        SpawnBlocks();
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
        obj.GetComponent<SpriteRenderer>().color = COLORS[Random.Range(0, COLORS.Length)];
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
    }

    public void ColumnAddBlock(GameObject block, int column)
    {
        columns[column].Add(block);
        numFalling--;
        if (block.transform.position.y >= 3)
        {
            running = false;
        }
        else if (numFalling == 0)
        {
            SpawnBlocks();
        }
    }
}
