using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;


//I got the general code from https://gamedevelopment.tutsplus.com/tutorials/unity-2d-tile-based-sokoban-game--cms-29714.
//It is on GitHub with a MIT license.
public class Sokoban : MonoBehaviour {
	public string levelName;//name of the text file in resources folder
	public float tileSize;//we are using square tiles tileSize x tileSize

	public string[] levelNames; //stores the names of all of the levels, so they can be cycled through.
	
	//tile values for different tile types
	public char invalidTile;
	public char dirtTile;
	public char glassTile;
	public char heroTile;
	public char rockTile;
	public char nothingTile;
	public char warpTile;
	public char heroOnGlassTile;
	public char rockOnGlassTile;
	public float keyPressInterval;
	int lastKeyPressed;

	public Color glassColor;//glass tile has a different color

	//sprites for different tiles
	public Sprite dirtSprite;
	public Sprite heroSprite;
	public Sprite rockSprite;
	public Sprite glassSprite;
	public Sprite nothingSprite;

	//the user input keys
	public KeyCode[] userInputKeys;//up, right, down, left
	int[,] levelData;//level array
	int rows;
	int cols;
	Vector2 middleOffset=new Vector2();//offset for aligning the level to middle of the screen
	int rockCount;//number of rocks in level
	GameObject hero;//hero/player character
	Dictionary<GameObject,Vector2> occupants;//reference to rocks & hero
	bool gameOver;
	int playerXRotation;
	int playerYRotation;

	bool rockIsFalling;
	public float rockFallInterval; //how long in between each frame of the rock falling.
	float rockFallTimer; //is set equal to rockFallInterval.
	Vector2 fallingRockPosition;
	
	void Start () {
        gameOver = false;
		rockIsFalling = false;
		rockCount =0;
		occupants=new Dictionary<GameObject, Vector2>();
		playerXRotation = 0;
		playerYRotation = 0;
		rockFallInterval = 0.3f;
		rockFallTimer = 0.3f;
		ParseLevel();//load text file & parse our level 2d array
		CreateLevel();//create the level based on the array

		//Would load all of the level file names from a directory, but not sure if we need to do that.
		//DirectoryInfo levelDir = new DirectoryInfo('levels');
		//FileInfo[] levelNames = levelDir.GetFiles("*.*");
		//
		//foreach (FileInfo f in info)
		//{
		//	Debug.Log(f.ToString());
		//}
	}

	//Parse Level key: D for Dirt, P for Player, R for Rock, N for Nothing. Unimplemented: 1 for Enemy 1, 2 for Enemy 2
	void ParseLevel(){
		TextAsset textFile = Resources.Load (levelName) as TextAsset;
		string[] lines = textFile.text.Split (new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);//split by new line, return
		string[] nums = lines[0].Split(new[] { ',' });//split by ,
		rows=lines.Length;//number of rows
		cols=nums.Length;//number of columns
		levelData = new int[rows, cols];
        for (int i = 0; i < rows; i++) {
			string st = lines[i];
            nums = st.Split(new[] { ',' });
			for (int j = 0; j < cols; j++) {
                char val;
                if (char.TryParse (nums[j], out val)){
					val = char.ToUpper(val); //converts it to uppercase, as the PS level editor outputs in lower case. The level creator uses upper case.
                	levelData[i,j] = val;
				}
                else{
                    levelData[i,j] = invalidTile;
				}
            }
        }
	}
	void CreateLevel(){
		//calculate the offset to align whole level to scene middle
		middleOffset.x=cols*tileSize*0.5f-tileSize*0.5f;
		middleOffset.y=rows*tileSize*0.5f-tileSize*0.5f;;
		GameObject tile;
		SpriteRenderer sr;
		GameObject rock;
		GameObject glass;
		GameObject ground;
		GameObject warp;
		int warpind = 0;
		int glassCount=0;
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < cols; j++) {
                int val=levelData[i,j];
				if(val!=invalidTile){//a valid tile
					if (val==glassTile){//if it is a glass tile
						//sr.color=glassColor;
						glass = new GameObject("glass" + i.ToString() + "_" + j.ToString());//create new tile
						sr = glass.AddComponent<SpriteRenderer>();
						sr.sortingOrder = 1;
						glass.transform.localScale = Vector2.one * (tileSize - 1);
						sr.sprite = glassSprite;
						glass.transform.position = GetScreenPointFromLevelIndices(i, j);
						occupants.Add(glass, new Vector2(i, j));//store the level indices of the glass in dict
						glassCount++;//count glass
					}else{
						if(val==heroTile){//the hero tile
							hero = new GameObject("hero");
							hero.transform.localScale=Vector2.one*(tileSize-1);
							sr = hero.AddComponent<SpriteRenderer>();
							sr.sprite=heroSprite;
							sr.sortingOrder=1;//hero needs to be over the ground tile
							//sr.color=Color.red;
							hero.transform.position=GetScreenPointFromLevelIndices(i,j);
							occupants.Add(hero, new Vector2(i,j));//store the level indices of hero in dict
						}else if(val==rockTile){//rock tile
							rockCount++;//increment number of rocks in level
							rock = new GameObject("rock"+rockCount.ToString());
							rock.transform.localScale=Vector2.one*(tileSize-1);
							sr = rock.AddComponent<SpriteRenderer>();
							sr.sprite=rockSprite;
							sr.sortingOrder=1;//rock needs to be over the ground tile
							//sr.color=Color.black;
							rock.transform.position=GetScreenPointFromLevelIndices(i,j);
							occupants.Add(rock, new Vector2(i,j));//store the level indices of rock in dict
						} else if (val == dirtTile)
						{//dirt tile
							ground = new GameObject("ground" + i.ToString() + "_" + j.ToString());//create new tile
							ground.transform.localScale = Vector2.one * (tileSize - 1);
							sr = ground.AddComponent<SpriteRenderer>();
							sr.sprite = dirtSprite;
							sr.sortingOrder = 0;
							ground.transform.position = GetScreenPointFromLevelIndices(i, j);
							occupants.Add(ground, new Vector2(i, j));//store the level indices of hero in dict
						} else if (val == warpTile)
						{//Warp tile
							if (warpind < levelNames.Length)
							{
								warp = new GameObject("Warp_" + levelNames[warpind]);//create new tile
								warp.transform.localScale = Vector2.one * (tileSize - 1);
								sr = warp.AddComponent<SpriteRenderer>();
								sr.sprite = glassSprite;
								sr.sortingOrder = 0;
								warp.transform.position = GetScreenPointFromLevelIndices(i, j);
								occupants.Add(warp, new Vector2(i, j));//store the level indices of hero in dict
								warpind++;
							}
						} else if (val==nothingTile)
                        {
							/*
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
		if(rockCount>glassCount)Debug.LogError("there are more rocks than glass");
	}
	void ClearLevel()
    {
		foreach (KeyValuePair<GameObject, Vector2> occupant in occupants)
		{
			Destroy(occupant.Key);
		}
		occupants.Clear();
	}

	void Update(){
		if(gameOver)return;
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
			if (rockFallTimer<0) {TryFallRock();}
        }
	}

	//I think I'll just change the rotations using the built-in functions. I don't understand quaternions well.
    private void ApplyUserInput()
    {
		if (Input.GetKey(userInputKeys[0]))
		{
			TryMoveHero(0);//up
            //playerXRotation = 180;
			//playerYRotation = 180;
		}
		else if (Input.GetKey(userInputKeys[1]))
		{
			TryMoveHero(1);//right
			playerXRotation = 0;
			playerYRotation = 0;
		}
		else if (Input.GetKey(userInputKeys[2]))
		{
            TryMoveHero(2);//down
            //playerXRotation = 180;
			//playerYRotation = 0;
		}
		else if (Input.GetKey(userInputKeys[3]))
		{
            TryMoveHero(3);//left
			playerXRotation = 0;
			playerYRotation = 180;
		}
		else keyPressInterval = 0f; //if no key is pressed this frame, make it so the user can press any key next frame to move the player.

		hero.transform.eulerAngles = new Vector3(playerXRotation, playerYRotation, 0);


		if (Input.GetKeyUp(KeyCode.R))
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
		if (lastKeyPressed==direction)
        {
			// and the key press interval has not gone to 0 yet.
			if (keyPressInterval > 0) return;
        }
		lastKeyPressed = direction;
		keyPressInterval = 0.2f;


		Vector2 heroPos;
		Vector2 oldHeroPos;
		Vector2 nextPos;

#warning NullReferenceException here when trying to move player to black spot
		occupants.TryGetValue(hero,out oldHeroPos);
		heroPos=GetNextPositionAlong(oldHeroPos,direction);//find the next array position in given direction
		
		if(IsValidPosition(heroPos)){//check if it is a valid position & falls inside the level array
			Debug.Log("valid");
			if (!IsOccuppiedByRock(heroPos)){//check if it is occuppied by a rock
				Debug.Log("no rock");
				//move hero
				if (levelData[(int)heroPos.x, (int)heroPos.y] == dirtTile)
				{//moving onto a ground tile
					levelData[(int)heroPos.x, (int)heroPos.y] = heroTile;
				}
				else if (levelData[(int)heroPos.x, (int)heroPos.y] == glassTile)
				{//moving onto a glass tile
					levelData[(int)heroPos.x, (int)heroPos.y] = heroOnGlassTile;
				}
				hero.transform.position = GetScreenPointFromLevelIndices((int)heroPos.x, (int)heroPos.y);
				occupants[hero] = heroPos;
				RemoveOccuppant(oldHeroPos); //makes the tile the hero was on empty.
				CheckIfRockShouldFall(oldHeroPos);
			}else if (direction == 1 || direction ==3){
				Debug.Log("d1 or 3");
				//nextPos holds where the rock will be pushed to.
				nextPos =GetNextPositionAlong(heroPos,direction);
				if(IsValidPosition(nextPos)){
					if(!IsOccuppiedByDirt(nextPos)){//The next two tiles are empty, so we move the rock and the hero.
						GameObject rock=GetOccupantAtPosition(heroPos);//find the rock at this position
						if(rock==null)Debug.Log("no rock");
						//move the rock
						rock.transform.position=GetScreenPointFromLevelIndices((int)nextPos.x,(int)nextPos.y);
						levelData[(int)nextPos.x, (int)nextPos.y] = rockTile;
						occupants[rock]=nextPos;

						//now move the hero
						hero.transform.position=GetScreenPointFromLevelIndices((int)heroPos.x,(int)heroPos.y);
						levelData[(int)heroPos.x, (int)heroPos.y] = heroTile;
						occupants[hero]=heroPos;
						//if(levelData[(int)heroPos.x,(int)heroPos.y]==dirtTile){
						//	levelData[(int)heroPos.x,(int)heroPos.y]=heroTile;
						//}else if(levelData[(int)heroPos.x,(int)heroPos.y]==glassTile){
						//	levelData[(int)heroPos.x,(int)heroPos.y]=heroTile;
						//}
						RemoveOccuppant(oldHeroPos); //make the tile the player was on empty.
					}
				}
			}
			CheckWarp(); //check if the player has stepped on a warp tile
			CheckCompletion();//check if all rocks have reached glasss
		}
    }
    private void CheckIfRockShouldFall(Vector2 objPos)
    {
        Vector2 potentialRockPos = new Vector2((int)objPos.x - 1, (int)objPos.y);
        if (!IsValidPosition(potentialRockPos)) return;

        //Is the tile above the dirt just removed a rock?
        if (levelData[(int)potentialRockPos.x, (int)potentialRockPos.y] == rockTile)
        {
			rockIsFalling = true;
			fallingRockPosition = potentialRockPos;
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
			//rockIsFalling = true; // set rock is falling to true. - harry
			levelData[(int)fallingRockPosition.x, (int)fallingRockPosition.y] = nothingTile;
			GameObject rock = GetOccupantAtPosition(fallingRockPosition);//find the rock at this position
			fallingRockPosition.x += 1;
			rock.transform.position = GetScreenPointFromLevelIndices((int)fallingRockPosition.x, (int)fallingRockPosition.y);
			levelData[(int)fallingRockPosition.x, (int)fallingRockPosition.y] = rockTile;
			occupants[rock] = fallingRockPosition;
		}
		else
        {
            rockIsFalling = false;
			levelData[(int)fallingRockPosition.x, (int)fallingRockPosition.y] = nothingTile;
			//Destroy the rock when it hits the dirt
			GameObject rock = GetOccupantAtPosition(fallingRockPosition);//find the rock at this position
			Destroy(rock);
        }
	}

	//It would be more explicit to say glass tiles shattered, though I don't think this could change 
	private void CheckCompletion()
    {
        int rocksOnglass=0;
		for (int i = 0; i < rows; i++) {
			for (int j = 0; j < cols; j++) {
                if(levelData[i,j]==rockOnGlassTile){
					rocksOnglass++;
				}
			}
		}
		if(rocksOnglass==rockCount && rockCount > 0){
			Debug.Log("level complete");
			gameOver=true;
		}
    }

	//It would be more explicit to say glass tiles shattered, though I don't think this could change 
	private void CheckWarp()
	{
		Vector2 HeroPos;
		occupants.TryGetValue(hero, out HeroPos);
		if (IsOccuppiedByWarp(HeroPos))
        {
			levelName = GetOccupantAtPosition(HeroPos).name;

#warning getting ArugmentOutOfRangeException at code below
			levelName = levelName.Remove(0, 5); 
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

	//Dirt tiles is being drawn by default.
    private void RemoveOccuppant(Vector2 objPos)
    {
			GameObject empty = GetOccupantAtPosition(objPos);
			levelData[(int)objPos.x, (int)objPos.y] = nothingTile;
			if (empty)
			{
				//empty.transform.position = GetScreenPointFromLevelIndices((int)objPos.x, (int)objPos.y);
				occupants.Remove(empty);
				levelData[(int)objPos.x, (int)objPos.y] = nothingTile;//rock moving from ground tile
				Destroy(empty);
			}
			else
		    {
				//empty = new GameObject("tile" + objPos.x.ToString() + "_" + objPos.y.ToString());
			}
    }

    private bool IsOccuppiedByDirt(Vector2 objPos)
    {//check if there is dirt at given array position
        return (levelData[(int)objPos.x,(int)objPos.y]==dirtTile);
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

	private bool IsValidPosition(Vector2 objPos)
    {//check if the given indices fall within the array dimensions
        if(objPos.x>-1&&objPos.x<rows&&objPos.y>-1&&objPos.y<cols){
			//return levelData[(int)objPos.x,(int)objPos.y]!=invalidTile;
			return true;
		}else return false;
    }

    private Vector2 GetNextPositionAlong(Vector2 objPos, int direction)
    {
        switch(direction){
			case 0:
			objPos.x-=1;//up
			break;
			case 1:
			objPos.y+=1;//right
			break;
			case 2:
			objPos.x+=1;//down
			break;
			case 3:
			objPos.y-=1;//left
			break;
		}
		return objPos;
    }
	Vector2 GetScreenPointFromLevelIndices(int row,int col){
		//converting indices to position values, col determines x & row determine y
		return new Vector2(col*tileSize-middleOffset.x,row*-tileSize+middleOffset.y);
	}
	/*//the reverse methods to find indices from a screen point
	Vector2 GetLevelIndicesFromScreenPoint(float xVal,float yVal){
		return new Vector2((int)(yVal-middleOffset.y)/-tileSize,(int)(xVal+middleOffset.x)/tileSize);
	}
	Vector2 GetLevelIndicesFromScreenPoint(Vector2 pos){
		return GetLevelIndicesFromScreenPoint(pos.x,pos.y);
	}*/
	public void RestartLevel(){
		//Application.LoadLevel(0);
		SceneManager.LoadScene(0);
	}
}
