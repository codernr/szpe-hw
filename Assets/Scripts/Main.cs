using UnityEngine;
using System.Collections;
using System.Threading;

public class Main : MonoBehaviour {

    // a 3D kocka �lm�rete, amiben a j�t�k folyik
    public int size = 10;

    // a t�r nagys�ga, amiben a kock�kat elhelyezz�k
    public float space = 10f;

    // a kock�kat t�rol� t�mb
    private GameObject[, ,] cubes;
    
    // annak az objektumnak a referenci�ja, amib�l a sz�lakat ind�tjuk
    private ComputingThread ct;
    
    // Unity3d event, az indul�skor van megh�vva minden GameObject-hez csatolt scripten
	public void Start ()
    {
        this.ct = new ComputingThread(this.size);

        this.cubes = this.GenerateCubes(this.size, this.space, this.ct.values);
	}
	
	// Update is called once per frame
	void Update () {
        
	}

    // ennek majd egy IEnumeratorban kell lennie, mert h�vni fogunk bel�le egy
    // m�sik coroutine-t (anim�ci�), aminek meg kell v�rnunk a lefut�s�t, teh�t
    // yield-et kell haszn�lni
    private IEnumerator Compute()
    {
        this.ct.StartThreads();

        Debug.Log("MAIN Started");
        //Debug.Break();
        //ciklus eleje
        
            //WaitHandle.WaitAll(ct.readAndComputeReady);
            //Debug.Log("MAIN Waited all readAndCompute, G");
            ////itt resetelj�k a readinget, amit az el�z� ciklus v�g�n setelt�nk, mert itt az �sszes h�tt�r sz�l v�rakozik
            //ct.startReading.Reset();

            //ct.startWriting.Set();
            //Debug.Log("MAIN Start writing");
            //Debug.Break();
            //WaitHandle.WaitAll(ct.writeReady);
            //Debug.Log("MAIN waited all write");
            //// itt kell megcsin�lni a resetet, mivel itt az �sszes t�bbi sz�l v�rakozik
            //ct.startWriting.Reset();

            ////ide kell majd az a k�d, ami �t�ll�tja a GameObject-eket
            ////this.RefreshCubes(this.size, this.ct.values);
            ////teszt
            //string debug = g+". Debug value\n";
            //for (int i = 0; i < this.size; i++)
            //{
            //    debug += i.ToString() + " plane\n";
            //    for (int j = 0; j < this.size; j++)
            //    {
            //        for (int k = 0; k < this.size; k++)
            //            debug += this.ct.values[i, j, k];
            //        debug += "\n";
            //    }
            //    debug += "\n";
            //}
            //Debug.Log(debug);

            ////yield return new WaitForSeconds(1f);

            //ct.startReading.Set();
            //Debug.Log("MAIN Start reading");
        
        // ciklus v�ge
        return null;
    }

    // ez gy�rtja le a kock�kat, amiket megjelen�t�nk
    // amelyikn�l 0-t kell megjelen�teni, azt l�thatatlann� tessz�k
    private GameObject[,,] GenerateCubes(int size, float space, int[,,] values)
    {
        GameObject[,,] cubeArray = new GameObject[size, size, size];

        // ez al� tessz�k a kock�kat, hogy az inspectorban �ttekinthet� legyen
        Transform parent = GameObject.Find("Parent").transform;

        // kisz�moljuk, mekkora lehet egy kocka, hogy belef�rjenek az adott t�rbe
        float cubeSize = space / (float)size;

        // kisz�moljuk, hogy hol legyen a kezd�poz�ci� ahhoz, hogy a teljes kocka k�zepe a (0,0,0) koordin�t�ban legyen
        // ha a [0,0,0] index� kocka az egyik sarok
        // nem �rt tudni, hogy a Unity egy balkezes koordin�ta-rendszert haszn�l, csak hogy ne legyen meglepet�s a kirajzol�sn�l
        // minden ir�nyban ugyanakkora �rt�ket kell tolni, l�v�n, hogy kocka
        // a kocka k�zep�t pozicion�ljuk, ez�rt hozz� kell adni a kis kocka m�ret�b�l fakad� eltol�st
        float start = -1 * (space / 2f) + (cubeSize / 2f);

        // szemb�l n�zve ez a unityben a bal h�ts� als� sarka lesz a kock�nak
        Vector3 startPos = new Vector3(start, start, start);

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    // l�trehozzuk fizikailag a kock�t
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.parent = parent;

                    // be�ll�tunk nekik egy kicsit �tl�tsz�, a teljes sokas�g egyik sark�t�l a m�sikig
                    // feket�b�l feh�rbe �tmen� sz�nt
                    cube.renderer.material.shader = Shader.Find("Transparent/VertexLit");
                    cube.renderer.material.color = new Color((float)k / (float)size, (float)j / (float)size, (float)i / (float)size, 0.7f);

                    // ha nem 0 tartozik a kock�hoz, akkor l�thatatlan
                    cube.renderer.enabled = (values[i, j, k] == 1);

                    // m�retezz�k �s a hely�re tessz�k a kock�t
                    cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
                    cube.transform.position = startPos + new Vector3(i * cubeSize, j * cubeSize, k * cubeSize);

                    cubeArray[i, j, k] = cube;
                }
            }
        }

        return cubeArray;
    }

    private void RefreshCubes(int size, int[, ,] values)
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    this.cubes[i,j,k].renderer.enabled = (values[i, j, k] == 1);
                }
            }
        }
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10,10,200,200), "Start"))
        {
            this.StartCoroutine("Compute");
        }
    }
}
