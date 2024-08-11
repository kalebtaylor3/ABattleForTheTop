using BFTT.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DragAndDropShop
{
    public class Inventory : MonoBehaviour
    {
        public int gold = 1000;
        public bool isPlayer = false;
        public AbstractCombat[] items;
    }
}