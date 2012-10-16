using UnityEngine;
using System.Collections;
using System.Threading;

public class Main : MonoBehaviour {

    // a 3D kocka élmérete, amiben a játék folyik
    public int size = 10;

    // a tér nagysága, amiben a kockákat elhelyezzük
    public float space = 10f;

    // a kockákat tároló tömb
    private GameObject[, ,] cubes;
    
    // annak az objektumnak a referenciája, amibõl a szálakat indítjuk
    private ComputingThread ct;
    
    // Unity3d event, az induláskor van meghívva minden GameObject-hez csatolt scripten
	public void Start ()
    {
        this.ct = new ComputingThread(this.size);

        this.cubes = this.GenerateCubes(this.size, this.space, this.ct.values);
	}
	
	// Update is called once per frame
	void Update () {
        
	}

    // ennek majd egy IEnumeratorban kell lennie, mert hívni fogunk belõle egy
    // másik coroutine-t (animáció), aminek meg kell várnunk a lefutását, tehát
    // yield-et kell használni
    private IEnumerator Compute()
    {
        this.ct.StartThreads();

        Debug.Log("MAIN Started");
        //Debug.Break();
        //ciklus eleje
        
            //WaitHandle.WaitAll(ct.readAndComputeReady);
            //Debug.Log("MAIN Waited all readAndCompute, G");
            ////itt reseteljük a readinget, amit az elõzõ ciklus végén seteltünk, mert itt az összes háttér szál várakozik
            //ct.startReading.Reset();

            //ct.startWriting.Set();
            //Debug.Log("MAIN Start writing");
            //Debug.Break();
            //WaitHandle.WaitAll(ct.writeReady);
            //Debug.Log("MAIN waited all write");
            //// itt kell megcsinálni a resetet, mivel itt az összes többi szál várakozik
            //ct.startWriting.Reset();

            ////ide kell majd az a kód, ami átállítja a GameObject-eket
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
        
        // ciklus vége
        return null;
    }

    // ez gyártja le a kockákat, amiket megjelenítünk
    // amelyiknél 0-t kell megjeleníteni, azt láthatatlanná tesszük
    private GameObject[,,] GenerateCubes(int size, float space, int[,,] values)
    {
        GameObject[,,] cubeArray = new GameObject[size, size, size];

        // ez alá tesszük a kockákat, hogy az inspectorban áttekinthetõ legyen
        Transform parent = GameObject.Find("Parent").transform;

        // kiszámoljuk, mekkora lehet egy kocka, hogy beleférjenek az adott térbe
        float cubeSize = space / (float)size;

        // kiszámoljuk, hogy hol legyen a kezdõpozíció ahhoz, hogy a teljes kocka közepe a (0,0,0) koordinátában legyen
        // ha a [0,0,0] indexû kocka az egyik sarok
        // nem árt tudni, hogy a Unity egy balkezes koordináta-rendszert használ, csak hogy ne legyen meglepetés a kirajzolásnál
        // minden irányban ugyanakkora értéket kell tolni, lévén, hogy kocka
        // a kocka közepét pozicionáljuk, ezért hozzá kell adni a kis kocka méretébõl fakadó eltolást
        float start = -1 * (space / 2f) + (cubeSize / 2f);

        // szembõl nézve ez a unityben a bal hátsó alsó sarka lesz a kockának
        Vector3 startPos = new Vector3(start, start, start);

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    // létrehozzuk fizikailag a kockát
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.parent = parent;

                    // beállítunk nekik egy kicsit átlátszó, a teljes sokaság egyik sarkától a másikig
                    // feketébõl fehérbe átmenõ színt
                    cube.renderer.material.shader = Shader.Find("Transparent/VertexLit");
                    cube.renderer.material.color = new Color((float)k / (float)size, (float)j / (float)size, (float)i / (float)size, 0.7f);

                    // ha nem 0 tartozik a kockához, akkor láthatatlan
                    cube.renderer.enabled = (values[i, j, k] == 1);

                    // méretezzük és a helyére tesszük a kockát
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
