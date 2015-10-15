using UnityEngine;
using System.Collections;

public class Gem : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public bool sameType(Gem other) {
        return GetComponent<SpriteRenderer>().sprite == other.GetComponent<SpriteRenderer>().sprite;
    }
}
