using System;
using UnityEngine;

[Flags]
public enum BonusType
{
	None,
	DestroyWholeRowColumn
}

public static class BonusTypeUtilities
{
	// Helper method to check for specific bonus type
	public static bool ContainsDestroyWHoleRowClumn(BonusType bt)
	{
		return (bt & BonusType.DestroyWholeRowColumn) == BonusType.DestroyWholeRowColumn;
	}
}

// The game state
public enum GameState
{
	None,
	SelectionStarted,
	Animating
}
