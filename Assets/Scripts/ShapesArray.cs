using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// A two dimensional array stores the glyph shapes
public class ShapesArray : MonoBehaviour {

	// Two dimensional array is declared with dimensions corresponding to values taken from the Constants class
	private GameObject[,] shapes = new GameObject[Constants.Rows, Constants.Columns];

	private GameObject backupG1;
	private GameObject backupG2;

	// Indexer that returns the specific GameObject via requested column/row
	public GameObject this[int row, int column]
	{
		get
		{
			try
			{
				return shapes[row, column];
			}
			catch (Exception ex)
			{
				throw;
			}
		}
		set
		{
			shapes[row, column] = value;
		}
	}

	public void Swap(GameObject g1, GameObject g2)
	{
		// Hold a backup in case no match is produced
		backupG1 = g1;
		backupG2 = g2;

		var g1Shape = g1.GetComponent<Shape>();
		var g2Shape = g2.GetComponent<Shape>();

		// Get array indexes
		int g1Row = g1Shape.Row;
		int g1Column = g1Shape.Column;
		int g2Row = g2Shape.Row;
		int g2Column = g2Shape.Column;

		// Swap them in the array
		var temp = shapes[g1Row, g1Column];
		shapes[g1Row, g1Column] = shapes[g2Row, g2Column];
		shapes[g2Row, g2Column] = temp;

		// Swap their respective properties
		Shape.SwapColumnRow(g1Shape, g2Shape);
	}

	// This method will undo the swap by simply calling the Swap method on the backup GameObjects
	public void UndoSwap()
	{
		if (backupG1 == null || backupG2 == null)
		{
			throw new Exception("Backup is null");
		}
		Swap(backupG1, backupG2);
	}

	// This method does a horizontal check for matches checking
	private IEnumerable<GameObject> GetMatchesHorizontally(GameObject go)
	{
		List<GameObject> matches = new List<GameObject>();
		matches.Add(go);
		var shape = go.GetComponent<Shape>();

		// Check left
		if (shape.Column != 0)
		{
			for (int column = shape.Column - 1; column >= 0; column--)
			{
				if (shapes[shape.Row, column].GetComponent<Shape>().IsSameType(shape))
				{
					matches.Add(shapes[shape.Row, column]);
				}
				else
				{
					break;
				}
			}
		}
		// Check right
		if (shape.Column != Constants.Columns - 1)
		{
			for (int column = shape.Column + 1; column < Constants.Columns; column++)
			{
				if (shapes[shape.Row, column].GetComponent<Shape>().IsSameType(shape))
				{
					matches.Add(shapes[shape.Row, column]);
				}
				else
				{
					break;
				}
			}
		}

		// If more than three matches
		if (matches.Count < Constants.MinimumMatches)
		{
			matches.Clear();
		}
		return matches.Distinct();
	}

	// This method does a horizontal check for matches checking
	private IEnumerable<GameObject> GetMatchesVertically(GameObject go)
	{
		List<GameObject> matches = new List<GameObject>();
		matches.Add(go);
		var shape = go.GetComponent<Shape>();

		// Check bottom
		if (shape.Row != 0)
		{
			for (int row = shape.Row - 1; row >= 0; row--)
			{
				if (shapes[row, shape.Column] != null &&
					shapes[row, shape.Column].GetComponent<Shape>().IsSameType(shape))
				{
					matches.Add(shapes[row, shape.Column]);
				}
				else
				{
					break;
				}
			}
		}

		// Check top
		if (shape.Row != Constants.Rows - 1)
		{
			for (int row = shape.Row + 1; row < Constants.Rows; row++)
			{
				if (shapes[row, shape.Column] != null &&
					shapes[row, shape.Column].GetComponent<Shape>().IsSameType(shape))
				{
					matches.Add(shapes[row, shape.Column]);
				}
				else
				{
					break;
				}
			}
		}

		// If more than three matches
		if (matches.Count < Constants.MinimumMatches)
		{
			matches.Clear();
		}
		return matches.Distinct();
	}

	// Returns the collection of GameObjects that belong in a specific row
	private IEnumerable<GameObject> GetEntireRow(GameObject go)
	{
		List<GameObject> matches = new List<GameObject>();
		int row = go.GetComponent<Shape>().Row;
		for (int column = 0; column < Constants.Columns; column++)
		{
			matches.Add(shapes[row, column]);
		}
		return matches;
	}

	// Returns the collection of GameObjects that belong in a specific column
	private IEnumerable<GameObject> GetEntireColumn(GameObject go)
	{
		List<GameObject> matches = new List<GameObject>();
		int column = go.GetComponent<Shape>().Column;
		for (int row = 0; row < Constants.Rows; row++)
		{
			matches.Add(shapes[row, column]);
		}
		return matches;
	}

	// Checks if a collection of matches contains a bonus glyph with type "DestroyRowColumn."
	// This marks the entire row/column to be removed later.
	private bool ContainsDestroyRowColumnBonus(IEnumerable<GameObject> matches)
	{
		if (matches.Count() >= Constants.MinimumMatches)
		{
			foreach (var go in matches)
			{
				if (BonusTypeUtilities.ContainsDestroyWholeRowColumn(go.GetComponent<Shape>().Bonus))
				{
					return true;
				}
			}
		}
		return false;
	}

	// Checks for horizontal matches.
	// If there are any bonuses there, it will retrieve the entire row. It will also add the DestroyWholeRowColumn
	// bonus flag to the matchesInfo.BonusesContained property if it does not already exist.
	// Adds the horizontal matches to the MatchesInfo instance.
	// Repeats the same 3 steps while checking vertically.
	public MatchesInfo GetMatches(GameObject go)
	{
		MatchesInfo matchesInfo = new MatchesInfo();

		var horizontalMatches = GetMatchesHorizontally(go);
		if (ContainsDestroyRowColumnBonus(horizontalMatches))
		{
			horizontalMatches = GetEntireRow(go);
			if (!BonusTypeUtilities.ContainsDestroyWholeRowColumn(matchesInfo.BonusesContained))
			{
				matchesInfo.BonusesContained |= BonusType.DestroyWholeRowColumn;
			}
		}
		matchesInfo.AddObjectRange(horizontalMatches);

		var verticalMatches = GetMatchesVertically(go);
		if (ContainsDestroyRowColumnBonus(verticalMatches))
		{
			verticalMatches = GetEntireColumn(go);
			if (!BonusTypeUtilities.ContainsDestroyWholeRowColumn(matchesInfo.BonusesContained))
			{
				matchesInfo.BonusesContained |= BonusType.DestroyWholeRowColumn;
			}
		}
		matchesInfo.AddObjectRange(verticalMatches);

		return matchesInfo;
	}

	// This overload gets a collection of GameObjects as a parameter.
	// For each one, it will use the previously described overload to check for matches.
	public IEnumerable<GameObject> GetMatches(IEnumerable<GameObject> gos)
	{
		List<GameObject> matches = new List<GameObject>();
		foreach (var go in gos)
		{
			matches.AddRange(GetMatches(go).MatchedGlyph);
		}
		return matches.Distinct();
	}

	// Removes (sets as null) an item from the array. It will be called once for each match encountered.
	public void Remove(GameObject item)
	{
		shapes[item.GetComponent<Shape>().Row, item.GetComponent<Shape>().Column] = null;
	}

	// Collapses the remaining glyphs in the specified columns, after the matched glyphs removal.
	// It searches for null items. If it finds any, it will move the nearest top glyph to the null item position.
	// It will continue until all null items are stacked on top positions of the column.
	// It will calculate the max distance a glyph will have to be moved - this will assist in calculating the
	// animation duration.
	// All the required information is passed into an AlteredGlyphInfo class, which is returned to the caller.
	public AlteredGlyphInfo Collapse(IEnumerable<int> columns)
	{
		AlteredGlyphInfo collapseInfo = new AlteredGlyphInfo();

		// Search in every column
		foreach (var column in columns)
		{
			// Begin from bottom row
			for (int row = 0; row < Constants.Rows - 1; row++)
			{
				// If you find a null item
				if (shapes[row, column] == null)
				{
					// Start searching for the first non-null
					for (int row2 = row + 1; row2 < Constants.Rows; row2++)
					{
						// If one is found, bring it down (replace it with the found null)
						if (shapes[row2, column] != null)
						{
							shapes[row, column] = shapes[row2, column];
							shapes[row2, column] = null;

							// Calculate the biggest distance
							if (row2 - row > collapseInfo.MaxDistance)
							{
								collapseInfo.MaxDistance = row2 - row;
							}

							// Assign new row and column (name does not change)
							shapes[row, column].GetComponent<Shape>().Row = row;
							shapes[row, column].GetComponent<Shape>().Column = column;

							collapseInfo.AddCandy(shapes[row, column]);
							break;
                        }
					}
				}
			}
		}
		return collapseInfo;
	}

	// Gets a specified column as a parameter. It will return the Shape details (positions)
	// via the ShapeInfo class in this column which are null.
	public IEnumerable<ShapeInfo> GetEmptyItemsOnColumn(int column)
	{
		List<ShapeInfo> emptyItems = new List<ShapeInfo>();
		for (int row = 0; row < Constants.Rows; row++)
		{
			if (shapes[row, column] == null)
			{
				emptyItems.Add(new ShapeInfo() { Row = row, Column = column });
			}
		}
		return emptyItems;
	}
}

// The ShapeInfo class contains details about row and column for a shape.
public class ShapeInfo
{
	public int Column { get; set; }
	public int Row { get; set; }
}
