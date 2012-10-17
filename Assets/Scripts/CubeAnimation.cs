using UnityEngine;
using System.Collections;

public class CubeAnimation : MonoBehaviour {

    private int i, j, k;
    private Main main;

    public void Initialize(Main _main, int _i, int _j, int _k)
    {
        this.main = _main;
        this.main.starterEvent += new Main.StartAnimation(this.StartAnimation);
        
        this.i = _i;
        this.j = _j;
        this.k = _k;
    }
	
	public void StartAnimation()
    {
        this.StartCoroutine(this.Animate());
    }

    private IEnumerator Animate()
    {
        if (this.renderer.enabled == (this.main.ct.values[i, j, k] == 1)) yield break;

        // 1000 kocka fölött már nem animálunk!
        if (this.main.size > 10)
        {
            this.renderer.enabled = (this.main.ct.values[i, j, k] == 1);
            yield break;
        }

        float dur = this.main.animationDuration;
        float time = 0;
        float scale = this.transform.localScale.x;
        if (!this.renderer.enabled)
        {
            this.renderer.enabled = true;
            while (time < dur)
            {
                float ts = scale * (time / dur);
                this.transform.localScale = new Vector3(ts, ts, ts);
                time += Time.deltaTime;
                yield return null;
            }
            this.transform.localScale = new Vector3(scale, scale, scale);
        }
        else
        {
            while (time < dur)
            {
                float ts = scale * (1 -(time / dur));
                this.transform.localScale = new Vector3(ts, ts, ts);
                time += Time.deltaTime;
                yield return null;
            }
            this.renderer.enabled = false;
            this.transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
