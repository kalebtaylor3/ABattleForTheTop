using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragAndDrop;

namespace DragAndDropShop
{
    public class CoinUI : MonoBehaviour
    {
        public Inventory inventory;

        Text text;
        // Start is called before the first frame update
        void Start()
        {
            text = GetComponent<Text>();
        }

        // Update is called once per frame
        void Update()
        {
            text.text = "$" + inventory.gold;
        }
    }
}