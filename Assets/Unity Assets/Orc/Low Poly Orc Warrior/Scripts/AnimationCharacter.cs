using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PolyNext.LowPolyOrcs
{
    public class AnimationCharacter : MonoBehaviour {
    	//public Text 
    	public TMP_Text Label_animation;
        public string[] Anim_mas;
    	[SerializeField] RiddingCharacter riddingCharacter;
    	[SerializeField] Toggle Toggle1;
        [SerializeField] Toggle Toggle2;

        int i;
    	 Animator anim;
    	// Use this for initialization
    	void Start () {
    		anim = this.GetComponent<Animator> ();
           if (Label_animation != null) Label_animation.text = Anim_mas[0];
            //anim.Play("01_Idle_1");
        }

    	// Update is called once per frame
    	void Update () {

    	}
    	public void checkAnim (int q){
    		if (q == 1) {
    			i++;
    			if (i < Anim_mas.Length) {
                    StartAnimation(i);

                } else {
    				i = 0;
                    StartAnimation(i);
    			}
    		}
    		if (q == -1) {
    			i--;
    			if (i >= 0) {
                    StartAnimation(i);
                } else {
    				i = 13;
                    StartAnimation(i);
                }
    		}
    		if (riddingCharacter != null) riddingCharacter.RiddingAnimSync(i);

            if (Toggle1 != null)
    		{
                if (i == 1 || i == 4 || i == 7 || i == 10 || i == 13 || i == 17)
                {
                    Toggle1.isOn = true;
                    Toggle2.isOn = false;
                }
                else
                    if (i == 2 || i == 5 || i == 8 || i == 11 || i == 14 || i == 18)
                    {
                        Toggle1.isOn = false;
                        Toggle2.isOn = true;
                    }
                    else
                    {
                        Toggle1.isOn = false;
                        Toggle2.isOn = false;
                    }
            }

            if (Label_animation != null)  Label_animation.text = Anim_mas [i];
    	}
    	public void StartAnimation (int a)
    	{
           if(anim != null) anim.Play(Anim_mas[a]);
        }
    }
}
