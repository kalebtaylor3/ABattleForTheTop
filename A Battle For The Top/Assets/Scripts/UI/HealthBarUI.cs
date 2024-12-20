using UnityEngine;
using UnityEngine.UI;
using BFTT.Components;

namespace BFTT.UI {

    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image healthBar;
        [SerializeField] private Health characterHealth;

        private void OnEnable()
        {
            if (healthBar)
            {
                characterHealth.OnHealthChanged += UpdateBar;
                UpdateBar();
            }
        }
        private void OnDisable()
        {
            if (healthBar)
                characterHealth.OnHealthChanged -= UpdateBar;
        }

        private void UpdateBar()
        {
            healthBar.fillAmount = (float)characterHealth.CurrentHP / characterHealth.MaxHP;
        }
    }
}