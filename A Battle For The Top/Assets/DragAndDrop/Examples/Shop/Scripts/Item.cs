using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DragAndDropShop
{
    [CreateAssetMenu(fileName = "Item", menuName = "Item")]
    public class Item : ScriptableObject
    {
        public int cost;
        public Sprite icon;
        public Color color = Color.white;
    }
}