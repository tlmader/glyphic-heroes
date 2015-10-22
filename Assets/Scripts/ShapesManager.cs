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

	// Initialize shapes
	private void InitializeTypesOnPrefabShapesAndBonuses()
	{
		// Assign the name of the prefab
		foreach (var item in GlyphPrefabs)
		{
			item.GetComponent<Shape>().Type = item.name;
		}

		// Assign the name of the respective "normal" glpyh as the type of the bonus
		foreach (var item in BonusPrefabs)
		{
			item.GetComponent<Shape>().Type = GlyphPrefabs.
				Where(x => x.GetComponent<Shape>().Type.Contains(item.name.Split('_')[1].Trim())).Single().name;
		}
	}

	// Initializes the score variables
	// Destroys all elements in the array
	// Reinitializes the array and the spawn positions for the new glyphs
	// Loops through all the array elements and creates new glyphs while not creating matches
	public void InitializeGlyphAndSpawnPositions()
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
		if (state == GameState.None)
		{
			// User has clicked or touched
			if (Input.GetMouseButtonDown(0))
			{
				// Get the hit position
				var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

				// If there is a hit
				if (hit.collider != null)
				{
					hitGo = hit.collider.gameObject;
					state = GameState.SelectionStarted;
				}
			}
		}
		else if (state == GameState.SelectionStarted)
		{
			// User dragged
			if (Input.GetMouseButton(0))
			{
				var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
				
				// If there is a hit
				if (hit.collider != null && hitGo != hit.collider.gameObject)
				{
					// Hide hints
					StopCheckForPotentialMatches();

					// If the two shapes are diagonally aligned (different row and column), return
					if (!Utilities.AreVerticalOrHorizontalNeighbors(hitGo.GetComponent<Shape>(),
						hit.collider.gameObject.GetComponent<Shape>()))
					{
						state = GameState.None;
					}
					else
					{
						state = GameState.Animating;
						FixSortingLayer(hitGo, hit.collider.gameObject);
						StartCoroutine(FindMatchesAndCollapse(hit));
					}
				}
			}
		}
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

	private IEnumerator FindMatchesAndCollapse(RaycastHit2D hit2)
	{
		// Get the second item that was part of the swipe
		var hitGo2 = hit2.collider.gameObject;
		shapes.Swap(hitGo, hitGo2);

		// Move the swapped ones
		hitGo.transform.positionTo(Constants.AnimationDuration, hitGo2.transform.position);
		hitGo2.transform.positionTo(Constants.AnimationDuration, hitGo.transform.position);
		yield return new WaitForSeconds(Constants.AnimationDuration);

		// Get the matches via the helper methods
		var hitGoMatchesInfo = shapes.GetMatches(hitGo);
		var hitGo2MatchesInfo = shapes.GetMatches(hitGo2);

		var totalMatches = hitGoMatchesInfo.MatchedGlyph.Union(hitGo2MatchesInfo.MatchedGlyph).Distinct();

		// If the swap did not create at least a 3-match, undo the swap
		if (totalMatches.Count() < Constants.MinimumMatches)
		{
			hitGo.transform.positionTo(Constants.AnimationDuration, hitGo2.transform.position);
			hitGo2.transform.positionTo(Constants.AnimationDuration, hitGo.transform.position);
			yield return new WaitForSeconds(Constants.AnimationDuration);

			shapes.UndoSwap();
		}

		// If more than 3 matches and no Bonus is contained in the line, award a new Bonus
		bool addBonus = totalMatches.Count() >= Constants.MinimumMatchesForBonus &&
			!BonusTypeUtilities.ContainsDestroyWholeRowColumn(hitGoMatchesInfo.BonusesContained) &&
			!BonusTypeUtilities.ContainsDestroyWholeRowColumn(hitGo2MatchesInfo.BonusesContained);

		Shape hitGoCache = null;
		if (addBonus)
		{
			hitGoCache = new Shape();

			// Get the game object that was of the same type
			var sameTypeGo = hitGoMatchesInfo.MatchedGlyph.Count() > 0 ? hitGo : hitGo2;
			var shape = sameTypeGo.GetComponent<Shape>();

			// Cache the game object
			hitGoCache.Assign(shape.Type, shape.Row, shape.Column);
		}

		int timesRun = 1;
		while (totalMatches.Count() >= Constants.MinimumMatches)
		{
			// Increase score
			IncreaseScore((totalMatches.Count() - 2) * Constants.Match3Score);

			if (timesRun >= 2)
			{
				IncreaseScore(Constants.SubsequentMatchScore);
			}

			soundManager.PlayGlyph();

			foreach (var item in totalMatches)
			{
				shapes.Remove(item);
				RemoveFromScene(item);
			}

			// Check and instantiate Bonus if needed
			if (addBonus)
			{
				CreateBonus(hitGoCache);
			}

			addBonus = false;

			// Get the columns that have a collapse
			var columns = totalMatches.Select(go => go.GetComponent<Shape>().Column).Distinct();

			// The order the two methods below get called is important

			// Collapse the ones gone
			var collapsedGlyphInfo = shapes.Collapse(columns);

			// Create new ones
			var newGlyphInfo = CreateNewGlyphsInSpecificColumns(columns);

			int maxDistance = Mathf.Max(collapsedGlyphInfo.MaxDistance, newGlyphInfo.MaxDistance);

			MoveAndAnimate(newGlyphInfo.AlteredGlyph, maxDistance);
			MoveAndAnimate(collapsedGlyphInfo.AlteredGlyph, maxDistance);

			// Wait for both of the above animations
			yield return new WaitForSeconds(Constants.MoveAnimationMinDuration * maxDistance);

			// Search if there are matches with the new/collapsed items
			totalMatches = shapes.GetMatches(collapsedGlyphInfo.AlteredGlyph).
				Union(shapes.GetMatches(newGlyphInfo.AlteredGlyph)).Distinct();

			timesRun++;
		}

		state = GameState.None;
		StartCheckForPotentialMatches();
	}

	// Creates a new bonus based on the glyph type given as parameter,
	// assigns the new GameObject to its proper position in the array,
	// sets necessary variables via the Assign method,
	// adds the DestroyWholeRowColumn bonus type to the Bonus property
	private void CreateBonus(Shape hitGoCache)
	{
		GameObject Bonus = Instantiate(GetBonusFromType(hitGoCache.Type), BottomRight
			+ new Vector2(hitGoCache.Column * GlyphSize.x, hitGoCache.Row * GlyphSize.y), Quaternion.identity)
			as GameObject;
		shapes[hitGoCache.Row, hitGoCache.Column] = Bonus;
		var BonusShape = Bonus.GetComponent<Shape>();

		// Will have the same type as the "normal" glyph
		BonusShape.Assign(hitGoCache.Type, hitGoCache.Row, hitGoCache.Column);

		// Add the proper Bonus type
		BonusShape.Bonus |= BonusType.DestroyWholeRowColumn;
	}

	// Takes the columns that have been missing glyphs as a parameter
	private AlteredGlyphInfo CreateNewGlyphsInSpecificColumns(IEnumerable<int> columnsWithMissingGlyphs)
	{
		AlteredGlyphInfo newGlyphInfo = new AlteredGlyphInfo();

		// Find how many null values the column has
		foreach (int column in columnsWithMissingGlyphs)
		{
			var emptyItems = shapes.GetEmptyItemsOnColumn(column);
			foreach (var item in emptyItems)
			{
				var go = GetRandomGlyph();
				GameObject newGlyph = Instantiate(go, SpawnPositions[column], Quaternion.identity)
					as GameObject;

				newGlyph.GetComponent<Shape>().Assign(go.GetComponent<Shape>().Type, item.Row, item.Column);

				if (Constants.Rows - item.Row > newGlyphInfo.MaxDistance)
				{
					newGlyphInfo.MaxDistance = Constants.Rows - item.Row;
				}

				shapes[item.Row, item.Column] = newGlyph;
				newGlyphInfo.AddGlyph(newGlyph);
			}
		}

		return newGlyphInfo;
	}

	// Utilizes the GoKit animation library to animate a collection of GameObjects to their new position
	private void MoveAndAnimate(IEnumerable<GameObject> movedGameObjects, int distance)
	{
		foreach (var item in movedGameObjects)
		{
			item.transform.positionTo(Constants.MoveAnimationMinDuration * distance,
				BottomRight + new Vector2(item.GetComponent<Shape>().Column * GlyphSize.x,
				item.GetComponent<Shape>().Row * GlyphSize.y));
		}
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

	private GameObject GetRandomGlyph()
	{
		return GlyphPrefabs[Random.Range(0, GlyphPrefabs.Length)];
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
}