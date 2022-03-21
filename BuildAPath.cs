using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Pathfinding;
using UnityEngine.EventSystems;

public class BuildAPath : MonoBehaviour
{
    public static BuildAPath Instance { get; private set; }

    private Vector3 mousePos;
    private Vector3 worldPos;
    private Vector3 tilePos;
    private Vector3 chunkPos;
    private Vector3 nearestChunkWorldPos;

    private Vector3Int chunkTilePos;
    private Vector3Int mousePosInt;
    private Vector3Int oldMousePosInt;
    private Vector3Int nearestChunkTilePos;

    [HideInInspector] public int block = -1;
    private int tempBlock;
    public int pathCost = 200;
    int pathsBought = 0;

    private float nearestChunkDist = 100;

    private bool[] hovering = new bool[7];
    [HideInInspector] public bool scanDone = false;

    public List<Block> savedBlocks = new List<Block>();

    //Tilemaps
    [SerializeField] private Tilemap ground;
    [SerializeField] private Tilemap path;
    [SerializeField] private Tilemap overlay;
    [SerializeField] private Tilemap overstlay;
    [SerializeField] private Tilemap minimap;
    [SerializeField] private Tilemap junkLayer;

    //Center tiles
    [SerializeField] private TileBase groundTile;
    [SerializeField] private TileBase horizontal;
    [SerializeField] private TileBase vertical;
    [SerializeField] private TileBase corner1;
    [SerializeField] private TileBase corner2;
    [SerializeField] private TileBase corner3;
    [SerializeField] private TileBase corner4;

    //Tiles
    [SerializeField] private TileBase centerPathTile1;
    [SerializeField] private TileBase centerPathTile2;
    [SerializeField] private TileBase centerPathTile3;
    [SerializeField] private TileBase topRightInPathTile;
    [SerializeField] private TileBase topLeftInPathTile;
    [SerializeField] private TileBase botRightInPathTile;
    [SerializeField] private TileBase botLeftInPathTile;
    [SerializeField] private TileBase topTile;
    [SerializeField] private TileBase botTile;
    [SerializeField] private TileBase leftTile;
    [SerializeField] private TileBase rightTile;
    [SerializeField] private TileBase topLeftTile;
    [SerializeField] private TileBase topRightTile;
    [SerializeField] private TileBase botLeftTile;
    [SerializeField] private TileBase botRightTile;

    private void Awake()
    {
        Instance = this;

        //Sets all the bools in hovering array to false
        for (int i = 0; i < hovering.Length; i++)
        {
            hovering[i] = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //block gets changed if any of the build buttons gets clicked
        if (block != -1)
        {
            NextBlock();
            ChooseBlock(2);
            GetMouseCoords();
        }
    }

    /// <summary>
    /// Makes it possible to rotate through the different building blocks with a keystroke
    /// </summary>
    void NextBlock()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            for (int i = 0; i < hovering.Length; i++)
            {
                hovering[i] = false;
            }

            block++;

            if (block == 7)
            {
                block = 0;
                overlay.ClearAllTiles();
                overstlay.ClearAllTiles();
            }

            hovering[block] = true;
        }
    }

    /// <summary>
    /// Method to call when path blocks is clicked. Saves the parameter in an int, and sets the correct bool to true.
    /// </summary>
    /// <param name="block"></param>
    public void HoveringTrue(int block)
    {
        if (block == 6)
        {
            this.block = block;
            hovering[block] = true;
        }
        else if (Currencies.Instance.coins >= pathCost)
        {
            this.block = block;
            hovering[block] = true;
        }
    }

    /// <summary>
    /// Calculates the mouse pos to grid space and saves in MousePosInt
    /// </summary>
    void GetMouseCoords()
    {
        //Gets mouse pos
        mousePos = Input.mousePosition;
        //Converts to world space
        worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        //Converts to grid space
        tilePos = ground.WorldToCell(worldPos);

        //Saves the grid coords in a Vector3Int
        mousePosInt.x = (int)tilePos.x;
        mousePosInt.y = (int)tilePos.y;
        mousePosInt.z = 0;
    }

    /// <summary>
    /// Calculates which chunk is the nearest and saves in the relevant variables
    /// </summary>
    void GetNearestChunk()
    {
        nearestChunkDist = 100;

        for (int i = 0; i < Chunks.Instance.chunkCenters.Length; i++)
        {
            chunkPos = Chunks.Instance.chunkCenters[i].position;
            chunkTilePos = ground.WorldToCell(chunkPos);

            float dist = Vector3Int.Distance(mousePosInt, chunkTilePos);

            if (dist < nearestChunkDist)
            {
                nearestChunkDist = dist;
                nearestChunkTilePos = chunkTilePos;
                nearestChunkWorldPos = ground.CellToWorld(nearestChunkTilePos);
            }
        }
    }

    /// <summary>
    /// Finds out which bool is true, and calls the correct path method
    /// </summary>
    /// <param name="type">Input 1 for placement, 2 for overlay</param>
    void ChooseBlock(int type)
    {
        if (hovering[0] == true)
        {
            VerticalPath(type, nearestChunkTilePos);
        }
        else if (hovering[1] == true)
        {
            HorizontalPath(type, nearestChunkTilePos);
        }
        else if (hovering[2] == true)
        {
            Corner1Path(type, nearestChunkTilePos);
        }
        else if (hovering[3] == true)
        {
            Corner2Path(type, nearestChunkTilePos);
        }
        else if (hovering[4] == true)
        {
            Corner3Path(type, nearestChunkTilePos);
        }
        else if (hovering[5] == true)
        {
            Corner4Path(type, nearestChunkTilePos);
        }
        else if (hovering[6] == true)
        {
            DynamicGround(type, nearestChunkTilePos);
        }
    }

    /// <summary>
    /// Updates overlay, places block if left mouse button is clicked, cancels block if right mouse button is clicked
    /// </summary>
    void PlaceBlock()
    {
        //Cleans overlay tilemaps if mouse has moved and calculates nearest chunk
        if (oldMousePosInt != mousePosInt)
        {
            overlay.ClearAllTiles();
            overstlay.ClearAllTiles();
            GetNearestChunk();
        }

        //Place block
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            SavePreviousBlock();

            if (block != 6)
            {
                Currencies.Instance.RemoveCoins(pathCost);
                UIHandler.Instance.AffordPaths();
            }

            ChooseBlock(1);
            overlay.ClearAllTiles();
            overstlay.ClearAllTiles();
            tempBlock = block;
            hovering[block] = false;
            block = -1;

            if (Input.GetKey(KeyCode.LeftControl))
            {
                HoveringTrue(tempBlock);
            }
        }
        //Checks if mouse is over another button
        else if (Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject())
        {
            overlay.ClearAllTiles();
            overstlay.ClearAllTiles();
            hovering[block] = false;
            ChooseBlock(2);
        }

        //Cancel block
        if (Input.GetMouseButtonDown(1))
        {
            overlay.ClearAllTiles();
            overstlay.ClearAllTiles();
            hovering[block] = false;
            block = -1;
        }

        //Updates mouse position
        oldMousePosInt = mousePosInt;
    }

    /// <summary>
    /// Gets called everytime a block is placed, saves the previous block in the location.
    /// </summary>
    void SavePreviousBlock()
    {
        Block tempBlock = new Block();
        tempBlock.pos = nearestChunkTilePos;

        foreach (Block chunk in savedBlocks)
        {
            if (chunk.pos == tempBlock.pos)
            {
                return;
            }
        }

        if (MapManager.Instance.GetTileType(path, nearestChunkWorldPos) != "null")
        {
            tempBlock.type = MapManager.Instance.GetTileType(path, nearestChunkWorldPos);
            Debug.Log(tempBlock.type);
        }
        else if (MapManager.Instance.GetTileType(ground, nearestChunkWorldPos) != "null")
        {
            tempBlock.type = MapManager.Instance.GetTileType(ground, nearestChunkWorldPos);
            Debug.Log(tempBlock.type);
        }

        if (block != 6)
        {
            pathsBought++;
        }

        savedBlocks.Add(tempBlock);
    }

    public Tilemap GetGroundMap()
    {
        return ground;
    }

    public Tilemap GetPathMap()
    {
        return path;
    }

    /// <summary>
    /// Gets called if player doesnt have enough currency to build the path, reverts all changes that is made.
    /// </summary>
    public void UndoMapChanges()
    {
        foreach (Block block in savedBlocks)
        {
            switch (block.type)
            {
                case "vertical":
                    VerticalPath(1, block.pos);
                    break;

                case "horizontal":
                    HorizontalPath(1, block.pos);
                    break;

                case "corner1":
                    Corner1Path(1, block.pos);
                    break;

                case "corner2":
                    Corner2Path(1, block.pos);
                    break;

                case "corner3":
                    Corner3Path(1, block.pos);
                    break;

                case "corner4":
                    Corner4Path(1, block.pos);
                    break;

                case "ground":
                    for (int i = 2; i > 0; i--)
                    {
                        DynamicGround(i, block.pos);
                    }
                    break;

                default:
                    break;
            }
        }

        for (int i = 0; i < hovering.Length; i++)
        {
            hovering[i] = false;
        }

        block = -1;
        Currencies.Instance.AddCoins(pathsBought * pathCost);
        pathsBought = 0;
        savedBlocks.Clear();
    }

    /// <summary>
    /// Runs an A* scan of the map, finds walkables and non-walkables
    /// </summary>
    public void Scan()
    {
        AstarPath.active.Scan();
        UIHandler.Instance.asm.UpdatePath();
        scanDone = true;
    }

    /// <summary>
    /// Returns true if it finds any of the 4 path tiles
    /// </summary>
    /// <param name="map">Which tilemap to look at</param>
    /// <param name="xoffset">Off set on the x-axis, from the middle</param>
    /// <param name="yoffset">Off set on the y-axis, from the middle</param>
    /// <returns></returns>
    bool CheckForCenterTiles(Tilemap map, int xoffset, int yoffset, Vector3Int pos)
    {
        TileBase tile;

        for (int i = 0; i < 9; i++)
        {
            switch (i)
            {
                case 0:
                    tile = centerPathTile1;
                    break;

                case 1:
                    tile = centerPathTile2;
                    break;

                case 2:
                    tile = centerPathTile3;
                    break;

                case 3:
                    tile = horizontal;
                    break;

                case 4:
                    tile = vertical;
                    break;

                case 5:
                    tile = corner1;
                    break;

                case 6:
                    tile = corner2;
                    break;

                case 7:
                    tile = corner3;
                    break;

                case 8:
                    tile = corner4;
                    break;

                default:
                    tile = corner1;
                    break;
            }

            if (map.GetTile(new Vector3Int(pos.x + xoffset, pos.y + yoffset, 0)) == tile)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates all the borders and corners, places the correct one to match the surronding area
    /// </summary>
    /// <param name="topLayer">Top layer you wanna draw on</param>
    /// <param name="botLayer">Bot layer you wanna draw on</param>
    void GroundMethod(Tilemap topLayer, Tilemap botLayer, int rndTiles, Vector3Int pos)
    {
        //Center ground tiles 3x3
        botLayer.SetTile(pos, groundTile); //Center
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y, 0), groundTile); //Right
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 1, 0), groundTile); //Top right
        botLayer.SetTile(new Vector3Int(pos.x, pos.y + 1, 0), groundTile); //Top
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y, 0), groundTile); //Left
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 1, 0), groundTile); //Top left
        botLayer.SetTile(new Vector3Int(pos.x, pos.y - 1, 0), groundTile); //Bot
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 1, 0), groundTile); //Bot right
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 1, 0), groundTile); //Bot left
        topLayer.SetTile(pos, null); //Center
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y, 0), null); //Right
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 1, 0), null); //Top right
        topLayer.SetTile(new Vector3Int(pos.x, pos.y + 1, 0), null); //Top
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y, 0), null); //Left
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 1, 0), null); //Top left
        topLayer.SetTile(new Vector3Int(pos.x, pos.y - 1, 0), null); //Bot
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 1, 0), null); //Bot right
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 1, 0), null); //Bot left

        //Checks the top tile
        if (CheckForCenterTiles(path, 0, 3, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), topTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x, pos.y + 3, 0)) == botTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x, pos.y + 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x, pos.y + 3, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), null);
        }

        //Checks the top right tile
        if (CheckForCenterTiles(path, 1, 3, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), topTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x + 1, pos.y + 3, 0)) == botTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 3, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), null);
        }

        //Checks the top left tile
        if (CheckForCenterTiles(path, 1, 3, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), topTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x - 1, pos.y + 3, 0)) == botTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 3, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), null);
        }

        //Forces top corners to be ground, if mid is ground
        if (overlay.GetTile(new Vector3Int(pos.x, pos.y + 3, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x + 1, pos.y + 3, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x - 1, pos.y + 3, 0)) == groundTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0), null);
        }

        //Checks the bot tile
        if (CheckForCenterTiles(path, 0, -3, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), botTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x, pos.y - 3, 0)) == topTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x, pos.y - 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x, pos.y - 3, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), null);
        }

        //Checks the bot right tile
        if (CheckForCenterTiles(path, 1, -3, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), botTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x + 1, pos.y - 3, 0)) == topTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 3, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), null);
        }

        //Checks the bot left tile
        if (CheckForCenterTiles(path, 1, -3, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), botTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x - 1, pos.y - 3, 0)) == topTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 3, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), null);
        }

        //Forces bot corners to be ground, if mid is ground
        if (overlay.GetTile(new Vector3Int(pos.x, pos.y - 3, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x + 1, pos.y - 3, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x - 1, pos.y - 3, 0)) == groundTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0), null);
        }

        //Checks the right tile
        if (CheckForCenterTiles(path, 3, 0, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), rightTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x + 3, pos.y, 0)) == leftTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), null);
        }

        //Checks the right top tile
        if (CheckForCenterTiles(path, 3, 1, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), rightTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x + 3, pos.y + 1, 0)) == leftTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y + 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y + 1, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), null);
        }

        //Checks the right bot tile
        if (CheckForCenterTiles(path, 3, -1, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), rightTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x + 3, pos.y - 1, 0)) == leftTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y - 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y - 1, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), null);
        }

        //Forces right corners to be ground, if mid is ground
        if (overlay.GetTile(new Vector3Int(pos.x + 3, pos.y, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x + 3, pos.y - 1, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x + 3, pos.y + 1, 0)) == groundTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0), null);
        }

        //Checks the left tile
        if (CheckForCenterTiles(path, -3, 0, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), leftTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x - 3, pos.y, 0)) == rightTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), null);
        }

        //Checks the left top tile
        if (CheckForCenterTiles(path, -3, 1, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), leftTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x - 3, pos.y + 1, 0)) == rightTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y + 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y + 1, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), null);
        }

        //Checks the left bot tile
        if (CheckForCenterTiles(path, -3, -1, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), leftTile);
        }
        else if (path.GetTile(new Vector3Int(pos.x - 3, pos.y - 1, 0)) == rightTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y - 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y - 1, 0), null);
        }
        else
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), null);
        }

        //Forces left corners to be ground, if mid is ground
        if (overlay.GetTile(new Vector3Int(pos.x - 3, pos.y, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x - 3, pos.y - 1, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x - 3, pos.y + 1, 0)) == groundTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0), null);
        }

        //Checks the top right right corner
        //Checks if its a full ground tile
        if (overlay.GetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0)) == groundTile &&
            ground.GetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0)) == groundTile ||
            overlay.GetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0)) == groundTile &&
            ground.GetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0)) == groundTile ||
            path.GetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0)) == topTile &&
            path.GetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0)) == rightTile ||
            overlay.GetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0)) == groundTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), null);
        }

        //Checks if its a top border
        if (overstlay.GetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0)) == topTile &&
            path.GetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0)) == topTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), topTile);
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
        }

        //Checks if its a right border
        if (overstlay.GetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0)) == rightTile &&
            path.GetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0)) == rightTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), rightTile);
        }

        //Checks if two paths meet
        //if (overstlay.GetTile(new Vector3Int(mousePosInt.x + 2, mousePosInt.y + 1, 0)) == rightTile &&
        //    path.GetTile(new Vector3Int(mousePosInt.x + 3, mousePosInt.y + 2, 0)) == botTile &&
        //    path.GetTile(new Vector3Int(mousePosInt.x + 2, mousePosInt.y + 3, 0)) == leftTile)
        //{
        //    overlay.SetTile(new Vector3Int(mousePosInt.x + 2, mousePosInt.y + 2, 0), crossRoadsTile);
        //}

        //Checks if its an up and left inwards corner
        if (overstlay.GetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0)) == topTile &&
            path.GetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0)) == leftTile ||
            overstlay.GetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0)) == topTile &&
            ground.GetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0)) == groundTile ||
            overstlay.GetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0)) == topTile &&
            path.GetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0)) == topLeftTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), botRightInPathTile); //Needs new tile
        }

        //Checks if its an up and right inwards corner
        if (overstlay.GetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0)) == rightTile &&
            path.GetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0)) == botTile ||
            path.GetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0)) == botRightTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), topLeftInPathTile); //Needs new tile
        }

        //Checks if its an outwards corner
        if (CheckForCenterTiles(path, 3, 2, pos) && CheckForCenterTiles(path, 2, 3, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), topRightTile);
        }

        //Checks the top left left corner
        //Checks if its a full ground tile
        if (overlay.GetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0)) == groundTile &&
            ground.GetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0)) == groundTile ||
            overlay.GetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0)) == groundTile &&
            ground.GetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0)) == groundTile ||
            path.GetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0)) == topTile &&
            path.GetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0)) == leftTile ||
            overlay.GetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0)) == groundTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), null);
        }

        //Checks if its a top border
        if (overstlay.GetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0)) == topTile &&
            path.GetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0)) == topTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), topTile);
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
        }

        //Checks if its a right border
        if (overstlay.GetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0)) == leftTile &&
            path.GetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0)) == leftTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), leftTile);
        }

        //Checks if its an down and right inwards corner
        if (overstlay.GetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0)) == topTile &&
            path.GetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0)) == rightTile ||
            overstlay.GetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0)) == topTile &&
            ground.GetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0)) == groundTile ||
            overstlay.GetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0)) == topTile &&
            path.GetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0)) == topRightTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), botLeftInPathTile); //Needs new tile
        }

        //Checks if its an up and left inwards corner
        if (overstlay.GetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0)) == leftTile &&
            path.GetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0)) == botTile ||
            path.GetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0)) == botLeftTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), topRightInPathTile); //Needs new tile
        }

        //Checks if its an outwards corner
        if (CheckForCenterTiles(path, -3, 2, pos) && CheckForCenterTiles(path, -2, 3, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), topLeftTile);
        }

        //Checks the bot right right corner
        //Checks if its a full ground tile
        if (overlay.GetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0)) == groundTile &&
            ground.GetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0)) == groundTile ||
            overlay.GetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0)) == groundTile &&
            ground.GetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0)) == groundTile ||
            path.GetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0)) == rightTile &&
            path.GetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0)) == botTile ||
            overlay.GetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0)) == groundTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), null);
        }

        //Checks if its a bot border
        if (overstlay.GetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0)) == botTile &&
            path.GetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0)) == botTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), botTile);
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
        }

        //Checks if its a right border
        if (overstlay.GetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0)) == rightTile &&
            path.GetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0)) == rightTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), rightTile);
        }

        //Checks if its an down and left inwards corner
        if (overstlay.GetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0)) == botTile &&
            path.GetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0)) == leftTile ||
            overstlay.GetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0)) == botTile &&
            ground.GetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0)) == groundTile ||
            overstlay.GetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0)) == botTile &&
            path.GetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0)) == botLeftTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), topRightInPathTile);
        }

        //Checks if its an up and right inwards corner
        if (overstlay.GetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0)) == rightTile &&
            path.GetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0)) == topTile ||
            path.GetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0)) == topRightTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), botLeftInPathTile);
        }

        //Checks if its an outwards corner
        if (CheckForCenterTiles(path, 2, -2, pos) && CheckForCenterTiles(path, 2, -3, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), botRightTile);
        }

        //Checks the bot left left corner
        //Checks if its a full ground tile
        if (overlay.GetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0)) == groundTile &&
            ground.GetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0)) == groundTile ||
            overlay.GetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0)) == groundTile &&
            ground.GetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0)) == groundTile ||
            path.GetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0)) == leftTile &&
            path.GetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0)) == botTile ||
            overlay.GetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0)) == groundTile &&
            overlay.GetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0)) == groundTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), groundTile);
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), null);
        }

        //Checks if its a bot border
        if (overstlay.GetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0)) == botTile &&
            path.GetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0)) == botTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), botTile);
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
        }

        //Checks if its a left border
        if (overstlay.GetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0)) == leftTile &&
            path.GetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0)) == leftTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), leftTile);
        }

        //Checks if its an down and right inwards corner
        if (overstlay.GetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0)) == botTile &&
            path.GetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0)) == rightTile ||
            overstlay.GetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0)) == botTile &&
            ground.GetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0)) == groundTile ||
            overstlay.GetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0)) == botTile &&
            path.GetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0)) == botRightTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), topLeftInPathTile); //Needs new tile
        }

        //Checks if its an up and left inwards corner
        if (overstlay.GetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0)) == leftTile &&
            path.GetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0)) == topTile ||
            path.GetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0)) == topLeftTile)
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), botRightInPathTile); //Needs new tile
        }

        //Checks if its an outwards corner
        if (CheckForCenterTiles(path, -2, -3, pos) && CheckForCenterTiles(path, -3, -2, pos))
        {
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), botLeftTile);
        }
    }

    /// <summary>
    /// Dynamic ground tile that calculates its own borders
    /// </summary>
    /// <param name="type">Input 1 for real placement, or 2 for overlay</param>
    public void DynamicGround(int type, Vector3Int pos)
    {
        if (type == 1)
        {
            GroundMethod(path, ground, 0, pos);
            SetTilesNull(minimap, pos);
            SetTilesNull(junkLayer, pos);
        }
        else if (type == 2)
        {
            GroundMethod(overstlay, overlay, 1, pos);
            PlaceBlock();
        }
    }

    void SetTilesNull(Tilemap map, Vector3Int pos)
    {
        for (int i = -2; i < 3; i++) // x
        {
            for (int j = -2; j < 3; j++) // y
            {
                map.SetTile(new Vector3Int(pos.x + i, pos.y + j, 0), null);
            }
        }
    }

    TileBase PickCenterTile(int rnd)
    {
        if (rnd == 0)
        {
            rnd = Random.Range(1, 3);
        }

        switch (rnd)
        {
            case 1:
                return centerPathTile1;

            case 2:
                return centerPathTile2;

            case 3:
                return centerPathTile3;

            default:
                return centerPathTile1;
        }
    }

    /// <summary>
    /// Draws the center tiles
    /// </summary>
    /// <param name="topLayer">Top layer you wanna draw on</param>
    /// <param name="botLayer">Bot layer you wanna draw on</param>
    void CenterPathTiles(Tilemap topLayer, Tilemap botLayer, int rndTiles, Vector3Int pos, string type)
    {
        //Center path tiles 3x3
        botLayer.SetTile(pos, null); //Center
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y, 0), null); //Right
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 1, 0), null); //Top right
        botLayer.SetTile(new Vector3Int(pos.x, pos.y + 1, 0), null); //Top
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y, 0), null); //Left
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 1, 0), null); //Top left
        botLayer.SetTile(new Vector3Int(pos.x, pos.y - 1, 0), null); //Bot
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 1, 0), null); //Bot right
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 1, 0), null); //Bot left
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y, 0), PickCenterTile(rndTiles)); //Right
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 1, 0), PickCenterTile(rndTiles)); //Top right
        topLayer.SetTile(new Vector3Int(pos.x, pos.y + 1, 0), PickCenterTile(rndTiles)); //Top
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y, 0), PickCenterTile(rndTiles)); //Left
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 1, 0), PickCenterTile(rndTiles)); //Top left
        topLayer.SetTile(new Vector3Int(pos.x, pos.y - 1, 0), PickCenterTile(rndTiles)); //Bot
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 1, 0), PickCenterTile(rndTiles)); //Bot right
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 1, 0), PickCenterTile(rndTiles)); //Bot left

        switch (type)
        {
            case "horizontal":
                topLayer.SetTile(pos, horizontal); //Center
                break;

            case "vertical":
                topLayer.SetTile(pos, vertical); //Center
                break;

            case "corner1":
                topLayer.SetTile(pos, corner1); //Center
                break;

            case "corner2":
                topLayer.SetTile(pos, corner2); //Center
                break;

            case "corner3":
                topLayer.SetTile(pos, corner3); //Center
                break;

            case "corner4":
                topLayer.SetTile(pos, corner4); //Center
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Draws the center tiles on the top
    /// </summary>
    /// <param name="topLayer">Top layer you wanna draw on</param>
    /// <param name="botLayer">Bot layer you wanna draw on</param>
    void CenterTilesTop(Tilemap topLayer, Tilemap botLayer, int rndTiles, Vector3Int pos)
    {
        //Extra center tiles on top
        botLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), PickCenterTile(rndTiles));
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), PickCenterTile(rndTiles));
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), PickCenterTile(rndTiles));
    }

    /// <summary>
    /// Draws the center tiles on the bot
    /// </summary>
    /// <param name="topLayer">Top layer you wanna draw on</param>
    /// <param name="botLayer">Bot layer you wanna draw on</param>
    void CenterTilesBot(Tilemap topLayer, Tilemap botLayer, int rndTiles, Vector3Int pos)
    {
        //Extra center tiles on bot
        botLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), PickCenterTile(rndTiles));
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), PickCenterTile(rndTiles));
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), PickCenterTile(rndTiles));
    }

    /// <summary>
    /// Draws the center tiles on the right
    /// </summary>
    /// <param name="topLayer">Top layer you wanna draw on</param>
    /// <param name="botLayer">Bot layer you wanna draw on</param>
    void CenterTilesRight(Tilemap topLayer, Tilemap botLayer, int rndTiles, Vector3Int pos)
    {
        //Extra center tiles on right
        botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), PickCenterTile(rndTiles));
        botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), PickCenterTile(rndTiles));
        botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), PickCenterTile(rndTiles));
    }

    /// <summary>
    /// Draws the center tiles on the left
    /// </summary>
    /// <param name="topLayer">Top layer you wanna draw on</param>
    /// <param name="botLayer">Bot layer you wanna draw on</param>
    void CenterTilesLeft(Tilemap topLayer, Tilemap botLayer, int rndTiles, Vector3Int pos)
    {
        //Extra center tiles on left
        botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), PickCenterTile(rndTiles));
        botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), PickCenterTile(rndTiles));
        botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), null);
        topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), PickCenterTile(rndTiles));
    }

    /// <summary>
    /// Draws the top border, only border tiles and corners if correct string is input
    /// </summary>
    /// <param name="topLayer">The top layer you wanna draw on</param>
    /// <param name="botLayer">The bot layer you wanna draw on</param>
    /// <param name="corner">Input "TopRightIn" or "TopLeftIn" to get a either of the corners</param>
    void TopPathBorder(Tilemap topLayer, Tilemap botLayer, string corner, int rndTiles, Vector3Int pos)
    {
        //Top border
        botLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x, pos.y + 2, 0), botTile);
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 2, 0), botTile);
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 2, 0), botTile);

        switch (corner)
        {
            case "TopRightIn":
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), topRightInPathTile);
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), botTile);
                break;

            case "TopLeftIn":
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), botTile);
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), topLeftInPathTile);
                break;

            default:
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), botTile);
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), botTile);
                break;
        }

        if (path.GetTile(new Vector3Int(pos.x, pos.y + 3, 0)) == botTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x, pos.y + 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x, pos.y + 3, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x + 1, pos.y + 3, 0)) == botTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y + 3, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x - 1, pos.y + 3, 0)) == botTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y + 3, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0)) == topLeftInPathTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 3, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0)) == topRightInPathTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 3, 0), groundTile);
        }
    }

    /// <summary>
    /// Draws the bot border, only border tiles and corners if correct string is input
    /// </summary>
    /// <param name="topLayer">The top layer you wanna draw on</param>
    /// <param name="botLayer">The bot layer you wanna draw on</param>
    /// <param name="corner">Input "BotRightIn" or "BotLeftIn" to get a either of the corners</param>
    void BotPathBorder(Tilemap topLayer, Tilemap botLayer, string corner, int rndTiles, Vector3Int pos)
    {
        //Bot border
        botLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x, pos.y - 2, 0), topTile);
        botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 2, 0), topTile);
        botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 2, 0), topTile);

        switch (corner)
        {
            case "BotRightIn":
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), topTile);
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), botRightInPathTile);
                break;

            case "BotLeftIn":
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), botLeftInPathTile);
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), topTile);
                break;

            default:
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), topTile);
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), topTile);
                break;
        }

        if (path.GetTile(new Vector3Int(pos.x, pos.y - 3, 0)) == topTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x, pos.y - 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x, pos.y - 3, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x + 1, pos.y - 3, 0)) == topTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 1, pos.y - 3, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x - 1, pos.y - 3, 0)) == topTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 1, pos.y - 3, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0)) == botLeftInPathTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 3, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0)) == botRightInPathTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 3, 0), groundTile);
        }
    }

    /// <summary>
    /// Draws the right border, only border tiles
    /// </summary>
    /// <param name="topLayer">The top layer you wanna draw on</param>
    /// <param name="botLayer">The bot layer you wanna draw on</param>
    /// <param name="corner">Input "Top" or "Bot" to skip drawing the other</param>
    void RightPathBorder(Tilemap topLayer, Tilemap botLayer, string corner, int rndTiles, Vector3Int pos)
    {
        //Right border
        topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), leftTile);
        botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), leftTile);
        botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 1, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), leftTile);
        botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 1, 0), PickCenterTile(rndTiles));

        switch (corner)
        {
            case "Top":
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), leftTile);
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                break;

            case "Bot":
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), leftTile);
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                break;

            default:
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), leftTile);
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), leftTile);
                botLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                break;
        }

        if (path.GetTile(new Vector3Int(pos.x + 3, pos.y, 0)) == leftTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x + 3, pos.y + 1, 0)) == leftTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y + 1, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y + 1, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x + 3, pos.y - 1, 0)) == leftTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y - 1, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y - 1, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0)) == topRightInPathTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y + 2, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0)) == botRightInPathTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x + 3, pos.y - 2, 0), groundTile);
        }
    }

    /// <summary>
    /// Draws the left border, only border tiles
    /// </summary>
    /// <param name="topLayer">The top layer you wanna draw on</param>
    /// <param name="botLayer">The bot layer you wanna draw on</param>
    /// <param name="corner">Input "Top" or "Bot" to skip drawing the other</param>
    void LeftPathBorder(Tilemap topLayer, Tilemap botLayer, string corner, int rndTiles, Vector3Int pos)
    {
        topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), rightTile);
        botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), rightTile);
        botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 1, 0), PickCenterTile(rndTiles));
        topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), rightTile);
        botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 1, 0), PickCenterTile(rndTiles));

        switch (corner)
        {
            case "Top":
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), rightTile);
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                break;

            case "Bot":
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), rightTile);
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                break;

            default:
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), rightTile);
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(rndTiles));
                topLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), rightTile);
                botLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(rndTiles));
                break;
        }

        if (path.GetTile(new Vector3Int(pos.x - 3, pos.y, 0)) == rightTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x - 3, pos.y + 1, 0)) == rightTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y + 1, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y + 1, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x - 3, pos.y - 1, 0)) == rightTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y - 1, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y - 1, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0)) == topLeftInPathTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y + 2, 0), groundTile);
        }

        if (path.GetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0)) == botLeftInPathTile)
        {
            topLayer.SetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0), null);
            botLayer.SetTile(new Vector3Int(pos.x - 3, pos.y - 2, 0), groundTile);
        }
    }

    /// <summary>
    /// Draws a path that goes Vertical
    /// </summary>
    /// <param name="type">Input 1 for actual placement on mouse pos, input 2 for overlay</param>
    public void VerticalPath(int type, Vector3Int pos)
    {
        if (type == 1)
        {
            CenterPathTiles(path, ground, 0, pos, "vertical");
            CenterTilesTop(path, ground, 0, pos);
            CenterTilesBot(path, ground, 0, pos);
            RightPathBorder(path, ground, "none", 0, pos);
            LeftPathBorder(path, ground, "none", 0, pos);

            CenterPathTiles(minimap, junkLayer, 0, pos, "vertical");
            CenterTilesTop(minimap, junkLayer, 0, pos);
            CenterTilesBot(minimap, junkLayer, 0, pos);
            RightPathBorder(minimap, junkLayer, "none", 0, pos);
            LeftPathBorder(minimap, junkLayer, "none", 0, pos);
        }

        if (type == 2)
        {
            CenterPathTiles(overstlay, overlay, 1, pos, "vertical");
            CenterTilesTop(overstlay, overlay, 1, pos);
            CenterTilesBot(overstlay, overlay, 1, pos);
            RightPathBorder(overstlay, overlay, "none", 1, pos);
            LeftPathBorder(overstlay, overlay, "none", 1, pos);
            PlaceBlock();
        }
    }

    /// <summary>
    /// Draws a path that goes Horizontal
    /// </summary>
    /// <param name="type">Input 1 for actual placement on mouse pos, input 2 for overlay</param>
    public void HorizontalPath(int type, Vector3Int pos)
    {
        if (type == 1)
        {
            CenterPathTiles(path, ground, 0, pos, "horizontal");
            TopPathBorder(path, ground, "none", 0, pos);
            BotPathBorder(path, ground, "none", 0, pos);
            CenterTilesLeft(path, ground, 0, pos);
            CenterTilesRight(path, ground, 0, pos);

            CenterPathTiles(minimap, junkLayer, 0, pos, "horizontal");
            TopPathBorder(minimap, junkLayer, "none", 0, pos);
            BotPathBorder(minimap, junkLayer, "none", 0, pos);
            CenterTilesLeft(minimap, junkLayer, 0, pos);
            CenterTilesRight(minimap, junkLayer, 0, pos);
        }
        else if (type == 2)
        {
            CenterPathTiles(overstlay, overlay, 1, pos, "horizontal");
            TopPathBorder(overstlay, overlay, "none", 1, pos);
            BotPathBorder(overstlay, overlay, "none", 1, pos);
            CenterTilesRight(overstlay, overlay, 1, pos);
            CenterTilesLeft(overstlay, overlay, 1, pos);
            PlaceBlock();
        }
    }

    /// <summary>
    /// Draws a path that goes UpAndLeft
    /// </summary>
    /// <param name="type">Input 1 for actual placement on mouse pos, input 2 for overlay</param>
    public void Corner1Path(int type, Vector3Int pos)
    {
        if (type == 1)
        {
            CenterPathTiles(path, ground, 0, pos, "corner1");
            CenterTilesBot(path, ground, 0, pos);
            CenterTilesLeft(path, ground, 0, pos);
            TopPathBorder(path, ground, "TopRightIn", 0, pos);
            RightPathBorder(path, ground, "Bot", 0, pos);

            //Bot left corner
            path.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), topRightTile);
            ground.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(0));

            CenterPathTiles(minimap, junkLayer, 0, pos, "corner1");
            CenterTilesBot(minimap, junkLayer, 0, pos);
            CenterTilesLeft(minimap, junkLayer, 0, pos);
            TopPathBorder(minimap, junkLayer, "TopRightIn", 0, pos);
            RightPathBorder(minimap, junkLayer, "Bot", 0, pos);

            //Bot left corner
            minimap.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), topRightTile);
            junkLayer.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(0));
        }
        else if (type == 2)
        {
            CenterPathTiles(overstlay, overlay, 1, pos, "corner1");
            CenterTilesBot(overstlay, overlay, 1, pos);
            CenterTilesLeft(overstlay, overlay, 1, pos);
            TopPathBorder(overstlay, overlay, "TopRightIn", 1, pos);
            RightPathBorder(overstlay, overlay, "Bot", 1, pos);

            //Bot left corner
            overstlay.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), topRightTile);
            overlay.SetTile(new Vector3Int(pos.x - 2, pos.y - 2, 0), PickCenterTile(1));

            PlaceBlock();
        }
    }

    /// <summary>
    /// Draws a path that goes UpAndRight
    /// </summary>
    /// <param name="type">Input 1 for actual placement on mouse pos, input 2 for overlay</param>
    public void Corner2Path(int type, Vector3Int pos)
    {
        if (type == 1)
        {
            CenterPathTiles(path, ground, 0, pos, "corner2");
            CenterTilesBot(path, ground, 0, pos);
            CenterTilesRight(path, ground, 0, pos);
            TopPathBorder(path, ground, "TopLeftIn", 0, pos);
            LeftPathBorder(path, ground, "Bot", 0, pos);

            //Bot right corner
            ground.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(0));
            path.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), topLeftTile);

            CenterPathTiles(minimap, junkLayer, 0, pos, "corner2");
            CenterTilesBot(minimap, junkLayer, 0, pos);
            CenterTilesRight(minimap, junkLayer, 0, pos);
            TopPathBorder(minimap, junkLayer, "TopLeftIn", 0, pos);
            LeftPathBorder(minimap, junkLayer, "Bot", 0, pos);

            //Bot right corner
            junkLayer.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(0));
            minimap.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), topLeftTile);
        }
        else if (type == 2)
        {
            CenterPathTiles(overstlay, overlay, 1, pos, "corner2");
            CenterTilesBot(overstlay, overlay, 1, pos);
            CenterTilesRight(overstlay, overlay, 1, pos);
            TopPathBorder(overstlay, overlay, "TopLeftIn", 1, pos);
            LeftPathBorder(overstlay, overlay, "Bot", 1, pos);

            //Bot right corner
            overlay.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), PickCenterTile(1));
            overstlay.SetTile(new Vector3Int(pos.x + 2, pos.y - 2, 0), topLeftTile);

            PlaceBlock();
        }
    }

    /// <summary>
    /// Draws a path that goes LeftAndUp
    /// </summary>
    /// <param name="type">Input 1 for actual placement on mouse pos, input 2 for overlay</param>
    public void Corner3Path(int type, Vector3Int pos)
    {
        if (type == 1)
        {
            CenterPathTiles(path, ground, 0, pos, "corner3");
            CenterTilesTop(path, ground, 0, pos);
            CenterTilesRight(path, ground, 0, pos);
            BotPathBorder(path, ground, "BotLeftIn", 0, pos);
            LeftPathBorder(path, ground, "Top", 0, pos);

            //Top right corner
            ground.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(0));
            path.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), botLeftTile);

            CenterPathTiles(minimap, junkLayer, 0, pos, "corner3");
            CenterTilesTop(minimap, junkLayer, 0, pos);
            CenterTilesRight(minimap, junkLayer, 0, pos);
            BotPathBorder(minimap, junkLayer, "BotLeftIn", 0, pos);
            LeftPathBorder(minimap, junkLayer, "Top", 0, pos);

            //Top right corner
            junkLayer.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(0));
            minimap.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), botLeftTile);
        }
        else if (type == 2)
        {
            CenterPathTiles(overstlay, overlay, 1, pos, "corner3");
            CenterTilesTop(overstlay, overlay, 1, pos);
            CenterTilesRight(overstlay, overlay, 1, pos);
            BotPathBorder(overstlay, overlay, "BotLeftIn", 1, pos);
            LeftPathBorder(overstlay, overlay, "Top", 1, pos);

            //Top right corner
            overlay.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), PickCenterTile(1));
            overstlay.SetTile(new Vector3Int(pos.x + 2, pos.y + 2, 0), botLeftTile);

            PlaceBlock();
        }
    }

    /// <summary>
    /// Draws a path that goes DownAndLeft
    /// </summary>
    /// <param name="type">Input 1 for actual placement on mouse pos, input 2 for overlay</param>
    public void Corner4Path(int type, Vector3Int pos)
    {
        if (type == 1)
        {
            CenterPathTiles(path, ground, 0, pos, "corner4");
            CenterTilesTop(path, ground, 0, pos);
            CenterTilesLeft(path, ground, 0, pos);
            RightPathBorder(path, ground, "Top", 0, pos);
            BotPathBorder(path, ground, "BotRightIn", 0, pos);

            //Top left corner
            ground.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(0));
            path.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), botRightTile);

            CenterPathTiles(minimap, junkLayer, 0, pos, "corner4");
            CenterTilesTop(minimap, junkLayer, 0, pos);
            CenterTilesLeft(minimap, junkLayer, 0, pos);
            RightPathBorder(minimap, junkLayer, "Top", 0, pos);
            BotPathBorder(minimap, junkLayer, "BotRightIn", 0, pos);

            //Top left corner
            junkLayer.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(0));
            minimap.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), botRightTile);
        }
        else if (type == 2)
        {
            CenterPathTiles(overstlay, overlay, 1, pos, "corner4");
            CenterTilesTop(overstlay, overlay, 1, pos);
            CenterTilesLeft(overstlay, overlay, 1, pos);
            RightPathBorder(overstlay, overlay, "Top", 1, pos);
            BotPathBorder(overstlay, overlay, "BotRightIn", 1, pos);

            //Top left corner
            overlay.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), PickCenterTile(1));
            overstlay.SetTile(new Vector3Int(pos.x - 2, pos.y + 2, 0), botRightTile);

            PlaceBlock();
        }
    }
}