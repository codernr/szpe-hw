using UnityEngine;
using System.Collections;
using System.Threading;

public class Main : MonoBehaviour {

    // a 3D kocka élmérete, amiben a játék folyik
    public int size = 10;

    // a tér nagysága, amiben a kockákat elhelyezzük
    public float space = 10f;

    // hány generációig fusson a program
    public int generation = 10;

    private bool running = false;

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
        if (this.ct.threadComputeReady)
        {
            Debug.Log("SIKER");
            this.ct.threadComputeReady = false;

            if (this.generation > 0)
            {
                this.generation--;
                Debug.Log("GENERATION START: " + this.generation);
                this.StartCoroutine(this.NewGeneration());
            }
        }
	}

    private IEnumerator NewGeneration()
    {
        this.RefreshCubes(this.size, this.ct.values);
        yield return new WaitForSeconds(1f);
        this.ct.StartThreads();
    }

    // ez gyártja le a kockákat, amiket megjelenítünk
    // amelyiknél 0-t kell megjeleníteni, azt láthatatlanná tesszük
    private GameObject[,,] GenerateCubes(int size, float space, int[,,] values)
    {
        GameObject[,,] cubeArray = new GameObject[size, size, size];

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
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.parent = parent;

                    // minden kocka ugyanazt a materialt használja, így lecsökken a draw call-ok száma
                    cube.renderer.sharedMaterial = material;
                    Mesh mesh = cube.GetComponent<MeshFilter>().mesh;
                    // beállítjuk, hogy a material colorpicker-ként funkcionáló textúrájának megfelelõ pixelével legyen szaínezve
                    mesh.uv = this.GenerateUV(mesh.vertexCount, tw, th, i, j, k);

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
        if (GUI.Button(new Rect(10,10,200,200), "Start") && !this.running)
        {
            this.running = true;
            this.ct.StartThreads();
        }
    }
}
