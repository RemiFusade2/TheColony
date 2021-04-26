using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct Pheromone
{
    float excitement;
    float fear;
}

[System.Serializable]
public enum AntType
{
    SCOUT, // walks fast, put pheromones, flee danger
    WORKER, // dig, carry, put pheromones, flee danger
    FIGHTER // walk, fight, put pheromones, go to danger
}

[System.Serializable]
public enum AntAction
{
    REST,
    WALK,
    RUN,
    DIG,
    CARRY
}

[System.Serializable]
public struct IntVec
{
    public int x;
    public int y;
}

public class Ant
{
    // describe the ant itself
    public AntType antType;
    public float efficiency; // between -1 and +1
    public int visionRange;
    public Color color;

    public float walkSpeed;
    public float runSpeed;
    public float digSpeed;

    // all runtime info
    public IntVec pos;

    public IntVec dir;

    public AntAction currentAction;

    public float hungry;

    public float health;

    public float energy;

    public bool carryFood;

    public bool isAlly;


    public Ant(IntVec _pos, AntType _antType, bool ally)
    {
        isAlly = ally;
        antType = _antType;
        efficiency = Random.Range(-1, 1.0f);
        float randomGrayValue = Random.Range(0, 0.05f);
        color = new Color(randomGrayValue, randomGrayValue, randomGrayValue);
        walkSpeed = Random.Range(0.05f,0.1f);
        runSpeed = Random.Range(0.12f, 0.25f);
        digSpeed = Random.Range(0.01f, 0.04f);

        switch(antType)
        {
            case AntType.FIGHTER:
                visionRange = 10;
                break;
            case AntType.SCOUT:
                visionRange = 20;
                break;
            case AntType.WORKER:
                visionRange = 6;
                break;
        }

        pos.x = _pos.x;
        pos.y = _pos.y;
        dir.x = 0;
        dir.y = 0;
        currentAction = AntAction.REST;
        hungry = 0;
        energy = 1;
        health = _antType.Equals(AntType.FIGHTER) ? Random.Range(10, 15) : Random.Range(5, 10);
        carryFood = false;
    }

    public void Eat()
    {
        if (GameEngine.instance.RemoveFood())
        {
            hungry = 0;
            health += 2;
        }
        else
        {
            hungry -= 0.5f;
            Damage();
        }
    }

    public void Walk()
    {
        bool doesMove = Random.Range(0, 1.0f) < walkSpeed;
        if (doesMove)
        {
            pos.x += dir.x;
            pos.y += dir.y;
            energy -= Random.Range(0, 0.01f);
            hungry += 0.01f;
        }
        currentAction = AntAction.WALK;
    }

    public void Run()
    {
        bool doesMove = Random.Range(0, 1.0f) < runSpeed;
        if (doesMove)
        {
            pos.x += dir.x;
            pos.y += dir.y;
            energy -= Random.Range(0.01f, 0.02f);
            hungry += 0.02f;
        }
        currentAction = AntAction.RUN;
    }

    public void Dig()
    {
        bool doesMove = Random.Range(0, 1.0f) < digSpeed;
        if (doesMove)
        {
            pos.x += dir.x;
            pos.y += dir.y;
            energy -= Random.Range(0.02f, 0.05f);
            hungry += 0.03f;
        }
        currentAction = AntAction.DIG;
    }

    public void Carry()
    {
        bool doesMove = Random.Range(0, 1.0f) < walkSpeed;
        if (doesMove)
        {
            pos.x += dir.x;
            pos.y += dir.y;
            energy -= Random.Range(0.01f, 0.03f);
            hungry = 0;
        }
        currentAction = AntAction.CARRY;
        carryFood = true;
    }

    public void Drop()
    {
        carryFood = false;
        currentAction = AntAction.WALK;
        GameEngine.instance.EarnFood();
    }

    public void ChangeDirection(IntVec newDir)
    {
        dir.x = newDir.x;
        dir.y = newDir.y;
    }

    public void Damage()
    {
        health--;
    }
    public bool IsDead()
    {
        return health <= 0;
    }

    public void Rest()
    {
        dir.x = 0;
        dir.y = 0;
        energy += Random.Range(0.01f, 0.02f);
    }

    public void Fall()
    {
        dir.x = 0;
        dir.y = 0;
        pos.y -= 1;
    }

    public void GrabFood()
    {
        currentAction = AntAction.CARRY;
        carryFood = true;
    }

    public void ComputeAction(List<IntVec> walkPossibilities, List<IntVec> digPossibilities)
    {
        int randomIndex = 0;
        IntVec walkDir, digDir;
        bool changeDirection = false;
        IntVec queenDirectionHorizontal;
        IntVec queenDirectionVertical;
        queenDirectionHorizontal.y = 0;
        queenDirectionHorizontal.x = Mathf.RoundToInt(Mathf.Sign(GameEngine.queenPosition.x - pos.x));
        queenDirectionVertical.x = 0;
        queenDirectionVertical.y = Mathf.RoundToInt(Mathf.Sign(GameEngine.queenPosition.y - pos.y));
        switch (currentAction)
        {
            case AntAction.REST:
                Rest();
                if ( (energy > 0.9f || Random.Range(0, 50) == 0) && walkPossibilities.Count > 0)
                {
                    randomIndex = Random.Range(0, walkPossibilities.Count);
                    walkDir = walkPossibilities[randomIndex];
                    ChangeDirection(walkDir);
                    if (carryFood)
                    {
                        currentAction = AntAction.CARRY;
                    }
                    else
                    {
                        currentAction = AntAction.WALK;
                    }
                }
                else if ((energy > 1 || Random.Range(0, 100) == 0) && antType.Equals(AntType.WORKER) && digPossibilities.Count > 0)
                {
                    randomIndex = Random.Range(0, digPossibilities.Count);
                    digDir = digPossibilities[randomIndex];
                    ChangeDirection(digDir);
                    if (carryFood)
                    {
                        currentAction = AntAction.CARRY;
                    }
                    else
                    {
                        currentAction = AntAction.DIG;
                    }
                }
                break;
            case AntAction.WALK:

                changeDirection = Random.Range(0, 100) == 0;
                if (!walkPossibilities.Contains(dir))
                {
                    changeDirection = true;
                }
                if (changeDirection && walkPossibilities.Count > 0)
                {
                    randomIndex = Random.Range(0, walkPossibilities.Count);
                    walkDir = walkPossibilities[randomIndex];
                    ChangeDirection(walkDir);
                }
                Walk();

                if (antType.Equals(AntType.WORKER) && Random.Range(-0.5f, 1) < efficiency && digPossibilities.Count > 0 && walkPossibilities.Count <= 1)
                {
                    randomIndex = Random.Range(0, digPossibilities.Count);
                    digDir = digPossibilities[randomIndex];
                    ChangeDirection(digDir);
                    currentAction = AntAction.DIG;
                }
                else if (antType.Equals(AntType.WORKER) && Random.Range(0.9f, 1) < efficiency && digPossibilities.Count > 0 && walkPossibilities.Count <= 2)
                {
                    randomIndex = Random.Range(0, digPossibilities.Count);
                    digDir = digPossibilities[randomIndex];
                    ChangeDirection(digDir);
                    currentAction = AntAction.DIG;
                }
                else if (antType.Equals(AntType.FIGHTER) && Random.Range(0f, 1) < efficiency)
                {
                    currentAction = AntAction.RUN;
                }

                break;
                
            case AntAction.DIG:
                changeDirection = Random.Range(0, 50) == 0;
                if (walkPossibilities.Contains(dir) || !digPossibilities.Contains(dir))
                {
                    currentAction = AntAction.WALK;
                }
                else
                {
                    if (changeDirection && digPossibilities.Count > 0)
                    {
                        randomIndex = Random.Range(0, digPossibilities.Count);
                        digDir = digPossibilities[randomIndex];
                        ChangeDirection(digDir);
                    }
                    Dig();
                }
                break;

            case AntAction.CARRY:

                // go towards queen

                if (pos.x == GameEngine.queenPosition.x && pos.y == GameEngine.queenPosition.y)
                {
                    Drop();
                    currentAction = AntAction.WALK;
                }
                else
                {
                    changeDirection = Random.Range(0, 50) == 0;
                    if (!walkPossibilities.Contains(dir))
                    {
                        changeDirection = true;
                    }
                    bool forceGoTowardsQueen = Random.Range(0, 5) == 0;
                    if (pos.x == GameEngine.queenPosition.x)
                    {
                        changeDirection = true;
                        forceGoTowardsQueen = true;
                    }

                    if (changeDirection)
                    {
                        walkDir.x = 0;
                        walkDir.y = 0;
                        if (forceGoTowardsQueen)
                        {
                            if (walkPossibilities.Contains(queenDirectionVertical))
                            {
                                walkDir = queenDirectionVertical;
                            }
                            else if (walkPossibilities.Contains(queenDirectionHorizontal))
                            {
                                walkDir = queenDirectionHorizontal;
                            }
                            else
                            {
                                randomIndex = Random.Range(0, walkPossibilities.Count);
                                walkDir = walkPossibilities[randomIndex];
                            }
                        }
                        else if (walkPossibilities.Count > 0)
                        {
                            randomIndex = Random.Range(0, walkPossibilities.Count);
                            walkDir = walkPossibilities[randomIndex];
                        }
                        ChangeDirection(walkDir);
                    }
                    Carry();
                }
                break;

            case AntAction.RUN:

                changeDirection = Random.Range(0, 50) == 0;
                if (!walkPossibilities.Contains(dir))
                {
                    changeDirection = true;
                }

                if (changeDirection)
                {
                    if (!isAlly && antType.Equals(AntType.FIGHTER) && Random.Range(-0.5f, 1) < efficiency)
                    {
                        // go towards queen
                        walkDir.x = 0;
                        walkDir.y = 0;
                        if (walkPossibilities.Contains(queenDirectionHorizontal))
                        {
                            walkDir = queenDirectionVertical;
                        }
                        else if (walkPossibilities.Contains(queenDirectionVertical))
                        {
                            walkDir = queenDirectionVertical;
                        }
                        else
                        {
                            if (walkPossibilities.Count > 0)
                            {
                                randomIndex = Random.Range(0, walkPossibilities.Count);
                                walkDir = walkPossibilities[randomIndex];
                            }
                            else
                            {
                                currentAction = AntAction.DIG;
                            }
                        }
                    }
                    else if (isAlly && antType.Equals(AntType.FIGHTER) && Random.Range(-0.5f, 1) < efficiency && Random.Range(0, 10) == 0)
                    {
                        if (pos.y >= 192)
                        {
                            walkDir.y = 0;
                            walkDir.x = Random.Range(0, 2) == 0 ? -1 : 1;
                        }
                        else
                        {
                            walkDir.y = 1;
                            walkDir.x = 0;
                        }
                    }
                    else
                    {
                        walkDir.x = 0;
                        walkDir.y = 0;
                        if (walkPossibilities.Count > 0)
                        {
                            randomIndex = Random.Range(0, walkPossibilities.Count);
                            walkDir = walkPossibilities[randomIndex];
                        }
                        else
                        {
                            currentAction = AntAction.DIG;
                        }
                    }
                    ChangeDirection(walkDir);
                }

                Run();

                break;

            default:
                break;
        }

        if (energy < Random.Range(0,0.3f))
        {
            currentAction = AntAction.REST;
        }

        if (hungry > 3)
        {
            Eat();
        }
    }
}

public class GameEngine : MonoBehaviour
{
    public static GameEngine instance;

    [Header("References")]
    public RawImage rawImageEnv;
    public RawImage rawImageAnts;
    public RawImage rawImageFogOfWar;
    [Space]
    public Slider queenCooldownSlider;
    public Text queenLvlText;
    public Text foodSupplyText;
    [Space]
    public Text workersCountText;
    public Text fightersCountText;
    public Text scoutsCountText;

    [Header("Settings")]
    public int width;
    public int height;

    public static IntVec queenPosition;

    [Header("Palette")]
    public Color skyColor;
    public Color dirtColor;
    public Color rockColor;
    public Color bkgDirtColor;
    public Color trunkColor;
    public Color leavesColor;
    public Color bushColor;
    public Color grassColor;
    public Color foodColor;
    [Space]
    public Color queenAntColor;
    public Color friendlyAntColor;
    public Color enemyAntColor;
    [Space]
    public Color noneColor;


    private List<Ant> allAnts;
    private List<Ant> enemyAnts;

    private Color[] allWorldPixels;
    private Color[] allAntsPixels;
    private Color[] fogOfWarPixels;

    private Texture2D worldTexture;
    private Texture2D antTexture;
    private Texture2D fogOfWarTexture;

    private int foodRessource;

    private float queenCooldown;
    private int queenLvl;

    private int workersCount;
    private int fightersCount;
    private int scoutsCount;

    public bool RemoveFood()
    {
        bool isThereFood = foodRessource > 0;
        if (isThereFood)
        {
            foodRessource--;
        }
        return isThereFood;
    }

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        workersCount = 0;
        fightersCount = 0;
        scoutsCount = 0;

        foodRessource = 1000;
        queenCooldown = 1;
        queenLvl = 1;
        queenCooldownSlider.value = 0;

        queenPosition.x = 128;
        queenPosition.y = 150;

        worldTexture = new Texture2D(width, height);
        rawImageEnv.texture = worldTexture;
        worldTexture.filterMode = FilterMode.Point;

        antTexture = new Texture2D(width, height);
        rawImageAnts.texture = antTexture;
        antTexture.filterMode = FilterMode.Point;

        fogOfWarTexture = new Texture2D(width, height);
        rawImageFogOfWar.texture = fogOfWarTexture;
        fogOfWarTexture.filterMode = FilterMode.Point;

        GenerateWorld();
        GenerateAnts();
        GenerateFogOfWar();
        WriteChanges();

        StartCoroutine(WaitAndSendEnemyWave(Random.Range(20, 40)));
        StartCoroutine(WaitAndComputeEnemyAnts(0.1f));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        MoveAnts();
        ClearDeadAnts();
        WriteChanges();
    }

    private IEnumerator WaitAndComputeEnemyAnts(float delay)
    {
        yield return new WaitForSeconds(delay);
        MoveEnemyAnts();
        if ((1 / Time.deltaTime) < 30)
        {
            StartCoroutine(WaitAndComputeEnemyAnts(delay + 0.01f));
        }
        else if ((1 / Time.deltaTime) > 50 && delay > 0.1f)
        {
            StartCoroutine(WaitAndComputeEnemyAnts(delay - 0.01f));
        }
        else
        {
            StartCoroutine(WaitAndComputeEnemyAnts(delay));
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateWorld();
            GenerateAnts();
            GenerateFogOfWar();
            workersCount = 0;
            fightersCount = 0;
            scoutsCount = 0;
            foodRessource = 1000;
            queenCooldown = 1;
            queenLvl = 1;
            WriteChanges();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            GenerateEnemyWave();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            rawImageFogOfWar.enabled = !rawImageFogOfWar.enabled;
        }

        queenCooldown += Time.deltaTime * ((5+queenLvl) / 10.0f) * 10;
        if (queenCooldown > 1)
        {
            queenCooldown = 1;
        }

        queenLvlText.text = "Queen lvl " + queenLvl;
        queenCooldownSlider.value = queenCooldown;
        foodSupplyText.text = "Supply: " + foodRessource + " food";

        workersCountText.text = "x " + workersCount;
        fightersCountText.text = "x " + fightersCount;
        scoutsCountText.text = "x " + scoutsCount;
    }

    public void EarnFood()
    {
        foodRessource += 100;
        queenLvl++;
    }

    public void SpawnWorker()
    {
        if (queenCooldown == 1 && foodRessource > 0)
        {
            SpawnAnt(AntType.WORKER);
            foodRessource -= 1;
            workersCount++;
        }
    }
    public void SpawnFighter()
    {
        if (queenCooldown == 1 && foodRessource > 0)
        {
            SpawnAnt(AntType.FIGHTER);
            foodRessource -= 1;
            fightersCount++;
        }
    }
    public void SpawnScout()
    {
        if (queenCooldown == 1 && foodRessource > 0)
        {
            SpawnAnt(AntType.SCOUT);
            foodRessource -= 1;
            scoutsCount++;
        }
    }

    public void SpawnAnt(AntType typeOfAnt)
    {
        queenCooldown = 0;
        queenCooldownSlider.value = queenCooldown;
        Ant ant = new Ant(queenPosition, typeOfAnt, true);
        allAnts.Add(ant);
    }

    private Color GetWorldPixel(int x, int y)
    {
        Color result = noneColor;
        try
        {
            result = allWorldPixels[x + y * width];
        }
        catch (System.Exception)
        {

        }
        return result;
    }

    private bool IsWalkablePixel(Color pixColor)
    {
        return pixColor.Equals(bkgDirtColor) || pixColor.Equals(skyColor) || pixColor.Equals(trunkColor) || pixColor.Equals(grassColor) || pixColor.Equals(bushColor) || pixColor.Equals(leavesColor);
    }
    private bool IsDigablePixel(Color pixColor)
    {
        return pixColor.Equals(dirtColor);
    }
    private bool IsEatablePixel(Color pixColor)
    {
        return pixColor.Equals(foodColor);
    }

    private void ClearDeadAnts()
    {
        List<Ant> deadAnts = new List<Ant>();
        foreach (Ant ant in enemyAnts)
        {
            if (ant.health <= 0)
            {
                deadAnts.Add(ant);
            }
        }
        foreach (Ant ant in deadAnts)
        {
            enemyAnts.Remove(ant);
        }

        deadAnts.Clear();
        foreach (Ant ant in allAnts)
        {
            if (ant.health <= 0)
            {
                deadAnts.Add(ant);
                if (ant.antType.Equals(AntType.WORKER))
                {
                    workersCount--;
                }
                if (ant.antType.Equals(AntType.FIGHTER))
                {
                    fightersCount--;
                }
                if (ant.antType.Equals(AntType.SCOUT))
                {
                    scoutsCount--;
                }
            }
        }
        foreach (Ant ant in deadAnts)
        {
            allAnts.Remove(ant);
        }
    }

    private void MoveEnemyAnts()
    {
        foreach (Ant ant in enemyAnts)
        {
            IntVec leftVec; leftVec.x = -1; leftVec.y = 0;
            IntVec rightVec; rightVec.x = 1; rightVec.y = 0;
            IntVec upVec; upVec.x = 0; upVec.y = 1;
            IntVec downVec; downVec.x = 0; downVec.y = -1;

            Color leftPixel = GetWorldPixel(ant.pos.x + leftVec.x, ant.pos.y + leftVec.y);
            Color rightPixel = GetWorldPixel(ant.pos.x + rightVec.x, ant.pos.y + rightVec.y);
            Color topPixel = GetWorldPixel(ant.pos.x + upVec.x, ant.pos.y + upVec.y);
            Color bottomPixel = GetWorldPixel(ant.pos.x + downVec.x, ant.pos.y + downVec.y);

            List<IntVec> walkPossibilities = new List<IntVec>();
            if (IsWalkablePixel(leftPixel))
            {
                walkPossibilities.Add(leftVec);
            }
            if (IsWalkablePixel(rightPixel))
            {
                walkPossibilities.Add(rightVec);
            }
            if (IsWalkablePixel(topPixel))
            {
                walkPossibilities.Add(upVec);
            }
            if (IsWalkablePixel(bottomPixel))
            {
                walkPossibilities.Add(downVec);
            }

            if (ant.pos.x >= 0 && ant.pos.x < width && ant.pos.y >= 0 && ant.pos.y < height)
            {
                allAntsPixels[ant.pos.x + ant.pos.y * width] = new Color(0, 0, 0, 0);
            }

            foreach (Ant allyAnt in allAnts)
            {
                if (allyAnt.pos.Equals(ant.pos))
                {
                    if (allyAnt.antType.Equals(AntType.FIGHTER))
                    {
                        allyAnt.health = 0;
                        ant.health = 0;
                    }
                    else
                    {
                        allyAnt.health = 0;
                    }
                    break;
                }
            }

            if (bottomPixel.Equals(skyColor))
            {
                // fall
                ant.Fall();
            }
            else
            {
                ant.ComputeAction(walkPossibilities, new List<IntVec>());
            }

            if (ant.pos.x >= 0 && ant.pos.x < width && ant.pos.y >= 0 && ant.pos.y < height)
            {
                allAntsPixels[ant.pos.x + ant.pos.y * width] = enemyAntColor;
            }
        }
    }

    private void MoveAnts()
    {
        foreach (Ant ant in allAnts)
        {
            // fog of war
            for (int y = - ant.visionRange; y <= ant.visionRange; y++)
            {
                for (int x = -ant.visionRange; x <= ant.visionRange; x++)
                {
                    float distance = (x * x + y * y) / (ant.visionRange * ant.visionRange * 1.0f);

                    IntVec pixPos;
                    pixPos.x = ant.pos.x + x;
                    pixPos.y = ant.pos.y + y;
                    if (pixPos.x >= 0 && pixPos.x < width && pixPos.y >= 0 && pixPos.y < height)
                    {
                        fogOfWarPixels[pixPos.x + pixPos.y * width] = Color.Lerp(new Color(0, 0, 0, 0), fogOfWarPixels[pixPos.x + pixPos.y * width], distance);
                    }
                }
            }

            IntVec leftVec; leftVec.x = -1; leftVec.y = 0;
            IntVec rightVec; rightVec.x = 1; rightVec.y = 0;
            IntVec upVec; upVec.x = 0; upVec.y = 1;
            IntVec downVec; downVec.x = 0; downVec.y = -1;

            Color leftPixel = GetWorldPixel(ant.pos.x + leftVec.x, ant.pos.y + leftVec.y);
            Color rightPixel = GetWorldPixel(ant.pos.x + rightVec.x, ant.pos.y + rightVec.y);
            Color topPixel = GetWorldPixel(ant.pos.x + upVec.x, ant.pos.y + upVec.y);
            Color bottomPixel = GetWorldPixel(ant.pos.x + downVec.x, ant.pos.y + downVec.y);

            List<IntVec> walkPossibilities = new List<IntVec>();
            List<IntVec> digPossibilities = new List<IntVec>();

            bool grabFood = false;

            if (IsWalkablePixel(leftPixel))
            {
                walkPossibilities.Add(leftVec);
            }
            else if (IsDigablePixel(leftPixel))
            {
                digPossibilities.Add(leftVec);
                digPossibilities.Add(leftVec);
            }
            else if (IsEatablePixel(leftPixel) && ant.antType.Equals(AntType.WORKER) && !ant.carryFood)
            {
                allWorldPixels[ant.pos.x + leftVec.x + (ant.pos.y + leftVec.y) * width] = rightPixel;
                grabFood = true;
                ant.GrabFood();
            }

            if (IsWalkablePixel(rightPixel))
            {
                walkPossibilities.Add(rightVec);
            }
            else if (IsDigablePixel(rightPixel))
            {
                digPossibilities.Add(rightVec);
                digPossibilities.Add(rightVec);
            }
            else if (IsEatablePixel(rightPixel) && ant.antType.Equals(AntType.WORKER) && !ant.carryFood)
            {
                allWorldPixels[ant.pos.x + rightVec.x + (ant.pos.y + rightVec.y) * width] = leftPixel;
                grabFood = true;
                ant.GrabFood();
            }

            if (IsWalkablePixel(topPixel))
            {
                walkPossibilities.Add(upVec);
            }
            else if (IsDigablePixel(topPixel))
            {
                digPossibilities.Add(upVec);
            }
            else if (IsEatablePixel(topPixel) && ant.antType.Equals(AntType.WORKER) && !ant.carryFood)
            {
                allWorldPixels[ant.pos.x + upVec.x + (ant.pos.y + upVec.y) * width] = bottomPixel;
                grabFood = true;
                ant.GrabFood();
            }

            if (IsWalkablePixel(bottomPixel))
            {
                walkPossibilities.Add(downVec);
            }
            else if (IsDigablePixel(bottomPixel))
            {
                digPossibilities.Add(downVec);
            }
            else if (IsEatablePixel(bottomPixel) && ant.antType.Equals(AntType.WORKER) && !ant.carryFood)
            {
                allWorldPixels[ant.pos.x + downVec.x + (ant.pos.y + downVec.y) * width] = topPixel;
                grabFood = true;
                ant.GrabFood();
            }


            if (ant.pos.x >= 0 && ant.pos.x < width && ant.pos.y >= 0 && ant.pos.y < height)
            {
                allAntsPixels[ant.pos.x + ant.pos.y * width] = new Color(0, 0, 0, 0);
            }
            if (ant.pos.x + ant.dir.x >= 0 && ant.pos.x + ant.dir.x < width && ant.pos.y + ant.dir.y >= 0 && ant.pos.y + ant.dir.y < height)
            {
                // undraw food
                allAntsPixels[ant.pos.x + ant.dir.x + (ant.pos.y + ant.dir.y) * width] = new Color(0, 0, 0, 0);
            }

            if (bottomPixel.Equals(skyColor))
            {
                // fall
                ant.Fall();
            }
            else if (grabFood)
            {
                // grab food
                ant.GrabFood();
            }
            else
            {
                ant.ComputeAction(walkPossibilities, digPossibilities);
            }

            if (ant.pos.x >= 0 && ant.pos.x < width && ant.pos.y >= 0 && ant.pos.y < height)
            {
                // draw food
                if (ant.carryFood && ant.pos.x + ant.dir.x >= 0 && ant.pos.x + ant.dir.x < width && ant.pos.y + ant.dir.y >= 0 && ant.pos.y + ant.dir.y < height)
                {
                    allAntsPixels[ant.pos.x + ant.dir.x + (ant.pos.y + ant.dir.y) * width] = foodColor;
                }

                allAntsPixels[ant.pos.x + ant.pos.y * width] = ant.color;

                // dig
                if (allWorldPixels[ant.pos.x + ant.pos.y * width].Equals(dirtColor))
                {
                    allWorldPixels[ant.pos.x + ant.pos.y * width] = bkgDirtColor;
                }
            }
        }
    }

    private void WriteChanges()
    {
        worldTexture.SetPixels(allWorldPixels);
        worldTexture.Apply();

        allAntsPixels[queenPosition.x + queenPosition.y * width] = queenAntColor;
        antTexture.SetPixels(allAntsPixels);
        antTexture.Apply();

        fogOfWarTexture.SetPixels(fogOfWarPixels);
        fogOfWarTexture.Apply();
    }

    private void GenerateFogOfWar()
    {
        fogOfWarPixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                fogOfWarPixels[x + y * width] = Color.black;
            }
        }

        // queen vision
        int queenVisionRange = 15;
        for (int y = -queenVisionRange; y <= queenVisionRange; y++)
        {
            for (int x = -queenVisionRange; x <= queenVisionRange; x++)
            {
                float distance = (x*x + y*y) / (queenVisionRange * queenVisionRange * 1.0f);

                IntVec pixPos;
                pixPos.x = queenPosition.x + x;
                pixPos.y = queenPosition.y + y;
                if (pixPos.x >= 0 && pixPos.x < width && pixPos.y >= 0 && pixPos.y < height)
                {
                    fogOfWarPixels[pixPos.x + pixPos.y * width] = Color.Lerp(new Color(0, 0, 0, 0), fogOfWarPixels[pixPos.x + pixPos.y * width], distance);
                }
            }
        }
    }

    private void GenerateAnts()
    {
        allAnts = new List<Ant>();
        enemyAnts = new List<Ant>();
    }

    private IEnumerator WaitAndSendEnemyWave(float delay)
    {
        yield return new WaitForSeconds(delay);
        GenerateEnemyWave();
        StartCoroutine(WaitAndSendEnemyWave(Random.Range(20, 40)));
    }

    private void GenerateEnemyWave()
    {
        int enemyCount = Random.Range(5, 10);
        for (int i = 0; i<enemyCount; i++)
        {
            IntVec pos;
            pos.x = (Random.Range(0, 2) == 0) ? 0 : (width - 1);
            pos.y = Mathf.CeilToInt(height * 0.75f) + 2;
            Ant enemyAnt = new Ant(pos, AntType.FIGHTER, false);
            enemyAnts.Add(enemyAnt);
        }
    }

    private void GenerateWorld()
    {
        // world itself
        allWorldPixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y > height*0.75f)
                {
                    allWorldPixels[x + y * width] = skyColor;
                }
                else
                {
                    allWorldPixels[x + y * width] = dirtColor;
                }
            }
        }

        // trees
        int treesCount = Random.Range(6, 15);
        for (int i = 0; i < treesCount; i++)
        {
            IntVec randomPosition;
            randomPosition.x = Random.Range(0, width);
            randomPosition.y = Mathf.CeilToInt(0.75f * height) + 1;
            int randomWidth = Random.Range(2, 10);
            int randomHeight = Random.Range(30, 50);

            for (int y = randomPosition.y; y <= randomPosition.y + randomHeight; y++)
            {
                for (int x = randomPosition.x - randomWidth / 2; x <= randomPosition.x + randomWidth / 2; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        allWorldPixels[x + y * width] = trunkColor;
                    }
                }
            }
            int leavesCount = Random.Range(20, 40);
            for (int j = 0; j < leavesCount; j++)
            {
                IntVec randomLeafPosition;
                randomLeafPosition.x = randomPosition.x + Random.Range(-randomWidth, randomWidth);
                randomLeafPosition.y = randomPosition.y + randomHeight + Random.Range(-10, 40);
                int randomLeafSize = Random.Range(3, 8);
                for (int y = randomLeafPosition.y - randomLeafSize; y <= randomLeafPosition.y + randomLeafSize; y++)
                {
                    for (int x = randomLeafPosition.x - randomLeafSize; x <= randomLeafPosition.x + randomLeafSize; x++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            allWorldPixels[x + y * width] = leavesColor;
                        }
                    }
                }
                for (int y = randomLeafPosition.y - 1; y <= randomLeafPosition.y + 1; y++)
                {
                    for (int x = randomLeafPosition.x - 1; x <= randomLeafPosition.x + 1; x++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            allWorldPixels[x + y * width] = foodColor;
                        }
                    }
                }
            }
        }

        // grass
        int grassCount = Random.Range(80, 160);
        for (int i = 0; i < grassCount; i++)
        {
            IntVec randomPosition;
            randomPosition.x = Random.Range(0, width);
            randomPosition.y = Mathf.CeilToInt(0.75f * height) + 1;
            int randomSize = Random.Range(3, 5);

            for (int y = randomPosition.y; y <= randomPosition.y + randomSize; y++)
            {
                if (randomPosition.x >= 0 && randomPosition.x < width && y >= 0 && y < height)
                {
                    allWorldPixels[randomPosition.x + y * width] = grassColor;
                }
            }
        }


        // bushes
        int bushesCount = Random.Range(4, 10);
        for (int i = 0; i < bushesCount; i++)
        {
            IntVec randomPosition;
            randomPosition.x = Random.Range(0, width);
            randomPosition.y = Mathf.CeilToInt(0.75f * height);
            int randomSize = Random.Range(5, 12);

            for (int y = randomPosition.y; y <= randomPosition.y + randomSize; y++)
            {
                for (int x = randomPosition.x - randomSize; x <= randomPosition.x + randomSize; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        if (Random.Range(0, 60) == 0)
                        {
                            allWorldPixels[x + y * width] = foodColor;
                        }
                        else
                        {
                            allWorldPixels[x + y * width] = bushColor;
                        }
                    }
                }
            }
        }

        // rocks
        int rockCount = Random.Range(40, 80);
        for (int i = 0; i < rockCount; i++)
        {
            IntVec randomPosition;
            randomPosition.x = Random.Range(0, width);
            randomPosition.y = Mathf.CeilToInt(Random.Range(height * 0.1f, height * 0.75f));
            int randomRadius = Random.Range(2, 5);
            for (int y = randomPosition.y - randomRadius; y <= randomPosition.y + randomRadius; y++)
            {
                for (int x = randomPosition.x - randomRadius; x <= randomPosition.x + randomRadius; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < (height * 0.75f + 1))
                    {
                        int distFromCenter = Mathf.Abs(randomPosition.y - y) + Mathf.Abs(randomPosition.x - x);
                        float randomExist = Random.Range(0, distFromCenter) + Random.Range(0, distFromCenter) + Random.Range(0, distFromCenter) + Random.Range(0, distFromCenter);
                        if (randomExist < 2 * randomRadius)
                        {
                            allWorldPixels[x + y * width] = rockColor;
                        }
                    }
                }
            }
        }

        // queen dig
        for (int y = queenPosition.y; y <= Mathf.CeilToInt(height * 0.75f); y++)
        {
            allWorldPixels[queenPosition.x + y * width] = bkgDirtColor;
        }

        // no ants
        allAntsPixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                allAntsPixels[x + y * width] = new Color(0,0,0,0);
            }
        }
        // queen
        allAntsPixels[queenPosition.x + queenPosition.y * width] = queenAntColor;

    }
}
