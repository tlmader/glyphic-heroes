using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ShapesManager : MonoBehaviour
{

	public Text DebugText, ScoreText;
	public bool ShowDebugInfo = false;

	public ShapesArray shapes;

	private int score;

	public readonly Vector2 BottomRight = new Vector2(-2.37f, -4.27f);
	public readonly Vector2 GlyphSize = new Vector2(0.7f, 0.7f);

	private GameState state = GameState.None;
	private GameObject hitGo = null;
	private Vector2[] SpawnPositions;

	public GameObject[] GlyphPrefabs;
	public GameObject[] ExplosionPrefabs;
	public GameObject[] BonusPrefabs;

	private IEnumerator CheckPotentialMatchesCoroutine;
	private IEnumerator AnimatePotentialMatchesCoroutine;

	IEnumerable<GameObject> potentialMatches;

	public SoundManager soundManager;

	void Awake()
	{
		DebugText.enabled = ShowDebugInfo;
	}

	// Calls three methods to initialize the game
	void Start()
	{
		InitializeTypesOnPrefabShapesAndBonuses();

		InitializeGlyphAndSpawnPositions();

		StartCheckForPotentialMatches();
	}

	// 
	private void InitializeTypesOnPrefabShapesAndBonuses()
	{
		foreach (var item in GlyphPrefabs)
		{
			item.GetComponent<Shape>().Type = item.name;
		}

		foreach (var item in BonusPrefabs)
		{
			item.GetComponent<Shape>().Type = GlyphPrefabs.
				Where(x => x.GetComponent<Shape>().Type.Contains(item.name.Split('_')[1].Trim())).Single().name;
		}
	}


	// Score-related methods
	private void InitializeVariables()
	{
		score = 0;
		ShowScore();
	}

	private void IncreaseScore(int amount)
	{
		score += amount;
		ShowScore();
	}

	private void ShowScore()
	{
		ScoreText.text = "Score: " + score.ToString();
	}
	
	private GameObject GetRandomGlyph()
	{
		return GlyphPrefabs[Random.Range(0, GlyphPrefabs.Length)];
	}

	// Creates a new glyph GameObject at the specified row and column at the specified position.
	private void InstantiateAndPlaceNewGlyph(int row, int column, GameObject newGlyph)
	{
		GameObject go = Instantiate(newGlyph,
			BottomRight + new Vector2(column * GlyphSize.x, row * GlyphSize.y), Quaternion.identity)
			as GameObject;

		// Assign the specific properties
		go.GetComponent<Shape>().Assign(newGlyph.GetComponent<Shape>().Type, row, column);
		shapes[row, column] = go;
	}

	// Gives initial values to spawn positions
	private void SetupSpawnPositions()
	{
		// Create the spawn positions for the new shapes (will pop from the top)
		for (int column = 0; column < Constants.Columns; column++)
		{
			SpawnPositions[column] = BottomRight
				+ new Vector2(column * GlyphSize.x, Constants.Rows * GlyphSize.y);
		}
	}

	// Calls the GameObject.Destroy method on all glyphs in the array
	private void DestroyAllGlyphs()
	{
		for (int row = 0; row < Constants.Rows; row++)
		{
			for (int column = 0; column < Constants.Columns; column++)
			{
				Destroy(shapes[row, column]);
			}
		}
	}

	void Update()
	{

	}

	// Initializes the score variables
	// Destroys all elements in the array
	// Reinitializes the array and the spawn positions for the new glyphs
	// Loops through all the array elements and creates new glyphs while not creating matches

	public void InitializeGlyphSpawnPositions()
	{
		InitializeVariables();

		if (shapes != null)
		{
			DestroyAllGlyphs();
		}

		shapes = new ShapesArray();
		SpawnPositions = new Vector2[Constants.Columns];

		for (int row = 0; row < Constants.Rows; row++)
		{
			for (int column = 0; column < Constants.Columns; column++)
			{
				GameObject newGlyph = GetRandomGlyph();

				// Check if two previous horizontal glyphs are of the same type
				while (column >= 2 && shapes[row, column - 1].GetComponent<Shape>()
					.IsSameType(newGlyph.GetComponent<Shape>())
					&& shapes[row, column - 2].GetComponent<Shape>().IsSameType(newGlyph.GetComponent<Shape>()))
				{
					newGlyph = GetRandomGlyph();
				}

				// Check if two previous vertical glyphs are of the same type
				while (row >= 2 && shapes[row - 1, column].GetComponent<Shape>()
					.IsSameType(newGlyph.GetComponent<Shape>())
					&& shapes[row - 2, column].GetComponent<Shape>().IsSameType(newGlyph.GetComponent<Shape>()))
				{
					newGlyph = GetRandomGlyph();
				}

				InstantiateAndPlaceNewGlyph(row, column, newGlyph);
			}
		}

		SetupSpawnPositions();
	}

	// Used during a user swap to make sure that the dragged glyph will appear on top of the other onej
	private void FixSortingLayer(GameObject hitGo, GameObject hitGo2)
	{
		SpriteRenderer sp1 = hitGo.GetComponent<SpriteRenderer>();
		SpriteRenderer sp2 = hitGo2.GetComponent<SpriteRenderer>();
		if (sp1.sortingOrder <= sp2.sortingOrder)
		{
			sp1.sortingOrder = 1;
			sp2.sortingOrder = 0;
		}
	}

	// If there are any matches, animate them
	private IEnumerator CheckPotentialMatches()
	{
		yield return new WaitForSeconds(Constants.WaitBeforePotentialMatchesCheck);
		potentialMatches = Utilities.GetPotentialMatches(shapes);
		if (potentialMatches != null)
		{
			while (true)
			{
				AnimatePotentialMatchesCoroutine = Utilities.AnimatePotentialMatches(potentialMatches);
				StartCoroutine(AnimatePotentialMatchesCoroutine);
				yield return new WaitForSeconds(Constants.WaitBeforePotentialMatchesCheck);
			}
		}
	}

	// Sets the opacity to default (1.0f) at the glyphs that were animated, as potential matches
	private void ResetOpacityOnPotentialMatches()
	{
		if (potentialMatches != null)
		{
			foreach (var item in potentialMatches)
			{
				if (item == null)
				{
					break;
				}

				Color c = item.GetComponent<SpriteRenderer>().color;
				c.a = 1.0f;
				item.GetComponent<SpriteRenderer>().color = c;
			}
		}
	}

	// Stops the check if already running and starts the CheckPotentialMatches coroutine
	private void StartCheckForPotentialMatches()
	{
		StopCheckForPotentialMatches();

		// Get a reference to stop it later
		CheckPotentialMatchesCoroutine = CheckPotentialMatches();
		StartCoroutine(CheckPotentialMatchesCoroutine);
	}

	// Attempts to stop both the AnimatePotentialMatches and the CheckPotentialMatches coroutines
	// Resets the opacity on the items that were previously animated
	private void StopCheckForPotentialMatches()
	{
		if (AnimatePotentialMatchesCoroutine != null)
		{
			StopCoroutine(AnimatePotentialMatchesCoroutine);
		}

		if (CheckPotentialMatchesCoroutine != null)
		{
			StopCoroutine(CheckPotentialMatchesCoroutine);
		}

		ResetOpacityOnPotentialMatches();
	}

	// Returns a random explosion prefab
	private GameObject GetRandomExplosion()
	{
		return ExplosionPrefabs[Random.Range(0, ExplosionPrefabs.Length)];
	}

	// Returns the bonus prefab that corresponds to a normal glyph type
	private GameObject GetBonusFromType(string type)
	{
		string color = type.Split('_')[1].Trim();
		foreach (var item in BonusPrefabs)
		{
			if (item.GetComponent<Shape>().Type.Contains(color))
			{
				return item;
			}
		}

		throw new System.Exception("Wrong type");
	}

	// Creates a new explosion, sets it to be destroyed after a specified time, and destroys the glyph
	// which is passed as a parameter
	private void RemoveFromScene(GameObject item)
	{
		GameObject explosion = GetRandomExplosion();
		var newExplosion = Instantiate(explosion, item.transform.position, Quaternion.identity) as GameObject;
		Destroy(newExplosion, Constants.ExplosionDuration);
		Destroy(item);
	}

	// Utilizes the GoKit animation library to animate a collection of GameObjects to their new position
	private void MoveAndAnimate(IEnumerable<GameObject> movedGameObjects, int distance)
	{
		foreach (var item in movedGameObjects)
		{
			item.transform.positionTo(Constants.MoveAnimationMinDuration * distance, BottomRight +
				new Vector2(item.GetComponent<Shape>().Column * GlyphSize.x, item.GetComponent<Shape>().Row * GlyphSize.y));
		}
	}

	private AlteredGlyphInfo CreateNewGlyphsInSpecificColumns(IEnumerable<int> columnsWithMissingGlyphs)
	{

	}

	private void CreateBonus(Shape hitGoCache)
	{

	}

	private IEnumerator FindMatchesAndCollapse(RaycastHit2D hit2)
	{

	}

	private GameObject GetSpecificGlyphOrBonusForPremadeLevel(string info)
	{

	}

	private void InitializeGlyphAndSpawnPositionsFromPremadeLevel()
	{

	}
}