using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour {

	// Use this for initialization
	void Start () {

        float startPos = -4.5f;

        float time = Time.time;

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                for (int k = 0; k < 10; k++)
                {
                    Graphics.CreateCube(new Vector3(startPos + i, startPos + j, startPos + k), 1f);
                }
            }
        }

        Debug.Log(Time.time - time);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
