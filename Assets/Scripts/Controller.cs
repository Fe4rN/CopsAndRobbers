using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    void Start()
    {
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //TODO: Inicializar matriz a 0's

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        //TODO: Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            int x = i % 8;      // columna
            int y = i / 8;      // fila

            if (y > 0) matriu[i, i - 8] = 1;      // arriba
            if (y < 7) matriu[i, i + 8] = 1;      // abajo
            if (x > 0) matriu[i, i - 1] = 1;      // izquierda
            if (x < 7) matriu[i, i + 1] = 1;      // derecha
        }

        //TODO: Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            tiles[i].adjacency = new List<int>();

            for (int j = 0; j < Constants.NumTiles; j++)
            {
                if (matriu[i, j] == 1)
                {
                    tiles[i].adjacency.Add(j);
                }
            }
        }
    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;
                break;
        }
    }

    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        int robberTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[robberTile].current = true;

        // Encuentra casillas alcanzables desde la posición actual del ladrón
        FindSelectableTiles(false);

        List<Tile> opciones = new List<Tile>();

        foreach (Tile t in tiles)
        {
            if (t.selectable)
            {
                opciones.Add(t);
            }
        }

        if (opciones.Count == 0)
        {
            Debug.Log("El ladrón no tiene casillas alcanzables.");
            return;
        }

        // Buscar la casilla más lejana de los dos policías
        int cop1Tile = cops[0].GetComponent<CopMove>().currentTile;
        int cop2Tile = cops[1].GetComponent<CopMove>().currentTile;

        Tile mejorOpcion = opciones[0];
        int mejorDistancia = -1;

        foreach (Tile t in opciones)
        {
            int distCop1 = CalcularDistancia(cop1Tile, t.numTile);
            int distCop2 = CalcularDistancia(cop2Tile, t.numTile);

            int minDist = Mathf.Min(distCop1, distCop2);

            if (minDist > mejorDistancia)
            {
                mejorDistancia = minDist;
                mejorOpcion = t;
            }
        }

        // Mover al ladrón a la mejor opción
        robber.GetComponent<RobberMove>().MoveToTile(mejorOpcion);
        robber.GetComponent<RobberMove>().currentTile = mejorOpcion.numTile;
    }


    public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;

    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        tiles[indexcurrentTile].current = true;

        // Reset casillas
        foreach (Tile t in tiles)
        {
            t.selectable = false;
            t.visited = false;
            t.parent = null;
            t.distance = 0;
        }

        Queue<Tile> queue = new Queue<Tile>();
        Tile startTile = tiles[indexcurrentTile];
        startTile.visited = true;
        startTile.distance = 0;

        queue.Enqueue(startTile);

        int otherCopTile = -1;

        if (cop)
        {
            // Calculamos la posición del otro policía
            int otherCopIndex = clickedCop == 0 ? 1 : 0;
            otherCopTile = cops[otherCopIndex].GetComponent<CopMove>().currentTile;
        }

        while (queue.Count > 0)
        {
            Tile current = queue.Dequeue();

            if (current.distance > 0) // No marcamos la casilla inicial
                current.selectable = true;

            if (current.distance < 2)
            {
                foreach (int adjIndex in current.adjacency)
                {
                    Tile neighbor = tiles[adjIndex];

                    // Si es policía, evitamos pasar por la casilla del otro policía
                    if (cop && adjIndex == otherCopTile)
                        continue;

                    if (!neighbor.visited)
                    {
                        neighbor.visited = true;
                        neighbor.parent = current;
                        neighbor.distance = current.distance + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    private int CalcularDistancia(int origen, int destino)
    {
        // BFS simple para encontrar la distancia entre dos casillas
        Queue<int> queue = new Queue<int>();
        Dictionary<int, int> dist = new Dictionary<int, int>();

        queue.Enqueue(origen);
        dist[origen] = 0;

        while (queue.Count > 0)
        {
            int actual = queue.Dequeue();

            if (actual == destino)
            {
                return dist[actual];
            }

            foreach (int vecino in tiles[actual].adjacency)
            {
                if (!dist.ContainsKey(vecino))
                {
                    dist[vecino] = dist[actual] + 1;
                    queue.Enqueue(vecino);
                }
            }
        }

        return int.MaxValue; // No se puede alcanzar (no debería ocurrir)
    }
}
