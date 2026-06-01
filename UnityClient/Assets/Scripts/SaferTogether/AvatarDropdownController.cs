using UnityEngine;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    /// <summary>
    /// Connects Unity UI dropdowns to the prefab-based avatar builder.
    /// </summary>
    public sealed class AvatarDropdownController : MonoBehaviour
    {
        public Dropdown avatarDropdown;
        public Dropdown accessoryDropdown;
        public Dropdown shirtDropdown;
        public Dropdown pantsDropdown;
        public Dropdown shoesDropdown;
        public AvatarBuilder avatarBuilder;

        private void Awake()
        {
            avatarDropdown?.onValueChanged.AddListener(_ => OnAvatarChanged());
            accessoryDropdown?.onValueChanged.AddListener(_ => OnAccessoryChanged());
            shirtDropdown?.onValueChanged.AddListener(_ => OnShirtChanged());
            pantsDropdown?.onValueChanged.AddListener(_ => OnPantsChanged());
            shoesDropdown?.onValueChanged.AddListener(_ => OnShoesChanged());
        }

        public void OnAvatarChanged()
        {
            avatarBuilder?.SelectAvatar(SelectedValue(avatarDropdown));
        }

        public void OnAccessoryChanged()
        {
            avatarBuilder?.SelectAccessory(SelectedValue(accessoryDropdown));
        }

        public void OnShirtChanged()
        {
            avatarBuilder?.SelectShirt(SelectedValue(shirtDropdown));
        }

        public void OnPantsChanged()
        {
            avatarBuilder?.SelectPants(SelectedValue(pantsDropdown));
        }

        public void OnShoesChanged()
        {
            avatarBuilder?.SelectShoes(SelectedValue(shoesDropdown));
        }

        private static string SelectedValue(Dropdown dropdown)
        {
            if (dropdown == null || dropdown.options.Count == 0)
            {
                return "";
            }

            int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
            return dropdown.options[index].text;
        }
    }
}
