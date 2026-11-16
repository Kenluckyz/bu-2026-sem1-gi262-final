using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PolyNext.LowPolyOrcs
{
    public class CustomizationCharacter : MonoBehaviour
    {
        public GameObject Canvas;
        //Objects
        public GameObject[] CharacterModule;

        int headPrev = 0;
        int bodyPrev = 3;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                HiddenCanvas();
            }
        }
        public void HiddenCanvas()
        {
            if (Canvas != null) {
                if (Canvas.activeSelf)Canvas.SetActive(false);
                else Canvas.SetActive(true);
            }
        }
        public void HeadModule(float value)
        {
            int module = (int)value;
            CharacterModule[module].SetActive(true);
            CharacterModule[headPrev].SetActive(false);
            headPrev = module;
        }
        public void BodyModule(float value)
        {
            int module = (int)value;
            CharacterModule[module].SetActive(true);
            CharacterModule[bodyPrev].SetActive(false);
            bodyPrev = module;
        }
        public void WeaponModule(float value)
        {
            int module = (int)value;
            if (CharacterModule[module].activeSelf) CharacterModule[module].SetActive(false);
            else CharacterModule[module].SetActive(true);
        }

    }
}
