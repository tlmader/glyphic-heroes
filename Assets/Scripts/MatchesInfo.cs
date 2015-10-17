using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This class contains useful information about the glyphs that were matches (either a match of three or more).
public class MatchesInfo {

	private List<GameObject> matchedGlyphs;
	public BonusType BonusesContained { get; set; }

	public MatchesInfo()
	{
		matchedGlyphs = new List<GameObject>();
		BonusesContained = BonusType.None;
	}

	public void AddObject(GameObject go)
	{
		if (!matchedGlyphs.Contains(go))
		{
			matchedGlyphs.Add(go);
		}
	}

	public void AddObjectRange(IEnumerable<GameObject> gos)
	{
		foreach (var item in gos)
		{
			AddObject(item);
		}
	}

	public IEnumerable<GameObject> MatchedGlyph
	{
		get
		{
			return matchedGlyphs.Distinct();
		}
	}
}
