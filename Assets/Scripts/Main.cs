using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class Main : MonoBehaviour {

    // a 3D kocka �lm�rete, amiben a j�t�k folyik
    public int size = 10;

    // a t�r nagys�ga, amiben a kock�kat elhelyezz�k
    public float space = 10f;

    // h�ny gener�ci�ig fusson a program
    public int generation = 10;

    // �llapotjelz� flagek, javar�szt a GUI haszn�lja annak meg�llap�t�s�ra, hogy mit kell ki�rnia
    public Dictionary<string, bool> state = new Dictionary<string, bool>
    {
        { "running", false }, { "loading", false }, { "start", true }, { "terminated", false }
    };

    // kocka renderer t�mb
    private Renderer[, ,] renderers;

    // k�t gener�ci� k�zt eltelt id�
    public float generationDuration = 1f;
    
    // annak az objektumnak a referenci�ja, amib�l a sz�lakat ind�tjuk
    public ComputingThread ct;

    // a guira ki�rt log
    private string log = "";

    // GUI attrib�tumok
    private Vector2 guiScrollPosition = Vector2.zero;
    private GUIStyle style = new GUIStyle();

    // log lock objektum
    object logLock = new object();
    
    // Unity3d event, az indul�skor van megh�vva minden GameObject-hez csatolt scripten
	public void Start ()
    {
        // Debug.Log h�v�sakor ez a callback is megh�v�dik a log �zenettel
        // ezzel �ratjuk ki a k�perny�re a logot
        Application.RegisterLogCallbackThreaded(new Application.LogCallback(this.LogHandler));
        Debug.Log("Application start");
	}
	
	// Update is called once per frame
	void Update () {
        if (this.ct != null && this.ct.threadComputeReady)
        {
            this.ct.threadComputeReady = false;

            // jelz�nk a GUInak, hogy engedje el a log scrollbar-j�t
            if (this.generation == 0 && !this.state["terminated"]) this.state["terminated"] = true;

            if (this.generation > 0)
            {
                // az utols� gener�ci�n�l jelezz�k, hogy ezut�n v�ge a ciklusnak
                // a v�gtelen ciklusban l�v� sz�lak minden ciklus v�g�n ellen�rzik,
                // hogy kell-e tov�bb futniuk, ha nem, kil�pnek
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

        // k�t gener�ci� k�zti v�lt�s sz�net
        yield return new WaitForSeconds(this.generationDuration);

        // a sz�m�t�st v�gz� sz�lak megkapj�k az eventet, hogy kezdhetik a sz�mol�st
        this.ct.startGeneration.Set();
    }

    #region Graphics code

    // ez gy�rtja le a kock�kat, amiket megjelen�t�nk
    // amelyikn�l 0-t kell megjelen�teni, azt l�thatatlann� tessz�k
    private IEnumerator GenerateCubes(int size, float space, int[,,] values)
    {
        this.state["loading"] = true;
        this.renderers = new Renderer[size,size,size];

        // a GPU azokat a text�r�kat kezeli hat�konyan, amelyek 2 hatv�ny sz�less�g�/hossz�s�g�ak
        int tw = Mathf.NextPowerOfTwo(this.size * this.size);
        int th = Mathf.NextPowerOfTwo(this.size);
        Texture2D tex = this.CreateTexture(tw, th);

        Material material = this.CreateMaterial(tex);

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
                    //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    GameObject cube = Graphics.CreateCube(startPos + new Vector3(i * cubeSize, j * cubeSize, k * cubeSize), cubeSize);
                    cube.transform.parent = parent;
                    cube.name = "C(" + i.ToString() + "," + j.ToString() + "," + k.ToString() + ")";

                    // minden kocka ugyanazt a materialt haszn�lja, �gy lecs�kken a draw call-ok sz�ma
                    cube.renderer.sharedMaterial = material;
                    Mesh mesh = cube.GetComponent<MeshFilter>().mesh;
                    // be�ll�tjuk, hogy a material colorpicker-k�nt funkcion�l� text�r�j�nak megfelel� pixel�vel legyen sz�nezve
                    mesh.uv = this.GenerateUV(mesh.vertexCount, tw, th, i, j, k);

                    // ha nem 0 tartozik a kock�hoz, akkor l�thatatlan
                    cube.renderer.enabled = (values[i, j, k] == 1);

                    this.renderers[i, j, k] = cube.renderer;
                }
                // v�runk egy frame-et, hogy l�that� legyen a kock�k kirajzol�s�nak folyamata
                yield return null;
            }
        }
        this.state["loading"] = false;
    }

    // gener�lunk egy text�r�t, ami colorpickerk�nt fog m�k�dni az egyes kock�k sz�neihez
    // mindegyik pixele 1-1 kocka sz�ne lesz
    // v�gigj�rjuk a teljes sz�nsk�l�t, teh�t mindh�rom ir�nyban felosztjuk a kockam�retnek megfelel�en az RGB sz�neket
    // pl ha 10x10x10-es kock�nk van, akkor ez 2D-ben 10 darab 10X10-es n�gyzet lesz egym�s mellett
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

    // ez a k�d �ll�tja be, hogy l�tszik-e egy kocka, vagy nem
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

    // GUI elemek kirajzol�sa
    void OnGUI()
    {
        // ind�t�skor megjelen�tj�k a GUI fel�letet, amin be�ll�thatunk egyet s m�st
        if (this.state["start"])
        {
            GUI.Label(new Rect(10, 10, 150, 20), "Kocka m�rete:");
            this.size = int.Parse(GUI.TextField(new Rect(160,10,50,20), this.size.ToString()));

            GUI.Label(new Rect(10, 40, 150, 20), "Gener�ci�k sz�ma:");
            this.generation = int.Parse(GUI.TextField(new Rect(160, 40, 50, 20), this.generation.ToString()));

            if (GUI.Button(new Rect(10, 70, 100, 20), "Gener�l�s"))
            {
                this.state["start"] = false;
                this.state["loading"] = true;

                // l�trehozzuk a sz�lkezel�s�rt felel�s objektumot
                this.ct = new ComputingThread(this.size);
                // elind�tjuk a kocka gener�l�s�t
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
            GUI.Label(new Rect(10, 10, 100, 20), "Bet�lt�s...");
        }

        // log ablak ki�rat�sa
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
