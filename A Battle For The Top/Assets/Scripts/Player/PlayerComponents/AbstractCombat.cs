using System;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Abilities;
using UnityEngine.UI;

namespace BFTT.Combat
{
    public abstract class AbstractCombat : MonoBehaviour
    {
        [SerializeField] private int combatPriority = 0;
        [SerializeField] private AbstractAbility[] AllowedAbilities;
        [SerializeField] private string abilityName;
        [SerializeField] private string abilityDescription;


        public bool IsCombatRunning { get; protected set; } = false;
        protected CharacterActions _action = new CharacterActions();
        protected AbstractAbility _abilityRunning = null;

        public event Action OnCombatStop = null;

        public int CombatPriority { get { return combatPriority; } }
        public GameObject abilityProp;
        public Sprite cardIcon;
        public CardManager _manager;

        protected float startedTime = 0;
        protected float stoppedTime = 0;

        private bool readyToExit = true;


        public abstract bool CombatReadyToRun();

        public abstract void UpdateCombat();

        public abstract void OnStartCombat();
        public abstract void OnStopCombat();

        public enum CardType
        {
            All = 127 // combination of all types
        }

        public CardType cardType = CardType.All;


        public void StartCombat()
        {
            IsCombatRunning = true;
            OnStartCombat();

            startedTime = Time.time;
        }

        public void StopCombat()
        {
            stoppedTime = Time.time;

            IsCombatRunning = false;
            OnStopCombat();
            _abilityRunning = null;
            OnCombatStop?.Invoke();
        }


        public void SetActionReference(ref CharacterActions action)
        {
            _action = action;
        }

        public bool IsAbilityAllowed(AbstractAbility abilityToCheck)
        {
            foreach(AbstractAbility ability in AllowedAbilities)
            {
                if(ability == abilityToCheck)
                    return true;
            }

            return false;
        }

        public void SetCurrentAbility(AbstractAbility newAbility)
        {
            _abilityRunning = newAbility;
        }

        public string GetDescription()
        {
            // name in bold, plus type
            string desc = "<b>" + abilityName + "</b> (" + abilityDescription + ")";

            return desc;

        }

        public abstract bool ReadyToExit();
    }
}