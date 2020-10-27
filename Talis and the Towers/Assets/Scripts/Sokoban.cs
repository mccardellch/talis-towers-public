using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Level : MonoBehaviour
{
    public string name;
    public bool complete;
}

//I got the general code from https://gamedevelopment.tutsplus.com/tutorials/unity-2d-tile-based-sokoban-game--cms-29714.
//It is on GitHub with a MIT license.
public class Sokoban : MonoBehaviour
{
    [SerializeField] private GameObject deathMenuUI;

    // to play the sfx
    public AudioSource dirt_breaking;
    public AudioSource rock_falling;

    public string levelName;//name of the text file in resources folder
    public float tileSize;//we are using square tiles tileSize x tileSize

    //tile values for different tile types
    public char invalidTile;
    public char dirtTile;
    public char glassTile;
    public char heroTile;
    public char rockTile;
    public char nothingTile;
    public char warpTile;
    public char heroOnGlassTile;
    public char shatteredGlassTile;
    public float keyPressInterval;
    int lastKeyPressed;

    public Color glassColor;//glass tile has a different color

    //sprites for different tiles
    public Sprite dirtSprite;
    public GameObject heroBlock;
    public GameObject rockBlock;
    public GameObject glassBlock;
    public Sprite nothingSprite;
    public Sprite incompleteWarpSprite;
    public GameObject warpBlock;
    public GameObject completeWarpBlock;

    //the user input keys
    public KeyCode[] userInputKeys;//up, right, down, left
    int[,] levelData;//level array
    int rows;
    int cols;
    Vector2 middleOffset = new Vector2();//offset for aligning the level to middle of the screen
    int glassCount;//number of glass blocks in level
    int enemyCount; //number of enemies in the level.
    GameObject hero;//hero/player character
    Dictionary<GameObject, Vector2> occupants;//reference to rocks & hero

    public Level[] levels; //stores the name of levels and whether they have been completed.
    public string[] levelNames; //stores the names of all levels in a way you can set from the editor.
    int currentLevelIndex = 0;

    bool gameOver;
    //It'd be better to make something like player.rotDegrees, but I don't want to look up how to do that in C# atm.
    int playerRotDegrees;
    enum playerDirections
    {
        up,
        right,
        down,
        left
    }
    playerDirections playerFacing;

    bool rockIsFalling;
    public float rockFallInterval; //how long in between each frame of the rock falling.
    float rockFallTimer; //is set equal to rockFallInterval.
    Vector2 fallingRockPosition;

    void Start()
    {
        
        gameOver = false;
        rockIsFalling = false;
        glassCount = 0;
        occupants = new Dictionary<GameObject, Vector2>();
        rockFallInterval = 0.3f;
        rockFallTimer = 0.3f;
        playerFacing = playerDirections.right;

        levels = new Level[levelNames.Length];
        //levels and levelNames have the same length, but levels length is not defined so levelNames is used.
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i] = new Level();
            levels[i].name = levelNames[i];
            levels[i].complete = false;
        }

        ParseLevel();//load text file & parse our level 2d array
        CreateLevel();//create the level based on the array
    }

    //Parse Level key: D for Dirt, P for Player, R for Rock, N for Nothing. Unimplemented: 1 for Enemy 1, 2 for Enemy 2
    void ParseLevel()
    {
        levelName = levels[currentLevelIndex].name;
        TextAsset textFile = Resources.Load(levelName) as TextAsset;
        string[] lines = textFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);//split by new line, return
        string[] nums = lines[0].Split(new[] { ',' });//split by ,
        rows = lines.Length;//number of rows
        cols = nums.Length;//number of columns
        levelData = new int[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            string st = lines[i];
            nums = st.Split(new[] { ',' });
            for (int j = 0; j < cols; j++)
            {
                char val;
                if (char.TryParse(nums[j], out val))
                {
                    val = char.ToUpper(val); //converts it to uppercase, as the PS level editor outputs in lower case. The level creator uses upper case.
                    levelData[i, j] = val;
                }
                else
                {
                    levelData[i, j] = invalidTile;
                }
            }
        }
    }
    void CreateLevel()
    {
        //calculate the offset to align whole level to scene middle
        middleOffset.x = cols * tileSize * 0.5f - tileSize * 0.5f;
        middleOffset.y = rows * tileSize * 0.5f - tileSize * 0.5f; ;
        //GameObject tile;
        SpriteRenderer sr;
        Animator anim; //used to control the speeds of animations
        GameObject rock;
        GameObject glass;
        GameObject ground;
        GameObject warp;
        glassCount = 0;
        int warpind = 1;
        int levelsComplete = 0;
        int levelsToDraw = 0;
        //Calculates the amount of levels completed, which is used to draw the correct amount of warp tiles.
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].complete == true)
            {
                levelsComplete += 1;
            }
        }
        if (levelsComplete <= 2) { levelsToDraw = 3; }
        else if (levelsComplete <=5) { levelsToDraw = 4; }


        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                int val = levelData[i, j];
                if (val != invalidTile)
                {//a valid tile
                    if (val == glassTile)
                    {//if it is a glass tile
                        glass = Instantiate(glassBlock);
                        //glass.name = ("Glass_" + i.ToString() + j.ToString());
                        sr = glass.GetComponent<SpriteRenderer>();
                        sr.sortingOrder = 1;
                        glass.transform.localScale = Vector2.one * (tileSize - 1);
                        glass.transform.position = GetScreenPointFromLevelIndices(i, j);
                        occupants.Add(glass, new Vector2(i, j));//store the level indices of the glass in dict
                        glassCount++;//count glass
                    }
                    else
                    {
                        if (val == heroTile)
                        {//the hero tile
                            hero = Instantiate(heroBlock);
                            hero.transform.localScale = Vector2.one * (tileSize - 1);
                            sr = hero.GetComponent<SpriteRenderer>();
                            sr.sortingOrder = 1;
                            hero.transform.position = GetScreenPointFromLevelIndices(i, j);
                            occupants.Add(hero, new Vector2(i, j));//store the level indices of hero in dict
                        }
                        else if (val == rockTile)
                        {//rock tile
                            rock = Instantiate(rockBlock);
                            rock.transform.localScale = Vector2.one * (tileSize - 1);
                            sr = rock.GetComponent<SpriteRenderer>();
                            sr.sortingOrder = 1;//rock should be above ground tile, though this never comes up because the dirt tile would have been deleted.
                            rock.transform.position = GetScreenPointFromLevelIndices(i, j);
                            occupants.Add(rock, new Vector2(i, j));//store the level indices of rock in dict
                        }
                        else if (val == dirtTile)
                        {//dirt tile
                            ground = new GameObject("ground" + i.ToString() + "_" + j.ToString());//create new tile
                            ground.transform.localScale = Vector2.one * (tileSize - 1);
                            sr = ground.AddComponent<SpriteRenderer>();
                            sr.sprite = dirtSprite;
                            sr.sortingOrder = 0;
                            ground.transform.position = GetScreenPointFromLevelIndices(i, j);
                            occupants.Add(ground, new Vector2(i, j));//store the level indices of the dirt in dict
                        }
                        else if (val == warpTile)
                        {//Warp tile
                            if (warpind <= levelsToDraw)
                            {
                                //UnityEngine.Debug.Log("Should be warp");
                                if (levels[warpind].complete)
                                {
                                    warp = Instantiate(completeWarpBlock);
                                }
                                else warp = Instantiate(warpBlock);

                                warp.name = ("Warp_" + levels[warpind].name);
                                warp.transform.localScale = Vector2.one * (tileSize - 1);
                                sr = warp.GetComponent<SpriteRenderer>();
                                anim = warp.GetComponent<Animator>();
                                anim.speed = 0.2f;

                                sr.sortingOrder = 0;
                                warp.transform.position = GetScreenPointFromLevelIndices(i, j);
                                occupants.Add(warp, new Vector2(i, j));//store the level indices of the warp in dict
                                warpind++;
                            }
                            else levelData[i, j] = nothingTile;
                        }
                        else if (val == nothingTile)
                        {
                            /* This is all commented out because nothingTile is just nothing at all for now. It is not a sprite.
							tile = new GameObject("tile" + i.ToString() + "_" + j.ToString());//create new tile
							tile.transform.localScale = Vector2.one * (tileSize - 1);//set tile size
							sr = tile.AddComponent<SpriteRenderer>();//add a sprite renderer
							sr.sprite = nothingSprite;//assign tile sprite
							sr.sortingOrder = 0;
							tile.transform.position = GetScreenPointFromLevelIndices(i, j);//place in scene based on level indices
							occupants.Add(tile, new Vector2(i, j));//store the level indices of the empty tile in dict
							*/
                        }
                    }
                }
            }
        }
    }
    void ClearLevel()
    {
        foreach (KeyValuePair<GameObject, Vector2> occupant in occupants)
        {
            Destroy(occupant.Key);
        }
        occupants.Clear();
    }

    void Update()
    {
        if (gameOver) return;
        Vector2 trueHeroPos;
        Vector2 trueRockPos;
        occupants.TryGetValue(hero, out trueHeroPos);
        hero.transform.position = new Vector2(UnityEngine.Mathf.Lerp(hero.transform.position.x, GetScreenPointFromLevelIndices((int)trueHeroPos.x, (int)trueHeroPos.y).x, .05f), UnityEngine.Mathf.Lerp(hero.transform.position.y, GetScreenPointFromLevelIndices((int)trueHeroPos.x, (int)trueHeroPos.y).y, .05f));
   
        foreach (KeyValuePair<GameObject, Vector2> occupant in occupants)
        {
            if (occupant.Key != null)
            {
                if (occupant.Key.name[0] == 'r')
                {
                    occupants.TryGetValue(occupant.Key, out trueRockPos);
                    occupant.Key.transform.position = new Vector2(UnityEngine.Mathf.Lerp(occupant.Key.transform.position.x, GetScreenPointFromLevelIndices((int)trueRockPos.x, (int)trueRockPos.y).x, .05f), UnityEngine.Mathf.Lerp(occupant.Key.transform.position.y, GetScreenPointFromLevelIndices((int)trueRockPos.x, (int)trueRockPos.y).y, .05f));
                }
            } 
        }
        
        if (!rockIsFalling)
        {
            ApplyUserInput();//check & use user input to move hero and rocks
            if (keyPressInterval > 0)
            {
                keyPressInterval -= Time.deltaTime;
            }
        }
        else
        {
            rockFallTimer -= Time.deltaTime;
            if (rockFallTimer < 0)
            {
                TryFallRock();
                CheckCompletion(); //the rock falling may have triggered the level being completed, so check if it has been completed.
            }
        }
    }

    private void ApplyUserInput()
    {
        //Changes which way the player is facing based on the key pressed, stored in a variable and shown through their rotation.
        //Player moves with arrow keys.
        if (Input.GetKeyDown("w") || Input.GetKeyDown("up"))
        {
            TryMoveHero(0);//up
            //hero.transform.up = Vector3.left;
            playerFacing = playerDirections.up;
        }
        else if (Input.GetKeyDown("d") || Input.GetKeyDown("right"))
        {
            TryMoveHero(1);//right
            hero.transform.right = Vector3.right;
            playerFacing = playerDirections.right;
        }
        else if (Input.GetKeyDown("s") || Input.GetKeyDown("down"))
        {
            TryMoveHero(2);//down
            //hero.transform.up = Vector3.right;
            playerFacing = playerDirections.down;
        }
        else if (Input.GetKeyDown("a") || Input.GetKeyDown("left"))
        {
            TryMoveHero(3);//left
            hero.transform.right = Vector3.left;
            playerFacing = playerDirections.left;
        }
        else keyPressInterval = 0f; //if no key is pressed this frame, make it so the user can press any key next frame to move the player.

        //hero.transform.right = Vector3.forward * playerRotDegrees;

        //R sends the player back to the world map. We may just make it restart the level, though this is not important atm.
        if (Input.GetKeyDown("r"))
        {
            RestartLevel();
        }
    }
    private void TryMoveHero(int direction)
    {
        //This if then statement makes sure the player can only move in the same direction every keyPressInterval seconds if they are holding a key down. 
        // (keyPressInterval is affected by deltaTime in update.)
        //They can press the key again and again to move as fast as they want though.
        // If the same key as last time is pressed:
        if (lastKeyPressed == direction)
        {
            // and the key press interval has not gone to 0 yet.
            if (keyPressInterval > 0) return;
        }
        lastKeyPressed = direction;
        keyPressInterval = 0.2f;


        Vector2 heroPos;
        Vector2 oldHeroPos;
        Vector2 nextPos;
        occupants.TryGetValue(hero, out oldHeroPos);
        heroPos = GetNextPositionAlong(oldHeroPos, direction);//find the next array position in given direction

        if (IsValidPosition(heroPos) && !IsOccuppiedByGlass(heroPos))
        {//check if it is a valid position & falls inside the level array
            //UnityEngine.Debug.Log("valid");
            if (!IsOccuppiedByRock(heroPos))
            {//check if it is occuppied by a rock
                //UnityEngine.Debug.Log("no rock");
                //move hero
                if (levelData[(int)heroPos.x, (int)heroPos.y] == dirtTile)
                {//moving onto a ground tile
                    levelData[(int)heroPos.x, (int)heroPos.y] = heroTile;
                    RemoveOccuppant(heroPos); //removes the dirt on the tile the player is moving to.

                    // play attack animation
                    //animator.SetTrigger("Attack");

                    // play the dirt sfx
                    dirt_breaking.Play();
                }
                else if (levelData[(int)heroPos.x, (int)heroPos.y] == glassTile) //this is never called because the the if statement doesn't execute is IsOccupiedByGlass is true.
                {//moving onto a glass tile
                    //levelData[(int)heroPos.x, (int)heroPos.y] = heroOnGlassTile;
                }
                //hero.transform.position = GetScreenPointFromLevelIndices((int)heroPos.x, (int)heroPos.y);
                occupants[hero] = heroPos;
                RemoveOccuppant(oldHeroPos); //makes the tile the hero was on empty.
                CheckIfRockShouldFall(oldHeroPos); //check if the dirt tile the player moved would make a rock fall.
            }
            else if (direction == 1 || direction == 3)
            {
                //UnityEngine.Debug.Log("d1 or 3");
                //nextPos holds where the rock will be pushed to.
                nextPos = GetNextPositionAlong(heroPos, direction);
                if (IsValidPosition(nextPos))
                {
                    if (!IsOccuppiedByDirt(nextPos) && !IsOccuppiedByRock(nextPos))
                    {//The next two tiles are empty, so we move the rock and the hero.
                        GameObject rock = GetOccupantAtPosition(heroPos);//find the rock at this position
                        if (rock == null) UnityEngine.Debug.Log("no rock");
                        //move the rock
                        rock.transform.position = GetScreenPointFromLevelIndices((int)nextPos.x, (int)nextPos.y);
                        levelData[(int)nextPos.x, (int)nextPos.y] = rockTile;
                        occupants[rock] = nextPos;

                        //now move the hero
                        hero.transform.position = GetScreenPointFromLevelIndices((int)heroPos.x, (int)heroPos.y);
                        levelData[(int)heroPos.x, (int)heroPos.y] = heroTile;
                        occupants[hero] = heroPos;
                        //if(levelData[(int)heroPos.x,(int)heroPos.y]==dirtTile){
                        //	levelData[(int)heroPos.x,(int)heroPos.y]=heroTile;
                        //}else if(levelData[(int)heroPos.x,(int)heroPos.y]==glassTile){
                        //	levelData[(int)heroPos.x,(int)heroPos.y]=heroTile;
                        //}
                        RemoveOccuppant(oldHeroPos); //make the tile the player was on empty.
                        Vector2 potentialRockPos = nextPos;
                        potentialRockPos.x += 1; //This tile represents directly below the rock. 
                        CheckIfRockShouldFall(potentialRockPos);
                    }
                }
            }
            CheckWarp(); //check if the player has stepped on a warp tile
            CheckCompletion();//check if all glass blocks have been shattered.
        }
    }

    //This checks if there is a rock above a point, and if there is an empty tile below that rock.
    //If both of these things are true, the rock should start falling.
    private void CheckIfRockShouldFall(Vector2 objPos)
    {
        Vector2 potentialRockPos = new Vector2((int)objPos.x - 1, (int)objPos.y);
        if (!IsValidPosition(potentialRockPos)) return;

        //Is the tile above the dirt that was just removed a rock?
        if (levelData[(int)potentialRockPos.x, (int)potentialRockPos.y] == rockTile)
        {
            if (levelData[(int)objPos.x, (int)objPos.y] == nothingTile)
            {
                rockIsFalling = true; //This stops the game update, calling TryFallRock until the rock has fallen far and been destroyed.
                fallingRockPosition = potentialRockPos;
                UnityEngine.Debug.Log("rock is falling");
            }
        }
    }

    private void TryFallRock()
    {
        Vector2 potentialNewPos = fallingRockPosition;
        potentialNewPos.x += 1;
        rockFallTimer = rockFallInterval;
        //destroy the rock if it is trying to move out of bounds
        if (!IsValidPosition(potentialNewPos))
        {
            rockIsFalling = false;
            levelData[(int)fallingRockPosition.x, (int)fallingRockPosition.y] = nothingTile;
            GameObject rock = GetOccupantAtPosition(fallingRockPosition);//find the rock at this position
            Destroy(rock);
            return;
        }
        //If the tile below the rock is empty, make it fall.
        if (levelData[(int)fallingRockPosition.x + 1, (int)fallingRockPosition.y] == nothingTile)
        {
            levelData[(int)fallingRockPosition.x, (int)fallingRockPosition.y] = nothingTile;
            GameObject rock = GetOccupantAtPosition(fallingRockPosition);//find the rock at this position
            fallingRockPosition.x += 1;
            rock.transform.position = GetScreenPointFromLevelIndices((int)fallingRockPosition.x, (int)fallingRockPosition.y);
            levelData[(int)fallingRockPosition.x, (int)fallingRockPosition.y] = rockTile;
            occupants[rock] = fallingRockPosition;
        }
        else if (levelData[(int)fallingRockPosition.x + 1, (int)fallingRockPosition.y] == glassTile)
        {
            levelData[(int)fallingRockPosition.x, (int)fallingRockPosition.y] = nothingTile;
            GameObject glass = GetOccupantAtPosition(new Vector2(fallingRockPosition.x + 1, fallingRockPosition.y));
            //Enabled is initially false and I set it to true because the glass was always breaking if I didn't do this.
            glass.GetComponent<Animator>().enabled=true;
            glass.GetComponent<Animator>().speed = 1;
            //glass.GetComponent<Animator>().Play("glass break");

            levelData[(int)fallingRockPosition.x, (int)fallingRockPosition.y] = nothingTile;
            levelData[(int)fallingRockPosition.x + 1, (int)fallingRockPosition.y] = shatteredGlassTile;

            GameObject rock = GetOccupantAtPosition(fallingRockPosition);//find the rock at this position
            rock.transform.position = GetScreenPointFromLevelIndices((int)fallingRockPosition.x, (int)fallingRockPosition.y);
            occupants[rock] = fallingRockPosition;
            rock.GetComponent<Animator>().enabled = true;
            rock.GetComponent<Animator>().speed = 0.8f;
            //rock.GetComponent<Animator>().Play("rock break");

            //rock.GetComponent<SpriteRenderer>().color = Color.red;
            //glass.GetComponent<SpriteRenderer>().color = Color.red;
            Destroy(rock, 0.5f);
            Destroy(glass, 0.5f);
            rockIsFalling = false;
        }
        else
        {
            rockIsFalling = false;
            levelData[(int)fallingRockPosition.x, (int)fallingRockPosition.y] = nothingTile;
            //Destroy the rock when it hits the dirt
            GameObject rock = GetOccupantAtPosition(fallingRockPosition);//find the rock at this position

            // play the rock falling sfx
            rock_falling.Play();

            rock.GetComponent<Animator>().enabled = true;
            rock.GetComponent<Animator>().speed = 1;
            rock.GetComponent<Animator>().Play("rock break");
            Destroy(rock, 0.4f);
        }
    }

    //If all glass tiles are shattered, then the level is complete
    private void CheckCompletion()
    {
        int shatteredGlass = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (levelData[i, j] == shatteredGlassTile)
                {
                    shatteredGlass++;
                    levels[currentLevelIndex].complete = true;
                }
            }
        }
        //If the level is complete, then load the world map.
        if (shatteredGlass == glassCount && currentLevelIndex != 0)
        {

            //Invoke("RestartLevel", 0.5f); //If the level is complete, wait half a second and then send the player back to the world map.
            //restart level just warps you back to the world map atm.
            Invoke("LevelComplete", 0.5f); //If the level is complete, wait half a second and then send the player back to the world map.
        }
    }

    private void LevelComplete()
    {
        glassCount = 0;
        currentLevelIndex = 0;
        UnityEngine.Debug.Log("level complete");

        ClearLevel();//remove all the objects from the current level
        ParseLevel();//load text file & parse our level 2d array
        CreateLevel();//create the new level based on the array
    }

    //This seems to be getting the actual hero occuppant instead of the warp tile, but only when in a different row.
    private void CheckWarp()
    {
        Vector2 HeroPos;
        occupants.TryGetValue(hero, out HeroPos);
        if (IsOccuppiedByWarp(HeroPos))
        {
            levelName = GetOccupantAtPosition(HeroPos).name;
            levelName = levelName.Remove(0, 5);
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i].name == levelName)
                {
                    currentLevelIndex = i;
                }
            }
            ClearLevel();//remove all the objects from the current level
            ParseLevel();//load text file & parse our level 2d array
            CreateLevel();//create the new level based on the array
        }
    }

    private GameObject GetOccupantAtPosition(Vector2 heroPos)
    {//loop through the occupants to find the rock at given position
        GameObject rock;
        foreach (KeyValuePair<GameObject, Vector2> pair in occupants)
        {
            if (pair.Value == heroPos)
            {
                rock = pair.Key;
                return rock;
            }
        }
        return null;
    }

    //This function mainly serves to remove dirt when the player is moving.
    private void RemoveOccuppant(Vector2 objPos)
    {
        GameObject empty = GetOccupantAtPosition(objPos);
        levelData[(int)objPos.x, (int)objPos.y] = nothingTile;
        //If there is an occupant on this tile, remove it.
        if (empty)
        {
            //empty.transform.position = GetScreenPointFromLevelIndices((int)objPos.x, (int)objPos.y);
            occupants.Remove(empty);
            Destroy(empty);
        }
        else
        {
            //UnityEngine.Debug.Log("No empty object.");
            //empty = new GameObject("tile" + objPos.x.ToString() + "_" + objPos.y.ToString());
        }
    }

    private bool IsOccuppiedByDirt(Vector2 objPos)
    {//check if there is dirt at given array position
        return (levelData[(int)objPos.x, (int)objPos.y] == dirtTile);
    }

    //Why not just check if it is a rock tile?
    private bool IsOccuppiedByRock(Vector2 objPos)
    {//check if there is a rock or dirt at given array position
        return (levelData[(int)objPos.x, (int)objPos.y] == rockTile);
    }

    private bool IsOccuppiedByWarp(Vector2 objPos)
    {//check if there is a warp tile at given array position
        return (levelData[(int)objPos.x, (int)objPos.y] == warpTile);
    }

    private bool IsOccuppiedByGlass(Vector2 objPos)
    {//check if there is a glass block at given array position
        return (levelData[(int)objPos.x, (int)objPos.y] == glassTile);
    }

    private bool IsValidPosition(Vector2 objPos)
    {//check if the given indices fall within the array dimensions
        if (objPos.x > -1 && objPos.x < rows && objPos.y > -1 && objPos.y < cols)
        {
            //return levelData[(int)objPos.x,(int)objPos.y]!=invalidTile;
            return true;
        }
        else return false;
    }

    private Vector2 GetNextPositionAlong(Vector2 objPos, int direction)
    {
        switch (direction)
        {
            case 0:
                objPos.x -= 1;//up
                break;
            case 1:
                objPos.y += 1;//right
                break;
            case 2:
                objPos.x += 1;//down
                break;
            case 3:
                objPos.y -= 1;//left
                break;
        }
        return objPos;
    }
    Vector2 GetScreenPointFromLevelIndices(int row, int col)
    {
        //converting indices to position values, col determines x & row determine y
        return new Vector2(col * tileSize - middleOffset.x, row * -tileSize + middleOffset.y);
    }
    /*//the reverse methods to find indices from a screen point
	Vector2 GetLevelIndicesFromScreenPoint(float xVal,float yVal){
		return new Vector2((int)(yVal-middleOffset.y)/-tileSize,(int)(xVal+middleOffset.x)/tileSize);
	}
	Vector2 GetLevelIndicesFromScreenPoint(Vector2 pos){
		return GetLevelIndicesFromScreenPoint(pos.x,pos.y);
	}*/
    public void RestartLevel()
    {
        //Application.LoadLevel(0);
        //SceneManager.LoadScene(0);
        deathMenuUI.SetActive(false);
        ClearLevel();//remove all the objects from the current level
        ParseLevel();//load text file & parse our level 2d array
        CreateLevel();//create the new level based on the array
    }
}
