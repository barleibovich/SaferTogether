using UnityEngine;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // hooks up the UI dropdowns to the avatar builder
    public sealed class AvatarDropdownController : MonoBehaviour
    {
        public Dropdown avatarDropdown;
        public Dropdown accessoryDropdown;
        public Dropdown shirtDropdown;
        public Dropdown pantsDropdown;
        public Dropdown shoesDropdown;
        public AvatarBuilder avatarBuilder;

        // wire up each dropdown to its handler on startup
        private void Awake()
        {
            avatarDropdown?.onValueChanged.AddListener(_ => OnAvatarChanged());
            accessoryDropdown?.onValueChanged.AddListener(_ => OnAccessoryChanged());
            shirtDropdown?.onValueChanged.AddListener(_ => OnShirtChanged());
            pantsDropdown?.onValueChanged.AddListener(_ => OnPantsChanged());
            shoesDropdown?.onValueChanged.AddListener(_ => OnShoesChanged());
        }

        // tell the builder which avatar got picked
        public void OnAvatarChanged()
        {
            avatarBuilder?.SelectAvatar(SelectedValue(avatarDropdown));
        }

        // pass the chosen accessory along
        public void OnAccessoryChanged()
        {
            avatarBuilder?.SelectAccessory(SelectedValue(accessoryDropdown));
        }

        // pass the chosen shirt along
        public void OnShirtChanged()
        {
            avatarBuilder?.SelectShirt(SelectedValue(shirtDropdown));
        }

        // pass the chosen pants along
        public void OnPantsChanged()
        {
            avatarBuilder?.SelectPants(SelectedValue(pantsDropdown));
        }

        // pass the chosen shoes along
        public void OnShoesChanged()
        {
            avatarBuilder?.SelectShoes(SelectedValue(shoesDropdown));
        }

        // grab the text of the currently selected dropdown option
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
