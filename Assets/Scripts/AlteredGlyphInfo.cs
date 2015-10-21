using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This class contains information about glyph that are about to be
// moved after a collapse or new glyph creation event.
public class AlteredGlyphInfo {

    // List with all the glyphs to be moved
    private List<GameObject> newGlyph { get; set; }
    public int MaxDistance { get; set; }

    // Returns distinct list of altered glyphs
    public IEnumerable<GameObject> AlteredGlyph
    {
        get
        {
            return newGlyph.Distinct();
        }
    }

    // Method to add a new glyph to the list
    public void AddGlyph(GameObject go)
    {
        if (!newGlyph.Contains(go))
        {
            newGlyph.Add(go);
        }
    }

    // Constructor that initializes the private list
    public AlteredGlyphInfo()
    {
        newGlyph = new List<GameObject>();
    }
}
