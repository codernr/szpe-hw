using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class Main : MonoBehaviour {

    // a 3D kocka élmérete, amiben a játék folyik
    public int size = 10;

    // a tér nagysága, amiben a kockákat elhelyezzük
    public float space = 10f;

    // hány generációig fusson a program
    public int generation = 10;

    // állapotjelzõ flagek, javarészt a GUI használja annak megállapítására, hogy mit kell kiírnia
    public Dictionary<string, bool> state = new Dictionary<string, bool>
    {
        { "running", false }, { "loading", false }, { "start", true }, { "terminated", false }
    };

    // kocka renderer tömb
    private Renderer[, ,] renderers;

    // két generáció közt eltelt idõ
    public float generationDuration = 1f;
    
    // annak az objektumnak a referenciája, amibõl a szálakat indítjuk
    public ComputingThread ct;

    // a guira kiírt log
    private string log = "";

    // GUI attribútumok
    private Vector2 guiScrollPosition = Vector2.zero;
    private GUIStyle style = new GUIStyle();

    // log lock objektum
    object logLock = new object();
    
    // Unity3d event, az induláskor van meghívva minden GameObject-hez csatolt scripten
	public void Start ()
    {
        // Debug.Log hívásakor ez a callback is meghívódik a log üzenettel
        // ezzel íratjuk ki a képernyõre a logot
        Application.RegisterLogCallbackThreaded(new Application.LogCallback(this.LogHandler));
        Debug.Log("Application start");
	}
	
	// Update is called once per frame
	void Update () {
        if (this.ct != null && this.ct.threadComputeReady)
        {
            this.ct.threadComputeReady = false;

            // jelzünk a GUInak, hogy engedje el a log scrollbar-ját
            if (this.generation == 0 && !this.state["terminated"]) this.state["terminated"] = true;

            if (this.generation > 0)
            {
                // az utolsó generációnál jelezzük, hogy ezután vége a ciklusnak
                // a végtelen ciklusban lévõ szálak minden ciklus végén ellenõrzik,
                // hogy kell-e tovább futniuk, ha nem, kilépnek
                if (this.generation == 1) this.ct.terminated = true;

                Debug.Log("GENERATION LEFT: " + this.generation);
                this.generation--;
                this.StartCoroutine(this.NewGeneration());
            }
        }
	}

    private IEnumerator NewGeneration()
    {
        this.RefreshCubes();

        // két generáció közti váltás szünet
        yield return new WaitForSeconds(this.generationDuration);

        // a számítást végzõ szálak megkapják az eventet, hogy kezdhetik a számolást
        this.ct.startGeneration.Set();
    }

    #region Graphics code

    // ez gyártja le a kockákat, amiket megjelenítünk
    // amelyiknél 0-t kell megjeleníteni, azt láthatatlanná tesszük
    private IEnumerator GenerateCubes(int size, float space, int[,,] values)
    {
        this.state["loading"] = true;
        this.renderers = new Renderer[size,size,size];

        // a GPU azokat a textúrákat kezeli hatékonyan, amelyek 2 hatvány szélességû/hosszúságúak
        int tw = Mathf.NextPowerOfTwo(this.size * this.size);
        int th = Mathf.NextPowerOfTwo(this.size);
        Texture2D tex = this.CreateTexture(tw, th);

        Material material = this.CreateMaterial(tex);

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
                    //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    GameObject cube = Graphics.CreateCube(startPos + new Vector3(i * cubeSize, j * cubeSize, k * cubeSize), cubeSize);
                    cube.transform.parent = parent;
                    cube.name = "C(" + i.ToString() + "," + j.ToString() + "," + k.ToString() + ")";

                    // minden kocka ugyanazt a materialt használja, így lecsökken a draw call-ok száma
                    cube.renderer.sharedMaterial = material;
                    Mesh mesh = cube.GetComponent<MeshFilter>().mesh;
                    // beállítjuk, hogy a material colorpicker-ként funkcionáló textúrájának megfelelõ pixelével legyen színezve
                    mesh.uv = this.GenerateUV(mesh.vertexCount, tw, th, i, j, k);

                    // ha nem 0 tartozik a kockához, akkor láthatatlan
                    cube.renderer.enabled = (values[i, j, k] == 1);

                    this.renderers[i, j, k] = cube.renderer;
                }
                // várunk egy frame-et, hogy látható legyen a kockák kirajzolásának folyamata
                yield return null;
            }
        }
        this.state["loading"] = false;
    }

    // generálunk egy textúrát, ami colorpickerként fog mûködni az egyes kockák színeihez
    // mindegyik pixele 1-1 kocka színe lesz
    // végigjárjuk a teljes színskálát, tehát mindhárom irányban felosztjuk a kockaméretnek megfelelõen az RGB színeket
    // pl ha 10x10x10-es kockánk van, akkor ez 2D-ben 10 darab 10X10-es négyzet lesz egymás mellett
    private Texture2D CreateTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h);

        for (int i = 0; i < this.size; i++)
        {
            for (int j = 0; j < this.size; j++)
            {
                for (int k = 0; k < this.size; k++)
                {
                    tex.SetPixel(((i * this.size) + j), k, new Color((float)i/(float)this.size, (float)j/(float)this.size, (float)k/(float)this.size, 1f)); 
                }
            }
        }

        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }

    private Material CreateMaterial(Texture2D tex)
    {
        Material mat = new Material(Shader.Find("Self-Illumin/VertexLit"));
        mat.mainTexture = tex;
        return mat;
    }

    private Vector2[] GenerateUV(int length, int textureWidth, int textureHeight, int i, int j, int k)
    {
        Vector2[] uvs = new Vector2[length];

        for (int uv = 0; uv < length; uv++)
        {
            uvs[uv] = new Vector2((float)((i * this.size) + j) / (float)textureWidth, (float)k / (float)textureHeight);
        }

        return uvs;
    }

    // ez a kód állítja be, hogy látszik-e egy kocka, vagy nem
    private void RefreshCubes()
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    this.renderers[i, j, k].enabled = (this.ct.values[i, j, k] == 1);
                }
            }
        }
    }

    #endregion

    #region GUI

    // GUI elemek kirajzolása
    void OnGUI()
    {
        // indításkor megjelenítjük a GUI felületet, amin beállíthatunk egyet s mást
        if (this.state["start"])
        {
            GUI.Label(new Rect(10, 10, 150, 20), "Kocka mérete:");
            this.size = int.Parse(GUI.TextField(new Rect(160,10,50,20), this.size.ToString()));

            GUI.Label(new Rect(10, 40, 150, 20), "Generációk száma:");
            this.generation = int.Parse(GUI.TextField(new Rect(160, 40, 50, 20), this.generation.ToString()));

            if (GUI.Button(new Rect(10, 70, 100, 20), "Generálás"))
            {
                this.state["start"] = false;
                this.state["loading"] = true;

                // létrehozzuk a szálkezelésért felelõs objektumot
                this.ct = new ComputingThread(this.size);
                // elindítjuk a kocka generálását
                this.StartCoroutine(this.GenerateCubes(this.size, this.space, this.ct.values));
            }

            return;
        }
        
        if (!this.state["running"] && !this.state["loading"])
        {
            if (GUI.Button(new Rect(10, 10, 50, 20), "Start"))
            {
                this.state["running"] = true;
                this.ct.startGeneration.Set();
            }
        }
        if (this.state["loading"])
        {
            GUI.Label(new Rect(10, 10, 100, 20), "Betöltés...");
        }

        // log ablak kiíratása
        GUIContent content = new GUIContent(this.log);

        float height = this.style.CalcHeight(content, 180f);

        this.guiScrollPosition = this.state["terminated"] ? this.guiScrollPosition : new Vector2(0, height);
        this.guiScrollPosition = GUI.BeginScrollView(new Rect(Screen.width - 210, 10, 200, 500), this.guiScrollPosition, new Rect(5,5,180,height));
        
        GUI.Label(new Rect(5, 5, 180, height), content);
        GUI.EndScrollView();

    }

    #endregion

    private void LogHandler(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Log)
        {
            lock (this.logLock)
            {
                this.log += condition + "\n";
            }
        }
    }
}
