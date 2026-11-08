using UnityEngine;

namespace PolyNext.LowPolyOrcs
{
    public class RiddingCharacter : MonoBehaviour
    {
        [SerializeField] AnimationCharacter WolfRiding;
        [SerializeField] int[] CharacterAnimSync;
        [SerializeField] int[] WolfAnimSync;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
        public void RiddingAnimSync(int anim)
        {
            if (WolfRiding != null)
            {
                for (int i = 0; i < CharacterAnimSync.Length; i++)
                {
                    if (CharacterAnimSync[i] == anim)
                    {
                        WolfRiding.gameObject.SetActive(true);
                        WolfRiding.StartAnimation(WolfAnimSync[i]);
                        return;
                    }
                    WolfRiding.gameObject.SetActive(false);
                }
            }
        }
    }
}
