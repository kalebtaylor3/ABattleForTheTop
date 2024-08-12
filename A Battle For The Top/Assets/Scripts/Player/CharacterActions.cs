using UnityEngine;

namespace BFTT.Abilities
{
    public class CharacterActions
    {
        public Vector2 move = Vector2.zero;

        public bool jump = false;
        public bool walk = false;
        public bool roll = false;
        public bool crouch = false;
        public bool drop = false;
        public bool crawl = false;
        public bool interact = false;
        public bool UseCard = false;
        public bool UseCardHold = false;
        public bool NextCard = false;
        public bool PreviousCard = false;
        public bool OpenCardMenu = false;
        public bool UIClick = false;
        public bool UIPoint = false;

        public bool zoom = false;
    }
}