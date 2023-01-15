using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public Text gameStateText;

    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private Board board;
    private Cell[,] state;
    private bool gameover;

    private void OnValidate()
    {
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    void Awake()
    {
        board = GetComponentInChildren<Board>();
    }

    void Start()
    {
        gameStateText.text = "";
        NewGame();
    }

    private void NewGame()
    {
        gameStateText.text = "";
        state = new Cell[width, height];
        gameover = false;

        GenerateCells();
        GenerateMines();
        GenerateNumbers();

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);
        board.Draw(state);
    }

    private void GenerateCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = new Cell();
                cell.position = new Vector3Int(x, y, 0);
                cell.type = Cell.Type.Empty;
                state[x, y] = cell;
            }
        }
    }

    private void GenerateMines()
    {
        GenerateShip(5);
        GenerateShip(4);
        GenerateShip(3);
        GenerateShip(3);
        GenerateShip(2);
    }

    private void GenerateShip(int shipLength)
    {
        int x;
        int y;
        bool reroll = true;
        Vector2Int rotPredicate = RotationCalculation();

        do
        {
            reroll = false;
            x = Random.Range(0, width);
            y = Random.Range(0, height);

            if (state[x, y].type == Cell.Type.Mine)
            {
                reroll = true;
                continue;
            }

            if ((x + (rotPredicate.x * shipLength)) > width - 1 || (x + (rotPredicate.x * shipLength)) < 0 || (y + (rotPredicate.y * shipLength)) > height - 1 || (y + (rotPredicate.y * shipLength)) < 0)
            {
                reroll = true;
                continue;
            }

            for (int i = 0; i < shipLength; i++)
            {
                //Debug.Log($"x = {x + (rotPredicate.x * i)}, y = {y + (rotPredicate.y * i)}"); // Used to tell where our generation code goes wrong
                if (state[x + (rotPredicate.x * i), y + (rotPredicate.y * i)].type == Cell.Type.Mine)
                {
                    reroll = true;
                    continue;
                }
            }
        } while (reroll);

        //Build ship
        //state[x, y].type = Cell.Type.Mine;
        for (int i = 0; i < shipLength; i++)
        {
            state[x + (rotPredicate.x * i), y + (rotPredicate.y * i)].type = Cell.Type.Mine;
        }
    }

    private Vector2Int RotationCalculation()
    {
        int rotation = Random.Range(0, 4);
        Vector2Int rotPredicate = new Vector2Int(0, 0);
        switch (rotation)
        {
            case 0:
                rotPredicate = new Vector2Int(0, 1);
                break;
            case 1:
                rotPredicate = new Vector2Int(0, -1);
                break;
            case 2:
                rotPredicate = new Vector2Int(1, 0);
                break;
            case 3:
                rotPredicate = new Vector2Int(-1, 0);
                break;
            default:
                break;
        }
        return rotPredicate;
    }

    private void GenerateNumbers()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = state[x, y];

                if (cell.type == Cell.Type.Mine)
                {
                    continue;
                }

                cell.number = CountMines(x, y);

                if (cell.number > 0)
                {
                    cell.type = Cell.Type.Number;
                }

                state[x, y] = cell;
            }
        }
    }

    private int CountMines(int cellX, int cellY)
    {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0)
                {
                    continue;
                }

                int x = cellX + adjacentX;
                int y = cellY + adjacentY;

                if (GetCell(x, y).type == Cell.Type.Mine)
                {
                    count += 1;
                }
            }
        }

        return count;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            NewGame();
        }
        if (!gameover)
        {
            if (Input.GetMouseButtonDown(1))
            {
                Flag();
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Reveal();
            }
        }
    }

    private void Flag()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        Cell cell = GetCell(cellPosition.x, cellPosition.y);

        if (cell.type == Cell.Type.Invalid || cell.revealed)
        {
            return;
        }

        cell.flagged = !cell.flagged;
        state[cellPosition.x, cellPosition.y] = cell;
        board.Draw(state);
    }

    private void Reveal()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        Cell cell = GetCell(cellPosition.x, cellPosition.y);

        if (cell.type == Cell.Type.Invalid || cell.revealed || cell.flagged)
        {
            return;
        }

        switch (cell.type)
        {
            case Cell.Type.Empty:
                Flood(cell);
                break;
            case Cell.Type.Mine:
                Explode(cell);
                CheckWinCondition();
                break;
            default:
                cell.revealed = true;
                state[cellPosition.x, cellPosition.y] = cell;
                CheckWinCondition();
                break;
        }

        board.Draw(state);
    }

    private void Flood(Cell cell)
    {
        if (cell.revealed) return;
        if (cell.type == Cell.Type.Mine || cell.type == Cell.Type.Invalid) return;

        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        if (cell.type == Cell.Type.Empty)
        {
            Flood(GetCell(cell.position.x - 1, cell.position.y));
            Flood(GetCell(cell.position.x + 1, cell.position.y));
            Flood(GetCell(cell.position.x, cell.position.y - 1));
            Flood(GetCell(cell.position.x, cell.position.y + 1));
        }
    }

    private void Explode(Cell cell)
    {
        //Debug.Log("Game Over!");
        gameStateText.text = "You lose.";
        gameover = true;

        cell.revealed = true;
        cell.exploded = true;
        state[cell.position.x, cell.position.y] = cell;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cell = state[x, y];
                if (cell.type == Cell.Type.Mine)
                {
                    cell.revealed = true;
                    state[x, y] = cell;
                }
            }
        }
    }

    private void CheckWinCondition()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = state[x, y];

                if (cell.type != Cell.Type.Mine && !cell.revealed)
                {
                    return;
                }
            }
        }

        //Debug.Log("Winner!");
        gameStateText.text = "You win!";
        gameover = true;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = state[x, y];
                if (cell.type == Cell.Type.Mine)
                {
                    cell.flagged = true;
                    state[x, y] = cell;
                }
            }
        }
    }

    private Cell GetCell(int x, int y)
    {
        if (IsValid(x, y))
        {
            return state[x, y];
        }
        else
        {
            return new Cell();
        }
    }

    private bool IsValid(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
