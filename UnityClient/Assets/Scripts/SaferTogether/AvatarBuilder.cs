using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaferTogether.UnityClient
{
    [Serializable]
    public sealed class AvatarPartPrefab
    {
        public string id;
        public AvatarAttachmentSlot slot;
        public GameObject prefab;
    }

    /// <summary>
    /// Switches avatar prefabs and attaches selected accessories/clothes to their body points.
    /// </summary>
    public sealed class AvatarBuilder : MonoBehaviour
    {
        [Header("Runtime Loading")]
        public Transform avatarRoot;
        public string resourcesRoot = "GeneratedAvatarBuilder";
        public bool instantiateAvatarPrefabs = true;

        [Header("Avatar Objects")]
        public GameObject dragonAvatar;
        public GameObject humanAvatar;
        public GameObject bearAvatar;
        public GameObject elephantAvatar;
        public GameObject devilAvatar;
        public GameObject angelAvatar;

        [Header("Part Prefabs")]
        public AvatarPartPrefab[] accessoryPrefabs;
        public AvatarPartPrefab[] shirtPrefabs;
        public AvatarPartPrefab[] pantsPrefabs;
        public AvatarPartPrefab[] shoePrefabs;

        [Header("Current Avatar")]
        public GameObject currentAvatar;

        private readonly Dictionary<AvatarAttachmentSlot, GameObject> attachedParts = new Dictionary<AvatarAttachmentSlot, GameObject>();
        private AvatarAttachmentSet currentAttachmentSet;
        private string selectedAccessory = CharacterAvatarOptions.NoAccessory;
        private string selectedShirt = CharacterAvatarOptions.Tee;
        private string selectedPants = CharacterAvatarOptions.Jeans;
        private string selectedShoes = CharacterAvatarOptions.Sneakers;
        private Color shirtColor = new Color32(102, 217, 239, 255);
        private Color pantsColor = new Color32(65, 105, 168, 255);
        private Color shoeColor = new Color32(248, 250, 252, 255);
        private bool resourcesLoaded;

        private void Start()
        {
            EnsureResourcesLoaded();

            if (currentAvatar == null)
            {
                SelectAvatar(CharacterAvatarOptions.Human);
            }
        }

        public void SelectAvatar(string avatarName)
        {
            EnsureResourcesLoaded();
            EnsureAvatarRoot();

            GameObject selectedAvatar = AvatarObjectFor(avatarName);

            if (selectedAvatar == null)
            {
                selectedAvatar = humanAvatar;
            }

            if (instantiateAvatarPrefabs)
            {
                if (currentAvatar != null)
                {
                    Destroy(currentAvatar);
                }

                currentAvatar = selectedAvatar != null
                    ? Instantiate(selectedAvatar, avatarRoot)
                    : null;
                if (currentAvatar != null)
                {
                    currentAvatar.transform.localPosition = Vector3.zero;
                    currentAvatar.transform.localRotation = Quaternion.identity;
                    currentAvatar.transform.localScale = Vector3.one;
                }
            }
            else
            {
                SetAvatarActive(dragonAvatar, false);
                SetAvatarActive(humanAvatar, false);
                SetAvatarActive(bearAvatar, false);
                SetAvatarActive(elephantAvatar, false);
                SetAvatarActive(devilAvatar, false);
                SetAvatarActive(angelAvatar, false);
                currentAvatar = selectedAvatar;
                SetAvatarActive(currentAvatar, true);
            }

            currentAttachmentSet = currentAvatar != null
                ? currentAvatar.GetComponentInChildren<AvatarAttachmentSet>(true)
                : null;
            RebuildAttachedParts();
        }

        public void SelectAccessory(string accessoryName)
        {
            EnsureResourcesLoaded();
            selectedAccessory = NormalizeId(accessoryName, CharacterAvatarOptions.NoAccessory);
            RebuildAccessory();
        }

        public void SelectShirt(string shirtName)
        {
            EnsureResourcesLoaded();
            selectedShirt = NormalizeId(shirtName, CharacterAvatarOptions.Tee);
            RebuildClothes();
        }

        public void SelectPants(string pantsName)
        {
            EnsureResourcesLoaded();
            selectedPants = NormalizeId(pantsName, CharacterAvatarOptions.Jeans);
            RebuildClothes();
        }

        public void SelectShoes(string shoesName)
        {
            EnsureResourcesLoaded();
            selectedShoes = NormalizeId(shoesName, CharacterAvatarOptions.Sneakers);
            RebuildClothes();
        }

        public void SetShirtColor(Color color)
        {
            shirtColor = color;
            ColorSlots(color, AvatarAttachmentSlot.Shirt);
        }

        public void SetPantsColor(Color color)
        {
            pantsColor = color;
            ColorSlots(color, AvatarAttachmentSlot.Pants);
        }

        public void SetShoeColor(Color color)
        {
            shoeColor = color;
            ColorSlots(color, AvatarAttachmentSlot.LeftShoe, AvatarAttachmentSlot.RightShoe);
        }

        private void RebuildAttachedParts()
        {
            ClearAllParts();
            RebuildAccessory();
            RebuildClothes();
        }

        private void RebuildAccessory()
        {
            ClearSlots(AvatarAttachmentSlot.Face, AvatarAttachmentSlot.Hat, AvatarAttachmentSlot.Horns, AvatarAttachmentSlot.Wings, AvatarAttachmentSlot.Tail);

            if (selectedAccessory == CharacterAvatarOptions.NoAccessory)
            {
                return;
            }

            AttachMatchingParts(accessoryPrefabs, selectedAccessory);
        }

        private void RebuildClothes()
        {
            ClearSlots(AvatarAttachmentSlot.Shirt, AvatarAttachmentSlot.Pants, AvatarAttachmentSlot.LeftShoe, AvatarAttachmentSlot.RightShoe);

            AttachMatchingParts(shirtPrefabs, selectedShirt);
            AttachMatchingParts(pantsPrefabs, selectedPants);

            if (selectedShoes != CharacterAvatarOptions.NoShoes)
            {
                AttachMatchingParts(shoePrefabs, selectedShoes);
            }

            ColorSlots(shirtColor, AvatarAttachmentSlot.Shirt);
            ColorSlots(pantsColor, AvatarAttachmentSlot.Pants);
            ColorSlots(shoeColor, AvatarAttachmentSlot.LeftShoe, AvatarAttachmentSlot.RightShoe);
        }

        private void AttachMatchingParts(AvatarPartPrefab[] catalog, string selectedId)
        {
            if (catalog == null || currentAttachmentSet == null)
            {
                return;
            }

            foreach (AvatarPartPrefab part in catalog)
            {
                if (part == null || part.prefab == null || NormalizeId(part.id, "") != selectedId)
                {
                    continue;
                }

                Transform point = currentAttachmentSet.PointFor(part.slot);

                if (point == null)
                {
                    continue;
                }

                GameObject instance = Instantiate(part.prefab, point);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                attachedParts[part.slot] = instance;
            }
        }

        private void ColorSlots(Color color, params AvatarAttachmentSlot[] slots)
        {
            foreach (AvatarAttachmentSlot slot in slots)
            {
                if (!attachedParts.TryGetValue(slot, out GameObject part) || part == null)
                {
                    continue;
                }

                foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.material.color = color;
                }
            }
        }

        private void ClearSlots(params AvatarAttachmentSlot[] slots)
        {
            foreach (AvatarAttachmentSlot slot in slots)
            {
                if (!attachedParts.TryGetValue(slot, out GameObject part) || part == null)
                {
                    attachedParts.Remove(slot);
                    continue;
                }

                Destroy(part);
                attachedParts.Remove(slot);
            }
        }

        private void ClearAllParts()
        {
            foreach (GameObject part in attachedParts.Values)
            {
                if (part != null)
                {
                    Destroy(part);
                }
            }

            attachedParts.Clear();
        }

        private GameObject AvatarObjectFor(string avatarName)
        {
            switch (NormalizeId(avatarName, CharacterAvatarOptions.Human))
            {
                case CharacterAvatarOptions.Dragon:
                    return dragonAvatar;
                case CharacterAvatarOptions.Bear:
                    return bearAvatar;
                case CharacterAvatarOptions.Elephant:
                    return elephantAvatar;
                case CharacterAvatarOptions.Devil:
                    return devilAvatar;
                case CharacterAvatarOptions.Angel:
                    return angelAvatar;
                default:
                    return humanAvatar;
            }
        }

        private static void SetAvatarActive(GameObject avatar, bool active)
        {
            if (avatar != null)
            {
                avatar.SetActive(active);
            }
        }

        private void EnsureAvatarRoot()
        {
            if (avatarRoot != null)
            {
                return;
            }

            var rootObject = new GameObject("AvatarPreviewRoot");
            rootObject.transform.SetParent(transform, false);
            avatarRoot = rootObject.transform;
        }

        public void EnsureResourcesLoaded()
        {
            if (resourcesLoaded)
            {
                return;
            }

            humanAvatar = humanAvatar != null ? humanAvatar : LoadAvatar(CharacterAvatarOptions.Human);
            dragonAvatar = dragonAvatar != null ? dragonAvatar : LoadAvatar(CharacterAvatarOptions.Dragon);
            bearAvatar = bearAvatar != null ? bearAvatar : LoadAvatar(CharacterAvatarOptions.Bear);
            elephantAvatar = elephantAvatar != null ? elephantAvatar : LoadAvatar(CharacterAvatarOptions.Elephant);
            devilAvatar = devilAvatar != null ? devilAvatar : LoadAvatar(CharacterAvatarOptions.Devil);
            angelAvatar = angelAvatar != null ? angelAvatar : LoadAvatar(CharacterAvatarOptions.Angel);

            if (accessoryPrefabs == null || accessoryPrefabs.Length == 0)
            {
                accessoryPrefabs = BuildAccessoryCatalog();
            }

            if (shirtPrefabs == null || shirtPrefabs.Length == 0)
            {
                shirtPrefabs = BuildCatalog("Parts/Shirts", AvatarAttachmentSlot.Shirt, CharacterAvatarOptions.Tops);
            }

            if (pantsPrefabs == null || pantsPrefabs.Length == 0)
            {
                pantsPrefabs = BuildCatalog("Parts/Pants", AvatarAttachmentSlot.Pants, CharacterAvatarOptions.Bottoms);
            }

            if (shoePrefabs == null || shoePrefabs.Length == 0)
            {
                shoePrefabs = BuildShoeCatalog();
            }

            resourcesLoaded = true;
        }

        private GameObject LoadAvatar(string id)
        {
            return Resources.Load<GameObject>(resourcesRoot + "/Avatars/" + id);
        }

        private GameObject LoadPart(string category, string id)
        {
            return Resources.Load<GameObject>(resourcesRoot + "/" + category + "/" + id);
        }

        private AvatarPartPrefab[] BuildCatalog(string category, AvatarAttachmentSlot slot, string[] ids)
        {
            var catalog = new List<AvatarPartPrefab>();

            foreach (string id in ids)
            {
                GameObject prefab = LoadPart(category, id);

                if (prefab == null)
                {
                    continue;
                }

                catalog.Add(new AvatarPartPrefab
                {
                    id = id,
                    slot = slot,
                    prefab = prefab
                });
            }

            return catalog.ToArray();
        }

        private AvatarPartPrefab[] BuildAccessoryCatalog()
        {
            var catalog = new List<AvatarPartPrefab>();
            AddPart(catalog, CharacterAvatarOptions.Glasses, AvatarAttachmentSlot.Face, "Parts/Accessories");
            AddPart(catalog, CharacterAvatarOptions.Cap, AvatarAttachmentSlot.Hat, "Parts/Accessories");
            AddPart(catalog, CharacterAvatarOptions.Crown, AvatarAttachmentSlot.Hat, "Parts/Accessories");
            AddPart(catalog, CharacterAvatarOptions.Mask, AvatarAttachmentSlot.Face, "Parts/Accessories");
            AddPart(catalog, CharacterAvatarOptions.Headphones, AvatarAttachmentSlot.Face, "Parts/Accessories");
            AddPart(catalog, CharacterAvatarOptions.Wings, AvatarAttachmentSlot.Wings, "Parts/Accessories");
            AddPart(catalog, CharacterAvatarOptions.Halo, AvatarAttachmentSlot.Hat, "Parts/Accessories");
            AddPart(catalog, CharacterAvatarOptions.Horns, AvatarAttachmentSlot.Horns, "Parts/Accessories");
            AddPart(catalog, CharacterAvatarOptions.Tail, AvatarAttachmentSlot.Tail, "Parts/Accessories");
            return catalog.ToArray();
        }

        private AvatarPartPrefab[] BuildShoeCatalog()
        {
            var catalog = new List<AvatarPartPrefab>();

            foreach (string id in CharacterAvatarOptions.Shoes)
            {
                if (id == CharacterAvatarOptions.NoShoes)
                {
                    continue;
                }

                GameObject prefab = LoadPart("Parts/Shoes", id);

                if (prefab == null)
                {
                    continue;
                }

                catalog.Add(new AvatarPartPrefab { id = id, slot = AvatarAttachmentSlot.LeftShoe, prefab = prefab });
                catalog.Add(new AvatarPartPrefab { id = id, slot = AvatarAttachmentSlot.RightShoe, prefab = prefab });
            }

            return catalog.ToArray();
        }

        private void AddPart(List<AvatarPartPrefab> catalog, string id, AvatarAttachmentSlot slot, string category)
        {
            GameObject prefab = LoadPart(category, id);

            if (prefab == null)
            {
                return;
            }

            catalog.Add(new AvatarPartPrefab
            {
                id = id,
                slot = slot,
                prefab = prefab
            });
        }

        private static string NormalizeId(string value, string fallback)
        {
            string cleanValue = string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(cleanValue) ? fallback : cleanValue;
        }
    }
}
