using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaferTogether.UnityClient
{
    [Serializable]
    public sealed class AvatarPartPrefab
    {
        public string species;
        public string id;
        public string color;
        public AvatarAttachmentSlot slot;
        public GameObject prefab;
    }

    /// <summary>
    /// Switches the selected avatar prefab and attaches the avatar-specific clothing/accessory prefab layers.
    /// </summary>
    public sealed class AvatarBuilder : MonoBehaviour
    {
        [Header("Runtime Loading")]
        public Transform avatarRoot;
        public string resourcesRoot = "GeneratedAvatarBuilder";
        public bool instantiateAvatarPrefabs = true;

        [Header("Avatar Objects")]
        public GameObject dragonAvatar;
        public GameObject maleAvatar;
        public GameObject femaleAvatar;
        public GameObject devilAvatar;

        [Header("Part Prefabs")]
        public AvatarPartPrefab[] accessoryPrefabs;
        public AvatarPartPrefab[] shirtPrefabs;
        public AvatarPartPrefab[] pantsPrefabs;
        public AvatarPartPrefab[] shoePrefabs;

        [Header("Current Avatar")]
        public GameObject currentAvatar;

        private readonly Dictionary<AvatarAttachmentSlot, GameObject> attachedParts = new Dictionary<AvatarAttachmentSlot, GameObject>();
        private readonly Dictionary<string, Texture2D> runtimeTextureCache = new Dictionary<string, Texture2D>();
        private AvatarAttachmentSet currentAttachmentSet;
        private string currentSpecies = CharacterAvatarOptions.Male;
        private static readonly Vector3 FemaleAvatarRootPosition = new Vector3(0.1f, 0.3f, 0f);
        private static readonly Vector3 FemaleAvatarRootScale = new Vector3(0.6f, 0.6f, 1f);
        private string selectedAccessory = CharacterAvatarOptions.NoAccessory;
        private string selectedShirt = CharacterAvatarOptions.Tee;
        private string selectedShirtColor = CharacterAvatarOptions.Black;
        private string selectedPants = CharacterAvatarOptions.Jeans;
        private string selectedPantsColor = CharacterAvatarOptions.Denim;
        private string selectedShoes = CharacterAvatarOptions.Sneakers;
        private string selectedShoeColor = CharacterAvatarOptions.Black;
        private bool resourcesLoaded;

        private void Start()
        {
            EnsureResourcesLoaded();

            if (currentAvatar == null)
            {
                SelectAvatar(CharacterAvatarOptions.Male);
            }
        }

        public void SelectAvatar(string avatarName)
        {
            EnsureResourcesLoaded();
            EnsureAvatarRoot();

            currentSpecies = CharacterAvatarId.NormalizeSpecies(avatarName);
            GameObject selectedAvatar = AvatarObjectFor(currentSpecies) ?? maleAvatar;

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
                    ApplyAvatarRootTransform(currentAvatar.transform, currentSpecies);
                    ApplyRuntimeTextures(currentAvatar);
                }
            }
            else
            {
                SetAvatarActive(dragonAvatar, false);
                SetAvatarActive(maleAvatar, false);
                SetAvatarActive(femaleAvatar, false);
                SetAvatarActive(devilAvatar, false);
                currentAvatar = selectedAvatar;
                SetAvatarActive(currentAvatar, true);

                if (currentAvatar != null)
                {
                    ApplyAvatarRootTransform(currentAvatar.transform, currentSpecies);
                    ApplyRuntimeTextures(currentAvatar);
                }
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

        public void SelectShirtColor(string colorId)
        {
            EnsureResourcesLoaded();
            selectedShirtColor = NormalizeClothingColor(colorId, CharacterAvatarOptions.Black, false);
            RebuildClothes();
        }

        public void SelectPants(string pantsName)
        {
            EnsureResourcesLoaded();
            selectedPants = NormalizeId(pantsName, CharacterAvatarOptions.Jeans);
            RebuildClothes();
        }

        public void SelectPantsColor(string colorId)
        {
            EnsureResourcesLoaded();
            selectedPantsColor = NormalizeClothingColor(colorId, CharacterAvatarOptions.Blue, true);
            RebuildClothes();
        }

        public void SelectShoes(string shoesName)
        {
            EnsureResourcesLoaded();
            selectedShoes = NormalizeId(shoesName, CharacterAvatarOptions.Sneakers);
            RebuildClothes();
        }

        public void SelectShoeColor(string colorId)
        {
            EnsureResourcesLoaded();
            selectedShoeColor = NormalizeClothingColor(colorId, CharacterAvatarOptions.Black, false);
            RebuildClothes();
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

            AttachMatchingParts(accessoryPrefabs, selectedAccessory, "");
        }

        private void RebuildClothes()
        {
            ClearSlots(
                AvatarAttachmentSlot.Shirt,
                AvatarAttachmentSlot.Pants,
                AvatarAttachmentSlot.Shoes,
                AvatarAttachmentSlot.LeftShoe,
                AvatarAttachmentSlot.RightShoe
            );

            AttachMatchingParts(shirtPrefabs, selectedShirt, selectedShirtColor);
            AttachMatchingParts(pantsPrefabs, selectedPants, PantsColorForSelection());
            AttachMatchingParts(shoePrefabs, selectedShoes, selectedShoeColor);
        }

        private string PantsColorForSelection()
        {
            return selectedPants == CharacterAvatarOptions.Jeans
                ? CharacterAvatarOptions.Denim
                : NormalizeClothingColor(selectedPantsColor, CharacterAvatarOptions.Blue, false);
        }

        private void AttachMatchingParts(AvatarPartPrefab[] catalog, string selectedId, string selectedColor)
        {
            if (catalog == null || currentAttachmentSet == null)
            {
                return;
            }

            string normalizedId = NormalizeId(selectedId, "");
            string normalizedColor = NormalizeId(selectedColor, "");

            foreach (AvatarPartPrefab part in catalog)
            {
                if (!PartMatches(part, normalizedId, normalizedColor))
                {
                    continue;
                }

                Transform point = currentAttachmentSet.PointFor(part.slot);

                if (point == null)
                {
                    continue;
                }

                GameObject instance = Instantiate(part.prefab, point);
                if (!UsesPrefabTransform(part))
                {
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                }

                ApplyRuntimeTextures(instance);
                attachedParts[part.slot] = instance;
            }
        }

        private bool UsesPrefabTransform(AvatarPartPrefab part)
        {
            string partId = NormalizeId(part.id, "");
            string partSpecies = NormalizeId(part.species, "");
            string partColor = NormalizeId(part.color, "");
            if (partSpecies == CharacterAvatarOptions.Male && partId == CharacterAvatarOptions.Jeans)
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Male
                && partId == CharacterAvatarOptions.Tee
                && (partColor == CharacterAvatarOptions.Blue
                    || partColor == CharacterAvatarOptions.White))
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Male
                && partId == CharacterAvatarOptions.Sweatshirt)
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Female
                && partId == CharacterAvatarOptions.Tee
                && (partColor == CharacterAvatarOptions.Red
                    || partColor == CharacterAvatarOptions.White
                    || partColor == CharacterAvatarOptions.Yellow
                    || partColor == CharacterAvatarOptions.Blue))
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Female
                && partId == CharacterAvatarOptions.SportsPants)
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Female
                && partId == CharacterAvatarOptions.Jeans)
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Female
                && (partId == CharacterAvatarOptions.Sweatshirt
                    || partId == CharacterAvatarOptions.Undershirt))
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Female
                && partId == CharacterAvatarOptions.Boots)
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Female
                && partId == CharacterAvatarOptions.SpaceShoes)
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Female
                && (partId == CharacterAvatarOptions.Crown
                    || partId == CharacterAvatarOptions.Bandana
                    || partId == CharacterAvatarOptions.Glasses
                    || partId == CharacterAvatarOptions.Mask))
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Dragon
                && (partId == CharacterAvatarOptions.Crown
                    || partId == CharacterAvatarOptions.Bandana
                    || partId == CharacterAvatarOptions.Glasses
                    || partId == CharacterAvatarOptions.Mask))
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Male
                && partId == CharacterAvatarOptions.Cargo
                && (partColor == CharacterAvatarOptions.Blue
                    || partColor == CharacterAvatarOptions.Red))
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Male
                && partId == CharacterAvatarOptions.SpaceShoes)
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Male
                && partId == CharacterAvatarOptions.Boots)
            {
                return true;
            }

            if (partSpecies == CharacterAvatarOptions.Male
                && (partId == CharacterAvatarOptions.Glasses
                    || partId == CharacterAvatarOptions.Bandana
                    || partId == CharacterAvatarOptions.Mask))
            {
                return true;
            }

            return partSpecies == CharacterAvatarOptions.Devil
                && (partId == CharacterAvatarOptions.Undershirt
                    || partId == CharacterAvatarOptions.Sweatshirt
                    || partId == CharacterAvatarOptions.Cargo
                    || partId == CharacterAvatarOptions.SportsPants
                    || partId == CharacterAvatarOptions.Boots
                    || partId == CharacterAvatarOptions.SpaceShoes
                    || partId == CharacterAvatarOptions.Bandana
                    || partId == CharacterAvatarOptions.Glasses
                    || partId == CharacterAvatarOptions.Mask);
        }

        private bool PartMatches(AvatarPartPrefab part, string selectedId, string selectedColor)
        {
            if (part == null || part.prefab == null || NormalizeId(part.id, "") != selectedId)
            {
                return false;
            }

            string partSpecies = NormalizeId(part.species, "");
            if (!string.IsNullOrEmpty(partSpecies) && partSpecies != currentSpecies)
            {
                return false;
            }

            string partColor = NormalizeId(part.color, "");
            return string.IsNullOrEmpty(partColor) || string.IsNullOrEmpty(selectedColor) || partColor == selectedColor;
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
            switch (CharacterAvatarId.NormalizeSpecies(avatarName))
            {
                case CharacterAvatarOptions.Male:
                    return maleAvatar;
                case CharacterAvatarOptions.Female:
                    return femaleAvatar;
                case CharacterAvatarOptions.Dragon:
                    return dragonAvatar;
                case CharacterAvatarOptions.Devil:
                    return devilAvatar;
                default:
                    return maleAvatar;
            }
        }

        private static void ApplyAvatarRootTransform(Transform avatarTransform, string species)
        {
            if (avatarTransform == null)
            {
                return;
            }

            if (species == CharacterAvatarOptions.Female)
            {
                avatarTransform.localPosition = FemaleAvatarRootPosition;
                avatarTransform.localRotation = Quaternion.identity;
                avatarTransform.localScale = FemaleAvatarRootScale;
                return;
            }

            avatarTransform.localPosition = Vector3.zero;
            avatarTransform.localRotation = Quaternion.identity;
            avatarTransform.localScale = Vector3.one;
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

            maleAvatar = maleAvatar != null ? maleAvatar : LoadAvatar(CharacterAvatarOptions.Male);
            femaleAvatar = femaleAvatar != null ? femaleAvatar : LoadAvatar(CharacterAvatarOptions.Female);
            dragonAvatar = dragonAvatar != null ? dragonAvatar : LoadAvatar(CharacterAvatarOptions.Dragon);
            devilAvatar = devilAvatar != null ? devilAvatar : LoadAvatar(CharacterAvatarOptions.Devil);

            if (accessoryPrefabs == null || accessoryPrefabs.Length == 0)
            {
                accessoryPrefabs = BuildAccessoryCatalog();
            }

            if (shirtPrefabs == null || shirtPrefabs.Length == 0)
            {
                shirtPrefabs = BuildColorCatalog("Parts/Shirts", AvatarAttachmentSlot.Shirt, CharacterAvatarOptions.Tops);
            }

            if (pantsPrefabs == null || pantsPrefabs.Length == 0)
            {
                pantsPrefabs = BuildColorCatalog("Parts/Pants", AvatarAttachmentSlot.Pants, CharacterAvatarOptions.Bottoms);
            }

            if (shoePrefabs == null || shoePrefabs.Length == 0)
            {
                shoePrefabs = BuildColorCatalog("Parts/Shoes", AvatarAttachmentSlot.Shoes, CharacterAvatarOptions.Shoes);
            }

            resourcesLoaded = true;
        }

        private GameObject LoadAvatar(string id)
        {
            return Resources.Load<GameObject>(resourcesRoot + "/Avatars/" + id);
        }

        private GameObject LoadPart(string category, string species, string id)
        {
            return Resources.Load<GameObject>(resourcesRoot + "/" + category + "/" + species + "/" + id);
        }

        private AvatarPartPrefab[] BuildColorCatalog(string category, AvatarAttachmentSlot slot, string[] ids)
        {
            var catalog = new List<AvatarPartPrefab>();

            foreach (string species in CharacterAvatarOptions.Species)
            {
                foreach (string id in ids)
                {
                    foreach (string color in ColorsForPart(id))
                    {
                        GameObject prefab = LoadPart(category, species, id + "-" + color);

                        if (prefab == null)
                        {
                            continue;
                        }

                        catalog.Add(new AvatarPartPrefab
                        {
                            species = species,
                            id = id,
                            color = color,
                            slot = slot,
                            prefab = prefab
                        });
                    }
                }
            }

            return catalog.ToArray();
        }

        private AvatarPartPrefab[] BuildAccessoryCatalog()
        {
            var catalog = new List<AvatarPartPrefab>();

            foreach (string species in CharacterAvatarOptions.Species)
            {
                foreach (string id in CharacterAvatarOptions.Accessories)
                {
                    if (id == CharacterAvatarOptions.NoAccessory)
                    {
                        continue;
                    }

                    GameObject prefab = LoadPart("Parts/Accessories", species, id);

                    if (prefab == null)
                    {
                        continue;
                    }

                    catalog.Add(new AvatarPartPrefab
                    {
                        species = species,
                        id = id,
                        slot = SlotForAccessory(id),
                        prefab = prefab
                    });
                }
            }

            return catalog.ToArray();
        }

        private static AvatarAttachmentSlot SlotForAccessory(string id)
        {
            switch (id)
            {
                case CharacterAvatarOptions.Crown:
                    return AvatarAttachmentSlot.Hat;
                default:
                    return AvatarAttachmentSlot.Face;
            }
        }

        private static string[] ColorsForPart(string id)
        {
            return id == CharacterAvatarOptions.Jeans
                ? new[] { CharacterAvatarOptions.Denim }
                : CharacterAvatarOptions.ClothingColors;
        }

        private static string NormalizeClothingColor(string value, string fallback, bool allowDenim)
        {
            string cleanValue = NormalizeId(value, "");

            if (allowDenim && cleanValue == CharacterAvatarOptions.Denim)
            {
                return cleanValue;
            }

            foreach (string option in CharacterAvatarOptions.ClothingColors)
            {
                if (option == cleanValue)
                {
                    return cleanValue;
                }
            }

            return fallback;
        }

        private static string NormalizeId(string value, string fallback)
        {
            string cleanValue = string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(cleanValue) ? fallback : cleanValue;
        }

        private void ApplyRuntimeTextures(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.materials;
                bool changed = false;

                for (int index = 0; index < materials.Length; index++)
                {
                    Texture2D texture = TextureForMaterial(materials[index] != null ? materials[index].name : "");

                    if (texture == null)
                    {
                        continue;
                    }

                    materials[index].mainTexture = texture;
                    materials[index].SetTexture("_MainTex", texture);
                    materials[index].color = Color.white;
                    changed = true;
                }

                if (changed)
                {
                    renderer.materials = materials;
                }
            }
        }

        private Texture2D TextureForMaterial(string materialName)
        {
            string resourcePath = SourceImageResourcePathForMaterial(materialName);

            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            if (!runtimeTextureCache.TryGetValue(resourcePath, out Texture2D texture))
            {
                texture = Resources.Load<Texture2D>(resourcePath);
                runtimeTextureCache[resourcePath] = texture;
            }

            return texture;
        }

        private string SourceImageResourcePathForMaterial(string materialName)
        {
            string name = NormalizeMaterialName(materialName);

            foreach (string species in CharacterAvatarOptions.Species)
            {
                if (name == "avatar-" + species)
                {
                    return resourcesRoot + "/SourceImages/" + species + "/" + Capitalize(species);
                }

                string accessoryPrefix = "accessory-" + species + "-";
                if (name.StartsWith(accessoryPrefix, StringComparison.Ordinal))
                {
                    string accessory = name.Substring(accessoryPrefix.Length);
                    return resourcesRoot + "/SourceImages/" + species + "/accesories/" + Capitalize(accessory);
                }

                string shirtPath = ClothingResourcePath(name, species, "part-shirts-" + species + "-", "shirts");
                if (!string.IsNullOrEmpty(shirtPath))
                {
                    return shirtPath;
                }

                string pantsPath = ClothingResourcePath(name, species, "part-pants-" + species + "-", "pants");
                if (!string.IsNullOrEmpty(pantsPath))
                {
                    return pantsPath;
                }

                string shoesPath = ClothingResourcePath(name, species, "part-shoes-" + species + "-", "shoes");
                if (!string.IsNullOrEmpty(shoesPath))
                {
                    return shoesPath;
                }
            }

            return "";
        }

        private string ClothingResourcePath(string materialName, string species, string prefix, string category)
        {
            if (!materialName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return "";
            }

            string partAndColor = materialName.Substring(prefix.Length);
            int colorSeparator = partAndColor.LastIndexOf('-');

            if (colorSeparator <= 0)
            {
                return "";
            }

            string id = partAndColor.Substring(0, colorSeparator);
            string color = partAndColor.Substring(colorSeparator + 1);
            string colorName = Capitalize(color);
            string sourceRoot = resourcesRoot + "/SourceImages/" + species;

            if (category == "shirts")
            {
                if (id == CharacterAvatarOptions.Tee)
                {
                    return sourceRoot + "/shirts/T-shirts/T-shirts " + colorName;
                }

                if (id == CharacterAvatarOptions.Sweatshirt)
                {
                    return sourceRoot + "/shirts/Sweatshirt/Sweatshirt " + colorName;
                }

                if (id == CharacterAvatarOptions.Undershirt)
                {
                    return sourceRoot + "/shirts/Undershirt/Undershirt " + colorName;
                }
            }

            if (category == "pants")
            {
                if (id == CharacterAvatarOptions.Jeans)
                {
                    return sourceRoot + "/pants/Jeans/Jeans";
                }

                if (id == CharacterAvatarOptions.Cargo)
                {
                    return sourceRoot + "/pants/Cargo pants/Cargo pants " + colorName;
                }

                if (id == CharacterAvatarOptions.SportsPants)
                {
                    return sourceRoot + "/pants/Sports pants/Sports pants " + colorName;
                }
            }

            if (category == "shoes")
            {
                if (id == CharacterAvatarOptions.Sneakers)
                {
                    return sourceRoot + "/shoes/Sneakres/Sneakres " + colorName;
                }

                if (id == CharacterAvatarOptions.Boots)
                {
                    return sourceRoot + "/shoes/Boots/Boots " + colorName;
                }

                if (id == CharacterAvatarOptions.SpaceShoes)
                {
                    return sourceRoot + "/shoes/Space shoes/Space shoes " + colorName;
                }
            }

            return "";
        }

        private static string NormalizeMaterialName(string value)
        {
            string name = NormalizeId(value, "");
            return name.Replace(" (instance)", "");
        }

        private static string Capitalize(string value)
        {
            return string.IsNullOrEmpty(value)
                ? ""
                : char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}
