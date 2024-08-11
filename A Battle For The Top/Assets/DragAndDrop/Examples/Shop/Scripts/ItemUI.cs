using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragAndDrop;

namespace DragAndDropShop
{
    public class ItemUI : Draggable
    {
        public Text nameTag;
        public Text cost;
        public Image icon;

        public override void UpdateObject()
        {
            Item item = obj as Item;

            if (item != null)
            {
                if (nameTag)
                    nameTag.text = item.name;
                if (cost)
                    cost.text = "$" + item.cost;
                if (icon)
                {
                    icon.sprite = item.icon;
                    icon.color = item.color;
                }
            }

            gameObject.SetActive(item != null);
        }
    }
}