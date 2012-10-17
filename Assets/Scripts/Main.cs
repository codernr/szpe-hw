using UnityEngine;
using System.Collections;
using System.Threading;

public class Main : MonoBehaviour {

    // a 3D kocka �lm�rete, amiben a j�t�k folyik
    public int size = 10;

    // a t�r nagys�ga, amiben a kock�kat elhelyezz�k
    public float space = 10f;

    // h�ny gener�ci�ig fusson a program
    public int generation = 10;

    private bool running = false;

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

    // ez gy�rtja le a kock�kat, amiket megjelen�t�nk
    // amelyikn�l 0-t kell megjelen�teni, azt l�thatatlann� tessz�k
    private GameObject[,,] GenerateCubes(int size, float space, int[,,] values)
    {
        GameObject[,,] cubeArray = new GameObject[size, size, size];

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
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.parent = parent;

                    // minden kocka ugyanazt a materialt haszn�lja, �gy lecs�kken a draw call-ok sz�ma
                    cube.renderer.sharedMaterial = material;
                    Mesh mesh = cube.GetComponent<MeshFilter>().mesh;
                    // be�ll�tjuk, hogy a material colorpicker-k�nt funkcion�l� text�r�j�nak megfelel� pixel�vel legyen sza�nezve
                    mesh.uv = this.GenerateUV(mesh.vertexCount, tw, th, i, j, k);

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
